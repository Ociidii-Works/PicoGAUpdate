namespace PicoGAUpdate.Components
{
    public static class DriverTypes
    {
        public const bool GameReady = false;
        public const bool Studio = true;
    }

    // TODO: Re-work how this is stored to allow creation via e.g. LinkItem(427.24), and string variant with included parser
    public class LinkItem
    {
        public string DetailsURL;
        public string DownloadUrl;
        public string DownloadURLFileSize;
        public string Name;

        // e.g. "GeForce%20Game%20Ready%20Driver"
        public string ReleaseDateTime;

        public string Version;
        //public string OtherNotes;
    }
}
