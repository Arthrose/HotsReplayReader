using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    public class PsionicStormUnitsData
    {
        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("units")]
        public List<PsionicStormUnit> PsionicStormUnits { get; set; }
    }

    public class PsionicStormUnit
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("universe")]
        public string Universe { get; set; }

        [JsonPropertyName("price_gems")]
        public int PriceGems { get; set; }

        [JsonPropertyName("price_gold")]
        public int PriceGold { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("sub_slug")]
        public string SubSlug { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("min_level")]
        public int MinLevel { get; set; }

        [JsonPropertyName("hp_base")]
        public double HpBase { get; set; }

        [JsonPropertyName("hp_scaling")]
        public double HpScaling { get; set; }

        [JsonPropertyName("hp_regen_base")]
        public double HpRegenBase { get; set; }

        [JsonPropertyName("hp_regen_scaling")]
        public double HpRegenScaling { get; set; }

        [JsonPropertyName("mana_base")]
        public double ManaBase { get; set; }

        [JsonPropertyName("mana_regen_base")]
        public double ManaRegenBase { get; set; }

        [JsonPropertyName("aa_dmg_base")]
        public double AaDmgBase { get; set; }

        [JsonPropertyName("aa_dmg_scaling")]
        public double AaDmgScaling { get; set; }

        [JsonPropertyName("aa_speed")]
        public double AaSpeed { get; set; }

        [JsonPropertyName("aa_range")]
        public double AaRange { get; set; }

        [JsonPropertyName("armor_pts")]
        public int ArmorPts { get; set; }

        [JsonPropertyName("armor_type")]
        public string ArmorType { get; set; }

        [JsonPropertyName("unit_radius")]
        public double UnitRadius { get; set; }
    }
}
