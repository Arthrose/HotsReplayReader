using Heroes.StormReplayParser.Player;

namespace HotsReplayReader
{
    internal class HotsPlayer : StormPlayer
    {
        public string? Party { get; set; }
        public string? TeamColor { get; set; }
        public int Kills { get; set; }
        public string? ComputerName { get; set; }
        public double MvpScore { get; set; }
        public double? MvpScoreKills { get; set; } = null;
        public double? MvpScoreAssists { get; set; } = null;
        public double? MvpScoreTimeSpentDead { get; set; } = null;
        public double? MvpScoreWinningTeam { get; set; } = null;
        public double? MvpScoreTopHeroDamageOnTeam { get; set; } = null;
        public double? MvpScoreTopHeroDamage { get; set; } = null;
        public double? MvpScoreTopSiegeDamageOnTeam { get; set; } = null;
        public double? MvpScoreTopSiegeDamage { get; set; } = null;
        public double? MvpScoreTopXPContributionOnTeam { get; set; } = null;
        public double? MvpScoreTopXPContribution { get; set; } = null;
        public double? MvpScoreTopHealing { get; set; } = null;
        public double? MvpScoreTopDamageTakenOnTeam { get; set; } = null;
        public double? MvpScoreTopDamageTaken { get; set; } = null;
        public double? MvpScoreHeroDamageBonus { get; set; } = null;
        public double? MvpScoreSiegeDamageBonus { get; set; } = null;
        public double? MvpScoreHealingBonus { get; set; } = null;
        public double? MvpScoreXPContributionBonus { get; set; } = null;
        public double? MvpScoreDamageTakenBonus { get; set; } = null;
        public HotsTeam? PlayerTeam { get; set; }
        public HotsTeam? EnemyTeam { get; set; }
        public string? HeroUnitId { get; set; }
        public new IReadOnlyList<HeroMasteryTier> HeroMasteryTiers { get; set; }
        public new int? HeroMasteryTiersCount { get; set; }
        public new IReadOnlyList<MatchAwardType>? MatchAwards { get; set; }
        public new int? MatchAwardsCount { get; set; }
        public new IReadOnlyList<PlayerDisconnect> PlayerDisconnects { get; set; }
        public new ScoreResult? ScoreResult { get; set; }
        public new IReadOnlyList<HeroTalent> Talents { get; set; }
        public HotsPlayer(StormPlayer stormPlayer)
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
            this.HeroMasteryTiers = stormPlayer.HeroMasteryTiers;
            this.HeroMasteryTiersCount = stormPlayer.HeroMasteryTiersCount;
            this.MatchAwards = stormPlayer.MatchAwards;
            this.MatchAwardsCount = stormPlayer.MatchAwardsCount;
            this.PlayerDisconnects = stormPlayer.PlayerDisconnects;
            this.ScoreResult = stormPlayer.ScoreResult;
            this.Talents = stormPlayer.Talents;
            if (stormPlayer.ScoreResult != null)
                this.Kills = stormPlayer.ScoreResult.SoloKills;
            else
                this.Kills = 0;
        }
    }
}
