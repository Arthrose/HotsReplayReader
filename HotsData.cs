using System.Globalization;
using System.Text.Json;

using Heroes.Element;
using Heroes.Element.Models;
using Heroes.Icons;
using Heroes.Models;

namespace HotsReplayReader
{
    internal class HotsData
    {
        private readonly Dictionary<string, Heroes.Models.Hero> heroesIconsData = [];
        private readonly Dictionary<string, Heroes.Element.Models.Hero> heroesElementData = [];
        private readonly Dictionary<string, HotsHero> hotsHeroes = [];
        private readonly Dictionary<string, HotsMatchAward> hotsMatchAwards = [];
        public static Dictionary<string, string> HeroNameFromHeroUnitId { get; private set; } = [];
        public static Dictionary<string, string> HeroIdFromHeroUnitId { get; private set; } = [];
        public static Dictionary<string, HotsRole?> HeroRoleFromHeroId { get; private set; } = [];
        private static readonly Dictionary<string, HotsRole> RoleTranslations = new() {
            // Tank
            ["Tank"] = HotsRole.Tank,
            ["Tanque"] = HotsRole.Tank,
            ["Guerrier défensif"] = HotsRole.Tank,
            ["Difensore"] = HotsRole.Tank,
            ["전사"] = HotsRole.Tank,
            ["Танк"] = HotsRole.Tank,
            ["坦克"] = HotsRole.Tank,
            ["肉盾"] = HotsRole.Tank,

            // Bruiser
            ["Bruiser"] = HotsRole.Bruiser,
            ["Frontbrecher"] = HotsRole.Bruiser,
            ["Agresor"] = HotsRole.Bruiser,
            ["Guerrero"] = HotsRole.Bruiser,
            ["Guerrier offensif"] = HotsRole.Bruiser,
            ["Combattente"] = HotsRole.Bruiser,
            ["투사"] = HotsRole.Bruiser,
            ["Brutal"] = HotsRole.Bruiser,
            ["Guerreiro"] = HotsRole.Bruiser,
            ["Рубака"] = HotsRole.Bruiser,
            ["斗士"] = HotsRole.Bruiser,
            ["鬥士"] = HotsRole.Bruiser,

            // Melee
            ["Melee Assassin"] = HotsRole.Melee,
            ["Nahkampfassassine"] = HotsRole.Melee,
            ["Asesino cuerpo a cuerpo"] = HotsRole.Melee,
            ["Asesino de melé"] = HotsRole.Melee,
            ["Assassin en mêlée"] = HotsRole.Melee,
            ["Assassino (Mischia)"] = HotsRole.Melee,
            ["근접 암살자"] = HotsRole.Melee,
            ["Zabójca w Zwarciu"] = HotsRole.Melee,
            ["Assassino Corpo a Corpo"] = HotsRole.Melee,
            ["Убийца ближнего боя"] = HotsRole.Melee,
            ["近战刺客"] = HotsRole.Melee,
            ["近戰刺客"] = HotsRole.Melee,

            // Ranged
            ["Ranged Assassin"] = HotsRole.Ranged,
            ["Fernkampfassassine"] = HotsRole.Ranged,
            ["Asesino a distancia"] = HotsRole.Ranged,
            ["Asesino de largo alcance"] = HotsRole.Ranged,
            ["Assassin à distance"] = HotsRole.Ranged,
            ["Assassino (Distanza)"] = HotsRole.Ranged,
            ["원거리 암살자"] = HotsRole.Ranged,
            ["Dystansowy Zabójca"] = HotsRole.Ranged,
            ["Assassino de Longo Alcance"] = HotsRole.Ranged,
            ["Убийца дальнего боя"] = HotsRole.Ranged,
            ["远程刺客"] = HotsRole.Ranged,
            ["遠程刺客"] = HotsRole.Ranged,

            // Healer
            ["Healer"] = HotsRole.Healer,
            ["Heiler"] = HotsRole.Healer,
            ["Sanador"] = HotsRole.Healer,
            ["Soigneur"] = HotsRole.Healer,
            ["Guaritore"] = HotsRole.Healer,
            ["치유사"] = HotsRole.Healer,
            ["Uzdrowiciel"] = HotsRole.Healer,
            ["Curandeiro"] = HotsRole.Healer,
            ["Лекарь"] = HotsRole.Healer,
            ["治疗者"] = HotsRole.Healer,
            ["治療者"] = HotsRole.Healer,

            // Support
            ["Support"] = HotsRole.Support,
            ["Unterstützer"] = HotsRole.Support,
            ["Apoyo"] = HotsRole.Support,
            ["Soutien"] = HotsRole.Support,
            ["Supporto"] = HotsRole.Support,
            ["지원가"] = HotsRole.Support,
            ["Pomocnik"] = HotsRole.Support,
            ["Suporte"] = HotsRole.Support,
            ["Поддержка"] = HotsRole.Support,
            ["支援者"] = HotsRole.Support,
            ["輔助"] = HotsRole.Support,
        };
        internal Version versionThreshold = new("2.55.16.97039");
        internal void Parse(string heroDataJsonPath, string gameStringsJsonPath, string matchAwardsJsonPath, Version dbVersion, List<string> heroList, List<string> matchAwardsList)
        {
            heroesIconsData.Clear();
            heroesElementData.Clear();
            hotsHeroes.Clear();
            HeroNameFromHeroUnitId = [];
            HeroIdFromHeroUnitId = [];
            HeroRoleFromHeroId = [];

            if (dbVersion < versionThreshold)
                ParseHeroesIcons(heroDataJsonPath, gameStringsJsonPath, matchAwardsJsonPath, heroList, matchAwardsList);
            else
                ParseHeroesElement(heroDataJsonPath, gameStringsJsonPath, matchAwardsJsonPath, heroList, matchAwardsList);
        }
        internal void ParseHeroesIcons(string heroDataJsonPath, string gameStringsJsonPath, string matchAwardsJsonPath, List<string> heroList, List<string> matchAwardsList)
        {
            GetHeroesIconsHeroUnitIdsFromHeroUnits(heroDataJsonPath);

            Heroes.Icons.GameStringDocument gameStringDocument = Heroes.Icons.GameStringDocument.Parse(gameStringsJsonPath);
            Heroes.Icons.DataDocument.HeroDataDocument heroDataDocument = Heroes.Icons.DataDocument.HeroDataDocument.Parse(heroDataJsonPath, gameStringDocument);
            Heroes.Icons.DataDocument.MatchAwardDataDocument matchAwardHeroesIconsDataDocument = Heroes.Icons.DataDocument.MatchAwardDataDocument.Parse(matchAwardsJsonPath, gameStringDocument);

            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                foreach (string matchAwardId in matchAwardsList)
                {
                    Heroes.Models.MatchAward matchAward = matchAwardHeroesIconsDataDocument!.GetMatchAwardById(matchAwardId);
                    hotsMatchAwards[matchAwardId] = new()
                    {
                        Name = matchAward.Name,
                        Description = matchAward.Description?.PlainText,
                        MVPScreenImageFileName = matchAward.MVPScreenImageFileName
                    };
                }

                foreach (string heroUnitId in heroList)
                {
                    string heroId = HeroIdFromHeroUnitId[heroUnitId];

                    Heroes.Models.Hero tmpHero = heroDataDocument.GetHeroById(heroId, true, true, true, true);

                    if (heroesElementData.ContainsKey(tmpHero.Id)) continue;

                    heroesIconsData[heroId] = tmpHero;

                    HotsRole? role = heroesIconsData[heroId].ExpandedRole is { } label && RoleTranslations.TryGetValue(label, out var found) ? found : null;

                    double aaDmg = 0.0, aaSpeed = 0.0, aaDps = 0.0, aaRange = 0.0;
                    if (heroesIconsData[heroId].Weapons != null && heroesIconsData[heroId].Weapons.Count > 0)
                    {
                        Heroes.Models.UnitWeapon? firstWeapon = heroesIconsData[heroId].Weapons.First();
                        aaDmg = Math.Round(firstWeapon.Damage * Math.Pow((1 + firstWeapon.DamageScaling), 1), 1);
                        aaSpeed = Math.Round(firstWeapon.AttacksPerSecond, 2);
                        aaDps = Math.Round(aaDmg * firstWeapon.AttacksPerSecond, 1);
                        aaRange = firstWeapon.Range;
                    }

                    hotsHeroes[heroId] = new()
                    {
                        Health = Math.Ceiling(heroesIconsData[heroId].Life.LifeMax * Math.Pow((1 + heroesIconsData[heroId].Life.LifeScaling), 1)).ToString(),
                        Regen = Math.Round(heroesIconsData[heroId].Life.LifeRegenerationRate * Math.Pow((1 + heroesIconsData[heroId].Life.LifeRegenerationRateScaling), 1), 2).ToString(),

                        AaDmg = aaDmg.ToString(),
                        AaSpeed = aaSpeed.ToString(),
                        AaDps = aaDps.ToString(),
                        AaRange = aaRange.ToString(),

                        HeroId = heroesIconsData[heroId].Id,
                        HeroName = heroesIconsData[heroId].Name,
                        HeroRole = role
                    };

                    HotsHeroUnit hero = new()
                    {
                        Id = heroesIconsData[heroId].Id,
                        Name = heroesIconsData[heroId].Name
                    };

                    ParseHeroesIconsTalents(heroId);
                    ParseHeroesIconsAbilities(heroId, hero);
                }
                foreach (HotsHero hotsHero in hotsHeroes.Values)
                {
                    if (hotsHero.HeroUnits == null) continue;
                    foreach (HotsHeroUnit unit in hotsHero.HeroUnits)
                    {
                        if (string.IsNullOrEmpty(unit.Id)) continue;
                        HeroNameFromHeroUnitId[unit.Id] = hotsHero.HeroName ?? "";
                    }
                    if (!string.IsNullOrEmpty(hotsHero.HeroName) && !string.IsNullOrEmpty(hotsHero.HeroId))
                        HeroRoleFromHeroId[hotsHero.HeroId] = hotsHero.HeroRole;
                }
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        }
        internal void ParseHeroesIconsTalents(string heroId)
        {
            foreach (Heroes.Models.AbilityTalents.Talent talent in heroesIconsData[heroId].Talents)
            {

                _ = int.TryParse(talent.Tier.ToString().Replace("Level", ""), out int talenLevel);
                hotsHeroes[heroId].Talents.Add
                (
                    new HotsTalent
                    {
                        ReferenceId = talent.AbilityTalentId.ReferenceId ?? null,
                        Level = talenLevel,
                        IconFileName = talent.IconFileName ?? null,
                        Cooldown = talent.Tooltip.Cooldown.CooldownTooltip?.PlainText ?? null,
                        Energy = talent.Tooltip.Energy.EnergyTooltip?.ColoredText ?? null,
                        Full = talent.Tooltip.FullTooltip?.ColoredText ?? null,
                        Life = talent.Tooltip.Life.LifeCostTooltip?.ColoredText ?? null,
                        Name = talent.Name ?? null,
                        Short = talent.Tooltip.ShortTooltip?.ColoredText ?? null
                    }
                );
            }
        }
        internal void ParseHeroesIconsAbilities(string heroId, HotsHeroUnit hero)
        {
            int qAbilitiesCount = 0, wAbilitiesCount = 0, eAbilitiesCount = 0, rAbilitiesCount = 0, dAbilitiesCount = 0, zAbilitiesCount = 0, activeAbilitiesCount = 0;
            foreach (Heroes.Models.AbilityTalents.Ability ability in heroesIconsData[heroId].Abilities)
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

            foreach (Heroes.Models.Hero heroUnitData in heroesIconsData[heroId].HeroUnits)
            {
                if (heroId == "Chen" || heroId == "LostVikings" || heroId == "Rexxar" || heroId == "Medivh") continue;

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
        internal void ParseHeroesElement(string heroDataJsonPath, string gameStringsJsonPath, string matchAwardsJsonPath, List<string> heroList, List<string> matchAwardsList)
        {
            GetHeroesElementHeroUnitIdsFromHeroUnits(heroDataJsonPath);

            Heroes.Element.GameStringsDocument gameStringsDocument = Heroes.Element.GameStringsDocument.Load(JsonDocument.Parse(File.OpenRead(gameStringsJsonPath)));
            Heroes.Element.HeroDataDocument heroDataDocument = Heroes.Element.HeroDataDocument.Load(JsonDocument.Parse(File.OpenRead(heroDataJsonPath)), gameStringsDocument);
            Heroes.Element.MatchAwardDataDocument matchAwardHeroesElementDataDocument = Heroes.Element.MatchAwardDataDocument.Load(JsonDocument.Parse(File.OpenRead(matchAwardsJsonPath)), gameStringsDocument);

            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                foreach (string matchAwardId in matchAwardsList)
                {
                    Heroes.Element.Models.MatchAward matchAward = matchAwardHeroesElementDataDocument!.GetElementById(matchAwardId);
                    hotsMatchAwards[matchAwardId] = new()
                    {
                        Name = matchAward.ScoreScreenName?.PlainText.Split(',', 2)[0],
                        Description = matchAward.ScoreScreenDescription?.PlainText,
                        MVPScreenImageFileName = matchAward.MVPScreenImage
                    };
                }

                foreach (string heroUnitId in heroList)
                {
                    string heroId = HeroIdFromHeroUnitId[heroUnitId];

                    Heroes.Element.Models.Hero tmpHero = heroDataDocument.GetElementById(heroId);

                    if (heroesElementData.ContainsKey(tmpHero.Id)) continue;

                    heroesElementData[tmpHero.Id] = tmpHero;

                    HotsRole? role = heroesElementData[heroId].ExpandedRole?.PlainText is { } label && RoleTranslations.TryGetValue(label, out var found) ? found : null;

                    double aaDmg = 0.0, aaSpeed = 0.0, aaDps = 0.0, aaRange = 0.0;
                    if (heroesElementData[heroId].Weapons != null && heroesElementData[heroId].Weapons.Count > 0)
                    {
                        aaDmg = Math.Round(heroesElementData[heroId].Weapons[0].Damage * Math.Pow((1 + heroesElementData[heroId].Weapons[0].DamageScaling), 1), 1);
                        aaSpeed = Math.Round(heroesElementData[heroId].Weapons[0].AttacksPerSecond, 2);
                        aaDps = Math.Round(aaDmg * heroesElementData[heroId].Weapons[0].AttacksPerSecond, 1);
                        aaRange = heroesElementData[heroId].Weapons[0].Range;
                    }

                    hotsHeroes[heroId] = new()
                    {
                        Health = Math.Ceiling(heroesElementData[heroId].Life.LifeMax * Math.Pow((1 + heroesElementData[heroId].Life.LifeMaxScaling), 1)).ToString(),
                        Regen = Math.Round(heroesElementData[heroId].Life.LifeRegenerationRate * Math.Pow((1 + heroesElementData[heroId].Life.LifeRegenerationRateScaling), 1), 2).ToString(),

                        AaDmg = aaDmg.ToString(),
                        AaSpeed = aaSpeed.ToString(),
                        AaDps = aaDps.ToString(),
                        AaRange = aaRange.ToString(),

                        HeroId = heroesElementData[heroId].Id,
                        HeroName = heroesElementData[heroId].Name?.PlainText,
                        HeroRole = role
                    };

                    HotsHeroUnit hero = new()
                    {
                        Id = heroesElementData[heroId].UnitId,
                        Name = heroesElementData[heroId].Name!.PlainText
                    };

                    ParseHeroesElementAbilities(heroId, hero);
                    ParseHeroesElementTalents(heroId);
                }
                foreach (HotsHero hotsHero in hotsHeroes.Values)
                {
                    if (hotsHero.HeroUnits == null) continue;
                    foreach (HotsHeroUnit unit in hotsHero.HeroUnits)
                    {
                        if (string.IsNullOrEmpty(unit.Id)) continue;
                        HeroNameFromHeroUnitId[unit.Id] = hotsHero.HeroName ?? "";
                    }
                    if (!string.IsNullOrEmpty(hotsHero.HeroName) && !string.IsNullOrEmpty(hotsHero.HeroId))
                        HeroRoleFromHeroId[hotsHero.HeroId] = hotsHero.HeroRole;
                }
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        }
        internal void ParseHeroesElementTalents(string heroId)
        {
            foreach (KeyValuePair<Heroes.Element.Models.Types.TalentTier, IList<Heroes.Element.Models.AbilityTalents.Talent>> talentTier in heroesElementData[heroId].Talents)
            {
                _ = int.TryParse(talentTier.Key.ToString().Replace("Level", ""), out int talentLevel);
                foreach (Heroes.Element.Models.AbilityTalents.Talent talent in talentTier.Value)
                {
                    // Si c'est un talent Heroic, cherche l'ability associée pour récupérer energy et cooldown
                    string? energy = null, cooldown = null, life = null;
                    if (talent.AbilityType == Heroes.Element.Models.Types.AbilityType.Heroic)
                    {
                        HotsAbility? heroicAbility = new[] { HotsAbilityType.R1, HotsAbilityType.R2 }
                            .Select(type => GetAbilitiesFromHeroIdAndAbilityType(heroId, type)?.FirstOrDefault())
                            .FirstOrDefault(a => a?.AbilityId == talent.AbilityElementId);

                        if (heroicAbility != null)
                        {
                            energy = heroicAbility.Energy;
                            cooldown = heroicAbility.Cooldown;
                            life = heroicAbility.Life;
                        }
                    }
                    else
                    {
                        energy = talent.EnergyText?.PlainText ?? null;
                        cooldown = talent.CooldownText?.PlainText ?? null;
                        life = talent.LifeText?.PlainText ?? null;
                    }

                    hotsHeroes[heroId].Talents.Add
                    (
                        new HotsTalent
                        {
                            ReferenceId = talent.TalentElementId ?? null,
                            Level = talentLevel,
                            IconFileName = talent.Icon ?? null,
                            Energy = energy,
                            Cooldown = cooldown,
                            Full = talent.FullText?.ColoredText ?? null,
                            Life = life,
                            Name = talent.Name?.PlainText ?? null,
                            Short = talent.ShortText?.ColoredText ?? null
                        }
                    );
                }
            }
        }
        internal void ParseHeroesElementAbilities(string heroId, HotsHeroUnit hero)
        {
            if (!heroesElementData.TryGetValue(heroId, out Heroes.Element.Models.Hero? heroTmp)) return;
            Heroes.Element.Models.AbilityTalents.Ability? ability;

            if (heroTmp.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Basic, out IList<Heroes.Element.Models.AbilityTalents.Ability>? basicAbilities))
            {
                ability = basicAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.Q);
                if (ability is not null)
                    hero.Abilities[ability.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, ability, HotsAbilityType.Q);

                ability = basicAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.W);
                if (ability is not null)
                    hero.Abilities[ability.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, ability, HotsAbilityType.W);

                ability = basicAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.E);
                if (ability is not null)
                    hero.Abilities[ability.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, ability, HotsAbilityType.E);
            }
            if (heroTmp.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Heroic, out IList<Heroes.Element.Models.AbilityTalents.Ability>? heroicAbilities))
            {
                for (int i = 0; i < heroicAbilities.Count && i < 2; i++)
                {
                    Heroes.Element.Models.AbilityTalents.Ability heroicAbility = heroicAbilities[i];
                    hero.Abilities[heroicAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, heroicAbility, i == 0 ? HotsAbilityType.R1 : HotsAbilityType.R2);
                }
            }
            if (heroTmp.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Trait, out IList<Heroes.Element.Models.AbilityTalents.Ability>? traitAbilities)) {
                ability = traitAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.Trait);
                if (ability is not null)
                    hero.Abilities[ability.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, ability, HotsAbilityType.D);
            }
            if (heroTmp.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Mount, out IList<Heroes.Element.Models.AbilityTalents.Ability>? mountAbilities)) {
                ability = mountAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.Z);
                if (ability is not null)
                    hero.Abilities[ability.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, ability, HotsAbilityType.Z);
            }
            if (heroId == "LostVikings")
            {
                if (heroTmp.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Activable, out IList<Heroes.Element.Models.AbilityTalents.Ability>? activableAbilities))
                {
                    for (int i = 0; i < activableAbilities.Count && i < 3; i++)
                    {
                        Heroes.Element.Models.AbilityTalents.Ability activableAbility = activableAbilities[i];
                        HotsAbilityType type = i switch { 0 => HotsAbilityType.Q, 1 => HotsAbilityType.W, 2 => HotsAbilityType.E, _ => throw new InvalidOperationException() };
                        hero.Abilities[activableAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, activableAbility, type);
                    }
                }
            }
            hotsHeroes[heroId].HeroUnits.Add(hero);

            if (heroesElementData[heroId].HeroUnits.Count > 0)
            {
                foreach (Heroes.Element.Models.Unit heroUnitData in heroesElementData[heroId].HeroUnits.Values)
                {
                    HotsHeroUnit heroUnit = new()
                    {
                        Id = heroUnitData.Id,
                        Name = heroUnitData.Name!.PlainText
                    };

                    if (heroId != "Chen" && heroId != "LostVikings" && heroId != "Rexxar" && heroId != "Medivh")
                    {
                        Heroes.Element.Models.AbilityTalents.Ability? unitAbility;

                        if (heroUnitData.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Basic, out IList<Heroes.Element.Models.AbilityTalents.Ability>? unitBasicAbilities))
                        {
                            unitAbility = unitBasicAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.Q);
                            if (unitAbility is not null)
                                heroUnit.Abilities[unitAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, unitAbility, HotsAbilityType.Q);

                            unitAbility = unitBasicAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.W);
                            if (unitAbility is not null)
                                heroUnit.Abilities[unitAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, unitAbility, HotsAbilityType.W);

                            unitAbility = unitBasicAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.E);
                            if (unitAbility is not null)
                                heroUnit.Abilities[unitAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, unitAbility, HotsAbilityType.E);
                        }
                        if (heroUnitData.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Heroic, out IList<Heroes.Element.Models.AbilityTalents.Ability>? unitHeroicAbilities))
                        {
                            for (int i = 0; i < unitHeroicAbilities.Count && i < 2; i++)
                            {
                                Heroes.Element.Models.AbilityTalents.Ability heroicAbility = unitHeroicAbilities[i];
                                heroUnit.Abilities[heroicAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, heroicAbility, i == 0 ? HotsAbilityType.R1 : HotsAbilityType.R2);
                            }
                        }
                        if (heroUnitData.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Trait, out IList<Heroes.Element.Models.AbilityTalents.Ability>? unitTraitAbilities))
                        {
                            unitAbility = unitTraitAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.Trait);
                            if (unitAbility is not null)
                                heroUnit.Abilities[unitAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, unitAbility, HotsAbilityType.D);
                        }
                        if (heroUnitData.Abilities.TryGetValue(Heroes.Element.Models.Types.AbilityTier.Mount, out IList<Heroes.Element.Models.AbilityTalents.Ability>? unitMountAbilities))
                        {
                            unitAbility = unitMountAbilities.FirstOrDefault(a => a.AbilityType == Heroes.Element.Models.Types.AbilityType.Z);
                            if (unitAbility is not null)
                                heroUnit.Abilities[unitAbility.ButtonElementId] = HeroesElementCreateHotsAbility(heroId, unitAbility, HotsAbilityType.Z);
                        }
                    }
                    hotsHeroes[heroId].HeroUnits.Add(heroUnit);
                }
            }
        }
        private static HotsAbility HeroesElementCreateHotsAbility(string heroId, Heroes.Element.Models.AbilityTalents.Ability ability, HotsAbilityType type)
        {
            HotsAbility hotsAbility = new()
            {
                HeroId = heroId,
                AbilityId = ability.ButtonElementId,
                IconFileName = ability.Icon,
                Cooldown = ability.CooldownText?.PlainText,
                Energy = ability.EnergyText?.PlainText,
                Full = ability.FullText?.ColoredText,
                Life = ability.LifeText?.ColoredText,
                Name = ability.Name?.PlainText,
                Short = ability.ShortText?.ColoredText,
                Type = type
            };

            hotsAbility.IconFileName = hotsAbility.IconFileName?.Replace("kel'thuzad", "kelthuzad");
            hotsAbility.IconFileName = hotsAbility.IconFileName?.Replace("storm_ui_icon_tracer_blink_empty.png", "storm_ui_icon_tracer_blink.png");

            return hotsAbility;
        }
        private static void GetHeroesIconsHeroUnitIdsFromHeroUnits(string jsonPath)
        {
            using FileStream? fileStream = File.OpenRead(jsonPath);
            using JsonDocument? json = JsonDocument.Parse(fileStream);

            JsonElement items = json.RootElement;

            foreach (JsonProperty hero in items.EnumerateObject())
            {
                string? heroName = hero.Name;
                JsonElement heroObj = hero.Value;

                // Adds unitId
                if (heroObj.TryGetProperty("unitId", out JsonElement unitIdProp) && unitIdProp.GetString() is string unitId)
                    HeroIdFromHeroUnitId[unitId] = heroName;

                // Adds heroUnits (tableau d'objets à une seule clé)
                if (heroObj.TryGetProperty("heroUnits", out JsonElement heroUnits))
                    foreach (JsonElement heroUnitWrapper in heroUnits.EnumerateArray())
                        foreach (JsonProperty heroUnit in heroUnitWrapper.EnumerateObject())
                            HeroIdFromHeroUnitId[heroUnit.Name] = heroName;
            }
        }
        private static void GetHeroesElementHeroUnitIdsFromHeroUnits(string jsonPath)
        {
            using FileStream? fileStream = File.OpenRead(jsonPath);
            using JsonDocument? json = JsonDocument.Parse(fileStream);

            JsonElement items = json.RootElement.GetProperty("items");

            foreach (JsonProperty hero in items.EnumerateObject())
            {
                string? heroName = hero.Name;
                JsonElement heroObj = hero.Value;

                // Adds unitId
                if (heroObj.TryGetProperty("unitId", out JsonElement unitIdProp) && unitIdProp.GetString() is string unitId)
                    HeroIdFromHeroUnitId[unitId] = heroName;

                // Adds heroUnits
                if (heroObj.TryGetProperty("heroUnits", out JsonElement heroUnits))
                    foreach (JsonProperty heroUnit in heroUnits.EnumerateObject())
                        HeroIdFromHeroUnitId[heroUnit.Name] = heroName;
            }
        }
        internal string GetHeroNameFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].HeroName ?? "";
        }
        internal string GetHeroHealthFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].Health ?? "";
        }
        internal string GetHeroRegenFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].Regen ?? "";
        }
        internal string GetHeroAaDmgFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].AaDmg ?? "";
        }
        internal string GetHeroAaSpeedFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].AaSpeed ?? "";
        }
        internal string GetHeroAaDpsFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].AaDps ?? "";
        }
        internal string GetHeroAaRangeFromHeroId(string heroId)
        {
            return hotsHeroes[heroId].AaRange ?? "";
        }
        internal HotsTalent? GetTalentsFromHeroIdAndTalentReferenceId(string heroId, string referenceId)
        {
            if (!hotsHeroes.TryGetValue(heroId, out HotsHero? hero)) return null;
            foreach (HotsTalent talent in hero.Talents)
                if (talent.ReferenceId == referenceId) return talent;
            return null;
        }
        internal int GetTalentMaxCountFromHeroId(string heroId)
        {
            if (!hotsHeroes.TryGetValue(heroId, out HotsHero? hero)) return 0;
            return hero.Talents
                .GroupBy(talent => talent.Level)
                .Select(group => group.Count())
                .DefaultIfEmpty(0)
                .Max();
        }
        internal List<HotsTalent> GetTalentsFromHeroIdAndLevel(string heroId, int talentLevel)
        {
            if (!hotsHeroes.TryGetValue(heroId, out HotsHero? hero)) return [];
            return [.. hero.Talents.Where(talent => talent.Level == talentLevel)];
        }
        internal List<HotsAbility?>? GetAbilitiesFromHeroIdAndAbilityType(string heroId, HotsAbilityType hotsAbilityType)
        {
            if (!hotsHeroes.TryGetValue(heroId, out var hero)) return null;

            List<HotsAbility?> abilities = [];

            foreach (HotsHeroUnit heroUnit in hero.HeroUnits)
            {
                bool abilityFound = false;
                foreach (HotsAbility ability in heroUnit.Abilities.Values)
                {
                    if (ability.Type == hotsAbilityType)
                    {
                        abilities.Add(ability);
                        abilityFound = true;
                    }
                }
                if (!abilityFound)
                {
                    abilities.Add(null);
                }
            }
            return abilities.Count > 0 ? abilities : null;
        }
        internal string GetMatchRewardsName(string matchAwardId)
        {
            return hotsMatchAwards[matchAwardId].Name ?? "";
        }
        internal string GetMatchRewardsDescription(string matchAwardId)
        {
            return hotsMatchAwards[matchAwardId].Description ?? "";
        }
        internal string GetMatchRewardsMvpScreenIcon(string matchAwardId)
        {
            return hotsMatchAwards[matchAwardId].MVPScreenImageFileName ?? "";
        }
    }
    internal class HotsHero
    {
        public string? Health { get; set; }
        public string? Regen { get; set; }
        public string? AaDmg { get; set; }
        public string? AaSpeed { get; set; }
        public string? AaDps { get; set; }
        public string? AaRange { get; set; }
        public string? HeroId { get; set; }
        public string? HeroName { get; set; }
        public HotsRole? HeroRole { get; set; }
        public List<HotsHeroUnit> HeroUnits { get; set; } = [];
        public List<HotsTalent> Talents { get; set; } = [];
    }
    internal class HotsHeroUnit
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public Dictionary<string, HotsAbility> Abilities { get; set; } = [];
    }
    internal class HotsTalent()
    {
        public string? ReferenceId { get; set; }
        public int? Level { get; set; }
        public string? IconFileName { get; set; }
        public string? Cooldown { get; set; }
        public string? Energy { get; set; }
        public string? Full { get; set; }
        public string? Life { get; set; }
        public string? Name { get; set; }
        public string? Short { get; set; }
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
        Q, W, E, R1, R2, D, Z
    }
    internal enum HotsRole
    {
        Tank, Bruiser, Melee, Ranged, Healer, Support
    }
    internal class HotsMatchAward
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MVPScreenImageFileName { get; set; }
    }
}