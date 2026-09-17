using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Heroes.StormReplayParser;
using Microsoft.Win32;

namespace HotsReplayReader
{
    internal partial class Init
    {
        internal List<HotsLocalAccount>? hotsLocalAccounts;
        internal HotsEmoticon? hotsEmoticons;
        public StormReplay? hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.StormPlayer>? hotsPlayers;
        internal string? DbDirectory { get; set; }
        internal Config? config = new();
        public Init()
        {
            config = Config.Load();

            DbDirectory = $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HotsReplayReader")}\db";

            ListHotsAccounts();
            LoadHotsEmoticons();
        }
        internal void ListHotsAccounts()
        {
            string[] accountsDirs;
            hotsLocalAccounts = [];

            RegistryKey? regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            if (regKey == null) return;

            string hotsDocumentsFolder = regKey.GetValue("Personal", "").ToString() + "\\Heroes of the Storm";
            if (Directory.Exists(hotsDocumentsFolder + "\\Accounts"))
            {
                accountsDirs = Directory.GetDirectories(hotsDocumentsFolder + "\\Accounts");

                string[] orderedDirs = [.. accountsDirs
                    .OrderBy(dir => {
                        string? folderName = Path.GetFileName(dir);
                        // Try parse folderName as number
                        bool isNumeric = long.TryParse(folderName, out long num);
                        // If numeric, sort by num; if not, use a fixed large value for numeric sort and alphabetically for secondary sort
                        return isNumeric ? (0, num, "") : (1, 0L, folderName);
                    })];

                foreach (string accountDir in orderedDirs)
                {
                    DirectoryInfo directoryInfo = new(accountDir);
                    string[] multiplayersReplayDirs = Directory.GetDirectories(accountDir);
                    foreach (string multiplayersReplayDir in multiplayersReplayDirs)
                    {
                        DirectoryInfo multiplayersReplayDirInfo = new(multiplayersReplayDir);
                        if (multiplayersReplayDirInfo.Name[..7] == $"{config?.Region}-Hero-")
                        {
                            DirectoryInfo hotsReplayFolder = new(multiplayersReplayDir + @"\Replays\Multiplayer");
                            FileInfo[] replayFiles = hotsReplayFolder.GetFiles(@"*.StormReplay");
                            if (replayFiles.Length > 0)
                            {
                                Array.Reverse(replayFiles);
                                for (int i = 0; i < replayFiles.Length; i++)
                                {
                                    try
                                    {
                                        if (StormReplayParse(replayFiles[i].FullName) && hotsReplay?.Owner != null)
                                        {
                                            hotsLocalAccounts.Add(new HotsLocalAccount
                                            {
                                                BattleTagName = hotsReplay.Owner.BattleTagName,
                                                FullPath = Path.GetDirectoryName(replayFiles[0].FullName)
                                            });
                                            break;
                                        }
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        internal void LoadHotsEmoticons()
        {
            JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

            hotsEmoticons = JsonSerializer.Deserialize<HotsEmoticon>(Encoding.UTF8.GetString(Resources.HotsResources.emoticondata), jsonOptions);
            HotsEmoticonAliase? hotsEmoticonAliases = JsonSerializer.Deserialize<HotsEmoticonAliase>(Encoding.UTF8.GetString(Resources.HotsResources.emoticonsaliases), jsonOptions);

            if (hotsEmoticonAliases?.Aliases == null) return;

            //MergeLocalizedAliases(hotsEmoticonAliases);

            foreach (KeyValuePair<string, string> aliases in hotsEmoticonAliases.Aliases)
            {
                if (hotsEmoticons != null && hotsEmoticons.TryGetValue(aliases.Key, out HotsEmoticonData? value))
                {
                    foreach (string alias in aliases.Value.Split(' '))
                        value.Aliases.Add(alias);
                }
            }
        }
        private void MergeLocalizedAliases(HotsEmoticonAliase hotsEmoticonAliases)
        {
            hotsEmoticonAliases.Aliases ??= [];

            JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

            if (DbDirectory is not null)
                foreach (string file in Directory.GetFiles(DbDirectory, "gamestrings_*.json"))
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    HotsGameStrings? gameStrings = JsonSerializer.Deserialize<HotsGameStrings>(json, jsonOptions);
    
                    Dictionary<string, List<string>>? localizedAliases = gameStrings?.Items?.Emoticon?.LocalizedAliases;
                    if (localizedAliases == null) continue;
    
                    foreach (KeyValuePair<string, List<string>> kvp in localizedAliases)
                    {
                        hotsEmoticonAliases.Aliases.TryGetValue(kvp.Key, out string? existingValue);
    
                        // Set des alias déjà présents (Aliases est une string séparée par des espaces)
                        HashSet<string> existingSet = new(
                            (existingValue ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    
                        string merged = existingValue ?? string.Empty;
    
                        foreach (string alias in kvp.Value)
                        {
                            if (string.IsNullOrWhiteSpace(alias)) continue;
    
                            if (existingSet.Add(alias))
                            {
                                merged = merged.Length == 0 ? alias : merged + " " + alias;
                            }
                        }
    
                        hotsEmoticonAliases.Aliases[kvp.Key] = merged;
                    }
                }
        }
        private bool StormReplayParse(string hotsReplayFilePath)
        {
            StormReplayResult? hotsReplayResult = StormReplay.Parse(hotsReplayFilePath);
            StormReplayParseStatus hotsReplayStatus = hotsReplayResult.Status;

            if (hotsReplayStatus == StormReplayParseStatus.Success)
            {
                hotsReplay = hotsReplayResult.Replay;
                hotsPlayers = hotsReplay.StormPlayers;
                return true;
            }
            else
            {
                if (hotsReplayStatus == StormReplayParseStatus.Exception)
                {
                    Debug.WriteLine($"Exception parsing replay: {hotsReplayResult.Exception?.Message}");
                }
                return false;
            }
        }
    }
    internal class Config
    {
        public string? LangCode { get; set; } = "en-US";
        public string? Region { get; set; } = "2";
        public string? LastSelectedAccount { get; set; }
        public string? LastSelectedAccountDirectory { get; set; }
        public string? LastBrowseDirectory { get; set; }
        public string? DeepLAPIKey { get; set; }
        public bool AskUpdate { get; set; } = true;
        internal JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
        private static string GetConfigPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HotsReplayReader");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "HotsReplayReader.json");
        }
        internal static Config Load()
        {
            string file = GetConfigPath();
            if (!File.Exists(file)) return new Config();

            string json = File.ReadAllText(file);
            if (json != null)
            {
                Config? config = JsonSerializer.Deserialize<Config>(json);
                if (config != null)
                {
                    config.LangCode ??= "en-US";
                    config.Region ??= "2";
                    return config;
                }
            }
            return new Config();
        }
        internal void Save()
        {
            string file = GetConfigPath();
            string json = JsonSerializer.Serialize(this, jsonOptions);
            File.WriteAllText(file, json);
        }
    }
}
