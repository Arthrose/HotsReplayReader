namespace HotsReplayReader
{
    public class HotsEmoticonAnimation
    {
        public string? Texture { get; set; }
        public int Frames { get; set; }
        public int? Duration { get; set; }
        public int Width { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
    }
    public class HotsEmoticonData
    {
        public string? HeroId { get; set; }
        public string? HeroSkinId { get; set; }
        public string? Image { get; set; }
        public HotsEmoticonAnimation? Animation { get; set; }
        public List<string> Aliases { get; set; } = [];
    }
    public class HotsEmoticon : Dictionary<string, HotsEmoticonData>
    {
    }
    public class HotsEmoticonAliase
    {
        public Dictionary<string, string>? Aliases { get; set; }
        public Dictionary<string, string>? Localizedaliases { get; set; }
    }
}
