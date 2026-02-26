using Heroes.StormReplayParser.GameEvent;
using Heroes.StormReplayParser.Player;
using Heroes.StormReplayParser.Replay;

namespace HotsReplayReader
{
    internal class HotsPlayer(StormPlayer stormPlayer)
    {
        // Copie de StormPlayer
        public int? AccountLevel { get; set; } = stormPlayer.AccountLevel;
        public string BattleTagName { get; set; } = stormPlayer.BattleTagName;
        public ComputerDifficulty ComputerDifficulty { get; set; } = stormPlayer.ComputerDifficulty;
        public int Handicap { get; set; } = stormPlayer.Handicap;
        public bool? HasActiveBoost { get; set; } = stormPlayer.HasActiveBoost;
        public bool IsAutoSelect { get; set; } = stormPlayer.IsAutoSelect;
        public bool? IsBlizzardStaff { get; set; } = stormPlayer.IsBlizzardStaff;
        public bool IsPlatformMac { get; set; } = stormPlayer.IsPlatformMac;
        public bool IsSilenced { get; set; } = stormPlayer.IsSilenced;
        public bool? IsVoiceSilenced { get; set; } = stormPlayer.IsVoiceSilenced;
        public bool IsWinner { get; set; } = stormPlayer.IsWinner;
        public string Name { get; set; } = stormPlayer.Name;
        public long? PartyValue { get; set; } = stormPlayer.PartyValue;
        public PlayerHero? PlayerHero { get; set; } = stormPlayer.PlayerHero;
        public PlayerLoadout PlayerLoadout { get; set; } = stormPlayer.PlayerLoadout;
        public PlayerType PlayerType { get; set; } = stormPlayer.PlayerType;
        public StormTeam Team { get; set; } = stormPlayer.Team;
        public ToonHandle? ToonHandle { get; set; } = stormPlayer.ToonHandle;
        public IReadOnlyList<HeroMasteryTier> HeroMasteryTiers { get; set; } = stormPlayer.HeroMasteryTiers;
        public int HeroMasteryTiersCount { get; set; } = stormPlayer.HeroMasteryTiersCount;
        public IReadOnlyList<MatchAwardType>? MatchAwards { get; set; } = stormPlayer.MatchAwards;
        public int? MatchAwardsCount { get; set; } = stormPlayer.MatchAwardsCount;
        public IReadOnlyList<PlayerDisconnect> PlayerDisconnects { get; set; } = stormPlayer.PlayerDisconnects;
        public ScoreResult? ScoreResult { get; set; } = stormPlayer.ScoreResult;
        public IReadOnlyList<HeroTalent> Talents { get; set; } = stormPlayer.Talents;

        // Nouvelles propriétés
        public string? ComputerName { get; set; }
        public string? HeroUnitId { get; set; }
        //public int Kills { get; set; } = stormPlayer.ScoreResult?.SoloKills ?? 0;
        public Mvp Mvp { get; set; } = new();
        public string? Party { get; set; }
        public HotsTeam? PlayerTeam { get; set; }
        public HotsTeam? EnemyTeam { get; set; }
        public List<PlayerDeath> PlayerDeaths { get; set; } = [];
        public string? TeamColor { get; set; }
        public TimeSpan TimeSpentAFK { get; set; } = TimeSpan.Zero;
        public List<TimeInterval> TimeSpentAFKIntervals { get; set; } = [];
        public List<StormGameEvent> UserActionGameEvents { get; set; } = [];
        public List<StormGameEvent> UserGameEvents { get; set; } = [];
    }
    internal class Mvp
    {
        public double Score { get; set; } = 0;
        public double? Kills { get; set; }
        public double? Assists { get; set; }
        public double? TimeSpentDead { get; set; }
        public double? WinningTeam { get; set; }
        public double? TopHeroDamageOnTeam { get; set; }
        public double? TopHeroDamage { get; set; }
        public double? TopSiegeDamageOnTeam { get; set; }
        public double? TopSiegeDamage { get; set; }
        public double? TopXPContributionOnTeam { get; set; }
        public double? TopXPContribution { get; set; }
        public double? TopHealing { get; set; }
        public double? TopDamageTakenOnTeam { get; set; }
        public double? TopDamageTaken { get; set; }
        public double? HeroDamageBonus { get; set; }
        public double? SiegeDamageBonus { get; set; }
        public double? HealingBonus { get; set; }
        public double? XPContributionBonus { get; set; }
        public double? DamageTakenBonus { get; set; }
    }
    internal class PlayerDeath
    {
        public TimeSpan Timestamp { get; set; }
        public int Level { get; set; } = 20;
        public TimeSpan TimestampRes { get; set; }
        public List<HotsPlayer> KillingPlayers { get; set; } = [];
    }
    public readonly struct TimeInterval
    {
        public TimeSpan Start { get; init; }
        public TimeSpan End { get; init; }
        public TimeSpan Duration => End - Start;
    }
}
