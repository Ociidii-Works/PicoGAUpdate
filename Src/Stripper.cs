using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace PicoGAUpdate
{
    static partial class Program
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
                    Console.WriteLine(NVUnst);
                    if (File.Exists(NVUnst))
                    {
                        Process NVU = new Process
                        {
                            StartInfo =
                        {
                            FileName = rundll32,
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            Arguments = String.Format("\"{0}\",{1} {2} -silent",NVUnst,cmd,GFE)
                        }
                        };
                        Console.WriteLine("Starting " + NVU.StartInfo.FileName + " " + NVU.StartInfo.Arguments);
                        NVU.Start();
                        NVU.WaitForExit();
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
}