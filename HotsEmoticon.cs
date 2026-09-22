namespace HotsReplayReader
{
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;

    public sealed class HeroesElementEmoticonEntry
    {
        [JsonPropertyName("universalAliases")]
        public List<string> UniversalAliases { get; set; } = new();

        [JsonPropertyName("isCaseSensitive")]
        public bool IsCaseSensitive { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }
    public sealed class HeroesElementEmoticonDataFile
    {
        [JsonPropertyName("items")]
        public Dictionary<string, HeroesElementEmoticonEntry> Items { get; set; } = new();
    }
    public sealed class HeroesElementLocalizedEmoticonSection
    {
        [JsonPropertyName("localizedAliases")]
        public Dictionary<string, List<string>> LocalizedAliases { get; set; } = new();
    }
    public sealed class HeroesElementLocalizedEmoticonFile
    {
        [JsonPropertyName("items")]
        public Dictionary<string, HeroesElementLocalizedEmoticonSection> Items { get; set; } = new();
    }

    public sealed class HeroesIconsLegacyEmoticonEntry
    {
        [JsonPropertyName("heroId")]
        public string? HeroId { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }
    public sealed class HeroesIconsEmoticonDataFile : Dictionary<string, HeroesIconsLegacyEmoticonEntry>
    {
    }
    public sealed class HeroesIconsEmoticonSection
    {
        [JsonPropertyName("aliases")]
        public Dictionary<string, string> Aliases { get; set; } = new();

        [JsonPropertyName("description")]
        public Dictionary<string, string> Description { get; set; } = new();
    }
    public sealed class HeroesIconsGamestringsRoot
    {
        [JsonPropertyName("emoticon")]
        public HeroesIconsEmoticonSection? Emoticon { get; set; }
    }
    public sealed class HeroesIconsGamestringsFile
    {
        [JsonPropertyName("gamestrings")]
        public HeroesIconsGamestringsRoot? Gamestrings { get; set; }
    }

    public static class EmoticonLoader
    {
        private static readonly Version FormatChangeVersion = new("2.55.16.97039");

        public static Dictionary<string, string> LoadAll(string dbDirectory, string dbVersion)
        {
            bool isLegacyFormat = IsHeroesIconsFormat(dbVersion);
            return isLegacyFormat ? LoadHeroesIcons(dbDirectory, dbVersion) : LoadHeoesElement(dbDirectory, dbVersion);
        }

        private static bool IsHeroesIconsFormat(string dbVersion)
        {
            // dbVersion peut contenir un suffixe/format inattendu ; en cas d'échec de parsing, on considère le format récent par défaut
            if (!Version.TryParse(dbVersion, out Version? parsedVersion))
                return false;

            return parsedVersion < FormatChangeVersion;
        }

        private static Dictionary<string, string> LoadHeoesElement(string dbDirectory, string dbVersion)
        {
            Dictionary<string, string>? emoticons = new(StringComparer.OrdinalIgnoreCase);
            var entriesById = new Dictionary<string, string>();

            string? mainPath = Directory.GetFiles($@"{dbDirectory}\{dbVersion}\data\", "emoticondata_*.json").FirstOrDefault();

            if (mainPath != null)
            {
                var mainData = JsonSerializer.Deserialize<HeroesElementEmoticonDataFile>(File.ReadAllText(mainPath));

                if (mainData?.Items != null)
                {
                    foreach (var (id, entry) in mainData.Items)
                    {
                        if (entry.Image is null)
                            continue;

                        entriesById[id] = entry.Image;

                        foreach (var alias in entry.UniversalAliases)
                            emoticons[alias] = entry.Image;
                    }
                }
            }

            string[]? langFiles = Directory.GetFiles($@"{dbDirectory}\{dbVersion}\gamestrings\", "gamestrings_*.json");

            foreach (var path in langFiles)
            {
                var data = JsonSerializer.Deserialize<HeroesElementLocalizedEmoticonFile>(File.ReadAllText(path));

                if (data?.Items == null || !data.Items.TryGetValue("emoticon", out var section))
                    continue;

                foreach (var (id, aliases) in section.LocalizedAliases)
                {
                    if (!entriesById.TryGetValue(id, out var image))
                        continue;

                    foreach (var alias in aliases)
                        emoticons[alias] = image;
                }
            }

            return emoticons;
        }

        private static readonly Regex AliasTokenPattern = new(@":\w+:", RegexOptions.Compiled);

        private static Dictionary<string, string> LoadHeroesIcons(string dbDirectory, string dbVersion)
        {
            var emoticons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entriesById = new Dictionary<string, string>();

            string? mainPath = Directory
                .GetFiles($@"{dbDirectory}\{dbVersion}\data\", "emoticondata_*_localized.json")
                .FirstOrDefault();

            if (mainPath != null)
            {
                var mainData = JsonSerializer.Deserialize<HeroesIconsEmoticonDataFile>(File.ReadAllText(mainPath));

                if (mainData != null)
                {
                    foreach (var (id, entry) in mainData)
                    {
                        if (entry.Image is null)
                            continue;

                        entriesById[id] = entry.Image;
                    }
                }
            }

            var langFiles = Directory.GetFiles($@"{dbDirectory}\{dbVersion}\gamestrings\", "gamestrings_*.json");

            bool aliasesLoaded = false;

            foreach (var path in langFiles)
            {
                var data = JsonSerializer.Deserialize<HeroesIconsGamestringsFile>(File.ReadAllText(path));
                var emoticonSection = data?.Gamestrings?.Emoticon;

                if (emoticonSection == null)
                    continue;

                // "aliases" est identique dans tous les fichiers de langue -> on ne le lit qu'une seule fois
                if (!aliasesLoaded && emoticonSection.Aliases.Count > 0)
                {
                    AddAliasesFromSpaceSeparatedString(emoticonSection.Aliases, entriesById, emoticons);
                    aliasesLoaded = true;
                }

                AddAliasesFromDescriptions(emoticonSection.Description, entriesById, emoticons);
            }

            return emoticons;
        }

        private static void AddAliasesFromSpaceSeparatedString(Dictionary<string, string> aliasesById, Dictionary<string, string> entriesById, Dictionary<string, string> emoticons)
        {
            foreach ((string id, string aliasesString) in aliasesById)
            {
                if (!entriesById.TryGetValue(id, out string? image))
                    continue;

                if (string.IsNullOrWhiteSpace(aliasesString))
                    continue;

                string[]? aliases = aliasesString.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                foreach (string? alias in aliases)
                    emoticons[alias] = image;
            }
        }

        private static void AddAliasesFromDescriptions(
            Dictionary<string, string> descriptionsById,
            Dictionary<string, string> entriesById,
            Dictionary<string, string> emoticons)
        {
            foreach ((string id, string description) in descriptionsById)
            {
                if (!entriesById.TryGetValue(id, out string? image))
                    continue;

                if (string.IsNullOrWhiteSpace(description))
                    continue;

                foreach (Match match in AliasTokenPattern.Matches(description))
                    emoticons[match.Value] = image;
            }
        }
    }
}
