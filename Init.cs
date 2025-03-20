using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Heroes.StormReplayParser;
using Microsoft.Win32;

namespace HotsReplayReader
{
    internal class Init
    {
        internal string? lastReplayFilePath;
        private string? hotsVariablesFile;
        internal List<hotsLocalAccount>? hotsLocalAccounts;
        internal hotsEmoticon? hotsEmoticons;
        private StormReplay? hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.StormPlayer>? hotsPlayers;
        public Init()
        {
            lastReplayFilePath = getLastReplayFilePath();
            listHotsAccounts();
            loadHotsEmoticons();
        }
        internal string getLastReplayFilePath()
        {
            string? userDocumentsFolder;
            RegistryKey? RegKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            userDocumentsFolder = RegKey.GetValue("Personal", "").ToString();

            bool lastReplayFilePathFound = false;
            string lastReplayFilePath = @"";
            hotsVariablesFile = userDocumentsFolder + @"\Heroes of the Storm\Variables.txt";

            if (File.Exists(hotsVariablesFile))
            {
                var lines = File.ReadLines(hotsVariablesFile);
                foreach (var line in lines)
                {
                    if (Regex.IsMatch(line.Trim(), @"^lastReplayFilePath=(.*)$"))
                    {
                        lastReplayFilePath = Path.GetDirectoryName(line.Substring(line.IndexOf('=') + 1));
                        if (lastReplayFilePath.Length > 0)
                            lastReplayFilePathFound = true;
                    }
                }
            }

            if (!lastReplayFilePathFound)
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

            return lastReplayFilePath;
        }
        internal void listHotsAccounts()
        {
            string[] accountsDirs;
            hotsLocalAccounts = new List<hotsLocalAccount>();
            if (Directory.Exists(Path.GetDirectoryName(hotsVariablesFile) + @"\Accounts"))
            {
                accountsDirs = Directory.GetDirectories(Path.GetDirectoryName(hotsVariablesFile) + @"\Accounts");
                foreach (string accountDir in accountsDirs)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(accountDir);
                    string[] multiplayersReplayDirs = Directory.GetDirectories(accountDir);
                    foreach (string multiplayersReplayDir in multiplayersReplayDirs)
                    {
                        DirectoryInfo multiplayersReplayDirInfo = new DirectoryInfo(multiplayersReplayDir);
                        if (multiplayersReplayDirInfo.Name.Substring(0, 7) == @"2-Hero-")
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
                                        if (StormReplayParse(replayFiles[i].FullName))
                                        {
                                            hotsLocalAccounts.Add(new hotsLocalAccount
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
        internal void loadHotsEmoticons()
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            hotsEmoticons = JsonSerializer.Deserialize<hotsEmoticon>(Encoding.UTF8.GetString(hotsResources.emoticondata), jsonOptions);
            hotsEmoticonAliase? hotsEmoticonAliases = JsonSerializer.Deserialize<hotsEmoticonAliase>(Encoding.UTF8.GetString(hotsResources.emoticonsaliases), jsonOptions);

            foreach (KeyValuePair<string, string> aliases in hotsEmoticonAliases.aliases)
            {
                if (hotsEmoticons != null && hotsEmoticons.ContainsKey(aliases.Key))
                {
                    foreach (string alias in aliases.Value.Split(' '))
                        hotsEmoticons[aliases.Key].aliases.Add(alias);
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
                    StormParseException? hotsParseException = hotsReplayResult.Exception;
                }
                return false;
            }
        }
    }
    internal class jsonConfig
    {
        public string? LastBrowseDirectory { get; set; }
    }
}
