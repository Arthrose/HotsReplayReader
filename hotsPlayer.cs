using Heroes.StormReplayParser.Player;

namespace HotsReplayReader
{
    internal class hotsPlayer : stormPlayer
    {
        public string Party { get; set; }
        public string teamColor { get; set; }
        public double mvpScore { get; set; }
        public hotsTeam playerTeam { get; set; }
        public hotsTeam enemyTeam { get; set; }
        public IReadOnlyList<HeroMasteryTier> heroMasteryTiers { get; set; }
        public int? heroMasteryTiersCount { get; set; }
        public IReadOnlyList<MatchAwardType>? matchAwards { get; set; }
        public int? matchAwardsCount { get; set; }
        public IReadOnlyList<PlayerDisconnect> playerDisconnects { get; set; }
        public ScoreResult? scoreResult { get; set; }
        public IReadOnlyList<HeroTalent> talents { get; set; }
        public hotsPlayer(stormPlayer stormPlayer)
        {
            this.AccountLevel = stormPlayer.AccountLevel;
            this.BattleTagName = stormPlayer.BattleTagName;
            this.ComputerDifficulty = stormPlayer.ComputerDifficulty;
            this.Handicap = stormPlayer.Handicap;
            this.HasActiveBoost = stormPlayer.HasActiveBoost;
            this.IsAutoSelect = stormPlayer.IsAutoSelect;
            this.IsBlizzardStaff = stormPlayer.IsBlizzardStaff;
            this.IsPlatformMac = stormPlayer.IsPlatformMac;
            this.IsSilenced = stormPlayer.IsSilenced;
            this.IsVoiceSilenced = stormPlayer.IsVoiceSilenced;
            this.IsWinner = stormPlayer.IsWinner;
            this.Name = stormPlayer.Name;
            this.PartyValue = stormPlayer.PartyValue;
            this.PlayerHero = stormPlayer.PlayerHero;
            this.PlayerLoadout = stormPlayer.PlayerLoadout;
            this.PlayerType = stormPlayer.PlayerType;
            this.Team = stormPlayer.Team;
            this.ToonHandle = stormPlayer.ToonHandle;
            this.heroMasteryTiers = stormPlayer.HeroMasteryTiers;
            this.heroMasteryTiersCount = stormPlayer.HeroMasteryTiersCount;
            this.matchAwards = stormPlayer.MatchAwards;
            this.matchAwardsCount = stormPlayer.MatchAwardsCount;
            this.playerDisconnects = stormPlayer.PlayerDisconnects;
            this.scoreResult = stormPlayer.ScoreResult;
            this.talents = stormPlayer.Talents;
        }
    }
}
