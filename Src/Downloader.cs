using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace PicoGAUpdate
{
    public class NewDownloader
    {
        public static bool Success;
        public static bool DownloadCancelled;
        private static WebClient _wc;
        private static long totalDLSize = 0;

        private bool _printing;

        // TODO: Work in a subdir
        private static readonly string FileName = Path.GetTempFileName();

        public static void RenameDownload(string newName)
        {
            if (string.IsNullOrEmpty(newName) || FileName == null)
            {
                return;
            }
            if (File.Exists(FileName))
            {
                if (File.Exists(newName))
                {
                    Console.WriteLine("Deleting old " + newName);
                    File.Delete(newName);
                }
                File.Move(FileName, newName);
            }
        }

        /// <summary>
        /// Download a file asynchronously
        /// </summary>
        public void Download(string url, string destination)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            Program.DownloadDone = false;
            using (_wc = new WebClient())
            {
                _wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                _wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                _wc.DownloadFileAsync(new Uri(url), FileName ?? throw new InvalidOperationException());
                //wc.DownloadFile(new Uri(url), destination);
                Console.CancelKeyPress += Console_CancelKeyPress;
            }
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Download Cancelled!");
            if (_wc != null) _wc.CancelAsync();
            if (File.Exists(FileName))
            {
                Console.WriteLine("Deleting incomplete file " + FileName);
                File.Delete(FileName ?? throw new InvalidOperationException());
            }
        }

        private string AutoSpacer(long input)
        {
            if (input < 10)
            {
                return "   ";
            }
            if (input < 100)
            {
                return "  ";
            }
            //if (input < 1000)
            return " ";
        }

        /// <summary>
        ///  Show the progress of the download in a progressbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (!_printing)
            {
                if (e != null)
                {
                    if (totalDLSize == 0)
                    {
                        totalDLSize = (e.TotalBytesToReceive / 1024 / 1024);
                    }
                    long currentDLSize = (e.BytesReceived / 1024 / 1024);
                    Program.RollingOutput(e.ProgressPercentage + "%" + AutoSpacer(e.ProgressPercentage) + "| " + currentDLSize
                        + AutoSpacer(currentDLSize) + "MB / " +
                                          totalDLSize + " MB");
                    _printing = false;
                }

                // 50% | 5000 bytes out of 10000 bytes retrieven.
            }
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //progressBar1.Value = 0;

            if (e != null && e.Cancelled)
            {
                Program.RollingOutput("The download has been cancelled", true);
                DownloadCancelled = true;
            }
            else if (e != null && e.Error != null) // We have an error! Retry a few times, then abort.
            {
                Program.RollingOutput("An error ocurred while trying to download file", true);
            }
            else
            {
                Program.RollingOutput("File succesfully downloaded", true);
                Success = true;
            }

            // For RollingOutput needs
            Console.WriteLine();
            Console.CursorVisible = true;
            Program.DownloadDone = true;
        }
    }
}