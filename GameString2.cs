using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    public class GameStrings2Root
    {
        [JsonPropertyName("meta")]
        public GameStrings2Meta? Meta { get; set; }

        [JsonPropertyName("items")]
        public GameStrings2Items? Items { get; set; }
    }

    public class GameStrings2Meta
    {
        [JsonPropertyName("heroesVersion")]
        public string? HeroesVersion { get; set; }

        [JsonPropertyName("hdpVersion")]
        public string? HdpVersion { get; set; }

        [JsonPropertyName("itemsType")]
        public string? ItemsType { get; set; }

        [JsonPropertyName("dataTypes")]
        public List<string>? DataTypes { get; set; }

        [JsonPropertyName("gameStringText")]
        public GameStringTextMeta? GameStringText { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class GameStringTextMeta
    {
        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("textType")]
        public string? TextType { get; set; }

        [JsonPropertyName("constantVars")]
        public VarFlags? ConstantVars { get; set; }

        [JsonPropertyName("styleVars")]
        public VarFlags? StyleVars { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class VarFlags
    {
        [JsonPropertyName("replaced")]
        public bool Replaced { get; set; }

        [JsonPropertyName("preserved")]
        public bool Preserved { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    /// <summary>
    /// Représente le bloc "items" du fichier 97650.
    /// </summary>
    public class GameStrings2Items
    {
        [JsonPropertyName("ability")]
        public GameStrings2Ability? Ability { get; set; }

        [JsonPropertyName("passive")]
        public Dictionary<string, string>? Passive { get; set; }

        [JsonPropertyName("shortText")]
        public Dictionary<string, string>? ShortText { get; set; }

        [JsonPropertyName("fullText")]
        public Dictionary<string, string>? FullText { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("hero")]
        public GameStrings2Hero? Hero { get; set; }

        [JsonPropertyName("unit")]
        public Dictionary<string, JsonElement>? Unit { get; set; }

        [JsonPropertyName("talent")]
        public Dictionary<string, JsonElement>? Talent { get; set; }

        [JsonPropertyName("matchAward")]
        public GameStrings2MatchAward? MatchAward { get; set; }

        // autres catégories possibles
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    /// <summary>
    /// Représente la section "items.ability".
    /// </summary>
    public class GameStrings2Ability
    {
        [JsonPropertyName("cooldownText")]
        public Dictionary<string, string>? CooldownText { get; set; }

        [JsonPropertyName("energyText")]
        public Dictionary<string, string>? EnergyText { get; set; }

        [JsonPropertyName("fullText")]
        public Dictionary<string, string>? FullText { get; set; }

        [JsonPropertyName("lifeText")]
        public Dictionary<string, string>? LifeText { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("shortText")]
        public Dictionary<string, string>? ShortText { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    /// <summary>
    /// Représente la section "items.hero".
    /// Contient les sous-catégories de texte pour chaque héros.
    /// </summary>
    public class GameStrings2Hero
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string>? Name { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string>? Description { get; set; }

        [JsonPropertyName("shortText")]
        public Dictionary<string, string>? ShortText { get; set; }

        [JsonPropertyName("fullText")]
        public Dictionary<string, string>? FullText { get; set; }

        [JsonPropertyName("role")]
        public Dictionary<string, string>? Role { get; set; }

        [JsonPropertyName("difficulty")]
        public Dictionary<string, string>? Difficulty { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string>? Title { get; set; }

        [JsonPropertyName("infotext")]
        public Dictionary<string, string>? InfoText { get; set; }

        [JsonPropertyName("searchtext")]
        public Dictionary<string, string>? SearchText { get; set; }

        [JsonPropertyName("sortname")]
        public Dictionary<string, string>? SortName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    /// <summary>
    /// Représente la section "items.matchAward".
    /// </summary>
    public class GameStrings2MatchAward
    {
        [JsonPropertyName("endOfMatchDescription")]
        public Dictionary<string, string>? EndOfMatchDescription { get; set; }

        [JsonPropertyName("endOfMatchName")]
        public Dictionary<string, string>? EndOfMatchName { get; set; }

        [JsonPropertyName("endOfMatchTooltipText")]
        public Dictionary<string, string>? EndOfMatchTooltipText { get; set; }

        [JsonPropertyName("scoreScreenDescription")]
        public Dictionary<string, string>? ScoreScreenDescription { get; set; }

        [JsonPropertyName("scoreScreenName")]
        public Dictionary<string, string>? ScoreScreenName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}