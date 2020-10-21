using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace PicoGAUpdate.Components
{
    public class NewDownloader
    {
        public static bool DownloadCancelled;
        public static bool Success;

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
                // TODO: Add a no-connection timeout
                _wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                _wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                _wc.DownloadFileAsync(new Uri(url), FileName ?? throw new InvalidOperationException());
                //wc.DownloadFile(new Uri(url), destination);
                Console.CancelKeyPress += Console_CancelKeyPress;
            }
        }

        // TODO: Work in a subdir
        private static readonly string FileName = Path.GetTempFileName();

        private static long _totalDlSize;
        private static WebClient _wc;
        private bool _printing;

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
                    if (_totalDlSize == 0)
                    {
                        _totalDlSize = (e.TotalBytesToReceive / 1024 / 1024);
                    }
                    long currentDlSize = (e.BytesReceived / 1024 / 1024);
                    Program.RollingOutput("                " + e.ProgressPercentage + "%" + AutoSpacer(e.ProgressPercentage) + "| " + currentDlSize
                        + AutoSpacer(currentDlSize) + "MB / " +
                                          _totalDlSize + " MB");
                    _printing = false;
                }

                // 50% | 5000 bytes out of 10000 bytes retrieven.
            }
        }
    }
}
