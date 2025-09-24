using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    // gameStringsRoot object
    public class GameStringsRoot
    {
        [JsonPropertyName("meta")]
        public Meta? Meta { get; set; }

        [JsonPropertyName("gamestrings")]
        public Gamestrings? Gamestrings { get; set; }
    }

    public class Meta
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }
    }

    public class Gamestrings
    {
        [JsonPropertyName("abiltalent")]
        public AbilTalent? AbilTalent { get; set; }

        [JsonPropertyName("announcer")]
        public NameOnly? Announcer { get; set; }

        [JsonPropertyName("award")]
        public TwoFields? Award { get; set; }

        [JsonPropertyName("banner")]
        public ThreeFields? Banner { get; set; }

        [JsonPropertyName("boost")]
        public NameOnly? Boost { get; set; }

        [JsonPropertyName("bundle")]
        public NameOnly? Bundle { get; set; }

        [JsonPropertyName("emoticon")]
        public Emoticon? Emoticon { get; set; }

        [JsonPropertyName("emoticonpack")]
        public TwoFields? EmoticonPack { get; set; }

        [JsonPropertyName("heroskin")]
        public HeroSkin? HeroSkin { get; set; }

        [JsonPropertyName("lootchest")]
        public TwoFields? LootChest { get; set; }

        [JsonPropertyName("mount")]
        public Mount? Mount { get; set; }

        [JsonPropertyName("portrait")]
        public NameOnly? Portrait { get; set; }

        [JsonPropertyName("rewardportrait")]
        public RewardPortrait? RewardPortrait { get; set; }

        [JsonPropertyName("spray")]
        public Spray? Spray { get; set; }

        [JsonPropertyName("unit")]
        public Unit? Unit { get; set; }

        [JsonPropertyName("voiceline")]
        public NameOnly? VoiceLine { get; set; }

        // catches any unexpected new categories
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    // abiltalent has many dictionary sections (cooldown, energy, full, life, name, short)
    public class AbilTalent
    {
        [JsonPropertyName("cooldown")]
        public Dictionary<string, string>? Cooldown { get; set; }

        [JsonPropertyName("energy")]
        public Dictionary<string, string>? Energy { get; set; }

        [JsonPropertyName("full")]
        public Dictionary<string, string>? Full { get; set; }

        [JsonPropertyName("life")]
        public Dictionary<string, string>? Life { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("short")]
        public Dictionary<string, string>? Short { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class AbilTalentEntry
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
    }

    // Simple types reused
    public class NameOnly
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class TwoFields
    {
        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class ThreeFields
    {
        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("sortname")]
        public Dictionary<string, string>? SortName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class Emoticon
    {
        [JsonPropertyName("aliases")]
        public Dictionary<string, string>? Aliases { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("descriptionlocked")]
        public Dictionary<string, string>? DescriptionLocked { get; set; }

        [JsonPropertyName("expression")]
        public Dictionary<string, string>? Expression { get; set; }

        [JsonPropertyName("localizedaliases")]
        public Dictionary<string, string>? LocalizedAliases { get; set; }

        [JsonPropertyName("searchtext")]
        public Dictionary<string, string>? SearchText { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class HeroSkin
    {
        [JsonPropertyName("infotext")]
        public Dictionary<string, string>? InfoText { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("searchtext")]
        public Dictionary<string, string>? SearchText { get; set; }

        [JsonPropertyName("sortname")]
        public Dictionary<string, string>? SortName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class Mount
    {
        [JsonPropertyName("infotext")]
        public Dictionary<string, string>? InfoText { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("searchtext")]
        public Dictionary<string, string>? SearchText { get; set; }

        [JsonPropertyName("sortname")]
        public Dictionary<string, string>? SortName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class RewardPortrait
    {
        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("descriptionunearned")]
        public Dictionary<string, string>? DescriptionUnearned { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class Spray
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("searchtext")]
        public Dictionary<string, string>? SearchText { get; set; }

        [JsonPropertyName("sortname")]
        public Dictionary<string, string>? SortName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class Unit
    {
        [JsonPropertyName("damagetype")]
        public Dictionary<string, string>? DamageType { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("difficulty")]
        public Dictionary<string, string>? Difficulty { get; set; }

        [JsonPropertyName("energytype")]
        public Dictionary<string, string>? EnergyType { get; set; }

        [JsonPropertyName("expandedrole")]
        public Dictionary<string, string>? ExpandedRole { get; set; }

        [JsonPropertyName("infotext")]
        public Dictionary<string, string>? InfoText { get; set; }

        [JsonPropertyName("lifetype")]
        public Dictionary<string, string>? LifeType { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("role")]
        public Dictionary<string, string>? Role { get; set; }

        [JsonPropertyName("searchtext")]
        public Dictionary<string, string>? SearchText { get; set; }

        [JsonPropertyName("shieldtype")]
        public Dictionary<string, string>? ShieldType { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string>? Title { get; set; }

        [JsonPropertyName("type")]
        public Dictionary<string, string>? Type { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}
