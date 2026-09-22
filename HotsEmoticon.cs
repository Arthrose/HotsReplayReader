namespace HotsReplayReader
{
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;

    // ----- Format récent (>= 2.55.16.97039) -----

    public sealed class EmoticonEntry
    {
        [JsonPropertyName("universalAliases")]
        public List<string> UniversalAliases { get; set; } = new();

        [JsonPropertyName("isCaseSensitive")]
        public bool IsCaseSensitive { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    public sealed class EmoticonDataFile
    {
        [JsonPropertyName("items")]
        public Dictionary<string, EmoticonEntry> Items { get; set; } = new();
    }

    public sealed class LocalizedEmoticonSection
    {
        [JsonPropertyName("localizedAliases")]
        public Dictionary<string, List<string>> LocalizedAliases { get; set; } = new();
    }

    public sealed class LocalizedEmoticonFile
    {
        [JsonPropertyName("items")]
        public Dictionary<string, LocalizedEmoticonSection> Items { get; set; } = new();
    }

    // ----- Ancien format (< 2.55.16.97039) -----

    public sealed class LegacyEmoticonEntry
    {
        [JsonPropertyName("heroId")]
        public string? HeroId { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    // emoticondata_*_localized.json : { "abathur_angry": { "heroId": ..., "image": ... }, ... }
    public sealed class LegacyEmoticonDataFile : Dictionary<string, LegacyEmoticonEntry>
    {
    }

    public sealed class LegacyEmoticonSection
    {
        // "abathur_angry": ":abathurangry: :abaangry:"
        [JsonPropertyName("aliases")]
        public Dictionary<string, string> Aliases { get; set; } = new();

        // "abathur_angry": "Abathur en colère :abagrr:"
        [JsonPropertyName("description")]
        public Dictionary<string, string> Description { get; set; } = new();
    }

    public sealed class LegacyGamestringsRoot
    {
        [JsonPropertyName("emoticon")]
        public LegacyEmoticonSection? Emoticon { get; set; }
    }

    public sealed class LegacyGamestringsFile
    {
        [JsonPropertyName("gamestrings")]
        public LegacyGamestringsRoot? Gamestrings { get; set; }
    }

    public static class EmoticonLoader
    {
        private static readonly Version FormatChangeVersion = new("2.55.16.97039");

        public static Dictionary<string, string> LoadAll(string dbDirectory, string dbVersion)
        {
            bool isLegacyFormat = IsLegacyFormat(dbVersion);

            return isLegacyFormat
                ? LoadLegacy(dbDirectory, dbVersion)
                : LoadCurrent(dbDirectory, dbVersion);
        }

        private static bool IsLegacyFormat(string dbVersion)
        {
            // dbVersion peut contenir un suffixe/format inattendu ; en cas d'échec de parsing,
            // on considère le format récent par défaut (comportement précédent, non régressif).
            if (!Version.TryParse(dbVersion, out var parsedVersion))
                return false;

            return parsedVersion < FormatChangeVersion;
        }

        // ----- Format récent -----

        private static Dictionary<string, string> LoadCurrent(string dbDirectory, string dbVersion)
        {
            var emoticons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entriesById = new Dictionary<string, string>();

            string? mainPath = Directory
                .GetFiles($@"{dbDirectory}\{dbVersion}\data\", "emoticondata_*.json")
                .FirstOrDefault();

            if (mainPath != null)
            {
                var mainData = JsonSerializer.Deserialize<EmoticonDataFile>(File.ReadAllText(mainPath));

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

            var langFiles = Directory.GetFiles(
                $@"{dbDirectory}\{dbVersion}\gamestrings\", "gamestrings_*.json");

            foreach (var path in langFiles)
            {
                var data = JsonSerializer.Deserialize<LocalizedEmoticonFile>(File.ReadAllText(path));

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

        // ----- Ancien format -----

        private static readonly Regex AliasTokenPattern = new(@":\w+:", RegexOptions.Compiled);

        private static Dictionary<string, string> LoadLegacy(string dbDirectory, string dbVersion)
        {
            var emoticons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entriesById = new Dictionary<string, string>();

            // 1. Fichier de data : id -> image
            string? mainPath = Directory
                .GetFiles($@"{dbDirectory}\{dbVersion}\data\", "emoticondata_*_localized.json")
                .FirstOrDefault();

            if (mainPath != null)
            {
                var mainData = JsonSerializer.Deserialize<LegacyEmoticonDataFile>(File.ReadAllText(mainPath));

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

            var langFiles = Directory.GetFiles(
                $@"{dbDirectory}\{dbVersion}\gamestrings\", "gamestrings_*.json");

            bool aliasesLoaded = false;

            foreach (var path in langFiles)
            {
                var data = JsonSerializer.Deserialize<LegacyGamestringsFile>(File.ReadAllText(path));
                var emoticonSection = data?.Gamestrings?.Emoticon;

                if (emoticonSection == null)
                    continue;

                // "aliases" est identique dans tous les fichiers de langue -> on ne le lit
                // qu'une seule fois, peu importe le fichier (le premier valide rencontré).
                if (!aliasesLoaded && emoticonSection.Aliases.Count > 0)
                {
                    AddAliasesFromSpaceSeparatedString(emoticonSection.Aliases, entriesById, emoticons);
                    aliasesLoaded = true;
                }

                // "description" en revanche contient des alias LOCALISÉS différents par langue,
                // noyés dans une phrase -> on doit le lire dans CHAQUE fichier et en extraire
                // les tokens ":xxx:" via regex.
                AddAliasesFromDescriptions(emoticonSection.Description, entriesById, emoticons);
            }

            return emoticons;
        }

        private static void AddAliasesFromSpaceSeparatedString(
            Dictionary<string, string> aliasesById,
            Dictionary<string, string> entriesById,
            Dictionary<string, string> emoticons)
        {
            foreach (var (id, aliasesString) in aliasesById)
            {
                if (!entriesById.TryGetValue(id, out var image))
                    continue;

                if (string.IsNullOrWhiteSpace(aliasesString))
                    continue;

                var aliases = aliasesString.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (var alias in aliases)
                    emoticons[alias] = image;
            }
        }

        private static void AddAliasesFromDescriptions(
            Dictionary<string, string> descriptionsById,
            Dictionary<string, string> entriesById,
            Dictionary<string, string> emoticons)
        {
            foreach (var (id, description) in descriptionsById)
            {
                if (!entriesById.TryGetValue(id, out var image))
                    continue;

                if (string.IsNullOrWhiteSpace(description))
                    continue;

                foreach (Match match in AliasTokenPattern.Matches(description))
                    emoticons[match.Value] = image;
            }
        }
    }





















public class HotsEmoticonAnimation
    {
        public string? Texture { get; set; }
        public int Frames { get; set; }
        public int? Duration { get; set; }
        public int Width { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
    }
    public class HotsEmoticonData
    {
        public string? HeroId { get; set; }
        public string? HeroSkinId { get; set; }
        public string? Image { get; set; }
        public HotsEmoticonAnimation? Animation { get; set; }
        public List<string> Aliases { get; set; } = [];
    }
    public class HotsEmoticon : Dictionary<string, HotsEmoticonData>
    {
    }
    public class HotsEmoticonAliase
    {
        public Dictionary<string, string>? Aliases { get; set; }
        public Dictionary<string, string>? Localizedaliases { get; set; }
    }
    public class HotsGameStrings
    {
        public HotsGameStringsItems? Items { get; set; }
    }
    public class HotsGameStringsItems
    {
        public HotsGameStringsEmoticon? Emoticon { get; set; }
    }
    public class HotsGameStringsEmoticon
    {
        public Dictionary<string, List<string>>? LocalizedAliases { get; set; }
    }
}
