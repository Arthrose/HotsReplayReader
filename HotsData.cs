using System.Diagnostics;
using System.Globalization;
using Heroes.Icons;

namespace HotsReplayReader
{
    internal class HotsData
    {
        private GameStringDocument? gameStringDocument;
        private Heroes.Icons.DataDocument.HeroDataDocument? heroDataDocument;
        private Heroes.Icons.DataDocument.MatchAwardDataDocument? matchAwardDataDocument;

        private Dictionary<string, Heroes.Models.Hero> heroesData = [];

        private Dictionary<string, HotsHero> hotsHeroes = [];

        internal Version versionThreshold = new("2.55.16.97039");
        internal void Parse(string heroDataJsonPath, string gameStringsJsonPath, string matchAwardsJsonPath, Version dbVersion, List<string> HeroIdList)
        {
            if (dbVersion < versionThreshold)
                Debug.WriteLine(heroDataJsonPath);

            gameStringDocument = Heroes.Icons.GameStringDocument.Parse(gameStringsJsonPath);
            heroDataDocument = Heroes.Icons.DataDocument.HeroDataDocument.Parse(heroDataJsonPath, gameStringDocument);
            matchAwardDataDocument = Heroes.Icons.DataDocument.MatchAwardDataDocument.Parse(matchAwardsJsonPath, gameStringDocument);

            heroesData.Clear();
            hotsHeroes.Clear();
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                foreach (string heroId in HeroIdList)
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    heroesData[heroId] = heroDataDocument.GetHeroById(heroId, true, true, true, true);

                    hotsHeroes[heroId] = new()
                    {
                        Name = heroesData[heroId].Name,
                        Health = Math.Ceiling(heroesData[heroId].Life.LifeMax * Math.Pow((1 + heroesData[heroId].Life.LifeScaling), 1)).ToString(),
                        Regen = Math.Round(heroesData[heroId].Life.LifeRegenerationRate * Math.Pow((1 + heroesData[heroId].Life.LifeRegenerationRateScaling), 1), 2).ToString()
                    };

                    HotsHeroUnit hero = new()
                    {
                        Id = heroesData[heroId].Id,
                        Name = heroesData[heroId].Name
                    };

                    int qAbilitiesCount = 0, wAbilitiesCount = 0, eAbilitiesCount = 0, rAbilitiesCount = 0, dAbilitiesCount = 0, zAbilitiesCount = 0, activeAbilitiesCount = 0;
                    foreach (Heroes.Models.AbilityTalents.Ability ability in heroesData[heroId].Abilities)
                    {
                        if (
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Q && qAbilitiesCount == 0 && heroId != "LostVikings") ||
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.W && wAbilitiesCount == 0 && heroId != "LostVikings") ||
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.E && eAbilitiesCount == 0 && heroId != "LostVikings") ||
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Heroic && rAbilitiesCount < 2) ||
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Trait && dAbilitiesCount == 0) ||
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Z && zAbilitiesCount == 0) ||
                            (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Active && activeAbilitiesCount < 3 && heroId == "LostVikings")
                        )
                        {
                            hero.Abilities[ability.AbilityTalentId.ReferenceId] = new()
                            {
                                HeroId = heroId,
                                AbilityId = ability.AbilityTalentId.ReferenceId,
                                IconFileName = ability.IconFileName ?? null,
                                Cooldown = ability.Tooltip.Cooldown.CooldownTooltip?.PlainText ?? null,
                                Energy = ability.Tooltip.Energy.EnergyTooltip?.ColoredText ?? null,
                                Full = ability.Tooltip.FullTooltip?.ColoredText ?? null,
                                Life = ability.Tooltip.Life.LifeCostTooltip?.ColoredText ?? null,
                                Name = ability.Name ?? null,
                                Short = ability.Tooltip.ShortTooltip?.ColoredText ?? null
                            };

                            hero.Abilities[ability.AbilityTalentId.ReferenceId].IconFileName = hero.Abilities[ability.AbilityTalentId.ReferenceId].IconFileName?.Replace("kel'thuzad", "kelthuzad");
                            hero.Abilities[ability.AbilityTalentId.ReferenceId].IconFileName = hero.Abilities[ability.AbilityTalentId.ReferenceId].IconFileName?.Replace("storm_ui_icon_tracer_blink_empty.png", "storm_ui_icon_tracer_blink.png");

                            switch (ability.AbilityTalentId.AbilityType)
                            {
                                case Heroes.Models.AbilityTalents.AbilityTypes.Q:
                                    qAbilitiesCount++;
                                    hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.Q;
                                    break;
                                case Heroes.Models.AbilityTalents.AbilityTypes.W:
                                    wAbilitiesCount++;
                                    hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.W;
                                    break;
                                case Heroes.Models.AbilityTalents.AbilityTypes.E:
                                    eAbilitiesCount++;
                                    hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.E;
                                    break;
                                case Heroes.Models.AbilityTalents.AbilityTypes.Heroic:
                                    rAbilitiesCount++;
                                    hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = rAbilitiesCount == 1 ? HotsAbilityType.R1 : HotsAbilityType.R2;
                                    break;
                                case Heroes.Models.AbilityTalents.AbilityTypes.Trait:
                                    dAbilitiesCount++;
                                    hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.D;
                                    break;
                                case Heroes.Models.AbilityTalents.AbilityTypes.Z:
                                    zAbilitiesCount++;
                                    hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.Z;
                                    break;
                                case Heroes.Models.AbilityTalents.AbilityTypes.Active:
                                    activeAbilitiesCount++;
                                    switch (activeAbilitiesCount)
                                    {
                                        case 1:
                                            hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.Q;
                                            break;
                                        case 2:
                                            hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.W;
                                            break;
                                        case 3:
                                            hero.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.E;
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    hotsHeroes[heroId].HeroUnits.Add(hero);

                    foreach (Heroes.Models.Hero heroUnitData in heroesData[heroId].HeroUnits)
                    {
                        if (heroId == "Chen" || heroId == "LostVikings" || heroId == "Rexxar") continue;

                        HotsHeroUnit heroUnit = new()
                        {
                            Id = heroUnitData.Id,
                            Name = heroUnitData.Name
                        };

                        qAbilitiesCount = wAbilitiesCount = eAbilitiesCount = rAbilitiesCount = dAbilitiesCount = zAbilitiesCount = 0;
                        foreach (Heroes.Models.AbilityTalents.Ability ability in heroUnitData.Abilities)
                        {
                            if (
                                (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Q && qAbilitiesCount == 0) ||
                                (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.W && wAbilitiesCount == 0) ||
                                (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.E && eAbilitiesCount == 0) ||
                                (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Heroic && rAbilitiesCount < 2) ||
                                (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Trait && dAbilitiesCount == 0) ||
                                (ability.AbilityTalentId.AbilityType == Heroes.Models.AbilityTalents.AbilityTypes.Z && zAbilitiesCount == 0)
                            )
                            {
                                heroUnit.Abilities[ability.AbilityTalentId.ReferenceId] = new()
                                {
                                    HeroId = heroId,
                                    AbilityId = ability.AbilityTalentId.ReferenceId,
                                    IconFileName = ability.IconFileName ?? null,
                                    Cooldown = ability.Tooltip.Cooldown.CooldownTooltip?.PlainText ?? null,
                                    Energy = ability.Tooltip.Energy.EnergyTooltip?.ColoredText ?? null,
                                    Full = ability.Tooltip.FullTooltip?.ColoredText ?? null,
                                    Life = ability.Tooltip.Life.LifeCostTooltip?.ColoredText ?? null,
                                    Name = ability.Name ?? null,
                                    Short = ability.Tooltip.ShortTooltip?.ColoredText ?? null
                                };
                                switch (ability.AbilityTalentId.AbilityType)
                                {
                                    case Heroes.Models.AbilityTalents.AbilityTypes.Q:
                                        qAbilitiesCount++;
                                        heroUnit.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.Q;
                                        break;
                                    case Heroes.Models.AbilityTalents.AbilityTypes.W:
                                        wAbilitiesCount++;
                                        heroUnit.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.W;
                                        break;
                                    case Heroes.Models.AbilityTalents.AbilityTypes.E:
                                        eAbilitiesCount++;
                                        heroUnit.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.E;
                                        break;
                                    case Heroes.Models.AbilityTalents.AbilityTypes.Heroic:
                                        rAbilitiesCount++;
                                        heroUnit.Abilities[ability.AbilityTalentId.ReferenceId].Type = rAbilitiesCount == 1 ? HotsAbilityType.R1 : HotsAbilityType.R2;
                                        break;
                                    case Heroes.Models.AbilityTalents.AbilityTypes.Trait:
                                        dAbilitiesCount++;
                                        heroUnit.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.D;
                                        break;
                                    case Heroes.Models.AbilityTalents.AbilityTypes.Z:
                                        zAbilitiesCount++;
                                        heroUnit.Abilities[ability.AbilityTalentId.ReferenceId].Type = HotsAbilityType.Z;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        hotsHeroes[heroId].HeroUnits.Add(heroUnit);
                    }
                }
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        }
        internal string GetHeroNameFromHeroId(string heroId)
        {
            return heroesData[heroId].Name ?? "";
        }
        internal string GetHeroHealthFromHeroUnitId(string heroId)
        {
            return Math.Ceiling(heroesData[heroId].Life.LifeMax * Math.Pow((1 + heroesData[heroId].Life.LifeScaling), 1)).ToString();
        }
        internal string GetHeroRegenFromHeroUnitId(string heroId)
        {
            return Math.Round(heroesData[heroId].Life.LifeRegenerationRate * Math.Pow((1 + heroesData[heroId].Life.LifeRegenerationRateScaling), 1), 2).ToString();
        }
        internal string GetMatchRewardsName(string matchAwardId)
        {
            Heroes.Models.MatchAward matchAward = matchAwardDataDocument!.GetMatchAwardById(matchAwardId);
            return matchAward?.Name ?? matchAwardId;
        }
        internal string GetMatchRewardsDescription(string matchAwardId)
        {
            Heroes.Models.MatchAward matchAward = matchAwardDataDocument!.GetMatchAwardById(matchAwardId);
            return matchAward?.Description?.PlainText ?? matchAwardId;
        }
        internal string GetMatchRewardsMvpScreenIcon(string matchAwardId)
        {
            Heroes.Models.MatchAward matchAward = matchAwardDataDocument!.GetMatchAwardById(matchAwardId);
            return matchAward?.MVPScreenImageFileName ?? matchAwardId;
        }
    }
    internal class HotsHero
    {
        public string? Name { get; set; }
        public string? Health { get; set; }
        public string? Regen { get; set; }
        public List<HotsHeroUnit> HeroUnits { get; set; } = [];
    }
    internal class HotsHeroUnit
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public Dictionary<string, HotsAbility> Abilities { get; set; } = [];
    }
    internal class HotsAbility
    {
        public string? HeroId { get; set; }
        public string? AbilityId { get; set; }
        public string? IconFileName { get; set; }
        public string? Cooldown { get; set; }
        public string? Energy { get; set; }
        public string? Full { get; set; }
        public string? Life { get; set; }
        public string? Name { get; set; }
        public string? Short { get; set; }
        public HotsAbilityType? Type { get; set; }
    }
    internal enum HotsAbilityType
    {
        Q,
        W,
        E,
        R1,
        R2,
        D,
        Z
    }
}
