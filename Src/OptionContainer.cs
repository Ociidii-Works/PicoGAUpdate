using System;
using System.Linq;

namespace PicoGAUpdate
{
    public static class Extensions
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        ///     Returns a string array that contains the substrings in this instance that are delimited by specified indexes.
        /// </summary>
        /// <param name="source">The original string.</param>
        /// <param name="index">An index that delimits the substrings in this string.</param>
        /// <returns>An array whose elements contain the substrings in this instance that are delimited by one or more indexes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="index" /> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An <paramref name="index" /> is less than zero or greater than the length of this instance.</exception>
        public static string[] SplitAt(this string source, params int[] index)
        {
            index = index.Distinct().OrderBy(x => x).ToArray();
            string[] output = new string[index.Length + 1];
            int pos = 0;

            for (int i = 0; i < index.Length; pos = index[i++])
                output[i] = source.Substring(pos, index[i] - pos);

            output[index.Length] = source.Substring(pos);
            return output;
        }
    }

    public class OptionContainer
    {
        public static Options Clean = new Options("--clean", "-c", false,
            "Cleans the installer \"leftovers\" in \'Installer2\'.");

        public static Options DownloadOnly = new Options("--download-only", "-d", false,
            @"Do not run the downloaded driver. Useful with --keep.");

        public static Options ForceDownload = new Options("--force", "-f", false,
            "Downloads the latest or specified driver version even if up-to-date.");

        public static Options Silent = new Options("--silent", "-s", false,
            "Run the installer silently. This however installs everything.");

        public static Options KeepDownloaded = new Options("--keep", "-k", false,
            "Do not delete the downloaded driver before exiting the program.");

        public static Options NoUpdate = new Options("--no-update", "-n", false,
            "Do not attempt to download and run a new driver package. Useful in combination with " + Clean.GetLongSwitch() +
            ".");

        public class Options
        {
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Options)obj);
            }

            protected bool Equals(Options other)
            {
                return CurrentValue == other.CurrentValue;
            }

            public override int GetHashCode()
            {
                return CurrentValue.GetHashCode();
            }

            // ReSharper disable MemberCanBePrivate.Global
            // ReSharper disable FieldCanBeMadeReadOnly.Global
            // ReSharper disable InconsistentNaming.Local

            internal Options(string longSwitch, string shortSwitch = "", bool defaultValue = false, string helpText = "")
            {
                DefaultValue = defaultValue;
                CurrentValue = defaultValue;
                LongSwitch = longSwitch;
                ShortSwitch = shortSwitch;
                HelpText = helpText;
            }

            public Options()
            {
            }

            private bool DefaultValue { get; set; }
            private bool CurrentValue { get; set; }
            private string LongSwitch { get; set; }
            private string ShortSwitch { get; set; }
            private string HelpText { get; set; }

            public static implicit operator bool(Options foo)
            {
                return !ReferenceEquals(foo, null) && foo.GetValue();
            }

            // TODO: Make this the implicit getter thingy
            public static bool operator ==(Options value1, bool valuebool)
            {
                return value1.GetValue();
            }

            public static bool operator !=(Options value1, bool valuebool)
            {
                return !value1.GetValue();
            }

            public static bool IsDefault(Options value1)
            {
                return value1.GetValue() == value1.GetDefault();
            }

            public bool GetValue()
            {
                return CurrentValue;
            }

            public void SetValue(bool newValue)
            {
                CurrentValue = newValue;
            }

            public bool GetDefault()
            {
                return DefaultValue;
            }

            public string GetShortSwitch()
            {
                return ShortSwitch;
            }

            public string GetLongSwitch()
            {
                return LongSwitch;
            }

            public string GetHelpText()
            {
                return HelpText;
            }

            private string GetPaddedHelpText()
            {
                string thisline = " " + GetShortSwitch() + ", " + GetLongSwitch();
                string paddedline = Program.AutoPad2(thisline, 25);
                string ht = HelpText;
                int newlen = paddedline.Length + ht.Length;
                // TODO: use WHILE() to split very long help text and re-assign ht inside loop to run until it fits
                if (newlen > Console.BufferWidth)
                {
                    string[] split = ht.SplitAt(-5 + Math.Abs((Console.BufferWidth) - newlen + ht.Length));
                    // TODO: Split to previous word
                    ht = split[0] + Environment.NewLine + Program.AutoPad2("", 25) + split[1];
                }

                return Program.AutoPad(thisline, 25) + ht + Environment.NewLine;
            }

            public static void PrintHelp()
            {
                string helpParagraph = "Usage: " + String.Format("{0} [OPTION]...", System.Diagnostics.Process.GetCurrentProcess().ProcessName) + Environment.NewLine + Environment.NewLine + "Options:" + Environment.NewLine;
                // ReSharper disable PossibleNullReferenceException
                helpParagraph += Clean.GetPaddedHelpText();
                helpParagraph += DownloadOnly.GetPaddedHelpText();
                helpParagraph += ForceDownload.GetPaddedHelpText();
                helpParagraph += Silent.GetPaddedHelpText();
                helpParagraph += KeepDownloaded.GetPaddedHelpText();
                helpParagraph += NoUpdate.GetPaddedHelpText();
                Console.WriteLine(helpParagraph);
                // ReSharper restore PossibleNullReferenceException
            }

            public static void Parse(string[] args)
            {
                if (args != null)
                {
                    var args_str = args.ToArray();
                    if (args_str.Contains("--help") || args_str.Contains("-h") || args_str.Contains("/?"))
                    {
                        PrintHelp();
                        Environment.Exit(2);
                    }
                    foreach (var variable in args)
                    {
                        if (variable == Clean.GetShortSwitch() || variable == Clean.GetLongSwitch())
                        {
                            Clean.SetValue(true);
                        }

                        if (variable == DownloadOnly.GetShortSwitch() || variable == DownloadOnly.GetLongSwitch())
                        {
                            DownloadOnly.SetValue(true);
                        }

                        if (variable == ForceDownload.GetShortSwitch() || variable == ForceDownload.GetLongSwitch())
                        {
                            ForceDownload.SetValue(true);
                        }

                        if (variable == Silent.GetShortSwitch() || variable == Silent.GetLongSwitch())
                        {
                            Silent.SetValue(true);
                        }

                        if (variable == KeepDownloaded.GetShortSwitch() || variable == KeepDownloaded.GetLongSwitch())
                        {
                            KeepDownloaded.SetValue(true);
                        }

                        if (variable == NoUpdate.GetShortSwitch() || variable == NoUpdate.GetLongSwitch())
                        {
                            NoUpdate.SetValue(true);
                        }
                    }
                }
            }
        }
    }
}