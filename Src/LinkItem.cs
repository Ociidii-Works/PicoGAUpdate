using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PicoGAUpdate
{
    public struct LinkItem
    {
        public string Href;
        public string Text;

        public override string ToString()
        {
            return Href + "\n\t" + Text;
        }
    }

    internal static class LinkFinder
    {
        public static List<LinkItem> Find(string file)
        {
            List<LinkItem> list = new List<LinkItem>();

            // 1.
            // Find all matches in file.
            // Example match:
            // <a data-click-id="body" class="SQnoC3ObvgnGjWt90zD9Z" href="/r/nvidia/comments/8u04qj/driver_39836_faqdiscussion/"><h2 class="fiq55l-0 ffGvgK"><span style="font-weight:normal">Driver 398.36 <em style="font-weight:700">FAQ/Discussion</em></span></h2></a>
            MatchCollection m1 = Regex.Matches(file ?? throw new ArgumentNullException(nameof(file)),
                //@"(<a.*?href=/r/nvidia/comments/.*?/driver_\d*?>.*?</a>)",
                @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();
                // 3.
                // Get href attribute.
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }

                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                    RegexOptions.Singleline);
                // TODO: Regex for 'Driver 382.05FAQ/Discussion'
                if (t.Contains("Driver") && t.Contains("FAQ/Discussion") && !t.Contains("Latest"))
                {
                    if (t.Contains("GeForce Hotfix Driver"))
                    {
                        string n = t.Replace("GeForce Hotfix Driver", "Driver");
#if DEBUG
                        Console.WriteLine(t + "=>" + n);
#endif
                        t = n;
                    }
                    i.Text = t;
#if DEBUG
                    Console.WriteLine("=>'" + t + "'");
#endif
                    list.Add(i);
                }
            }
            return list;
        }
    }
}