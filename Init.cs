using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Heroes.StormReplayParser;
using Microsoft.Win32;

namespace HotsReplayReader
{
    internal partial class Init
    {
        internal string? lastReplayFilePath;
        private readonly string? hotsVariablesFile;
        private readonly string? userDocumentsFolder;
        internal List<HotsLocalAccount>? hotsLocalAccounts;
        internal HotsEmoticon? hotsEmoticons;
        public StormReplay? hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.StormPlayer>? hotsPlayers;
        internal string? DbDirectory { get; set; }
        public Init()
        {
            RegistryKey? regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            
            if (regKey == null) return;

            userDocumentsFolder = regKey.GetValue("Personal", "").ToString();
            hotsVariablesFile = userDocumentsFolder + @"\Heroes of the Storm\Variables.txt";

            ListHotsAccounts();
            lastReplayFilePath = GetLastReplayFilePath();
            LoadHotsEmoticons();

            DbDirectory = $@"{Directory.GetCurrentDirectory()}\db";
        }
        internal string? GetLastReplayFilePath()
        {
            JsonConfig? jsonConfig;
            string jsonFile;
            if (File.Exists($@"{Directory.GetCurrentDirectory()}\{HotsReplayWebReader.jsonConfigFile}"))
            {
                jsonFile = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\{HotsReplayWebReader.jsonConfigFile}");
                jsonConfig = JsonSerializer.Deserialize<JsonConfig>(jsonFile);
                if (jsonConfig == null) return @"";
                if (Directory.Exists(jsonConfig.LastSelectedAccountDirectory))
                {
                    if (jsonConfig.LastSelectedAccount == null) return @"";
                    HotsReplayWebReader.currentAccount = jsonConfig.LastSelectedAccount;
                    return jsonConfig.LastSelectedAccountDirectory;
                }
            }
            lastReplayFilePath = @"";
            bool lastReplayFilePathFound = false;
            if (File.Exists(hotsVariablesFile))
            {
                var lines = File.ReadLines(hotsVariablesFile);
                foreach (var line in lines)
                {
                    if (MyRegexLastReplayFilePath().IsMatch(line.Trim()))
                    {
                        lastReplayFilePath = Path.GetDirectoryName(line[(line.IndexOf('=') + 1)..]);
                        if (lastReplayFilePath != null)
                            if (lastReplayFilePath.Length > 0)
                                lastReplayFilePathFound = true;
                    }
                }
            }

            if (!lastReplayFilePathFound && userDocumentsFolder != null)
            {
                if (userDocumentsFolder.Length > 0)
                {
                    lastReplayFilePath = userDocumentsFolder;
                }
                else
                {
                    lastReplayFilePath = @"";
                }
            }

            if (hotsLocalAccounts == null) return lastReplayFilePath;

            foreach (HotsLocalAccount hotsLocalAccount in hotsLocalAccounts)
            {
                if (hotsLocalAccount.FullPath == lastReplayFilePath && hotsLocalAccount.BattleTagName != null)
                {
                    HotsReplayWebReader.currentAccount = hotsLocalAccount.BattleTagName;
                    break;
                }
            }
            return lastReplayFilePath;
        }
        internal void ListHotsAccounts()
        {
            string[] accountsDirs;
            hotsLocalAccounts = [];
            if (Directory.Exists(Path.GetDirectoryName(hotsVariablesFile) + @"\Accounts"))
            {
                accountsDirs = Directory.GetDirectories(Path.GetDirectoryName(hotsVariablesFile) + @"\Accounts");
                foreach (string accountDir in accountsDirs)
                {
                    DirectoryInfo directoryInfo = new(accountDir);
                    string[] multiplayersReplayDirs = Directory.GetDirectories(accountDir);
                    foreach (string multiplayersReplayDir in multiplayersReplayDirs)
                    {
                        DirectoryInfo multiplayersReplayDirInfo = new(multiplayersReplayDir);
                        if (multiplayersReplayDirInfo.Name[..7] == @"2-Hero-")
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
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            hotsEmoticons = JsonSerializer.Deserialize<HotsEmoticon>(Encoding.UTF8.GetString(HotsResources.emoticondata), jsonOptions);
            HotsEmoticonAliase? hotsEmoticonAliases = JsonSerializer.Deserialize<HotsEmoticonAliase>(Encoding.UTF8.GetString(HotsResources.emoticonsaliases), jsonOptions);

            if (hotsEmoticonAliases?.Aliases == null) return;

            foreach (KeyValuePair<string, string> aliases in hotsEmoticonAliases.Aliases)
            {
                if (hotsEmoticons != null && hotsEmoticons.TryGetValue(aliases.Key, out HotsEmoticonData? value))
                {
                    foreach (string alias in aliases.Value.Split(' '))
                        value.Aliases.Add(alias);
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

        // lLastReplayFilePath=...
        [GeneratedRegex(@"^lastReplayFilePath=(.*)$")]
        private static partial Regex MyRegexLastReplayFilePath();
    }
    internal class JsonConfig
    {
        public string? LastSelectedAccount { get; set; }
        public string? LastSelectedAccountDirectory { get; set; }
        public string? LastBrowseDirectory { get; set; }
    }
}
