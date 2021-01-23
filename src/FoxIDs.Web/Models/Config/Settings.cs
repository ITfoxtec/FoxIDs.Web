namespace FoxIDs.Web.Models.Config
{
    public class Settings
    {
        public string BaseSitePath { get; set; }

        public string BlobConnectionString { get; set; }

        public string ReloadGitHubPassword { get; set; }

        public string GitHubSiteFolder { get; set; }

        public string GitHubApiToken { get; set; }

        public GitHubSettings FoxIDsGitHub { get; set; }

        public GoogleAnalyticsSettings GoogleAnalytics { get; set; }
    }
}
