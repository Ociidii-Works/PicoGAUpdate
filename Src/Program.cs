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
    internal static class Program
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

        public static bool IsOutOfDate(float newVersion)
        {
            if (OptionContainer.ForceDownload)
            {
                return true;
            }
            Console.WriteLine("Determining driver version...");
            // Add fallback value required for math, if driver is missing/not detected.
            float currVer = 0.0f;
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
                            currVer = StringToFloat(nvidiaVersion);
                        }
                    }
                }
            }

            if (currVer < newVersion)
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
                float version = float.Parse(input ?? throw new ArgumentNullException(nameof(input)), NumberStyles.Any, ci);
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

        private static void DownloadDriver(string url, float version)
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
                    Console.WriteLine("Downloading Driver version " + version + "from " + url + Environment.NewLine + "to " + newFile + "...");
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
                        InstallDriver(newFile);
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

        private static void InstallDriver(string installerPath)
        {
            string extractPath = Path.GetTempPath() + @"DriverUpdateEX";
            try
            {
                // Find WinRar
                bool legacySilent = true;
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
                    legacySilent = !OptionContainer.GraphicUI;
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
                    wProcess.Start();
                    wProcess.WaitForExit();
                    installerPath = extractPath + @"\setup.exe";

                    // Hack up the installer a little to remove unwanted "features" such as Telemetry
                    //if(GOptions.Contains("strip"))
                    {
                        Safe.DirectoryDelete(extractPath + @"\NvTelemetry");
                    }
                }
                else
                {
                    Console.WriteLine("Cannot extract installer data; falling back to silent default install!");
                }

                Process p = new Process();
                p.StartInfo.FileName = installerPath ?? throw new ArgumentNullException(nameof(installerPath));
                if (legacySilent)
                {
                    Console.WriteLine("Running Installer silently... Your monitor(s) may flicker several times...");
                    p.StartInfo.Arguments = "-s";
                }
                else
                {
                    Console.WriteLine("Running GUI Installer...");
                }

                p.Start();
                p.WaitForExit();
                Console.WriteLine("Driver installed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Safe.DirectoryDelete(extractPath);
            }

            ExitImmediately = true;
        }

        private static void Main(string[] args)
        {
#if !DEBUG
            Console.Clear();
#endif
            OptionContainer.Options.Parse(args);
#if !DEBUG
            int H = 15;
            int W = 80;
            Console.SetWindowSize(W, H);
            Console.SetBufferSize(W, H);
#endif
#if DEBUG
            Console.WriteLine("Elevated Process : " + CheckAdmin.IsElevated);
#endif
            if (!OptionContainer.NoUpdate)
            {
                Console.WriteLine("Finding latest Nvidia Driver Version...");
                WebClient w = new WebClient();
                string s = w.DownloadString(address: WebsiteUrls.DriverListSource);
#if DEBUG
                //Console.WriteLine(s.ToString());
#endif

                List<float> driverTitles = new List<float>();
                foreach (LinkItem i in LinkFinder.Find(s))
                {
#if DEBUG
                    //Console.WriteLine(i.ToString());
#endif
                    string iS = i.Text;
                    {
                        //Console.WriteLine("iS = '" + iS + "'");
                        //if (iS.Contains("FAQ/Discussion Thread") && !iS.Contains("Latest Driver"))
                        //if (iS.Contains("FAQ/Discussion"))
                        {
                            //Console.WriteLine("Filtered iS = '" + iS + "'");
                            // HTTPS URL example:  'https://www.reddit.com/r/nvidia/comments/6k8pas/driver_38476_faqdiscussion_thread/    Driver 384.76 FAQ/Discussion Thread'
                            // Reddit URL example: '/r/nvidia/comments/4stpdj/driver_36881_faqdiscussion_thread/  DiscussionDriver 368.81 FAQ/Discussion Thread'
                            // We need to strip the parts we don't want to uniformize the parsing
                            iS = iS.Replace("https://www.reddit.com/r/nvidia/comments", "/r/nvidia/comments");
                            //string iFlat = SpaceCompactor.CompactWhitespaces(iS);
                            // NOTE: Sometimes reddit returns https, sometimes the /r/ URL, the latter does not contain a space between
                            // the version and "FAQ", in which case we need to add it for split to work properly.
                            //if (iFlat == null)
                            //{
                            //    return;
                            //}

                            //if (iFlat.StartsWith("/r/nvidia/comments/"))
                            //{
                            //    // Funny Reddit returned a local link; handle the missing space here
                            //    iFlat = iFlat.Replace("FAQ/Discussion", " FAQ/Discussion");
                            //    // That url also contains web formatting victims, such as "DiscussionDriver"
                            //    iFlat = iFlat.Replace(" DiscussionDriver", "");
                            //}
                            //else
                            //{
                            //    iFlat = iFlat.Replace(" Driver", "");
                            //}

                            //#if DEBUG
                            //                            Console.WriteLine("i_flat = '" + iFlat + "'");
                            //#endif
                            //                            string[] titleNolink = iFlat.Split(' ');
                            //                            string parsedVersion = titleNolink.GetValue(1)?.ToString();
                            //#if DEBUG
                            //                            Console.WriteLine("Parsed Version = '" + parsedVersion + "'");
                            //#endif
                            //
                            //                            driverTitles.Add(StringToFloat(parsedVersion));

                            string[] prefix = i.Text.Split(new string[] { "FAQ" }, StringSplitOptions.None);
                            string version = prefix.First().Split(new string[] { "Driver " }, StringSplitOptions.None).Last();
#if DEBUG
                            Console.WriteLine(version);
#endif
                            if (version.Contains("."))
                            {
                                driverTitles.Add(StringToFloat(version));
                            }
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
                    float latestDriver = driverTitles.Last();
#if !DEBUG
                    Console.WriteLine("Latest Driver: " + driverTitles.Last());
#endif

                    // Build new URL from latest version
                    // Note: '388.00' becomes '388' somewhere above, need to add '.00' at the end if trying to use that one.
                    // http://us.download.nvidia.com/Windows/397.93/397.93-desktop-win10-64bit-international-whql.exe
                    string newUrl =
                        String.Format(
                            "http://us.download.nvidia.com/Windows/{0}/{0}-desktop-win10-64bit-international-whql.exe",
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
        public const string DriverListSource = "https://www.reddit.com/r/nvidia/search?q=FAQ/Discussion&restrict_sr=1&sort=new";
    }
}