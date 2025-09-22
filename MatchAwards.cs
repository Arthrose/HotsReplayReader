using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    internal class MatchAward
    {
        [JsonPropertyName("gameLink")]
        public string GameLink { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("mvpScreenIcon")]
        public string MvpScreenIcon { get; set; }

        [JsonPropertyName("scoreScreenIcon")]
        public string ScoreScreenIcon { get; set; }
        public string Description { get; set; }
    }
    internal class MatchAwards : Dictionary<string, MatchAward> { }
}
