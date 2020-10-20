using System;
using System.Diagnostics;
using System.IO;

namespace PicoGAUpdate.Components
{
    public static class Stripper
    {
        public static bool StripComponentsViaUninstall()
        {
            Console.WriteLine("Looking for components to strip via uninstall...");
            try
            {
                // Break down uninstall command '"C:\Windows\SysWOW64\rundll32.exe" C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL",UninstallPackage HDAudio.Driver'
                string rundll32 = @"C:\Windows\SysWOW64\rundll32.exe";
                string NVUnst = @"C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL";
                string cmd = "UninstallPackage";
                //string audioDriver = "HDAudio.Driver"; // Audio Driver
                string GFE = "Display.GFExperience"; // GeForce Experience
                //string extraSwitches = "-silent"; // this don't work anymore.
                string extraSwitches = "";
                Console.WriteLine(NVUnst);
                if (File.Exists(NVUnst))
                {
                    Process installerProcess = new Process
                    {
                        StartInfo =
                        {
                            FileName = rundll32,
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            Arguments = String.Format("\"{0}\",{1} {2} {3}", NVUnst, cmd, GFE, extraSwitches)
                        }
                    };
                    Console.WriteLine("Starting " + installerProcess.StartInfo.FileName + " " +
                                      installerProcess.StartInfo.Arguments);
                    installerProcess.Start();
                    installerProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return true;
        }
    }
}