namespace HotsReplayReader
{
    public class hotsEmoticonAnimation
    {
        public string texture { get; set; }
        public int frames { get; set; }
        public int? duration { get; set; }
        public int width { get; set; }
        public int columns { get; set; }
        public int rows { get; set; }
    }
    public class hotsEmoticonData
    {
        public string heroId { get; set; }
        public string? heroSkinId { get; set; }
        public string image { get; set; }
        public hotsEmoticonAnimation? animation { get; set; }
        public List<string> aliases { get; set; } = new List<string>();
    }
    public class hotsEmoticon : Dictionary<string, hotsEmoticonData>   
    {
    }
    public class hotsEmoticonAliase
    {
        public Dictionary<string, string> aliases { get; set; }
        public Dictionary<string, string> localizedaliases { get; set; }
    }
}
