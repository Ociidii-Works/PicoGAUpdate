using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PicoGAUpdate

{
    static partial class Program
    {
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

        public static bool IsOutOfDate(string newVersion)
        {
            if (OptionContainer.ForceDownload)
            {
                return true;
            }
            Console.WriteLine("Determining driver version...");
            // Add fallback value required for math, if driver is missing/not detected.
            string currVer = "000.00";
            //ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");
            ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver where deviceclass = 'DISPLAY'");
            ManagementObjectCollection objCollection = objSearcher.Get();
            foreach (var o in objCollection)
            {
                ManagementObject obj = (ManagementObject)o;
                if ((string)obj["Manufacturer"] != "NVIDIA")
                {
                }
                else
                {
                    string device = obj["DeviceName"].ToString();
                    if ((device.Contains("GeForce") || device.Contains("TITAN") || device.Contains("Quadro") || device.Contains("Tesla")))
                    {
                        // Rebuild version according to the nvidia format
                        string[] version = obj["DriverVersion"].ToString().Split('.');
                        {
                            string nvidiaVersion = ((version.GetValue(2) + version.GetValue(3)?.ToString()).Substring(1)).Insert(3, ".");
                            Console.WriteLine("Current Driver Version: " + nvidiaVersion);
                            currVer = nvidiaVersion;
                        }
                    }
                }
            }

            if (StringToFloat(currVer) < StringToFloat(newVersion))
            {
                Console.WriteLine("A new driver version is available! ({0} => {1})", currVer, newVersion);
                return true;
            }
            Console.WriteLine("Your driver is up-to-date! Well done!");
            return false;
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
                float version = float.Parse(input ?? throw new ArgumentNullException(nameof(input)), NumberStyles.Currency, ci);

                return version;
            }
            return 0.0f;
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

        private static void DownloadDriver(string url, string version)
        {
            string newFile;
            if (IsOutOfDate(version))
            {
                // TODO: Figure what to do when downloading an older version
                string versionS = version.ToString(CultureInfo.InvariantCulture);
                newFile = String.Format(@"{0}{1}.{2}.exe", Path.GetTempPath(), "DriverUpdate", versionS);
                if ((!OptionContainer.ForceDownload || OptionContainer.KeepDownloaded) && File.Exists(newFile))
                {
                    //#if DEBUG
                    Console.WriteLine("Using Existing installer at " + newFile);
                    //#endif
                    DownloadDone = true;
                    NewDownloader.Success = true;
                }
                else
                {
                    // http://us.download.nvidia.com/Windows/398.82/398.82-desktop-win10-64bit-international-whql.exe
                    // http://us.download.nvidia.com/Windows/398.86/398.86-desktop-win10-64bit-international-whql.exe <= invalid
                    Console.WriteLine("Downloading Driver version " + version
#if DEBUG
                    + " from " + url + Environment.NewLine + "to " + newFile
#endif
                     + "...");
                    Task.Run(() => new NewDownloader().Download(url, newFile));
                    while (!DownloadDone)
                    {
                        // wait
                    }
                    if (NewDownloader.Success)
                    {
                        if (File.Exists(newFile))
                        {
                            Console.WriteLine("Deleting " + newFile);
                            File.Delete(newFile);
                        }
                        NewDownloader.RenameDownload(newFile);
                    }
                }
                if (!string.IsNullOrEmpty(newFile) && File.Exists(newFile))
                {
                    if (OptionContainer.NoUpdate)
                    {
                        Console.WriteLine("Uh-oh. we shouldn't be here!");
                    }

                    if (!OptionContainer.DownloadOnly)
                    {
                        InstallDriver(newFile, version);
                    }

                    if (!OptionContainer.KeepDownloaded)
                    {
                        try
                        {
                            File.Delete(newFile);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            //throw;
                        }
                    }
                }
            }
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

        private static void InstallDriver(string installerPath, string version)
        {
            string extractPath = Path.GetTempPath() + @"DriverUpdateEX";
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
                    if (!Directory.Exists(extractPath))
                    {
                        Console.WriteLine("Creating " + extractPath);
                        Directory.CreateDirectory(extractPath);
                    }

                    Process wProcess = new Process
                    {
                        StartInfo =
                        {
                            FileName = winRar,
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            Arguments = String.Format("x -ibck -mt2 -o+ -inul {0} {1}", installerPath, extractPath)
                        }
                    };
                    Console.WriteLine("Extracting installer " + installerPath + "...");
#if DEBUG
                    Console.WriteLine(String.Format(" % \"{0}\" {1}", wProcess.StartInfo.FileName,
                    wProcess.StartInfo.Arguments));
#endif
                    // Try to create the full path just in case...
                    //Directory.CreateDirectory(@"C:\NVIDIA");
                    //Directory.CreateDirectory(@"C:\NVIDIA\DisplayDriver");
                    wProcess.Start();
                    wProcess.WaitForExit();
                    // Move to C:\NVIDIA (where the installer expects it) and remove Win10_64\International\ hierarchy levels
                    // string newDir = @"C:\NVIDIA\DisplayDriver\" + version.ToString();
                    // if (Directory.Exists(newDir))
                    // {
                    //     Console.WriteLine("Deleting " + newDir);
                    //     Safe.DirectoryDelete(newDir, false);
                    // }
                    // Directory.CreateDirectory(newDir);
                    // Console.WriteLine("Copying " + extractPath + " => " + newDir);
                    // System.IO.DirectoryInfo dirsrc = new System.IO.DirectoryInfo(extractPath);
                    // System.IO.DirectoryInfo dirtarget = new System.IO.DirectoryInfo(newDir);
                    // CopyAll(dirsrc, dirtarget);
                    // // NOOO!! See below; Cannot delete this.
                    // //extractPath = newDir;
                    // installerPath = newDir + @"\setup.exe";
                    
                    Console.WriteLine("Done.");

                    // Hack up the installer a little to remove unwanted "features" such as Telemetry
                    if (OptionContainer.Strip)
                    {
                        if (Directory.Exists(extractPath + @"\NvTelemetry"))
                        {
                            Safe.DirectoryDelete(extractPath + @"\NvTelemetry", false);
                            Console.WriteLine("Removed NvTelemetry.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Driver modification requires WinRAR. Please install in the default location.");
                    Environment.Exit(1);
                }
                string setupPath = extractPath + @"\setup.exe";
                Process p = new Process();
                p.StartInfo.FileName = setupPath ?? throw new ArgumentNullException(nameof(setupPath));
                if (OptionContainer.Silent)
                {
                    Console.WriteLine("Running Installer silently... Your monitor(s) may flicker several times...");
                    p.StartInfo.Arguments = "-s";
                }
                else
                {
                    Console.WriteLine("Running GUI Installer"
#if DEBUB
                        + " from " + p.StartInfo.FileName
#endif
                        + "...");
                }

                p.Start();
                p.WaitForExit();
                Console.WriteLine("Driver installed.");
                //Cleanup();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (!OptionContainer.KeepDownloaded)
                {
                    Safe.DirectoryDelete(extractPath);
                }
            }

            ExitImmediately = true;
        }

        private static void Main(string[] args)
        {
            OptionContainer.Option.Parse(args);
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

            if (!OptionContainer.NoUpdate)
            {
                Console.WriteLine("Finding latest Nvidia Driver Version...");
                WebClient w = new WebClient();
                string s = w.DownloadString(address: WebsiteUrls.DriverListSource);
                //#if DEBUG
                //                File.WriteAllText(@"C:\reddit.html", s);
                //#endif

                List<string> driverTitles = new List<string>();
                foreach (LinkItem i in LinkFinder.Find(s))
                {
#if DEBUG
                    //Console.WriteLine(i.ToString());
#endif
                    string iS = i.Text;
                    {
#if DEBUG
                        Console.WriteLine("iS = '" + iS + "'");
#endif
                        string[] prefix = i.Text.Split(new string[] { "FAQ" }, StringSplitOptions.None);
                        string version = prefix.First().Split(new string[] { "Driver " }, StringSplitOptions.None).Last();
#if DEBUG
                        Console.WriteLine(version);
#endif
                        if (version.Contains("."))
                        {
                            driverTitles.Add(version);
                        }
                    }
                }

                if (driverTitles.Any())
                {
                    driverTitles.Sort();
#if DEBUG
                    Console.WriteLine("Available Versions:");
                    foreach (float driverVersion in driverTitles)
                    {
                        Console.WriteLine(driverVersion.ToString(CultureInfo.InvariantCulture));
                    }
                    Console.WriteLine("^~~~ Latest");
#endif
                    string latestDriver = driverTitles.Last();
                    // Fix parsed numbers not matching the driver name format
                    //string tempver = latestDriver.ToString();
                    //latestDriver = StringToFloat(tempver);
#if !DEBUG
                    Console.WriteLine("Latest Driver: " + driverTitles.Last());
#endif

                    // Build new URL from latest version
                    // Note: '388.00' becomes '388' somewhere above, need to add '.00' at the end if trying to use that one.
                    // http://us.download.nvidia.com/Windows/397.93/397.93-desktop-win10-64bit-international-whql.exe
                    string newUrl =
                        String.Format(
                            "http://us.download.nvidia.com/Windows/{0}/{0:#.##}-desktop-win10-64bit-international-whql.exe",
                            latestDriver);
                    DownloadDriver(newUrl, latestDriver);
                }
                else
                {
                    Console.WriteLine("Something went wrong; unable to parse driver list from webpage");
                }
            }
 
            if (OptionContainer.Clean)
            {
                Cleanup();
            }

            Console.WriteLine("Updater is done here.");

            if (!ExitImmediately)
            {
                Console.WriteLine();
                Console.Out.Flush();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

    internal static class WebsiteUrls
    {
        // Don't abuse the search API
        //public const string DriverListSource = "https://www.reddit.com/r/nvidia/search?q=FAQ/Discussion&restrict_sr=1&sort=new";
        // TODO: Cache results to avoid spamming the site
        public const string DriverListSource = "https://old.reddit.com/r/nvidia/search?q=Driver%20FAQ/Discussion&restrict_sr=1&sort=new";
    }
}