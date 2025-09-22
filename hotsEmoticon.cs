namespace HotsReplayReader
{
    public class HotsEmoticonAnimation
    {
        public string texture { get; set; }
        public int frames { get; set; }
        public int? duration { get; set; }
        public int width { get; set; }
        public int columns { get; set; }
        public int rows { get; set; }
    }
    public class HotsEmoticonData
    {
        public string heroId { get; set; }
        public string? heroSkinId { get; set; }
        public string image { get; set; }
        public HotsEmoticonAnimation? animation { get; set; }
        public List<string> aliases { get; set; } = new List<string>();
    }
    public class HotsEmoticon : Dictionary<string, HotsEmoticonData>
    {
    }
    public class HotsEmoticonAliase
    {
        public Dictionary<string, string> aliases { get; set; }
        public Dictionary<string, string> localizedaliases { get; set; }
    }
}
