namespace HotsReplayReader
{
    internal class HotsLocalAccount
    {
        public string? BattleTagName { get; set; }
        public string? FullPath { get; set; }
        public bool RegionAmerica { get; set; } = false;
        public string? RegionAmericaFullPath { get; set; } = "";
        public bool RegionEurope { get; set; } = false;
        public string? RegionEuropeFullPath { get; set; } = "";
        public bool RegionAsia { get; set; } = false;
        public string? RegionAsiaFullPath { get; set; } = "";
        public bool RegionPTR { get; set; } = false;
        public string? RegionPTRFullPath { get; set; } = "";
    }
}
