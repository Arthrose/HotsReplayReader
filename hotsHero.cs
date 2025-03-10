/*
 * mkdir /src
 * cd /src
 * git clone https://github.com/HeroesToolChest/heroes-data.git
 * git clone https://github.com/HeroesToolChest/heroes-images.git
 * git clone https://github.com/tattersoftware/heroes-convert.git
 * /src/heroes-convert/src/data-convert /src/heroes-data/heroesdata/2.55.9.93640
 * 
 * var json = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\heroes\{hotsPlayers[id].PlayerHero.HeroName}.json");
 * hotsHero? hotsHero = JsonSerializer.Deserialize<hotsHero>(json);
*/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotsReplayReader
{
    public class hotsHeroAbility
    {
        public string? uid { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? hotkey { get; set; }
        public bool? trait { get; set; } = false;
        public string? abilityId { get; set; }
        public float? cooldown { get; set; }
        public string? icon { get; set; }
        public string? type { get; set; }

    }
    public class hotsHeroTalent
    {
        public string tooltipId { get; set; }
        public string talentTreeId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string type { get; set; }
        public int sort { get; set; }
        public string abilityId { get; set; }
        public List<string> abilityLinks { get; set; }
    }
    public class hotsHero
    {
        public int? id { get; set; }
        public string? shortName { get; set; }
        public string? hyperlinkId { get; set; }
        public string? attributeId { get; set; }
        public string? cHeroId { get; set; }
        public string? cUnitId { get; set; }
        public string? name { get; set; }
        public string? icon { get; set; }
        public string? role { get; set; }
        public string? expandedRole { get; set; }
        public string? type { get; set; }
        public string? releaseDate { get; set; }
        public string? releasePatch { get; set; }
        public List<string> tags { get; set; }
        public Dictionary<string, List<hotsHeroAbility>> abilities { get; set; }
        public Dictionary<string, List<hotsHeroTalent>> talents { get; set; }
    }
}
