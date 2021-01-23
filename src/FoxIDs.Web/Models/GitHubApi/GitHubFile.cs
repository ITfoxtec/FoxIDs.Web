using Newtonsoft.Json;

namespace FoxIDs.Web.Models
{
    public class GitHubFile : GitHubFileInfo
    {
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "encoding")]
        public string Encoding { get; set; }
    }
}
