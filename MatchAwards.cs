using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    internal class MatchAward
    {
        public string? Description { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("gameLink")]
        public string? GameLink { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("mvpScreenIcon")]
        public string? MvpScreenIcon { get; set; }

        [JsonPropertyName("scoreScreenIcon")]
        public string? ScoreScreenIcon { get; set; }
    }
    internal class MatchAwards : Dictionary<string, MatchAward> { }
}
