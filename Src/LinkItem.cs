using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PicoGAUpdate
{
    public struct LinkItem
    {
        public string Href;
        public string Version;
        public bool studio;
        public string dlurl;

        public override string ToString()
        {
            return Href + "\n\t" + Version;
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
                //Console.WriteLine("[m] " + value + "\n");
                LinkItem i = new LinkItem();
                // 3.
                // Get href attribute.
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;

                    // match driver thread format
                    /// Fucking h00mans!
                    /// /r/nvidia/comments/95chif/geforce_hotfix_driver_39886/
                    Match m3 = Regex.Match(i.Href, @"^(https://(www|old).reddit.com)?/r/nvidia/comments/",
                    //Match m3 = Regex.Match(i.Href, @"^/r/nvidia/comments/(.*?)/(geforce_hotfix_driver_(.*?)|driver_(.*?)_faq)",
                        RegexOptions.Singleline);
                    if (m3.Success)
                    {
                        
                        Match m4 = Regex.Match(i.Href, "r/nvidia/comments/(.*)/(|game_ready_)(|studio_)(|driver_)(.*?)_faq(discussion_thread)?", RegexOptions.Singleline);
                        if (m4.Success)
                        {
//#if DEBUG
//                            Console.WriteLine("LINK!!!!!! \n\n" + i.Href + "\n\n");
//#endif
                            // 4.
                            // Remove inner tags from text.
                            string t = Regex.Replace(value, @"\s*<.*?>\s*", "", RegexOptions.Singleline).ToLowerInvariant();
                            t.Replace(Environment.NewLine,"");
                            t.Replace("\t","");
                            if (t == "")
                            {
                                continue;
                            }
                            if (t.EndsWith("comments"))
                            {
                                continue;
                            }
                            // TODO: use "latest driver" when available: "found: 'latest driver faq/discussion thread'"
#if DEBUG
                            Console.WriteLine("Found '" + t + "'");
#endif
                            // NOTE: We don't use hotfix drivers because the URLs are different/private
                            // TODO: Regex for 'Driver 382.05FAQ/Discussion'
                            //if (t.Contains("Driver") && (t.Contains("Hotfix") || (t.Contains("FAQ/Discussion") && !t.Contains("Latest"))))
                            if (t.Contains("driver "))
                            {
                                // TODO: cut off at first number instead
                                t = t.Replace(" faq/discussion", "");
                                if(t.StartsWith("discussion"))
                                {
                                    continue;
                                }
//#if DEBUG
//                                Console.WriteLine("String Trim=> '" + t + "'");
//#endif
                                t = t.Replace(" thread", "");
//#if DEBUG
//                                Console.WriteLine("String Trim=> '" + t + "'");
//#endif
                                t = t.Replace("driver ", "");
//#if DEBUG
//                                Console.WriteLine("String Trim=> '" + t + "'");
//#endif
                                t = t.Replace("&amp; ", "");
//#if DEBUG
//                                Console.WriteLine("String Trim=> '" + t + "'");
//#endif
                                if (t.Contains("studio"))
                                {
                                    i.studio = true;
                                }
                                t = t.Replace("studio ", "");
//#if DEBUG
//                                Console.WriteLine("String Trim=> '" + t + "'");
//#endif
                                t = t.Replace("game ready", "");

                                i.Href = i.Href.Trim();
                                
                                // Sometimes Trim fails
                                t = t.Replace(" ", "");
                                if (t.Contains("latestdiscussion"))
                                {
                                    continue;
                                }
                                i.Version = t;
//#if DEBUG
//                                Console.WriteLine("Found Thread =>'" + t + "'");
//#endif
#if DEBUG
                                Console.WriteLine("Adding '" + t + "' (" + i.Href + ")");
#endif
                                i.dlurl = String.Format("http://us.download.nvidia.com/Windows/{0}/{0:#.##}-desktop-win10-64bit-international-{1}whql.exe", i.Version, i.studio ? "nsd-" : "");
#if DEBUG
                                Console.WriteLine("URL: " + i.dlurl);
#endif
                                if (list.FindIndex(x => x.Version == i.Version) == -1)
                                {
                                    list.Add(i);
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            // inform us of potentially valid entries that need to be regex matched
                            if (i.Href.Contains("driver"))
                            {
                                Console.WriteLine("Regex Failed on => " + i.Href);
                            }
#endif
                            // Skip to next link
                            //continue;
                        }
                    }
                }
            }
            // Sort the list
            List<LinkItem> SortedList = list.OrderBy(o => o.Version).ToList();
            return SortedList;
        }
    }
}
