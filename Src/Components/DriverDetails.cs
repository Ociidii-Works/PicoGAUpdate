namespace PicoGAUpdate.Components
{
    // TODO: Re-work how this is stored to allow creation via e.g. LinkItem(427.24), and string variant with included parser
    public class DriverDetails
    {
        public string DetailsURL;

        public string DownloadUrl;

        public string DownloadURLFileSize;

        public string Name;

        // e.g. "GeForce%20Game%20Ready%20Driver"
        public string ReleaseDateTime;

        public System.Version Version;

        // Weirdly encoded and not really relevant for the user
        //public string ReleaseNotes;

        public static DriverDetails CreateFromVersion(string version_in)
        {
            DriverDetails d = new DriverDetails();
            d.Version = new System.Version(version_in);

            return d;
        }

        public static class Types
        {
            public const bool GameReady = false;
            public const bool Studio = true;
        }
    }

    //public string OtherNotes;
}
