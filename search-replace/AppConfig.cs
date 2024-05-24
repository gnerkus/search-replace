using Newtonsoft.Json;

namespace search_replace
{
    public class AppConfig
    {
        [JsonProperty("directory_path")] public string DirectoryPath { get; set; }

        [JsonProperty("replace_with")] public string ReplaceWith { get; set; }

        [JsonProperty("find_what")] public string FindWhat { get; set; }
    }
}