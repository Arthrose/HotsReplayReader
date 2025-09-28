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
        public Dictionary<string, string> HeroNameFromHeroUnitId = new()
        {
            ["HeroAbathur"]               = "Abathur",
            ["HeroAlarak"]                = "Alarak",
            ["HeroAlexstrasza"]           = "Alexstrasza",
            ["HeroAlexstraszaDragon"]     = "Alexstrasza",
            ["HeroAna"]                   = "Ana",
            ["HeroAnduin"]                = "Anduin",
            ["HeroAnubarak"]              = "Anub'arak",
            ["HeroArtanis"]               = "Artanis",
            ["HeroArthas"]                = "Arthas",
            ["HeroAuriel"]                = "Auriel",
            ["HeroAzmodan"]               = "Azmodan",
            ["HeroFirebat"]               = "Blaze",
            ["HeroFaerieDragon"]          = "Brightwing",
            ["HeroAmazon"]                = "Cassia",
            ["HeroChen"]                  = "Chen",
            ["HeroCho"]                   = "Cho",
            ["HeroChromie"]               = "Chromie",
            ["HeroDVaMech"]               = "D.Va",
            ["HeroDVaPilot"]              = "D.Va",
            ["DeathwingDragonflightUnit"] = "Deathwing",
            ["HeroDeathwing"]             = "Deathwing",
            ["HeroDeckard"]               = "Deckard",
            ["HeroDehaka"]                = "Dehaka",
            ["HeroDiablo"]                = "Diablo",
            ["HeroL90ETC"]                = "E.T.C.",
            ["HeroFalstad"]               = "Falstad",
            ["HeroFenix"]                 = "Fenix",
            ["HeroGall"]                  = "Gall",
            ["HeroGarrosh"]               = "Garrosh",
            ["HeroTinker"]                = "Gazlowe",
            ["HeroGenji"]                 = "Genji",
            ["HeroGreymane"]              = "Greymane",
            ["HeroGuldan"]                = "Gul'dan",
            ["HeroHanzo"]                 = "Hanzo",
            ["HeroHogger"]                = "Hogger",
            ["HeroIllidan"]               = "Illidan",
            ["HeroImperius"]              = "Imperius",
            ["HeroJaina"]                 = "Jaina",
            ["HeroCrusader"]              = "Johanna",
            ["HeroJunkrat"]               = "Junkrat",
            ["HeroKaelthas"]              = "Kael'thas",
            ["HeroKelThuzad"]             = "Kel'Thuzad",
            ["HeroKerrigan"]              = "Kerrigan",
            ["HeroMonk"]                  = "Kharazim",
            ["HeroLeoric"]                = "Leoric",
            ["HeroLiLi"]                  = "Li Li",
            ["HeroWizard"]                = "Li-Ming",
            ["HeroMedic"]                 = "Lt. Morales",
            ["HeroLucio"]                 = "Lucio",
            ["HeroDryad"]                 = "Lunara",
            ["HeroMaiev"]                 = "Maiev",
            ["HeroMalfurion"]             = "Malfurion",
            ["HeroMalGanis"]              = "Mal'Ganis",
            ["HeroMalthael"]              = "Malthael",
            ["HeroMedivh"]                = "Medivh",
            ["HeroMedivhRaven"]           = "Medivh",
            ["HeroMeiOW"]                 = "Mei",
            ["HeroMephisto"]              = "Mephisto",
            ["HeroMuradin"]               = "Muradin",
            ["HeroMurky"]                 = "Murky",
            ["HeroWitchDoctor"]           = "Nazeebo",
            ["HeroNova"]                  = "Nova",
            ["HeroOrphea"]                = "Orphea",
            ["HeroProbius"]               = "Probius",
            ["HeroNexusHunter"]           = "Qhira",
            ["HeroRagnaros"]              = "Ragnaros",
            ["HeroRaynor"]                = "Raynor",
            ["HeroRehgar"]                = "Rehgar",
            ["HeroRexxar"]                = "Rexxar",
            ["HeroSamuro"]                = "Samuro",
            ["HeroSgtHammer"]             = "Sgt. Hammer",
            ["HeroBarbarian"]             = "Sonya",
            ["HeroStitches"]              = "Stitches",
            ["HeroStukov"]                = "Stukov",
            ["HeroSylvanas"]              = "Sylvanas",
            ["HeroTassadar"]              = "Tassadar",
            ["HeroButcher"]               = "The Butcher",
            ["HeroLostVikingsController"] = "The Lost Vikings",
            ["HeroThrall"]                = "Thrall",
            ["HeroTracer"]                = "Tracer",
            ["HeroTychus"]                = "Tychus",
            ["HeroTyrael"]                = "Tyrael",
            ["HeroTyrande"]               = "Tyrande",
            ["HeroUther"]                 = "Uther",
            ["HeroValeera"]               = "Valeera",
            ["HeroDemonHunter"]           = "Valla",
            ["HeroVarian"]                = "Varian",
            ["HeroWhitemane"]             = "Whitemane",
            ["HeroNecromancer"]           = "Xul",
            ["HeroYrel"]                  = "Yrel",
            ["HeroZagara"]                = "Zagara",
            ["HeroZarya"]                 = "Zarya",
            ["HeroZeratul"]               = "Zeratul",
            ["HeroZuljin"]                = "Zul'jin"
        };
        public Dictionary<string, string> HeroNameFromHeroId = new()
        {
            ["Abathur"]      = "Abathur",
            ["Alarak"]       = "Alarak",
            ["Alexstrasza"]  = "Alexstrasza",
            ["Ana"]          = "Ana",
            ["Anduin"]       = "Anduin",
            ["Anubarak"]     = "Anub'arak",
            ["Artanis"]      = "Artanis",
            ["Arthas"]       = "Arthas",
            ["Auriel"]       = "Auriel",
            ["Azmodan"]      = "Azmodan",
            ["Firebat"]      = "Blaze",
            ["FaerieDragon"] = "Brightwing",
            ["Amazon"]       = "Cassia",
            ["Chen"]         = "Chen",
            ["Cho"]          = "Cho",
            ["Chromie"]      = "Chromie",
            ["Deathwing"]    = "Deathwing",
            ["Deckard"]      = "Deckard",
            ["Dehaka"]       = "Dehaka",
            ["Diablo"]       = "Diablo",
            ["DVa"]          = "D.Va",
            ["L90ETC"]       = "E.T.C.",
            ["Falstad"]      = "Falstad",
            ["Fenix"]        = "Fenix",
            ["Gall"]         = "Gall",
            ["Garrosh"]      = "Garrosh",
            ["Tinker"]       = "Gazlowe",
            ["Genji"]        = "Genji",
            ["Greymane"]     = "Greymane",
            ["Guldan"]       = "Gul'dan",
            ["Hanzo"]        = "Hanzo",
            ["Hogger"]       = "Hogger",
            ["Illidan"]      = "Illidan",
            ["Imperius"]     = "Imperius",
            ["Jaina"]        = "Jaina",
            ["Crusader"]     = "Johanna",
            ["Junkrat"]      = "Junkrat",
            ["Kaelthas"]     = "Kael'thas",
            ["KelThuzad"]    = "Kel'Thuzad",
            ["Kerrigan"]     = "Kerrigan",
            ["Monk"]         = "Kharazim",
            ["Leoric"]       = "Leoric",
            ["LiLi"]         = "Li Li",
            ["Lucio"]        = "Lucio",
            ["Dryad"]        = "Lunara",
            ["Maiev"]        = "Maiev",
            ["Malfurion"]    = "Malfurion",
            ["MalGanis"]     = "Mal'Ganis",
            ["Malthael"]     = "Malthael",
            ["Medic"]        = "Lt. Morales",
            ["Medivh"]       = "Medivh",
            ["MeiOW"]        = "Mei",
            ["Mephisto"]     = "Mephisto",
            ["WitchDoctor"]  = "Nazeebo",
            ["Muradin"]      = "Muradin",
            ["Murky"]        = "Murky",
            ["Necromancer"]  = "Xul",
            ["NexusHunter"]  = "Qhira",
            ["Nova"]         = "Nova",
            ["Orphea"]       = "Orphea",
            ["Probius"]      = "Probius",
            ["Ragnaros"]     = "Ragnaros",
            ["Raynor"]       = "Raynor",
            ["Rehgar"]       = "Rehgar",
            ["Rexxar"]       = "Rexxar",
            ["Samuro"]       = "Samuro",
            ["SgtHammer"]    = "Sgt. Hammer",
            ["Barbarian"]    = "Sonya",
            ["Stitches"]     = "Stitches",
            ["Stukov"]       = "Stukov",
            ["Sylvanas"]     = "Sylvanas",
            ["Tassadar"]     = "Tassadar",
            ["Butcher"]      = "The Butcher",
            ["LostVikings"]  = "The Lost Vikings",
            ["Thrall"]       = "Thrall",
            ["Tracer"]       = "Tracer",
            ["Tychus"]       = "Tychus",
            ["Tyrael"]       = "Tyrael",
            ["Tyrande"]      = "Tyrande",
            ["Uther"]        = "Uther",
            ["Valeera"]      = "Valeera",
            ["DemonHunter"]  = "Valla",
            ["Varian"]       = "Varian",
            ["Whitemane"]    = "Whitemane",
            ["Wizard"]       = "Li-Ming",
            ["Yrel"]         = "Yrel",
            ["Zagara"]       = "Zagara",
            ["Zarya"]        = "Zarya",
            ["Zeratul"]      = "Zeratul",
            ["Zuljin"]       = "Zul'jin",
            ["NONE"]         = "NONE"
        };
        public Dictionary<string, string> HeroRoleFromHeroUnitId = new()
        {
            ["HeroAnubarak"]              = "Tank",
            ["HeroArthas"]                = "Tank",
            ["HeroFirebat"]               = "Tank",
            ["HeroCho"]                   = "Tank",
            ["HeroDiablo"]                = "Tank",
            ["HeroL90ETC"]                = "Tank",
            ["HeroGarrosh"]               = "Tank",
            ["HeroCrusader"]              = "Tank",
            ["HeroMalGanis"]              = "Tank",
            ["HeroMeiOW"]                 = "Tank",
            ["HeroMuradin"]               = "Tank",
            ["HeroStitches"]              = "Tank",
            ["HeroTyrael"]                = "Tank", 
            
            ["HeroArtanis"]               = "Bruiser",
            ["HeroChen"]                  = "Bruiser",
            ["HeroDeathwing"]             = "Bruiser",
            ["DeathwingDragonflightUnit"] = "Bruiser",
            ["HeroDehaka"]                = "Bruiser",
            ["HeroDVaPilot"]              = "Bruiser",
            ["HeroTinker"]                = "Bruiser",
            ["HeroHogger"]                = "Bruiser",
            ["HeroImperius"]              = "Bruiser",
            ["HeroLeoric"]                = "Bruiser",
            ["HeroMalthael"]              = "Bruiser",
            ["HeroRagnaros"]              = "Bruiser",
            ["HeroRexxar"]                = "Bruiser",
            ["HeroBarbarian"]             = "Bruiser",
            ["HeroThrall"]                = "Bruiser",
            ["HeroVarian"]                = "Bruiser",
            ["HeroNecromancer"]           = "Bruiser",
            ["HeroYrel"]                  = "Bruiser", 
            
            ["HeroAzmodan"]               = "Ranged",
            ["HeroAmazon"]                = "Ranged",
            ["HeroChromie"]               = "Ranged",
            ["HeroFalstad"]               = "Ranged",
            ["HeroFenix"]                 = "Ranged",
            ["HeroGall"]                  = "Ranged",
            ["HeroGenji"]                 = "Ranged",
            ["HeroGreymane"]              = "Ranged",
            ["HeroGuldan"]                = "Ranged",
            ["HeroHanzo"]                 = "Ranged",
            ["HeroJaina"]                 = "Ranged",
            ["HeroJunkrat"]               = "Ranged",
            ["HeroKaelthas"]              = "Ranged",
            ["HeroKelThuzad"]             = "Ranged",
            ["HeroWizard"]                = "Ranged",
            ["HeroDryad"]                 = "Ranged",
            ["HeroMephisto"]              = "Ranged",
            ["HeroWitchDoctor"]           = "Ranged",
            ["HeroNova"]                  = "Ranged",
            ["HeroOrphea"]                = "Ranged",
            ["HeroProbius"]               = "Ranged",
            ["HeroRaynor"]                = "Ranged",
            ["HeroSgtHammer"]             = "Ranged",
            ["HeroSylvanas"]              = "Ranged",
            ["HeroTassadar"]              = "Ranged",
            ["HeroTracer"]                = "Ranged",
            ["HeroTychus"]                = "Ranged",
            ["HeroDemonHunter"]           = "Ranged",
            ["HeroZagara"]                = "Ranged",
            ["HeroZuljin"]                = "Ranged", 
            
            ["HeroAlarak"]                = "Melee",
            ["HeroIllidan"]               = "Melee",
            ["HeroKerrigan"]              = "Melee",
            ["HeroMaiev"]                 = "Melee",
            ["HeroMurky"]                 = "Melee",
            ["HeroNexusHunter"]           = "Melee",
            ["HeroSamuro"]                = "Melee",
            ["HeroButcher"]               = "Melee",
            ["HeroValeera"]               = "Melee",
            ["HeroZeratul"]               = "Melee", 
            
            ["HeroAlexstrasza"]           = "Healer",
            ["HeroAlexstraszaDragon"]     = "Healer",
            ["HeroAna"]                   = "Healer",
            ["HeroAnduin"]                = "Healer",
            ["HeroAuriel"]                = "Healer",
            ["HeroFaerieDragon"]          = "Healer",
            ["HeroDeckard"]               = "Healer",
            ["HeroMonk"]                  = "Healer",
            ["HeroLiLi"]                  = "Healer",
            ["HeroMedic"]                 = "Healer",
            ["HeroLucio"]                 = "Healer",
            ["HeroMalfurion"]             = "Healer",
            ["HeroRehgar"]                = "Healer",
            ["HeroStukov"]                = "Healer",
            ["HeroTyrande"]               = "Healer",
            ["HeroUther"]                 = "Healer",
            ["HeroWhitemane"]             = "Healer", 
            
            ["HeroAbathur"]               = "Support",
            ["HeroMedivh"]                = "Support",
            ["HeroMedivhRaven"]           = "Support",
            ["HeroLostVikingsController"] = "Support",
            ["HeroZarya"]                 = "Support" 
        };
        public Dictionary<string, string> HeroIdFromHeroUnitId = new()
        {
            ["HeroAbathur"]               = "Abathur",
            ["HeroAlarak"]                = "Alarak",
            ["HeroAlexstrasza"]           = "Alexstrasza",
            ["HeroAlexstraszaDragon"]     = "Alexstrasza",
            ["HeroAna"]                   = "Ana",
            ["HeroAnduin"]                = "Anduin",
            ["HeroAnubarak"]              = "Anubarak",
            ["HeroArtanis"]               = "Artanis",
            ["HeroArthas"]                = "Arthas",
            ["HeroAuriel"]                = "Auriel",
            ["HeroAzmodan"]               = "Azmodan",
            ["HeroFirebat"]               = "Firebat",
            ["HeroFaerieDragon"]          = "FaerieDragon",
            ["HeroAmazon"]                = "Amazon",
            ["HeroChen"]                  = "Chen",
            ["HeroCho"]                   = "Cho",
            ["HeroChromie"]               = "Chromie",
            ["HeroDVaMech"]               = "DVa",
            ["HeroDVaPilot"]              = "DVa",
            ["DeathwingDragonflightUnit"] = "Deathwing",
            ["HeroDeathwing"]             = "Deathwing",
            ["HeroDeckard"]               = "Deckard",
            ["HeroDehaka"]                = "Dehaka",
            ["HeroDiablo"]                = "Diablo",
            ["HeroL90ETC"]                = "L90ETC",
            ["HeroFalstad"]               = "Falstad",
            ["HeroFenix"]                 = "Fenix",
            ["HeroGall"]                  = "Gall",
            ["HeroGarrosh"]               = "Garrosh",
            ["HeroTinker"]                = "Tinker",
            ["HeroGenji"]                 = "Genji",
            ["HeroGreymane"]              = "Greymane",
            ["HeroGuldan"]                = "Guldan",
            ["HeroHanzo"]                 = "Hanzo",
            ["HeroHogger"]                = "Hogger",
            ["HeroIllidan"]               = "Illidan",
            ["HeroImperius"]              = "Imperius",
            ["HeroJaina"]                 = "Jaina",
            ["HeroCrusader"]              = "Crusader",
            ["HeroJunkrat"]               = "Junkrat",
            ["HeroKaelthas"]              = "Kaelthas",
            ["HeroKelThuzad"]             = "KelThuzad",
            ["HeroKerrigan"]              = "Kerrigan",
            ["HeroMonk"]                  = "Monk",
            ["HeroLeoric"]                = "Leoric",
            ["HeroLiLi"]                  = "LiLi",
            ["HeroWizard"]                = "Wizard",
            ["HeroMedic"]                 = "Medic",
            ["HeroLucio"]                 = "Lucio",
            ["HeroDryad"]                 = "Dryad",
            ["HeroMaiev"]                 = "Maiev",
            ["HeroMalfurion"]             = "Malfurion",
            ["HeroMalGanis"]              = "MalGanis",
            ["HeroMalthael"]              = "Malthael",
            ["HeroMedivh"]                = "Medivh",
            ["HeroMedivhRaven"]           = "Medivh",
            ["HeroMeiOW"]                 = "MeiOW",
            ["HeroMephisto"]              = "Mephisto",
            ["HeroMuradin"]               = "Muradin",
            ["HeroMurky"]                 = "Murky",
            ["HeroWitchDoctor"]           = "WitchDoctor",
            ["HeroNova"]                  = "Nova",
            ["HeroOrphea"]                = "Orphea",
            ["HeroProbius"]               = "Probius",
            ["HeroNexusHunter"]           = "NexusHunter",
            ["HeroRagnaros"]              = "Ragnaros",
            ["HeroRaynor"]                = "Raynor",
            ["HeroRehgar"]                = "Rehgar",
            ["HeroRexxar"]                = "Rexxar",
            ["HeroSamuro"]                = "Samuro",
            ["HeroSgtHammer"]             = "SgtHammer",
            ["HeroBarbarian"]             = "Barbarian",
            ["HeroStitches"]              = "Stitches",
            ["HeroStukov"]                = "Stukov",
            ["HeroSylvanas"]              = "Sylvanas",
            ["HeroTassadar"]              = "Tassadar",
            ["HeroButcher"]               = "Butcher",
            ["HeroLostVikings"]           = "LostVikings",
            ["HeroLostVikingsController"] = "LostVikings",
            ["HeroThrall"]                = "Thrall",
            ["HeroTracer"]                = "Tracer",
            ["HeroTychus"]                = "Tychus",
            ["HeroTyrael"]                = "Tyrael",
            ["HeroTyrande"]               = "Tyrande",
            ["HeroUther"]                 = "Uther",
            ["HeroValeera"]               = "Valeera",
            ["HeroDemonHunter"]           = "DemonHunter",
            ["HeroVarian"]                = "Varian",
            ["HeroWhitemane"]             = "Whitemane",
            ["HeroNecromancer"]           = "Necromancer",
            ["HeroYrel"]                  = "Yrel",
            ["HeroZagara"]                = "Zagara",
            ["HeroZarya"]                 = "Zarya",
            ["HeroZeratul"]               = "Zeratul",
            ["HeroZuljin"]                = "Zuljin"
        };
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

            hotsEmoticons = JsonSerializer.Deserialize<HotsEmoticon>(Encoding.UTF8.GetString(Resources.HotsResources.emoticondata), jsonOptions);
            HotsEmoticonAliase? hotsEmoticonAliases = JsonSerializer.Deserialize<HotsEmoticonAliase>(Encoding.UTF8.GetString(Resources.HotsResources.emoticonsaliases), jsonOptions);

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
