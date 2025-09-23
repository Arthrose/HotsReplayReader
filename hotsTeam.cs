namespace HotsReplayReader
{
    internal class HotsTeam
    {
        public string Name { get; set; }
        public int maxKills { get; set; }
        public int maxTakedowns { get; set; }
        public int maxDeaths { get; set; }
        public int maxSiegeDmg { get; set; }
        public int maxHeroDmg { get; set; }
        public int maxHealing { get; set; }
        public int maxDmgTaken { get; set; }
        public int maxExp { get; set; }
        public int totalKills { get; set; }
        public int totalDeath { get; set; }
        public bool isWinner { get; set; }

        public HotsTeam(string Name)
        {
            this.Name = Name;
            this.maxKills = 0;
            this.maxTakedowns = 0;
            this.maxDeaths = 999999999;
            this.maxSiegeDmg = 0;
            this.maxHeroDmg = 0;
            this.maxHealing = 0;
            this.maxDmgTaken = 0;
            this.maxExp = 0;
            this.totalKills = 0;
            this.totalDeath = 0;
            this.isWinner = false;
        }
    }
}
