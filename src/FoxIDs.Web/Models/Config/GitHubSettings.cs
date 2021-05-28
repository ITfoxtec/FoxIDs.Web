namespace FoxIDs.Web.Models.Config
{
    public class GitHubSettings
    {
#if DEBUG
        public bool LoadFiles { get; set; }
        public string DocsFileDirectory { get; set; }
#endif

        public string Repository { get; set; }
        public string Project { get; set; }
        public string Branch { get; set; }
        public string Folder { get; set; }
    }
}
