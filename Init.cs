using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Heroes.StormReplayParser;
using Microsoft.Win32;

namespace HotsReplayReader
{
    internal partial class Init
    {
        internal List<HotsLocalAccount>? hotsLocalAccounts;
        public StormReplay? hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.StormPlayer>? hotsPlayers;
        internal string? DbDirectory { get; set; }
        internal Config? config = new();
        public Init()
        {
            config = Config.Load();

            DbDirectory = $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HotsReplayReader")}\db";

            ListHotsAccounts();
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
                    bool    accountRegionAmerica = false,
                            accountRegionEurope = false,
                            accountRegionAsia = false,
                            accountRegionPTR = false,
                            accountNameFound = false;

                    string? battleTagName = null,
                            fullPath = null,
                            accountRegionAmericaFullPath = null,
                            accountRegionEuropeFullPath = null,
                            accountRegionAsiaFullPath = null,
                            accountRegionPTRFullPath = null;

                    foreach (string accountRegionDir in Directory.GetDirectories(accountDir, "*-Hero-*"))
                    {
                        FileInfo[] regionReplayFiles = new DirectoryInfo(Path.Combine(accountRegionDir, "Replays", "Multiplayer")).GetFiles("*.StormReplay");
                        if (regionReplayFiles.Length > 0)
                        {
                            switch (int.Parse(Path.GetFileName(accountRegionDir)[..Path.GetFileName(accountRegionDir).IndexOf('-')]))
                            {
                                case 1:
                                    accountRegionAmerica = true;
                                    accountRegionAmericaFullPath = Path.Combine(accountRegionDir, "Replays", "Multiplayer");
                                    break;
                                case 2:
                                    accountRegionEurope = true;
                                    accountRegionEuropeFullPath = Path.Combine(accountRegionDir, "Replays", "Multiplayer");
                                    break;
                                case 3:
                                    accountRegionAsia = true;
                                    accountRegionAsiaFullPath = Path.Combine(accountRegionDir, "Replays", "Multiplayer");
                                    break;
                                case 98:
                                    accountRegionPTR = true;
                                    accountRegionPTRFullPath = Path.Combine(accountRegionDir, "Replays", "Multiplayer");
                                    break;
                                default:
                                    break;
                            }

                            if (!accountNameFound)
                            {
                                Array.Reverse(regionReplayFiles);
                                for (int i = 0; i < regionReplayFiles.Length; i++)
                                {
                                    try
                                    {
                                        if (StormReplayParse(regionReplayFiles[i].FullName) && hotsReplay?.Owner != null)
                                        {
                                            battleTagName = hotsReplay.Owner.BattleTagName;
                                            fullPath = Path.GetDirectoryName(regionReplayFiles[0].FullName)!;
                                            accountNameFound = true;
                                            break;
                                        }
                                    }
                                    catch { continue; }
                                }
                            }
                        }
                    }
                    if (accountNameFound)
                        hotsLocalAccounts.Add(new HotsLocalAccount
                        {
                            BattleTagName = battleTagName,
                            FullPath = fullPath,
                            RegionAmerica = accountRegionAmerica,
                            RegionAmericaFullPath = accountRegionAmericaFullPath,
                            RegionEurope = accountRegionEurope,
                            RegionEuropeFullPath = accountRegionEuropeFullPath,
                            RegionAsia = accountRegionAsia,
                            RegionAsiaFullPath = accountRegionAsiaFullPath,
                            RegionPTR = accountRegionPTR,
                            RegionPTRFullPath = accountRegionPTRFullPath
                        });
                }
            }
        }
        private bool StormReplayParse(string hotsReplayFilePath)
        {
            StormReplayResult? hotsReplayResult = StormReplay.Parse(hotsReplayFilePath);
            StormReplayParseStatus hotsReplayStatus = hotsReplayResult.Status;

            if (hotsReplayStatus == StormReplayParseStatus.Success || hotsReplayStatus == StormReplayParseStatus.PTRRegion)
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
        public string? LastBrowseDirectory { get; set; }
        public string? DeepLAPIKey { get; set; }
        public bool AskUpdate { get; set; } = true;
        public bool DisplayGameMode { get; set; } = true;
        public bool DisplayDate { get; set; } = true;
        public bool DisplayReplaySideBar { get; set; } = true;
        public bool DisplayPingButton { get; set; } = true;
        public bool DisplayDraftOrder { get; set; } = true;
        public DarkModeType DarkMode { get; set; } = DarkModeType.Automatic;
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
                try
                {
                    Config? config = JsonSerializer.Deserialize<Config>(json);
                    if (config != null)
                    {
                        config.LangCode ??= "en-US";
                        config.Region ??= "2";
                        return config;
                    }
                }
                catch (Exception) { return new Config(); }
            }
            return new Config();
        }
        internal void Save()
        {
            string file = GetConfigPath();
            string json = JsonSerializer.Serialize(this, jsonOptions);
            File.WriteAllText(file, json);
        }
        public enum DarkModeType
        {
            Light,
            Dark,
            Automatic
        }
    }
}
