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
        public bool DisplayReplaySideBar { get; set; } = true;
        public bool DisplayPingButton { get; set; } = true;
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
