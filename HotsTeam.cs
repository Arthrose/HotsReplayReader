namespace HotsReplayReader
{
    internal class HotsTeam(string Name)
    {
        public string Name { get; set; } = Name;
        public int Level { get; set; } = 0;
        public int MaxKills { get; set; } = 0;
        public int MaxTakedowns { get; set; } = 0;
        public int MaxDeaths { get; set; } = 999999999;
        public int MaxSiegeDmg { get; set; } = 0;
        public int MaxHeroDmg { get; set; } = 0;
        public int MaxHealing { get; set; } = 0;
        public int MaxDmgTaken { get; set; } = 0;
        public int MaxExp { get; set; } = 0;
        public int TotalKills { get; set; } = 0;
        public int TotalDeath { get; set; } = 0;
        public bool IsWinner { get; set; } = false;
    }
}
