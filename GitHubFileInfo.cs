using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    internal class GitHubFileInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("path")]
        public string? Path { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("download_url")]
        public string? DownloadURL { get; set; }
        [JsonPropertyName("url")]
        public string? URL { get; set; }
    }
}
