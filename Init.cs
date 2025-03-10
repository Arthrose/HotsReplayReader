using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text.Json;
using System.Resources;
using System.Diagnostics;

namespace HotsReplayReader
{
    internal class Init
    {
        internal string? lastReplayFilePath;
        private string? hotsVariablesFile;
        internal List<hotsLocalAccount>? hotsLocalAccounts;
        private StormReplay? hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.stormPlayer>? hotsPlayers;
        public Init()
        {
            lastReplayFilePath = getLastReplayFilePath();
            listHotsAccounts();
        }
        internal string getLastReplayFilePath()
        {
            string userDocumentsFolder;
            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
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
                        // MessageBox.Show(lastReplayFilePath);
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
                                if (StormReplayParse(replayFiles[0].FullName))
                                {
                                    hotsLocalAccounts.Add(new hotsLocalAccount
                                    {
                                        BattleTagName = hotsReplay.Owner.BattleTagName,
                                        FullPath = Path.GetDirectoryName(replayFiles[0].FullName)
                                    });
                                    //comboBoxHotsAccounts.Items.Add(hotsReplay.Owner.BattleTagName);
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
                    StormParseException? hotsParseException = hotsReplayResult.Exception;
                }
                return false;
            }
        }
    }
    internal class jsonConfig
    {
        public string? lastBrowseDirectory { get; set; }
    }
}
