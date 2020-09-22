﻿using System;
using System.Collections.Generic;
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
        static List<Option> OptionsList = new List<Option>();

        public static Option Clean = new Option("--clean", "-c", false,
            "Cleans the installer \"leftovers\" in \'Installer2\'. Note: This will break the built-in uninstaller.");

        public static Option DownloadOnly = new Option("--download-only", "-d", false,
            @"Do not run the downloaded driver. Useful with --keep.");

        public static Option ForceDownload = new Option("--download", "-d", false,
            "Force Download of the latest or specified driver version even if present/up to date");

        public static Option ForceInstall = new Option("--install", "-f", false,
            "Force installation of the latest driver version even if up-to-date.");

        public static Option Silent = new Option("--silent", "-s", false,
            "Run the installer silently.");

        public static Option DeleteDownloaded = new Option("--keep", "-k", false,
            "Delete the downloaded driver before exiting the program.");

        public static Option Studio = new Option("--studio", "-S", false,
           "Use the NVIDIA Studio driver where available. Uses the GameReady driver otherwise.");

        public static Option Strip = new Option("--strip", "-x", false,
        "Attempt to strip all components deemed useless by this tool's developer(s).");

        public static Option NoUpdate = new Option("--no-update", "-n", false,
            "Do not attempt to download and run a new driver package. Useful in combination with " + Clean.GetLongSwitch() +
            " or " + Strip.GetLongSwitch() + ".");

        public static Option Help = new Option("--help", "-h", false,
            "This help text.");

        public static Option BareDriver = new Option("--bare", "-X", false,
            "Only install the bare INF driver. Experimental.");

        public class Option
        {
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Option)obj);
            }

            protected bool Equals(Option other)
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

            internal Option(string longSwitch, string shortSwitch = "", bool defaultValue = false, string helpText = "")
            {
                DefaultValue = defaultValue;
                CurrentValue = defaultValue;
                LongSwitch = longSwitch;
                ShortSwitch = shortSwitch;
                HelpText = helpText;
                OptionsList.Add(this);
            }
            
            public Option()
            {
            }

            private bool DefaultValue { get; set; }
            private bool CurrentValue { get; set; }
            private string LongSwitch { get; set; }
            private string ShortSwitch { get; set; }
            private string HelpText { get; set; }

            public static implicit operator bool(Option foo)
            {
                return !ReferenceEquals(foo, null) && foo.GetValue();
            }
            public bool Is(Option other)
            {
                return this.LongSwitch.Equals(other.LongSwitch);
            }

            // TODO: Make this the implicit getter thingy
            public static bool operator ==(Option value1, bool valuebool)
            {
                return value1.GetValue();
            }

            public static bool operator !=(Option value1, bool valuebool)
            {
                return !value1.GetValue();
            }

            public static bool IsDefault(Option value1)
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
                Console.WriteLine("Usage: " + String.Format("{0} [OPTION]...", System.Diagnostics.Process.GetCurrentProcess().ProcessName) + Environment.NewLine + Environment.NewLine + "Options:" + Environment.NewLine);
                foreach (Option item in OptionsList)
                {
                    Console.WriteLine(item.GetPaddedHelpText());
                }
            }

            public static void Parse(string[] args)
            {

                if (args != null)
                {

                    if (args.Length > 0)
                    {
                        foreach (var arg in args.ToArray())
                        {
                            //Console.WriteLine("Processing switch " + arg);
                            if (arg.Equals(OptionContainer.Help.GetShortSwitch()) || arg.Equals(OptionContainer.Help.GetLongSwitch()) || arg.Equals("/?"))
                            {
                                PrintHelp();
                                Environment.Exit(2);
                            }

                            foreach (Option o in OptionsList)
                            {
                                //Console.WriteLine("Looking for " + o.LongSwitch + "...");
                                if (o.LongSwitch.Equals(arg) || o.ShortSwitch.Equals(arg))
                                {
                                    //Console.WriteLine("Found " + arg);
                                    o.SetValue(true);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Console.WriteLine("No arguments given, using defaults");
                        OptionContainer.Strip.SetValue(false);
                        OptionContainer.Silent.SetValue(true);
                    }
                }
                
            }
        }
    }
}