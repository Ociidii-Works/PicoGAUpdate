﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PicoGAUpdate

{
    static partial class Program
    {
        public static readonly string NvidiaExtractedPath = Path.GetTempPath() + @"DriverUpdateEX";

        public static readonly IList<String> NvidiaCoreComponents = new ReadOnlyCollection<string>(new List<String> {
                            "Display.Driver",
                            "NVI2",
                            "PhysX",
                            "NvContainer"
                        });

        private static string CollapseSpaces(string value)
        {
            return Regex.Replace(value, @"\s+", " ");
        }

        public static bool DownloadDone;
        public static bool ExitImmediately = true;

        // TODO: Remove
        private static bool _isoutputting;

        public static string AutoPad(string stringIn, int targetLength = 0)
        {
            if (targetLength == 0)
            {
                //targetLength = Console.BufferWidth - Console.CursorLeft - 3;
                targetLength = Console.BufferWidth - 3;
            }
            while (stringIn != null && stringIn.Length < targetLength)
            {
                stringIn += " ";
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
                spaces += " ";
            }
            string_in = spaces + string_in;
            return string_in;
        }

        public static string GetChipsetModel()
        {
#if DEBUG
			Console.WriteLine("===== Motherboard =====");
			Console.WriteLine("Manufacturer: " + MotherboardInfo.Manufacturer);
			Console.WriteLine("Model: " + MotherboardInfo.Model);
			Console.WriteLine("PNPDeviceID: " + MotherboardInfo.PNPDeviceID);
			Console.WriteLine("Product: " + MotherboardInfo.Product);
			Console.Out.Flush();
#endif
            // A crude implementation until more testing is done
            if (MotherboardInfo.Model != null)
            {
                if (MotherboardInfo.Model.Contains("X570"))
                {
                    return "X570";
                }
            }
            return null;
        }

        // TODO: Make generic device enumerator that includes other device types
        public static string GetCurrentVersion()
        {
            Console.Write("Checking installed Device Drivers...");
            // Add fallback value required for math, if driver is missing/not detected.
            string currVer = "000.00";

            //ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");
            ManagementObjectSearcher DisplaySearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver where deviceclass = 'DISPLAY'");
            ManagementObjectCollection DisplayCollection = DisplaySearcher.Get();

#if DEBUG
			foreach (ManagementObject obj in DisplayCollection)
			{
				string info = String.Format("Device='{0}',Manufacturer='{1}',DriverVersion='{2}' ", obj["DeviceName"], obj["Manufacturer"], obj["DriverVersion"]);
				Console.Out.WriteLine(info);
			}
#endif
            foreach (var o in DisplayCollection)
            {
                ManagementObject obj = (ManagementObject)o;
                string mfg = obj["Manufacturer"].ToString().ToUpperInvariant();
                switch (mfg)
                {
                    case "NVIDIA":
                        {
                            string device = obj["DeviceName"].ToString();
                            if ((device.Contains("GeForce") || device.Contains("TITAN") || device.Contains("Quadro") || device.Contains("Tesla")))
                            {
                                // Rebuild version according to the nvidia format
                                string[] version = obj["DriverVersion"].ToString().Split('.');
                                {
                                    string nvidiaVersion = ((version.GetValue(2) + version.GetValue(3)?.ToString()).Substring(1)).Insert(3, ".");
                                    Console.Write("NVIDIA Driver v" + nvidiaVersion);
                                    Console.WriteLine();
                                    currVer = nvidiaVersion;
                                }
                            }
                        }
                        break;

                    case "AMD":
                        {
                            string device = obj["DeviceName"].ToString();
                            Console.WriteLine("Found AMD device '" + device + "'");
                            Console.WriteLine("Sorry, support for AMD graphic cards is not currently implemented.");
                        }
                        break;

                    case "INTEL":
                        {
                            string device = obj["DeviceName"].ToString();
                            Console.WriteLine("Found Intel device '" + device + "'");
                            Console.WriteLine("Sorry, support for Intel graphic cards is not currently implemented.");
                        }
                        break;

                    default:
                        // do nothing
                        break;
                }
            }
            return currVer;
        }

        public static void RollingOutput(string data, bool clearRestOfLine = false)
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
                        Console.SetCursorPosition(0, Console.CursorTop);
                        var top = Console.CursorTop;

                        if (Console.CursorLeft != 0)
                        {
                            Console.SetCursorPosition(0, top);
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
                            Console.SetCursorPosition(0, Console.CursorTop - top);
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
                string result = input.Substring(input.LastIndexOf(ci.NumberFormat.CurrencyDecimalSeparator) + 1);
                if (result.Length < 2)
                {
                    Console.WriteLine("Hmmm... Result is " + result);
                    input += "0";
                    Console.WriteLine("New version string is " + input);
                }

                bool success = false;
                try
                {
                    success = float.TryParse(input ?? throw new ArgumentNullException(nameof(input)),
                       NumberStyles.Currency, ci, out version);
                }
                catch (System.FormatException)
                {
                    // Nothing
                }
            }
            return version;
        }

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

        private static bool DownloadDriver(string url, string version, string destination)
        {
            bool dirty = false;
            // TODO: Figure what to do when downloading an older version

            if (File.Exists(destination) && !(OptionContainer.ForceDownload))
            {
                //#if DEBUG
                Console.WriteLine("Using Existing installer at " + destination);
                //#endif
                DownloadDone = true;
                NewDownloader.Success = true;
                dirty = true;
            }
            else
            {
                Console.WriteLine("Downloading Driver version " + version
#if DEBUG
					+ " from " + url + Environment.NewLine + "to " + destination
#endif
                     + "...");
                Task.Run(() => new NewDownloader().Download(url, destination));
                while (!DownloadDone)
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
                    dirty = true;
                }
            }
            return dirty;
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
#if DEBUG
				Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
#endif
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

        [DllImport("Setupapi.dll", EntryPoint = "InstallHinfSection", CallingConvention = CallingConvention.StdCall)]
        public static extern void InstallHinfSection(
                            [In] IntPtr hwnd,
                            [In] IntPtr ModuleHandle,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string CmdLineBuffer,
                            int nCmdShow);

        public static void StripDriver(string installerPath, string version)
        {
            try
            {
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
#if DEBUG
					Console.WriteLine("WinRAR = " + winRar);
#endif
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
#if DEBUG
						Console.Write(String.Format(" using  '\"{0}\" {1}' ", wProcess.StartInfo.FileName,
						wProcess.StartInfo.Arguments));
#endif
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

                    // Hack up the installer a little to remove unwanted "features" such as Telemetry
                    if (OptionContainer.Strip)
                    {
                        Console.WriteLine("Stripping driver...");

                        List<string> components = Directory.EnumerateDirectories(NvidiaExtractedPath).ToList<string>();

                        foreach (string c in components)
                        {
                            if (NvidiaCoreComponents.Contains(System.IO.Path.GetFileName(c)))

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
            catch (Exception e)
            {
                throw e;
            }
        }

        public static bool InstallDriver(string installerPath, string version)
        {
            bool dirty = false;
            if ((string.IsNullOrEmpty(installerPath) || !File.Exists(installerPath)) && !Directory.Exists(NvidiaExtractedPath))
            {
                throw new Exception("Installer file does not exist!");
            }
            StripDriver(installerPath, version);

            try
            {
                string setupPath = NvidiaExtractedPath + @"\setup.exe";
                Process p = new Process();
                p.StartInfo.FileName = setupPath ?? throw new ArgumentNullException(nameof(setupPath));
#if DEBUG
				Console.WriteLine("Installer Location: " + p.StartInfo.FileName);
#endif
                if (!OptionContainer.BareDriver)
                {
                    if (OptionContainer.Silent || OptionContainer.Strip)
                    {
                        Console.WriteLine("Running Installer silently... Your monitor(s) may flicker several times...");
                        p.StartInfo.Arguments = "-s";
                    }
                    else
                    {
                        Console.WriteLine("Running GUI Installer"
#if DEBUG
						+ " from " + p.StartInfo.FileName
#endif
                        + "...");
                    }
                    //if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        try
                        {
                            p.Start();
                            p.WaitForExit();
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }
                }
                Console.WriteLine("Driver installed.");
                //Cleanup();
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                if (OptionContainer.DeleteDownloaded)
                {
                    Safe.DirectoryDelete(NvidiaExtractedPath);
                    File.Delete(installerPath);
                }
                dirty = true;
            }

            ExitImmediately = true;
            return dirty;
        }

        private static void Main(string[] args)
        {
            OptionContainer.Option.Parse(args);
            // TODO: Implement system tray icon similarly to https://social.msdn.microsoft.com/Forums/en-US/a7128bdc-783a-4dcc-9de1-652af625627b/console-app-wnotifyicon?forum=netfxcompact
            // An alternative approach is to use https://stackoverflow.com/questions/38062177/is-it-possible-to-send-toast-notification-from-console-application
            // to have the ability to send a balloon tip
            MainProgramLoop(args);

#if DEBUG
			Console.WriteLine("Press any key to quit...");
			Console.ReadKey();
#endif
        }

        private static LinkItem GetLatestDriverVersion()
        {
            // FIXME: This shouldn't run unless we have to...
            Console.Write("Finding latest Nvidia Driver Version... ");

            int textEndCursorPos = Console.CursorLeft;
            WebClient w = new WebClient();
            string s = w.DownloadString(address: WebsiteUrls.RedditSource);
            List<LinkItem> list = new List<LinkItem>();

            list = LinkFinderReddit.Find(s);
            LinkItem latestVersion = list.FindLast(x => x.studio == OptionContainer.Studio);
            foreach (LinkItem i in list)
            {
                Console.Write(i.Version);
                // TODO: Implement specific version downloading here.
                if (i.Version == latestVersion.Version)
                {
                    Console.Write(" (" + (i.studio ? "Studio" : "GameReady") + ")" + Environment.NewLine);
                    break;
                }
                Console.CursorLeft = textEndCursorPos;
                System.Threading.Thread.Sleep(25);
            }
            return latestVersion;
        }

        private static void MainProgramLoop(string[] args)
        {
            //OptionContainer.Option.Parse(args);
#if !DEBUG
            //Console.Clear();
            //int H = 15;
            //int W = 80;
            //Console.SetWindowSize(W, H);
            //Console.SetBufferSize(W, H);
#endif
#if DEBUG
			Console.WriteLine("Elevated Process : " + CheckAdmin.IsElevated);
#endif
            string currentDriverVersion = GetCurrentVersion();
            bool dirty = false;
            if (!OptionContainer.NoUpdate)
            {
                // WIP Chipset updater
                switch (GetChipsetModel())
                {
                    case "X570":
                        // TODO: Get url
                        Console.WriteLine("Your chipset was recognized, but Chipset driver download is still a Work In Progress!");
                        break;

                    default:
                        // no-op
                        break;
                }
                LinkItem latestDriver = GetLatestDriverVersion();
                // Fallback path
                string versionS = latestDriver.Version.ToString(CultureInfo.InvariantCulture);
                string downloadedFile = String.Format(@"{0}{1}.{2}.exe", Path.GetTempPath(), "DriverUpdate", versionS);
                // TODO: Remove need for calling StringToFloat again
                bool currentIsOutOfDate = StringToFloat(currentDriverVersion) < StringToFloat(latestDriver.Version);
                if (currentIsOutOfDate)
                {
                    Console.WriteLine("A new driver version is available! ({0} => {1})", currentDriverVersion, latestDriver.Version);
                }
                else if (OptionContainer.ForceDownload)
                {
                    Console.WriteLine("Downloading driver as requested.");
                }
                else if (!OptionContainer.ForceInstall)
                {
                    Console.WriteLine("Your driver is up-to-date! Well done!");
                }
                if (!File.Exists(downloadedFile) || OptionContainer.ForceDownload || (currentIsOutOfDate && OptionContainer.ForceInstall && (!Directory.Exists(NvidiaExtractedPath))))
                {
                    dirty = DownloadDriver(latestDriver.dlurl, latestDriver.Version, downloadedFile);
                }
                if (OptionContainer.ForceInstall || (dirty && !OptionContainer.DownloadOnly))
                {
                    dirty = InstallDriver(downloadedFile, latestDriver.Version);
                }
                else
                {
                    // show baloon tip
                    // Program.sTrayIcon.ShowBalloonTi
                }
                if (OptionContainer.DeleteDownloaded)
                {
                    try
                    {
                        File.Delete(downloadedFile);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
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
        }
    }

    internal static class WebsiteUrls
    {
        // TODO: Cache results to avoid spamming the site
        public const string RedditSource = "https://old.reddit.com/r/nvidia/search?q=Driver%20FAQ/Discussion&restrict_sr=1&sort=new";

        public const string NvSource = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&psid=111&pfid=890&osID=57&languageCode=1078&beta=null&isWHQL=0&dltype=-1&dch=1&upCRD=0&sort1=0&numberOfResults=10";
    }
}