using System;
using System.Diagnostics;
using System.IO;

namespace PicoGAUpdate.Components
{
    internal static class Safe
    {
        public static void DirectoryDelete(string targetDir, bool print = false)
        {
            if (print)
            {
                Console.WriteLine("Deleting " + targetDir);
            }
            try
            {
                if (string.IsNullOrEmpty(targetDir))
                {
                    Console.WriteLine("Aborting, no parameter specified!");
                    return;
                }
                if (!Directory.Exists(targetDir))
                {
                    Debug.WriteLine("Skipping deletion of non-existent folder " + targetDir);
                    return;
                }

                string[] files = Directory.GetFiles(targetDir);
                string[] dirs = Directory.GetDirectories(targetDir);

                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                foreach (string dir in dirs)
                    DirectoryDelete(dir);

                Directory.Delete(targetDir, false);
            }
            catch (Exception ex)
            {
                // We said silently.
                Console.WriteLine("Something went wrong deleting " + targetDir + ":" + Environment.NewLine + ex);
                Program.ExitImmediately = false;
            }
        }
    }
}
