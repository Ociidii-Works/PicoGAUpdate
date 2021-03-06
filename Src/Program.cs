﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PicoGAUpdate.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PicoGAUpdate

{
    internal static class Program
    {
        public static readonly IList<String> NvidiaCoreComponents = new ReadOnlyCollection<string>(new List<String> {
       "Display.Driver",
       "NVI2",
       "PhysX",
       "NvContainer"});

        public static readonly string NvidiaExtractedPath = Path.GetTempPath() + @"DriverUpdateEXNvidia";
        public static bool DownloadDone;

        public static bool ExitImmediately = true;
        public static Version LatestNvidiaVersion = new Version(0, 0, 0, 0);

        public static string AutoPad(string stringIn, int targetLength = 0)
        {
            if (targetLength == 0)
            {
                //targetLength = Console.BufferWidth - Console.CursorLeft - 3;
                targetLength = Console.BufferWidth - 3;
            }
            while (stringIn != null && stringIn.Length < targetLength)
            {
                stringIn += "";
            }
            Console.SetCursorPosition(0, Console.CursorTop);
            return stringIn;
        }

        public static string AutoPad2(string string_in, int target_length = 0)
        {
            if (target_length == 0)
            {
                target_length = Console.BufferWidth - Console.CursorLeft - 3;
            }

            string spaces = "";
            while (string_in.Length + spaces.Length < target_length)
            {
                spaces += "";
            }
            string_in = spaces + string_in;
            return string_in;
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                 target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        // Then this function goes after installing the driver, I guess. Not sure how the installer behaves when the device is not enabled.
        public static void DisableAudio()
        {
            ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPEntity");
            ManagementObjectCollection objCollection = objSearcher.Get();
            foreach (ManagementBaseObject o in objCollection)
            {
                var obj = (ManagementObject)o;
                var properties = obj.Properties;
                var rawDeviceID = properties["PNPDeviceID"].Value;
                if (rawDeviceID != null && rawDeviceID.ToString().StartsWith(@"PCI"))
                {
                    List<string> deviceID = obj["PNPDeviceID"].ToString().Substring(4).Split('&').ToList();
                    if (deviceID != null)
                    {
                        string vendor = deviceID.ElementAt(0);
                        string model = deviceID.ElementAt(1);
                        switch (vendor)
                        {
                            case "VEN_10DE": // NVIDIA
                                switch (model)
                                {
                                    case "DEV_10F0": // NVIDIA HMDI Audio
                                        DisableHardware.DisableDevice(n => n.ToUpperInvariant().Contains(vendor + "&" + model));
                                        continue;
                                }
                                continue;
                        }
                    }
                }
            }
        }

        // TODO: Re-write logic to use inheritance and other fun logic to cascade which GPU is currently being processed instead of looping through vendors inside each function.
        public static string Dump(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static void ExtractDriver(string installerPath)
        {
            if (!File.Exists(installerPath))
            {
                return;
            }
            // Find WinRar
            string winRar = "";
            // 64-bit Winrar on 64-bit architecture
            string winRar6432P = Environment.GetEnvironmentVariable("ProgramW6432") +
                  @"\WinRAR\WinRAR.exe";
            // 64-bit WinRAR from 32-bit app
            string winRar6464P = Environment.SpecialFolder.ProgramFiles + @"\WinRAR\WinRAR.exe";
            // 32-bit Winrar on 64-bit architecture
            string winRar3264P = Environment.SpecialFolder.ProgramFilesX86 + @"\WinRAR\WinRAR.exe";
            if (File.Exists(winRar6464P))
            {
                winRar = winRar6464P;
            }
            else if (File.Exists(winRar6432P))
            {
                winRar = winRar6432P;
            }
            else if (File.Exists(winRar3264P))
            {
                winRar = winRar3264P;
            }

            if (!string.IsNullOrEmpty(winRar))
            {
                // TODO: Optimize checks here
                if (!Directory.Exists(NvidiaExtractedPath) || !File.Exists(NvidiaExtractedPath + @"\setup.exe") || !Directory.Exists(NvidiaExtractedPath + @"\Display.Driver"))
                {
                    //Safe.DirectoryDelete(NvidiaExtractedPath);
                    Console.WriteLine("Creating " + NvidiaExtractedPath);
                    Directory.CreateDirectory(NvidiaExtractedPath);

                    Process wProcess = new Process
                    {
                        StartInfo =
      {
       FileName = winRar,
       UseShellExecute = false,
       CreateNoWindow = false,
       Arguments = String.Format("x -ibck -mt2 -o+ -inul {0} {1}", installerPath, NvidiaExtractedPath)
      }
                    };
                    Console.Write("Extracting installer '" + installerPath + "'");
                    Console.WriteLine("");
                    wProcess.Start();
                    wProcess.WaitForExit();
                    Console.WriteLine("Done.");
                }

                if (OptionContainer.BareDriver)
                {
                    Console.WriteLine("Installing bare driver...");
                    string[] array2 = Directory.GetFiles(NvidiaExtractedPath + @"\Display.Driver", "*.INF");
                    foreach (string name in array2)
                    {
                        Console.WriteLine(name);
                        InstallHinfSection(IntPtr.Zero, IntPtr.Zero, name, 0);
                    }
                }

                // Hack up the installer a little to remove unwanted "features"such as Telemetry
                if (!OptionContainer.NoStrip)
                {
                    Console.WriteLine("Stripping driver...");

                    List<string> components = Directory.EnumerateDirectories(NvidiaExtractedPath).ToList();

                    foreach (string c in components)
                    {
                        if (NvidiaCoreComponents.Contains(Path.GetFileName(c)))

                        {
                            continue;
                        }
                        Safe.DirectoryDelete(c, true);
                    }
                    // edit setup.cfg to prevent failure
                    string text = File.ReadAllText(NvidiaExtractedPath + @"\setup.cfg");
                    text = text.Replace(@"<file name=""${{EulaHtmlFile}}""/>", "");
                    text = text.Replace(@"<file name=""${{FunctionalConsentFile}}""/>", "");
                    text = text.Replace(@"<file name=""${{PrivacyPolicyFile}}""/>", "");
                    File.WriteAllText(NvidiaExtractedPath + @"\setup.cfg", text);
                }
            }
            else
            {
                Console.WriteLine("Driver modification requires WinRAR. Please install in the default location.");
                Environment.Exit(1);
            }
        }

        // TODO: Remove in favor of ProcessRawDevices
        public static string GetChipsetModel()
        {
            Console.WriteLine("* Motherboard");
            Console.WriteLine("Manufacturer: " + MotherboardInfo.Manufacturer);
            Console.WriteLine("Product: " + MotherboardInfo.Product);
            Console.WriteLine("ID: " + MotherboardInfo.PNPDeviceID);
            Console.Out.Flush();
            // A crude implementation until more testing is done
            if (MotherboardInfo.Product != null)
            {
                if (MotherboardInfo.Product.Contains("X570"))
                {
                    return "X570";
                }
            }
            return null;
        }

        // TODO: Make generic device enumerator that includes other device types
        // TODO: Make function launch downloader and installer for each device to work around having to stop at the first match
        public static void GetCurrentDevicesDeprecated(string vendor_string, out string out_version)
        {
            Console.Write("Finding display adapters...");
            // Add fallback value required for math, if driver is missing/not detected.
            out_version = "000.00";
            ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");
            ManagementObjectCollection objCollection = objSearcher.Get();
            bool found = false;
            // TODO: Handle multiple display adapters. Needs testing.
            // TODO: Replace with better code from DisableAudio()
            foreach (var o in objCollection)
            {
                var obj = (ManagementObject)o;
                //string info = String.Format("Device='{0}',Manufacturer='{1}',DriverVersion='{2}' ", obj["DeviceName"], obj["Manufacturer"], obj["DriverVersion"]);
                //Console.Out.WriteLine(info);
                string mfg = obj["Manufacturer"]?.ToString().ToUpperInvariant();
                switch (mfg)
                {
                    case "NVIDIA":
                        {
                            string device = obj["DeviceName"].ToString();
                            if (device.Equals("NVIDIA High Definition Audio"))
                            {
                                continue;
                            }

                            // TODO: List match unless much slower.
                            if ((device.Contains("GeForce") || device.Contains("TITAN") || device.Contains("Quadro") ||
                              device.Contains("Tesla")))
                            {
                                // Rebuild version according to the nvidia format
                                string[] version = obj["DriverVersion"].ToString().Split('.');
                                {
                                    string nvidiaVersion =
                                     ((version.GetValue(2) + version.GetValue(3)?.ToString()).Substring(1)).Insert(3,
                                      ".");
                                    Console.WriteLine("NVIDIA Driver v" + nvidiaVersion);
                                    out_version = nvidiaVersion;
                                    found = true;
                                }
                            }

                            vendor_string = "NVIDIA";
                        }
                        break;

                    case "AMD":
                        {
                            string device = obj["DeviceName"].ToString();
                            Console.WriteLine("Found AMD device '" + device + "'");
                            Console.WriteLine(
                             "Sorry, support for AMD graphic cards is not currently implemented.");
                        }
                        break;

                    case "INTEL":
                        {
                            string device = obj["DeviceName"].ToString();
                            Console.WriteLine("Found Intel device '" + device + "'");
                            Console.WriteLine(
                             "Sorry, support for Intel graphic cards is not currently implemented.");
                        }
                        break;
                }

                //break;
            }

            if (!found)
            {
                ManagementObjectSearcher AdapterSearcher =
                new ManagementObjectSearcher("Select * from Win32_videocontroller");
                ManagementObjectCollection AdapterCollection = AdapterSearcher.Get();
                // try to find devices without drivers.
                foreach (var o in AdapterCollection)
                {
                    var obj = (ManagementObject)o;
                    string deviceID = obj["PNPDeviceID"].ToString();
                    string vendor = deviceID.Split('&').First().Split('\\').ElementAt(1);
                    //string info = String.Format("{3} -- {0}, Driver version '{1}'", obj["DeviceName"], obj["DriverVersion"], obj["PNPDeviceID"]);
                    string info = String.Format("{0}", vendor);
                    Console.Out.WriteLine(info);
                    switch (vendor)
                    {
                        case "VEN_10DE": // NVIDIA
                            vendor_string = "NVIDIA";
                            break;
                    }

                    break;
                }
            }
        }

        public static bool InstallDriver(string installerPath)
        {
            try
            {
                if ((string.IsNullOrEmpty(installerPath) || (!File.Exists(installerPath)) && !Directory.Exists(NvidiaExtractedPath)))
                {
                    Console.WriteLine("Installer file does not exist!");
                    return false;
                }
                string setupPath = NvidiaExtractedPath + @"\setup.exe";
                Process p = new Process();
                p.StartInfo.FileName = setupPath;
                if (!OptionContainer.BareDriver)
                {
                    if (OptionContainer.Silent)
                    {
                        Console.WriteLine("Running Installer silently... Your monitor(s) may flicker several times...");
                        p.StartInfo.Arguments = "-s";
                    }
                    else
                    {
                        Console.WriteLine("Running GUI Installer" + "...");
                    }
                    //if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        if (!OptionContainer.Pretend)
                        {
                            p.Start();
                            p.WaitForExit();
                        }
                    }
                }
                Console.WriteLine("Driver installed.");
                //Cleanup();
            }
            finally
            {
                if (OptionContainer.DeleteDownloaded)
                {
                    Safe.DirectoryDelete(NvidiaExtractedPath);
                    File.Delete(installerPath ?? throw new ArgumentNullException(nameof(installerPath)));
                }
            }

            ExitImmediately = true;
            return true;
        }

        [DllImport("Setupapi.dll", EntryPoint = "InstallHinfSection", CallingConvention = CallingConvention.StdCall)]
        public static extern void InstallHinfSection(
               [In] IntPtr hwnd,
               [In] IntPtr ModuleHandle,
               [In, MarshalAs(UnmanagedType.LPWStr)] string CmdLineBuffer,
               int nCmdShow);

        public static bool IsExpired(string filename, int hours)
        {
            var threshold = DateTime.Now.AddHours(hours);
            var time = File.GetCreationTime(filename);
            return time >= threshold;
        }

        public static bool LookupForDriverDetails(string vendor, out List<DriverDetails> out_details)
        {
            out_details = new List<DriverDetails>();
            bool success = true;
            if (string.IsNullOrEmpty(vendor))
            {
                return false;
            }
            try
            {
                // FIXME: This shouldn't run unless we have to...
                Console.Write("Finding latest Driver Version... ");
                //Store result for a little bit
                string cached_result = Path.GetTempPath() + "\\DriverUpdate.SearchResults." + vendor + ".txt";
                var content = "";
                bool use_cache = File.Exists(cached_result) && !IsExpired(cached_result, 4);
                if (use_cache)
                {
                    content = File.ReadAllText(cached_result);
                }
                // Will fall through if file is empty
                if (string.IsNullOrEmpty(content))
                {
                    try
                    {
                        // Build driver query based on the operating system's values
                        System.UriBuilder driverJsonUrl = new UriBuilder("http://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php");
                        var querystr = System.Web.HttpUtility.ParseQueryString("");
                        querystr["func"] = "DriverManualLookup";
                        // TODO: Handle a few generations of GPUs via gpu id.
                        querystr["psid"] = "111"; // ???????
                        querystr["pfid"] = "816"; // GTX 1070
                        querystr["osID"] = "57"; // "Windows 10 64-bit"
                        querystr["languageCode"] = "1033"; // "English (US)
                        querystr["beta"] = "0";
                        querystr["isWHQL"] = "0";
                        querystr["dltype"] = "-1";
                        querystr["sort"] = "0";
                        querystr["numberOfResults"] = "20";
                        driverJsonUrl.Query = querystr.ToString();
                        var url = driverJsonUrl.ToString();
                        content = ReadTextFromUrl(url);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to contact NVIDIA website");
                        return false;
                    }
                    File.WriteAllText(cached_result, content);
                }
                var o = JObject.Parse(content);
                var ids = o["IDS"].Children().ToList();
                foreach (var id in ids)
                {
                    var item = id["downloadInfo"];
                    if (item != null)
                    {
                        DriverDetails temp_driver = item.ToObject<DriverDetails>();
                        if (temp_driver != null)
                        {
                            out_details.Add(temp_driver);
                            //var version = item["Version"];
                            //string spacing = "        ";
                            //Console.WriteLine("\n* {1}:\n{0}Release Date: {2}\n{0}Driver Type:  {3}\n{0}Size:         {4}\n{0}Details:      {5}", spacing,
                            //    temp_driver.Version,
                            //    temp_driver.ReleaseDateTime,
                            //    WebUtility.UrlDecode(temp_driver.Name),
                            //    temp_driver.DownloadURLFileSize,
                            //    temp_driver.DetailsURL);
                        }
                    }
                }
            }
            catch (Exception)
            {
                success = false;
            }
            return success;
        }

        public static void ProcessRawDevices()
        {
            ManagementObjectSearcher pnpEntitySearcher = new ManagementObjectSearcher("Select * from Win32_PnPEntity");
            foreach (var o in pnpEntitySearcher.Get())
            {
                var queryObj = (ManagementObject)o;
                var rawDeviceID = queryObj["PNPDeviceID"]?.ToString();
                if (null == rawDeviceID)
                {
                    continue;
                }
                if (!rawDeviceID.ToString().StartsWith("PCI\\"))
                {
                    continue;
                }
                List<string> deviceID = rawDeviceID.ToString().Substring(4).Split('&').ToList();
                if (null == deviceID)
                {
                    continue;
                }
                string vendor = deviceID.ElementAt(0);
                string model = deviceID.ElementAt(1);
                Version no_version = new Version(0, 0, 0, 0);
                switch (vendor)
                {
                    default:
                        break;

                    case "VEN_10DE": // NVIDIA
                        if ("DEV_10F0" == model) // NVIDIA HMDI Audio
                        {
                            try
                            {
                                if (OptionContainer.NoAudio)
                                {
                                    DisableHardware.DisableDevice(n => n.ToUpperInvariant().Contains(vendor + "&" + model));
                                }
                            }
                            catch (Exception) { };
                        }
                        else
                        {
                            // TODO: Better handling of "replace outdated driver with same outdated driver" in case it is present and no update check was desired, and force-install was enabled, etc etc.
                            if (OptionContainer.NoUpdate)
                            {
                                continue;
                            }
                            if (LatestNvidiaVersion != null)
                            {
                                LookupForDriverDetails("NVIDIA", out List<DriverDetails> out_details);
                                if (out_details.Count > 0)
                                {
                                    LatestNvidiaVersion = out_details.First().Version;
                                    Console.WriteLine(LatestNvidiaVersion);
                                }
                                ManagementObjectSearcher signedDriverSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");
                                foreach (var p in signedDriverSearcher.Get())
                                {
                                    try
                                    {
                                        var queryDriver = (ManagementObject)p;
                                        var mfg = queryDriver["Manufacturer"]?.ToString()?.ToUpperInvariant();
                                        if ("NVIDIA" != mfg)
                                        {
                                            continue;
                                        }
                                        string device = queryDriver["DeviceName"].ToString();
                                        Version current_version = no_version;
                                        if (device.Contains("GeForce") || device.Contains("TITAN") || device.Contains("Quadro") || device.Contains("Tesla"))
                                        {
                                            string[] version = queryDriver["DriverVersion"].ToString().Split('.');
                                            current_version = new Version(
                                             ((version.GetValue(2) + version.GetValue(3)?.ToString()).Substring(1)).Insert(3,
                                              "."));
                                            Console.WriteLine("Current driver: " + current_version.ToString());
                                            if (LatestNvidiaVersion > current_version && !OptionContainer.NoUpdate)
                                            {
                                                Console.WriteLine("A new driver version is available!");
                                                if (OptionContainer.NoDownload)
                                                {
                                                    continue;
                                                }
                                                var latest_driver = out_details.First();
                                                var installer_file = String.Format(@"{0}{1}.{2}.exe", Path.GetTempPath(), "DriverUpdate", latest_driver.Version.ToString());
                                                if (DownloadDriver(latest_driver, installer_file))
                                                {
                                                    if (OptionContainer.NoInstall)
                                                    {
                                                        continue;
                                                    }
                                                    ExtractDriver(installer_file);
                                                    if (InstallDriver(installer_file))
                                                    {
                                                        Console.WriteLine("Tada!");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception) { }
                                }
                            }
                        }
                        break;

                    case "AMD":
                        // [...]
                        // if ("ADVANCED MICRO DEVICES" != mfg)
                        // {
                        //      continue;
                        // }
                        break;
                }
            }
        }

        public static void RollingOutput(string data, bool clearRestOfLine = false, int cursor_left_cap = 0)
        {
            // Gross hack to prevent multiple outputting at once.
            if (!_isoutputting)
            {
                _isoutputting = true;

                if (!string.IsNullOrEmpty(data))
                {
                    data = data.Replace(Environment.NewLine, "");
                    try
                    {
                        Console.CursorVisible = false;
                        Console.Write('\r');
                        Console.SetCursorPosition(cursor_left_cap, Console.CursorTop);
                        var top = Console.CursorTop;

                        if (Console.CursorLeft != cursor_left_cap)
                        {
                            Console.SetCursorPosition(cursor_left_cap, top);
                        }

                        if (clearRestOfLine)
                        {
                            Console.Write(AutoPad(data));
                        }
                        else
                        {
                            Console.Write(data);
                        }

                        if (Console.CursorTop != top)
                        {
                            Console.SetCursorPosition(cursor_left_cap, Console.CursorTop - top);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                _isoutputting = false;
            }
        }

        public static float StringToFloat(string input)
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            float version = 0.0f;
            if (input.Contains(ci.NumberFormat.CurrencyDecimalSeparator))
            {
                // Add a zero if the resulting minor version is under to (ie 411.7 instead of 411.70)
                string result = input.Substring(input.LastIndexOf(ci.NumberFormat.CurrencyDecimalSeparator, StringComparison.Ordinal) + 1);
                if (result.Length < 2)
                {
                    Console.WriteLine("Hmmm... Result is " + result);
                    input += "0";
                    Console.WriteLine("New version string is " + input);
                }

                try
                {
                    float.TryParse(input,
                     NumberStyles.Currency, ci, out version);
                }
                catch (FormatException)
                {
                    // Nothing
                }
            }
            return version;
        }

        // TODO: Remove
        private static bool _isoutputting;

        // ReSharper disable once UnusedMember.Local
        private static void Cleanup()
        {
            string installer2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\NVIDIA Corporation\Installer2";
            if (!CheckAdmin.IsElevated)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Administrative rights are required to clean " + installer2 + ".");
                Console.ResetColor();
                return;
            }

            try
            {
                Console.WriteLine("Deleting installer2...");
                DeleteFilesFromDirectory(installer2, true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            Console.WriteLine("Done");
        }

        private static void DeleteFilesFromDirectory(string directoryPath, bool verbose = false)
        {
            if (Directory.Exists(directoryPath))
            {
                DirectoryInfo d = new DirectoryInfo(directoryPath);

                foreach (FileInfo fi in d.GetFiles())
                {
                    try
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Deleting " + fi);
                        }
                        fi.Delete();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                foreach (DirectoryInfo di in d.GetDirectories())
                {
                    DeleteFilesFromDirectory(di.FullName);

                    try
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Deleting " + di);
                        }
                        di.Delete();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        ExitImmediately = false;
                    }
                }
            }
        }

        private static bool DownloadDriver(DriverDetails driver, string destination)
        {
            // TODO: Figure what to do when downloading an older version
            // FIXME: Still gets called when using nodownload switch
            if (OptionContainer.NoDownload)
            {
                return false;
            }

            if (File.Exists(destination) && !(OptionContainer.ForceDownload))
            {
                NewDownloader.Success = true;
                Program.DownloadDone = true;
                Console.WriteLine("Using Existing installer at " + destination);
            }
            else
            {
                Console.Write("Downloading Driver version " + driver.Version
                  + "...");
                Task.Run(() => new NewDownloader().Download(driver.DownloadUrl, destination));
                while (!Program.DownloadDone) // this breaks
                {
                    // wait
                }
                if (NewDownloader.Success)
                {
                    if (File.Exists(destination))
                    {
                        Console.WriteLine("Deleting old copy");
                        File.Delete(destination);
                    }
                    NewDownloader.RenameDownload(destination);
                }
                else
                {
                    Console.WriteLine("uh-oh...");
                }
            }
            return NewDownloader.Success;
        }

        private static void Main(string[] args)
        {
            OptionContainer.Option.Parse(args);
            // TODO: Implement system tray icon similarly to https://social.msdn.microsoft.com/Forums/en-US/a7128bdc-783a-4dcc-9de1-652af625627b/console-app-wnotifyicon?forum=netfxcompact
            // An alternative approach is to use https://stackoverflow.com/questions/38062177/is-it-possible-to-send-toast-notification-from-console-application
            // to have the ability to send a balloon tip
            MainProgramLoop();
            if (!ExitImmediately)
            {
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
            }
            Console.WriteLine();
        }

        private static void MainProgramLoop()
        {
            //// WIP Chipset updater
            //switch (GetChipsetModel())
            //{
            //    case "X570":
            //        // TODO: Get url
            //        Console.WriteLine("Your chipset was recognized, but Chipset driver download is still a Work In Progress!");
            //        break;
            //}
            ProcessRawDevices();
            // TODOL Deprecate this code path and chain-load download and installation inside getCurrentVersion (and rename it...)
            // ReSharper disable once UnusedVariable
            //GetCurrentVersion(out string currentDriverVendor, out string currentDriverVersion);
            //bool success = GetDriverInfo(out DriverDetails Driver);
            //// TODO: Make installer work without network connection
            //if (success)
            //{
            // // Fallback path
            // string versionS = Driver.Version.ToString(CultureInfo.InvariantCulture);
            // string InstallerPackageDestination = String.Format(@"{0}{1}.{2}.exe", Path.GetTempPath(), "DriverUpdate", versionS);
            // // TODO: Remove need for calling StringToFloat again
            // bool currentIsOutOfDate = StringToFloat(currentDriverVersion) < StringToFloat(Driver.Version);
            // if (currentIsOutOfDate)
            // {
            //  Console.WriteLine("A new driver version is available! ({0} => {1})", currentDriverVersion, Driver.Version);
            // }
            // else if (OptionContainer.ForceDownload)
            // {
            //  Console.WriteLine("Downloading driver as requested.");
            // }
            // else if (!OptionContainer.ForceInstall)
            // {
            //  Console.WriteLine("Your driver is up-to-date! Well done!");
            // }
            // bool no_downloaded_driver = (!File.Exists(NvidiaExtractedPath + @"\"+
            //                "setup.exe") &&
            //               !File.Exists(InstallerPackageDestination));
            // // TODO: Handle missing file inside the proper function to allow different vendors and partial recovery from missing file instead of downloading everything for no reason
            // //if (!File.Exists(downloadedFile) || OptionContainer.ForceDownload || (currentIsOutOfDate && OptionContainer.ForceInstall && (!Directory.Exists(NvidiaExtractedPath))))
            // bool do_download = (OptionContainer.ForceDownload || (!OptionContainer.NoUpdate && currentIsOutOfDate) ||
            //               (OptionContainer.ForceInstall && no_downloaded_driver));
            // //if (OptionContainer.ForceDownload || !File.Exists(InstallerPackageDestination) || OptionContainer.ForceDownload || (currentIsOutOfDate && OptionContainer.ForceInstall && (!Directory.Exists(NvidiaExtractedPath))))
            // if (!OptionContainer.NoDownload && do_download)
            // {
            //  if (no_downloaded_driver)
            //  {
            //   Console.WriteLine("Downloading driver as there is no local copy to process");
            //  }
            //  DownloadDriver(Driver.DownloadUrl, Driver.Version, InstallerPackageDestination);
            // }
            // if (currentIsOutOfDate || OptionContainer.ForceInstall) // TODO: Run on extracted path if present instead of relying on file version
            //  StripDriver(InstallerPackageDestination, Driver.Version);
            //
            // // TODO: Add ExtractDriver step
            // // }
            // if (OptionContainer.ForceInstall || !OptionContainer.NoInstall)
            // {
            //  InstallDriver(InstallerPackageDestination, Driver.Version);
            // }
            // else
            // {
            //  // show baloon tip
            //  // Program.sTrayIcon.ShowBalloonTi
            // }
            // if (OptionContainer.NoAudio)
            // {
            //  DisableAudio();
            // }
            //
            // if (OptionContainer.DeleteDownloaded)
            // {
            //  try
            //  {
            //   File.Delete(InstallerPackageDestination);
            //  }
            //  catch (Exception e)
            //  {
            //   Console.WriteLine(e);
            //  }
            // }
            //}
            //if (dirty && OptionContainer.Strip)
            //{
            //	Stripper.StripComponentsViaUninstall();
            //}
            if (OptionContainer.Clean)
            {
                Cleanup();
            }

            if (!ExitImmediately)
            {
                Console.WriteLine();
                Console.Out.Flush();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static string ReadTextFromUrl(string url)
        {
            // WebClient is still convenient
            // Assume UTF8, but detect BOM - could also honor response charset I suppose
            // using System.Net;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new WebClient())
            using (var stream = client.OpenRead(url))
            using (var textReader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return textReader.ReadToEnd();
            }
        }
    }
}
