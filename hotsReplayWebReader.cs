using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameStrings;
using Heroes.Icons.DataDocument;
using Heroes.Models;
using Heroes.StormReplayParser.Player;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class hotsReplayWebReader : Form
    {
        private Rectangle listBoxHotsReplaysOriginalRectangle;
        private Rectangle webViewOriginalRectangle;

        private string? hotsReplayFolder;
        internal static string jsonConfigFile = "HotsReplayReader.json";
        internal static string currentAccount = string.Empty;

        hotsReplay? hotsReplay;
        hotsTeam? redTeam;
        hotsTeam? blueTeam;
        private hotsPlayer[]? hotsPlayers;

        private string formTitle = "Hots Replay Reader";

        // Liste des replays avec leur index et leur chemin
        private Dictionary<int, string> replayList;

        // Écoute les notifications de modifications du système de fichiers
        private FileSystemWatcher FileSystemWatcher;

        internal string? htmlContent;
        internal string? replayVersion;

        internal HeroDataDocument? heroDataDocument;
        internal GameStringsRoot? gameStringsRoot;

        //string apiKey = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx:xx";
        string apiKey = "f67e8f89-a1e6-40d0-9f65-df409134342f:fx";

        DeepLTranslator translator;

        Init Init = new Init();
        public hotsReplayWebReader()
        {
            InitializeComponent();

            translator = new DeepLTranslator(apiKey);
            replayList = new Dictionary<int, string>();

            ToolStripMenuItem[] accountsToolStripMenu = new ToolStripMenuItem[Init.hotsLocalAccounts.Count];
            for (int i = 0; i < accountsToolStripMenu.Length; i++)
            {
                accountsToolStripMenu[i] = new ToolStripMenuItem();
                accountsToolStripMenu[i].Name = Init.hotsLocalAccounts[i].BattleTagName;
                accountsToolStripMenu[i].Tag = "Account";
                accountsToolStripMenu[i].Text = Init.hotsLocalAccounts[i].BattleTagName.Remove(Init.hotsLocalAccounts[i].BattleTagName.IndexOf(@"#"));
                accountsToolStripMenu[i].Click += new EventHandler(MenuItemClickHandler);
            }
            accountsToolStripMenuItem.DropDownItems.AddRange(accountsToolStripMenu);
        }
        private void InitFileWatcher(string path)
        {
            if (FileSystemWatcher != null)
            {
                // Arrête et libére l'ancien FileSystemWatcher
                FileSystemWatcher.EnableRaisingEvents = false;
                FileSystemWatcher.Created -= OnFileCreated;
                FileSystemWatcher.Dispose();
            }

            FileSystemWatcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            FileSystemWatcher.Created += OnFileCreated;
        }
        private async void HotsReplayWebReader_Load(object sender, EventArgs e)
        {
            listBoxHotsReplaysOriginalRectangle = new Rectangle(listBoxHotsReplays.Location.X, listBoxHotsReplays.Location.Y, listBoxHotsReplays.Width, listBoxHotsReplays.Height);
            webViewOriginalRectangle = new Rectangle(webViewOriginalRectangle.Location.X, webViewOriginalRectangle.Location.Y, webViewOriginalRectangle.Width, webViewOriginalRectangle.Height);

            if (Directory.Exists(Init.lastReplayFilePath))
            {
                ListHotsReplays(Init.lastReplayFilePath);
                this.Text = $"{formTitle} - {currentAccount}";
                this.Update();
            }

            await webView.EnsureCoreWebView2Async();

            Debug.WriteLine("WebView2 Runtime version: " + webView.CoreWebView2.Environment.BrowserVersionString);

            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Image);
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            string appAsetsFolder = @$"{Directory.GetCurrentDirectory()}";
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", appAsetsFolder, CoreWebView2HostResourceAccessKind.Allow);

            // Traite les messages de JavaScript vers C#
            webView.CoreWebView2.WebMessageReceived += async (sender, args) =>
            {
                var json = args.WebMessageAsJson;

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Vérifie si le message contient les propriétés "action" et "callbackId"
                if (root.TryGetProperty("action", out var actionElement) &&
                    root.TryGetProperty("callbackId", out var callbackIdElement))
                {
                    // Récupère les valeurs de "action" et "callbackId"
                    string action = actionElement.GetString();
                    string callbackId = callbackIdElement.GetString();

                    // Vérifie si l'action est "translate" et si le message contient "text"
                    if (action == "translate" && root.TryGetProperty("text", out var textElement))
                    {
                        // Récupère le texte à traduire
                        string inputText = textElement.GetString();
                        string translated = string.Empty;

                        try
                        {
                            translated = await translator.TranslateText(inputText, "EN");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Erreur : " + ex.Message);
                            Console.WriteLine("Erreur : " + ex.Message);
                        }

                        // Sérialise le texte traduit en JSON
                        string returnedText = JsonSerializer.Serialize(translated);

                        // Appelle le callback JavaScript puis nettoie
                        string script = $"window['{callbackId}']({returnedText}); delete window['{callbackId}'];";
                        await webView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                }
            };

            htmlContent = $@"<body style=""background: url(app://hotsResources/Welcome.jpg) no-repeat center center; background-size: cover; background-color: black; margin: 0; height: 100%;""></body>";

            // Bouton de test pour appeler la fonction translateWithCSharp
            /*
                        htmlContent = @"
            <script>
                function translateWithCSharp(text) {
                    const callbackId = ""cb_"" + Date.now();
                    window.chrome.webview.postMessage({
                        action: ""translate"",
                        callbackId: callbackId,
                        text: text
                    });
                    window[callbackId] = function(result) {
                        alert(result);
                    };
                }
            </script>

            <button onclick = ""translateWithCSharp('Bonjour le monde!')"" >Get a response from C#</button>
            ";
            */
            webView.NavigateToString(htmlContent);

        }
        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            Uri uri = new Uri(e.Request.Uri);

            // Vérifier si le schéma correspond à celui défini (ici "app")
            if (uri.Scheme == "app")
            {
                // Récupérer le nom du fichier (exemple : "MyImage.png")
                string fileName = System.IO.Path.GetFileName(uri.LocalPath);
                string imageName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string extension = System.IO.Path.GetExtension(fileName);

                // Récupérer l'image depuis les ressources (adapté selon vos ressources)
                Bitmap image = new hotsImage(uri.Host, imageName, extension).Bitmap;
                if (image != null)
                {
                    MemoryStream ms = new MemoryStream();
                    // Convertir l'image en MemoryStream
                    if (extension == ".png")
                    {
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;
                        e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/png");
                    }
                    else if (extension == ".jpg")
                    {
                        // Suppression du canal Alpha pour ne pas gérer la transparence
                        Bitmap newImage = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        using (Graphics g = Graphics.FromImage(newImage))
                        {
                            g.Clear(Color.White);
                            g.DrawImage(image, 0, 0, image.Width, image.Height);
                        }
                        newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Position = 0;
                        e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/jpeg");
                    }
                    else if (extension == ".gif")
                    {
                        // Handle GIF images
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                        ms.Position = 0;
                        e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/gif");
                    }
                }
            }
        }
        private void ResizeControl(Rectangle r, Control c, bool growWidth)
        {
            int newWidth;
            if (growWidth)
                newWidth = (int)(this.Width - 276); //384
            else
                newWidth = c.Width;
            int newHeight = (int)(this.Height - 63);
            c.Size = new Size(newWidth, newHeight);
        }
        private void HotsReplayWebReader_Resize(object sender, EventArgs e)
        {
            ResizeControl(listBoxHotsReplaysOriginalRectangle, listBoxHotsReplays, false);
            ResizeControl(webViewOriginalRectangle, webView, true);
        }
        private void MenuItemClickHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            if (clickedItem.Tag.ToString() == "Account")
                for (int i = 0; i < Init.hotsLocalAccounts.Count; i++)
                    if (Init.hotsLocalAccounts[i].BattleTagName == clickedItem.Name)
                    {
                        currentAccount = clickedItem.Name;
                        ListHotsReplays(Init.hotsLocalAccounts[i].FullPath);
                        string jsonFile;
                        jsonConfig? jsonConfig = new jsonConfig();
                        if (File.Exists($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}"))
                        {
                            jsonFile = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}");
                            jsonConfig = JsonSerializer.Deserialize<jsonConfig>(jsonFile);
                        }
                        jsonConfig.LastSelectedAccount = clickedItem.Name;
                        jsonConfig.LastSelectedAccountDirectory = Init.hotsLocalAccounts[i].FullPath;
                        File.WriteAllText($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}", JsonSerializer.Serialize(jsonConfig, new JsonSerializerOptions { WriteIndented = true }));
                    }
            this.Text = $"{formTitle} - {currentAccount}";
            this.Update();
        }
        private void ListHotsReplays(string path)
        {
            hotsReplayFolder = path;
            listBoxHotsReplays.Items.Clear();
            replayList.Clear();
            if (Directory.Exists(path))
            {
                // Initie l'observateur de fichiers
                InitFileWatcher(path);

                DirectoryInfo hotsReplayFolder = new(path);
                FileInfo[] replayFiles = hotsReplayFolder.GetFiles(@"*.StormReplay");
                Array.Reverse(replayFiles);
                string replayDisplayedName = string.Empty;
                int i = 0;
                foreach (FileInfo replayFile in replayFiles)
                {
                    replayDisplayedName = replayFile.Name.ToString().Replace(replayFile.Extension.ToString(), @"");
                    replayDisplayedName = Regex.Replace(replayDisplayedName, @"(\d{4})-(\d{2})-(\d{2}) (\d{2}).(\d{2}).(\d{2}) (.*)", "$3/$2/$1 $4:$5 $7");
                    listBoxHotsReplays.Items.Add(replayDisplayedName);

                    replayList.Add(i, replayFile.FullName);
                    i++;
                }

                // Attends que le composant webView2 soit chargé
                if (webView.CoreWebView2 != null)
                {
                    this.Invoke(new Action(() =>
                    {
                        listBoxHotsReplays.SelectedIndex = 0;
                    }));
                }
            }
        }
        internal string HTMLGetHeader()
        {
            string css = System.Text.Encoding.UTF8.GetString(hotsResources.styles);

            if (hotsReplay.stormReplay.Owner.IsWinner)
                css = css.Replace(@"#backColor#", @"#001100");
            else
                css = css.Replace(@"#backColor#", @"#110000");

            string html = $@"<html>
<head>
<style type=""text/css"">
{css}
</style>
<script>
  // Désactive le menu contextuel
  document.addEventListener('DOMContentLoaded', () => {{
    document.addEventListener('contextmenu', (e) => {{
      e.preventDefault()
    }})
  }})

  // Traduit le texte avec C#
  function translateWithCSharp(text) {{
    // Appelle la fonction C# pour traduire le texte
    return new Promise((resolve, reject) => {{
      // Crée un identifiant de rappel unique
      const callbackId = ""cb_"" + Date.now();
      // Envoie le message à C#
      window.chrome.webview.postMessage({{
        action: ""translate"",
        callbackId: callbackId,
        text: text
      }});
      // Définit la fonction de rappel pour traiter la réponse
      window[callbackId] = function(result) {{
        resolve(result);
      }};
    }});
  }}
</script>
</head>
<body>
<div class=""parentDiv"">
";
            return html;
        }
        internal string HTMLGetFooter()
        {
            string html = "\n<div>\n</body>\n</html>";
            return html;
        }
        internal string HTMLGetHeadTable()
        {
            string isBlueTeamWinner = blueTeam.isWinner ? " teamBestScoreBlue" : "";
            string isRedTeamWinner = redTeam.isWinner ? " teamBestScoreRed" : "";
            string winnerTeamClass = blueTeam.isWinner ? "titleBlueTeam" : "titleRedTeam";
            string html = $@"<table class=""headTable"">
  <tr>
    <td colSpan=""11"" class=""{winnerTeamClass}"" title=""{hotsReplay.stormReplay.ReplayVersion}"">{hotsReplay.stormReplay.MapInfo.MapName}</td>
  </tr>
  <tr>
    <td colspan=""5"" class=""titleBlueTeam{isBlueTeamWinner}"">Blue Team</td>
    <td></td>
    <td colspan=""5"" class=""titleRedTeam{isRedTeamWinner}"">Red Team</td>
  </tr>
  <tr>
";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetHeadTableCell(stormPlayer);

            html += "    <td width=\"100\"></td>\n";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetHeadTableCell(stormPlayer);

            string replayLength;
            if (hotsReplay.stormReplay.ReplayLength.Hours == 0)
                replayLength = $@"{hotsReplay.stormReplay.ReplayLength.ToString().Substring(3)}";
            else
                replayLength = $@"{hotsReplay.stormReplay.ReplayLength.ToString()}";
            string time = hotsReplay.stormReplay.ReplayLength.ToString();

            html += "  </tr>\n";

            if (hotsReplay.stormReplay.DraftPicks.Count > 0)
            {
                html += "  <tr>\n    <td>&nbsp;</td>\n";
                foreach (Heroes.StormReplayParser.Replay.StormDraftPick draftPick in hotsReplay.stormReplay.DraftPicks)
                    if (draftPick.PickType == Heroes.StormReplayParser.Replay.StormDraftPickType.Banned && draftPick.Team == Heroes.StormReplayParser.Replay.StormTeam.Blue)
                        html += $"    <td class=\"headTableTd\"><img src=\"app://heroesIcon/{GetHeroNameFromHeroId(draftPick.HeroSelected)}.png\" class=\"heroIcon\">\n";
                html += "    <td colSpan=\"3\" class=\"titleWhite\" style=\"zoom: 50%;\">Bans</td>\n";
                foreach (Heroes.StormReplayParser.Replay.StormDraftPick draftPick in hotsReplay.stormReplay.DraftPicks)
                    if (draftPick.PickType == Heroes.StormReplayParser.Replay.StormDraftPickType.Banned && draftPick.Team == Heroes.StormReplayParser.Replay.StormTeam.Red)
                        html += $"    <td class=\"headTableTd\"><img src=\"app://heroesIcon/{GetHeroNameFromHeroId(draftPick.HeroSelected)}.png\" class=\"heroIcon\">\n";
                html += "    <td>&nbsp;</td>\n  <tr>\n";
            }

            html += $@"  <tr>
    <td>&nbsp;</td>
    <td colSpan=""3"" class=""titleBlueTeam"" style=""zoom: 50%;"">Kills<br />{blueTeam.totalKills}</td>
    <td colSpan=""3"" class=""titleWhite"" style=""zoom: 50%;"">Game Length<br />{replayLength}</td>
    <td colSpan=""3"" class=""titleRedTeam"" style=""zoom: 50%;"">Kills<br />{redTeam.totalKills}</td>
    <td>&nbsp;</td>
  </tr>
</table>
";
            return html;
        }
        internal string HTMLGetHeadTableCell(StormPlayer stormPlayer)
        {
            string playerName;
            playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            string html = $"    <td class=\"headTableTd\"><span class=\"tooltip\"><img src=\"app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png\" class=\"heroIcon";
            html += $" heroIconTeam{GetParty(stormPlayer.BattleTagName)}";
            html += $"\" /><span class=\"tooltipHero\">BattleTag: {stormPlayer.BattleTagName}<br />AccountLevel: {stormPlayer.AccountLevel}</span></span><div class=\"battleTag\">{playerName}</div></td>\n";
            return html;
        }
        internal string HTMLGetChatMessages()
        {
            List<hotsMessage> hotsMessages = new List<hotsMessage>();
            foreach (Heroes.StormReplayParser.MessageEvent.IStormMessage chatMessage in hotsReplay.stormReplay.ChatMessages)
            {
                string msg = HTMLGetChatMessageEmoticon(((Heroes.StormReplayParser.MessageEvent.ChatMessage)chatMessage).Text);
                hotsMessages.Add(new hotsMessage(GetHotsPlayer(chatMessage.MessageSender.BattleTagName), chatMessage.Timestamp, msg));
            }
            foreach (hotsPlayer hotsPlayer in hotsPlayers)
            {
                foreach (PlayerDisconnect playerDisconnect in hotsPlayer.playerDisconnects)
                {
                    hotsMessages.Add(new hotsMessage(hotsPlayer, playerDisconnect.From, "<span class=\"disconnected\">Disconnected</span>", false));
                    if (playerDisconnect.To != null)
                        hotsMessages.Add(new hotsMessage(hotsPlayer, playerDisconnect.To.Value, "<span class=\"reconnected\">Reconnected</span>", false));
                }
            }
            hotsMessages = hotsMessages.OrderBy(o => o.TotalMilliseconds).ToList();

            bool lastMessageAfterAnHour = hotsMessages.Count > 0 && Int32.Parse(hotsMessages.Last().Hours) > 0 ? true : false;

            string html = $@"";
            html += "<div class=\"chat-container\">\n";
            foreach (hotsMessage hotsMessage in hotsMessages)
            {
                html += HTMLGetChatMessage(hotsMessage, lastMessageAfterAnHour);
            }
            html += "</div>\n";

            html += @"<script>
  // Selectionne tous les elements avec la classe chat-message et ajoute un evenement de clic
  document.querySelectorAll("".chat-message"").forEach(function (element) {
    element.addEventListener(""click"", function () {
      // Récupère le texte du span avec la classe chat-message-corps
      const span = element.querySelector("".chat-message-corps"");
      const currentText = span.textContent;
      // Appelle la fonction translateWithCSharp pour traduire le texte
      translateWithCSharp(currentText)
        // Attends la réponse de la fonction
        .then(translated => {
          // Met à jour le texte du span avec le texte traduit
          span.textContent = translated;
        })
    });
  });
</script>";
            return $"{html}\n";
        }
        internal string HTMLGetChatMessage(hotsMessage hotsMessage, bool lastMessageAfterAnHour)
        {
            string msgHours = hotsMessage.Hours;
            string msgMinutes = hotsMessage.Minutes;
            string msgSeconds = hotsMessage.Seconds;
            string msgSenderName = hotsMessage.HotsPlayer.Name;

            int? msgSenderAccountLevel = hotsMessage.HotsPlayer.AccountLevel;
            string msgBattleTagName = hotsMessage.HotsPlayer.BattleTagName;
            string msgCharacter = (hotsMessage.HotsPlayer.PlayerHero.HeroName).Replace(" ", "&nbsp;");

            string html = "  <div class=\"chat-message\">\n";
            if (lastMessageAfterAnHour)
                html += $"    [{msgHours}:{msgMinutes}:{msgSeconds}]\n";
            else
                html += $"    [{msgMinutes}:{msgSeconds}]\n";
            html += $"    <span class=\"chat-user\"><img src=\"app://minimapicons/{hotsMessage.HotsPlayer.PlayerHero.HeroName}.png\" class=\"chat-image\" title=\"{hotsMessage.HotsPlayer.PlayerHero.HeroName}\"/>\n";
            html += $"    <span class=\"team{hotsMessage.HotsPlayer.Party}\">{msgSenderName}: </span>\n";
            if (hotsMessage.translate)
                html += $"    <span class=\"chat-message-corps\">{hotsMessage.Message}</span><img class=\"translate-icon\" style=\"float: right\" src=\"app://hotsResources/translate.png\" height=\"24\" /></span>\n";
            else
                html += $"    {hotsMessage.Message}\n";
            html += $"  </div>\n";
            return html;
        }
        internal string GetEmoticonImgFromTag(string tag)
        {
            foreach (KeyValuePair<string, hotsEmoticonData> hotsEmoticonData in Init.hotsEmoticons)
            {
                foreach (string alias in hotsEmoticonData.Value.aliases)
                {
                    if (tag == alias)
                    {
                        if (hotsEmoticonData.Value.image.Contains("storm_emoji_nexus"))
                            return $@"<img src=""app://emoticons/{hotsEmoticonData.Value.image}"" class=""chat-image"" title=""{hotsEmoticonData.Value.aliases[0]}"" />";
                        else
                            return $@"<img src=""app://emoticons/{hotsEmoticonData.Value.image}"" class=""chat-image chat-image-emoticon"" title=""{hotsEmoticonData.Value.aliases[0]}"" />";
                    }
                }
            }
            return tag;
        }
        internal string HTMLGetChatMessageEmoticon(string chatMessage)
        {
            string pattern = @"(:\w+:)";
            return Regex.Replace(chatMessage, pattern, match =>
            {
                string emoticonTag = match.Groups[1].Value;
                return GetEmoticonImgFromTag(emoticonTag);
            });
        }
        private string HTMLGetScoreTable()
        {
            string html = @$"
<table class=""tableScore"">
  <tr class=""teamHeader"">
    <td>&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;Kills&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;Takedown&nbsp;</td>
    <td>&nbsp;&nbsp;Deaths&nbsp;</td>
    <td>Time<br />&nbsp;Spent&nbsp;<br />Dead</td>
    <td>Siege Dmge</td>
    <td>&nbsp;Hero Dmg&nbsp;</td>
    <td>&nbsp;Healing&nbsp;&nbsp;</td>
    <td>Dmg Taken&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;Exp&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>MVP Score</td>
  </tr>
";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetScoreTr(stormPlayer, blueTeam, GetParty(stormPlayer.BattleTagName));
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetScoreTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));

            html += "</table>\n";
            return html;
        }
        private string HTMLGetScoreTr(StormPlayer stormPlayer, hotsTeam team, string partyColor)
        {
            string timeSpentDead = "&nbsp;";
            if (stormPlayer.ScoreResult.Deaths > 0)
            {
                if (stormPlayer.ScoreResult.TimeSpentDead.Hours == 0)
                    timeSpentDead = $@"{stormPlayer.ScoreResult.TimeSpentDead.ToString().Substring(3)}";
                else
                    timeSpentDead = $@"{stormPlayer.ScoreResult.TimeSpentDead.ToString()}";
            }

            string playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            string html = @"";
            html += $"  <tr class=\"team{team.Name}\">\n";
            html += $"    <td><img class=\"scoreIcon\" src=\"app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png\" /></td>\n";
            html += $"    <td class=\"tdPlayerName {team.Name} team{partyColor}\">{stormPlayer.PlayerHero.HeroName}<br /><font size=\"-1\">{playerName}</font></td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.SoloKills == team.maxKills)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.SoloKills}</td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.Takedowns == team.maxTakedowns)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.Takedowns}</td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.Deaths == team.maxDeaths)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.Deaths}</td>\n";

            html += $"    <td>{timeSpentDead}</td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.SiegeDamage == team.maxSiegeDmg)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.SiegeDamage:n0}</td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.HeroDamage == team.maxHeroDmg)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.HeroDamage:n0}</td>\n";

            html += "    <td";
            if ((stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing) == team.maxHealing)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing:n0}</td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.DamageTaken == team.maxDmgTaken)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.DamageTaken:n0}</td>\n";

            html += "    <td";
            if (stormPlayer.ScoreResult.ExperienceContribution == team.maxExp)
                html += " class=\"teamBestScore\"";
            html += $">{stormPlayer.ScoreResult.ExperienceContribution:n0}</td>\n";

            html += "    <td";
            if (stormPlayer.MatchAwardsCount > 0)
                if (stormPlayer.MatchAwards[0].ToString() == "MVP")
                    html += " class=\"teamBestScore\"";
            html += $">{Math.Round(GetHotsPlayer(stormPlayer.BattleTagName).mvpScore, 2)}</td>\n";

            html += "  </tr>\n";
            return html;
        }
        private string HTMLGetTalentsTable()
        {
            string playerName;
            string html = @$"
<table class=""tableScore"">
  <tr class=""teamHeader"">
    <td>&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;4&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;7&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;10&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;13&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;16&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
    <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;20&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>
  </tr>
";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetTalentsTr(stormPlayer, blueTeam, GetParty(stormPlayer.BattleTagName));
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetTalentsTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));

            html += "</table>\n";
            return html;
        }
        private string HTMLGetTalentsTr(StormPlayer stormPlayer, hotsTeam team, string partyColor)
        {
            string heroUnitId = stormPlayer.PlayerHero.HeroUnitId;
            if (heroUnitId == "HeroDVaPilot")
                heroUnitId = "HeroDVaMech";

            Hero heroData = heroDataDocument.GetHeroByUnitId(heroUnitId, true, true, true, true);

            string playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            string html = @"";
            html += $"  <tr class=\"team{team.Name}\">\n";
            html += $"    <td><img class=\"scoreIcon\" src=\"app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png\" /></td>\n";
            html += $"    <td class=\"tdPlayerName {team.Name} team{partyColor}\">{stormPlayer.PlayerHero.HeroName}<br /><font size=\"-1\">{playerName}</font></td>\n";
            for (int i = 0; i <= 6; i++)
            {
                if (i < stormPlayer.Talents.Count)
                    html += $"{GetTalentImgString(stormPlayer, heroData, i)}\n";
                else
                    html += "    <td>&nbsp;</td>\n";
            }
            html += "  </tr>\n";
            return html;
        }
        private string GetHeroNameFromHeroId(string heroId)
        {
            string HeroName;
            return HeroName = heroId switch
            {
                "Anubarak" => "Anub'arak",
                "Firebat" => "Blaze",
                "FaerieDragon" => "Brightwing",
                "Butcher" => "The Butcher",
                "Amazon" => "Cassia",
                "DVa" => "D.Va",
                "L90ETC" => "E.T.C.",
                "Tinker" => "Gazlowe",
                "Guldan" => "Gul'dan",
                "SgtHammer" => "Sgt. Hammer",
                "Crusader" => "Johanna",
                "Kaelthas" => "Kael'thas",
                "KelThuzad" => "Kel'Thuzad",
                "Monk" => "Kharazim",
                "LiLi" => "Li Li",
                "Wizard" => "Li-Ming",
                "LostVikings" => "The Lost Vikings",
                "Lucio" => "Lúcio",
                "Dryad" => "Lunara",
                "MalGanis" => "Mal'Ganis",
                "MeiOW" => "Mei",
                "Medic" => "Lt. Morales",
                "WitchDoctor" => "Nazeebo",
                "NexusHunter" => "Qhira",
                "Barbarian" => "Sonya",
                "DemonHunter" => "Valla",
                "Necromancer" => "Xul",
                "Zuljin" => "Zul'jin",
                "NONE" => "NONE",
                _ => heroId,
            };
        }
        private string GetTalentImgString(StormPlayer stormPlayer, Hero heroData, int i)
        {
            int tier = 0;
            switch (i)
            {
                case 0:
                    tier = 1;
                    break;
                case 1:
                    tier = 4;
                    break;
                case 2:
                    tier = 7;
                    break;
                case 3:
                    tier = 10;
                    break;
                case 4:
                    tier = 13;
                    break;
                case 5:
                    tier = 16;
                    break;
                case 6:
                    tier = 20;
                    break;
            }

            AbilTalentEntry AbilTalentEntry = GetAbilTalent(heroData, stormPlayer.Talents[i].TalentNameId);

            string iconPath = $@"app://abilityTalents/{AbilTalentEntry.IconFileName}";
            iconPath = iconPath.Replace("kel'thuzad", "kelthuzad");

            // Si la description est vide, on n'affiche pas le talent
            if (AbilTalentEntry.Full == null || AbilTalentEntry.Full == string.Empty)
                return "    <td>&nbsp;</td>";

            // Affiche le coût en mana si il y en a un
            if (AbilTalentEntry.Energy != null)
                AbilTalentEntry.Energy = Regex.Replace(AbilTalentEntry.Energy, @"<s\s+val=""(.*?)""[^>]*>(.*?)</s>", "<font color=\"#${1}\">${2}</font>");
            string abilityManaCost = AbilTalentEntry.Energy != null ? $"<br />\n            {AbilTalentEntry.Energy}" : "";
            // Affiche le cooldown si il y en a un
            string talentCooldown = AbilTalentEntry.Cooldown != null ? $"<br />\n            <font color=\"#bfd4fd\">{AbilTalentEntry.Cooldown}</font>" : "";

            // Suppression des balises <img> dans la description
            string description = Regex.Replace(AbilTalentEntry.Full, @"<img\s.*?\/>", string.Empty);

            // Remplace <c val="color">text</c> par du texte coloré
            description = Regex.Replace(description, @"<c\s+val=""(.*?)"">(.*?)</c>", "<font color=\"#${1}\">${2}</font>");

            description = Regex.Replace(description, @"\~\~([0-9.]+)\~\~(</font>)?",
                match =>
                {
                    // Conversion du nombre capturé
                    double value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    // Conversion en pourcentage (4% pour 0.04)
                    int percent = (int)Math.Round(value * 100);
                    // Mise en forme du texte final
                    string replacement = $" (<font color=\"#bfd4fd\">+{percent}%</font> per level)";
                    // Si la balise </font> était présente, la déplacer avant le texte remplacé
                    if (match.Groups[2].Success)
                        return $"{match.Groups[2].Value}{replacement}";
                    else
                        return replacement;
                });

            // Remplace <n/> par un saut de ligne <br />
            description = Regex.Replace(description, @"<n/>", "<br \\>");

            // Place le tooltip a gauche ou a droite de l'icône
            string toolTipPosition = tier > 10 ? "Left" : "Right";
            // Met une bordure sur les talents de niveau 10 et 20
            string imgTalentBorderClass;
            if (tier == 10 | tier == 20)
                imgTalentBorderClass = "imgTalent10Border";
            else
                imgTalentBorderClass = "imgTalentBorder";
            return @$"    <td>
      <div class=""tooltip"">
        <img src=""{iconPath}"" class=""heroTalentIcon {imgTalentBorderClass}"" />
        <span class=""tooltiptext tooltiptext{toolTipPosition}"">
          <font color=""White"">
            <b>{AbilTalentEntry.Name}</b>{abilityManaCost}{talentCooldown}
          </font>
          <br /><br />
          {description}
        </span>
      </div>
    </td>";
        }
        private AbilTalentEntry GetAbilTalent(Hero heroData, string TalentNameId)
        {
            AbilTalentEntry abilTalentEntry = new AbilTalentEntry();

            bool MatchPrefix(string key) => key.Split('|')[0].Equals(TalentNameId, StringComparison.OrdinalIgnoreCase);
            abilTalentEntry.HeroId = heroData.CHeroId;
            abilTalentEntry.AbilityId = TalentNameId;
            abilTalentEntry.IconFileName = heroData.Talents.FirstOrDefault(t => t.AbilityTalentId.ToString().Split('|')[0].Equals(TalentNameId, StringComparison.OrdinalIgnoreCase)).IconFileName;
            abilTalentEntry.Cooldown = gameStringsRoot.Gamestrings.AbilTalent.Cooldown?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Energy = gameStringsRoot.Gamestrings.AbilTalent.Energy?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Full = gameStringsRoot.Gamestrings.AbilTalent.Full?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Life = gameStringsRoot.Gamestrings.AbilTalent.Life?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Name = gameStringsRoot.Gamestrings.AbilTalent.Name?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Short = gameStringsRoot.Gamestrings.AbilTalent.Short?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;

            return abilTalentEntry;
        }
        private string GetParty(string playerBattleTag)
        {
            foreach (hotsPlayer hotsPlayer in hotsPlayers)
            {
                if (hotsPlayer.BattleTagName == playerBattleTag)
                {
                    return hotsPlayer.Party;
                }
            }
            return "0";
        }
        private hotsPlayer GetHotsPlayer(string playerBattleTag)
        {
            foreach (hotsPlayer hotsPlayer in hotsPlayers)
            {
                if (hotsPlayer.BattleTagName == playerBattleTag)
                {
                    return hotsPlayer;
                }
            }
            return null;
        }
        private void InitTeamDatas(hotsTeam team)
        {
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
            {
                if (stormPlayer.Team.ToString() == team.Name)
                {
                    if (stormPlayer.ScoreResult.SoloKills >= team.maxKills)
                        team.maxKills = stormPlayer.ScoreResult.SoloKills;
                    if (stormPlayer.ScoreResult.Takedowns >= team.maxTakedowns)
                        team.maxTakedowns = stormPlayer.ScoreResult.Takedowns;
                    if (stormPlayer.ScoreResult.Deaths <= team.maxDeaths)
                        team.maxDeaths = stormPlayer.ScoreResult.Deaths;
                    if (stormPlayer.ScoreResult.SiegeDamage >= team.maxSiegeDmg)
                        team.maxSiegeDmg = stormPlayer.ScoreResult.SiegeDamage;
                    if (stormPlayer.ScoreResult.HeroDamage >= team.maxHeroDmg)
                        team.maxHeroDmg = stormPlayer.ScoreResult.HeroDamage;
                    if (stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing >= team.maxHealing)
                        team.maxHealing = stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing;
                    if (stormPlayer.ScoreResult.DamageTaken >= team.maxDmgTaken)
                        team.maxDmgTaken = stormPlayer.ScoreResult.DamageTaken;
                    if (stormPlayer.ScoreResult.ExperienceContribution >= team.maxExp)
                        team.maxExp = stormPlayer.ScoreResult.ExperienceContribution;
                    team.totalDeath += stormPlayer.ScoreResult.Deaths;
                    team.totalKills += stormPlayer.ScoreResult.SoloKills;
                }
            }
            if (team.Name == hotsReplay.stormReplay.WinningTeam.ToString())
                team.isWinner = true;
        }
        private void InitPlayersData()
        {
            long? opponentsFirstParty = null;
            hotsPlayers = null;
            hotsPlayers = new hotsPlayer[10];
            int i = 0;
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                {
                    InitPlayerData(stormPlayer, ref opponentsFirstParty, i);
                    i++;
                }
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                {
                    InitPlayerData(stormPlayer, ref opponentsFirstParty, i);
                    i++;
                }

            i = 0;
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
            {
                //                if (stormPlayer.PlayerHero.HeroName == "Alarak")
                //                    MessageBox.Show("Alarak");
                foreach (hotsPlayer hotsPlayer in hotsPlayers)
                {
                    if (hotsPlayer.BattleTagName == stormPlayer.BattleTagName)
                    {
                        // Kills
                        hotsPlayer.mvpScore = stormPlayer.ScoreResult.SoloKills;

                        // Assists
                        if (stormPlayer.PlayerHero.HeroUnitId == "HeroLostVikingsController" || stormPlayer.PlayerHero.HeroUnitId == "HeroAbathur" || stormPlayer.PlayerHero.HeroUnitId == "HeroDVaPilot")
                            hotsPlayer.mvpScore += stormPlayer.ScoreResult.Takedowns * 0.75;
                        else
                            hotsPlayer.mvpScore += stormPlayer.ScoreResult.Takedowns;

                        // Deaths
                        double timeSpentDead = (stormPlayer.ScoreResult.TimeSpentDead.TotalSeconds / hotsReplay.stormReplay.ReplayLength.TotalSeconds) * 100;
                        if (stormPlayer.PlayerHero.HeroUnitId == "HeroMurky" || stormPlayer.PlayerHero.HeroUnitId == "HeroGall")
                            hotsPlayer.mvpScore += (timeSpentDead * -1);
                        else if (stormPlayer.PlayerHero.HeroUnitId == "HeroCho")
                            hotsPlayer.mvpScore += (timeSpentDead * -0.85);
                        else
                            hotsPlayer.mvpScore += (timeSpentDead * -0.5);

                        // Hero Damage
                        if (stormPlayer.ScoreResult.HeroDamage == hotsPlayer.playerTeam.maxHeroDmg)
                        {
                            hotsPlayer.mvpScore += 1;
                            if (stormPlayer.ScoreResult.HeroDamage >= hotsPlayer.enemyTeam.maxHeroDmg)
                                hotsPlayer.mvpScore += 1;
                        }

                        // Siege Damage
                        if (stormPlayer.ScoreResult.SiegeDamage == hotsPlayer.playerTeam.maxSiegeDmg)
                        {
                            hotsPlayer.mvpScore += 1;
                            if (stormPlayer.ScoreResult.SiegeDamage >= hotsPlayer.enemyTeam.maxSiegeDmg)
                                hotsPlayer.mvpScore += 1;
                        }

                        // Healing
                        if (stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing == hotsPlayer.playerTeam.maxHealing)
                        {
                            hotsPlayer.mvpScore += 1;
                            if (stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing >= hotsPlayer.enemyTeam.maxHealing)
                                hotsPlayer.mvpScore += 1;
                        }

                        // XP Contribution
                        if (stormPlayer.ScoreResult.ExperienceContribution == hotsPlayer.playerTeam.maxExp)
                        {
                            hotsPlayer.mvpScore += 1;
                            if (stormPlayer.ScoreResult.ExperienceContribution >= hotsPlayer.enemyTeam.maxExp)
                                hotsPlayer.mvpScore += 1;
                        }

                        if (GetHeroIdRole(stormPlayer.PlayerHero.HeroUnitId) == "Tank" || GetHeroIdRole(stormPlayer.PlayerHero.HeroUnitId) == "Bruiser")
                        {
                            if (stormPlayer.ScoreResult.DamageTaken == hotsPlayer.playerTeam.maxDmgTaken)
                            {
                                hotsPlayer.mvpScore += 0.5;
                                if (stormPlayer.ScoreResult.DamageTaken >= hotsPlayer.enemyTeam.maxDmgTaken)
                                    hotsPlayer.mvpScore += 1;
                            }
                        }
                    }
                }
                i++;
            }
        }
        private void InitPlayerData(StormPlayer stormPlayer, ref long? opponentsFirstParty, int id)
        {
            string playerName;
            hotsPlayers[id] = new hotsPlayer(stormPlayer);
            hotsPlayers[id].Party = "0";
            hotsPlayers[id].teamColor = stormPlayer.Team.ToString();

            if (hotsPlayers[id].teamColor == "Blue")
            {
                hotsPlayers[id].playerTeam = blueTeam;
                hotsPlayers[id].enemyTeam = redTeam;
            }
            else
            {
                hotsPlayers[id].playerTeam = redTeam;
                hotsPlayers[id].enemyTeam = blueTeam;
            }

            if (stormPlayer.Team == hotsReplay.stormReplay.Owner.Team)
            {
                if (hotsReplay.stormReplay.Owner.BattleTagName == stormPlayer.BattleTagName)
                {
                    hotsPlayers[id].Party = "1";
                }
                else if ((stormPlayer.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (stormPlayer.PartyValue != null))
                {
                    hotsPlayers[id].Party = "1";
                }
                else if (stormPlayer.PartyValue != null)
                {
                    hotsPlayers[id].Party = "2";
                }
            }
            else
            {
                if ((stormPlayer.PartyValue != null) && ((opponentsFirstParty == null) || (opponentsFirstParty == stormPlayer.PartyValue)))
                {
                    opponentsFirstParty = stormPlayer.PartyValue;
                    hotsPlayers[id].Party = "3";
                }
                else if ((stormPlayer.PartyValue != null) && (opponentsFirstParty != stormPlayer.PartyValue))
                {
                    hotsPlayers[id].Party = "4";
                }
            }
        }
        public string GetHeroIdRole(string HeroId)
        {
            List<string> Tanks = new List<string>(new string[] {
                "HeroAnubarak", // Anub'arak
                "HeroArthas", // Arthas
                "HeroFirebat", // Blaze
                "HeroCho", // Cho
                "HeroDiablo", // Diablo
                "HeroL90ETC", // E.T.C.
                "HeroGarrosh", // Garrosh
                "HeroCrusader", // Johanna
                "HeroMalGanis", // Mal'Ganis
                "HeroMeiOW", // Mei
                "HeroMuradin", // Muradin
                "HeroStitches", // Stitches
                "HeroTyrael" // Tyrael
            });
            List<string> Bruisers = new List<string>(new string[] {
                "HeroArtanis", // Artanis
                "HeroChen", // Chen
                "HeroDeathwing", // Deathwing
                "DeathwingDragonflightUnit", // Deathwing
                "HeroDehaka", // Dehaka
                "HeroDVaPilot", // D.Va
                "HeroTinker", // Gazlowe
                "HeroHogger", // Hogger
                "HeroImperius", // Imperius
                "HeroLeoric", // Leoric
                "HeroMalthael", // Malthael
                "HeroRagnaros", // Ragnaros
                "HeroRexxar", // Rexxar
                "HeroBarbarian", // Sonya
                "HeroThrall", // Thrall
                "HeroVarian", // Varian
                "HeroNecromancer", // Xul
                "HeroYrel" // Yrel
            });
            List<string> Rangeds = new List<string>(new string[] {
                "HeroAzmodan", // Azmodan
                "HeroAmazon", // Cassia
                "HeroChromie", // Chromie
                "HeroFalstad", // Falstad
                "HeroFenix", // Fenix
                "HeroGall", // Cho'gall
                "HeroGenji", // Genji
                "HeroGreymane", // Greymane
                "HeroGuldan", // Gul'dan
                "HeroHanzo", // Hanzo
                "HeroJaina", // Jaina
                "HeroJunkrat", // Junkrat
                "HeroKaelthas", // Kael'thas
                "HeroKelThuzad", // Kel'Thuzad
                "HeroWizard", // Li-Ming
                "HeroDryad", // Lunara
                "HeroMephisto", // Mephisto
                "HeroWitchDoctor", // Nazeebo
                "HeroNova", // Nova
                "HeroOrphea", // Orphea
                "HeroProbius", // Probius
                "HeroRaynor", // Raynor
                "HeroSgtHammer", // Sgt. Hammer
                "HeroSylvanas", // Sylvanas 
                "HeroTassadar", // Tassadar
                "HeroTracer", // Tracer
                "HeroTychus", // Tychus
                "HeroDemonHunter", // Valla
                "HeroZagara", // Zagara
                "HeroZuljin" // Zul'jin
            });
            List<string> Melees = new List<string>(new string[] {
                "HeroAlarak", // Alarak
                "HeroIllidan", // Illidan
                "HeroKerrigan", // Kerrigan
                "HeroMaiev", // Maiev
                "HeroMurky", // Murky
                "HeroNexusHunter", // Qhira
                "HeroSamuro", // Samuro
                "HeroButcher", // The Butcher
                "HeroValeera", // Valeera
                "HeroZeratul" // Zeratul
            });
            List<string> Healers = new List<string>(new string[] {
                "HeroAlexstrasza", // Alexstrasza
                "HeroAlexstraszaDragon", // Alexstrasza
                "HeroAna", // Ana
                "HeroAnduin", // Anduin
                "HeroAuriel", // Auriel
                "HeroFaerieDragon", // Brightwing
                "HeroDeckard", // Deckard
                "HeroMonk", // Kharazim
                "HeroLiLi", // Li Li
                "HeroMedic", // Lt. Morales
                "HeroLucio", // Lucio
                "HeroMalfurion", // Malfurion
                "HeroRehgar", // Rehgar
                "HeroStukov", // Stukov
                "HeroTyrande", // Tyrande
                "HeroUther", // Uther
                "HeroWhitemane" // Whitemane
            });
            List<string> Supports = new List<string>(new string[] {
                "HeroAbathur", // Abathur
                "HeroMedivh", // Medivh
                "HeroMedivhRaven", // Medivh
                "HeroLostVikingsController", // The Lost Vikings
                "HeroZarya" // Zarya
            });
            if (Tanks.Contains(HeroId))
                return "Tank";
            else if (Bruisers.Contains(HeroId))
                return "Bruiser";
            else if (Rangeds.Contains(HeroId))
                return "Ranged";
            else if (Melees.Contains(HeroId))
                return "Melee";
            else if (Healers.Contains(HeroId))
                return "Healer";
            else if (Supports.Contains(HeroId))
                return "Support";
            else return "";
        }
        public static async Task<string?> FindVersionGitHubFolder(HttpClient httpClient, string replayVersion)
        {
            string replayVersionShort = replayVersion.Substring(replayVersion.LastIndexOf('.') + 1);
            string url = "https://api.github.com/repositories/214500273/contents/heroesdata";

            using var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                string name = item.GetProperty("name").GetString() ?? "";
                string type = item.GetProperty("type").GetString() ?? "";

                if (type == "dir" && name.EndsWith(replayVersionShort, StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            return null;
        }
        private async Task DownloadHeroesJsonFiles(HttpClient httpClient, string version)
        {
            // Correction de la version pour les replays par exemple : 2.55.10.94387 qui sont en fait des 2.55.11.94387
            string versionGitHubFolder = await FindVersionGitHubFolder(httpClient, version);

            string rootFolder = $@"{Init.dbDirectory}\{version}";

            string gitHubApiUrl = $@"https://api.github.com/repos/HeroesToolChest/heroes-data/contents/heroesdata/{versionGitHubFolder}";

            if (!Directory.Exists(Init.dbDirectory))
                Directory.CreateDirectory(Init.dbDirectory);

            if (!Directory.Exists(rootFolder))
                Directory.CreateDirectory(rootFolder);

            Debug.WriteLine($"Téléchargement des fichiers json des héros pour la version {version}...");
            Debug.WriteLine($"{gitHubApiUrl}");

            await DownloadGitHubFolderRecursive(httpClient, gitHubApiUrl, rootFolder);
        }
        private async Task DownloadGitHubFolderRecursive(HttpClient httpClient, string apiUrl, string localPath)
        {
            string json = await httpClient.GetStringAsync(apiUrl);
            var items = JsonSerializer.Deserialize<GitHubFileInfo[]>(json);

            foreach (var item in items)
            {
                if (item.type == "file")
                {
                    Console.WriteLine($"Téléchargement {item.path}...");
                    byte[] data = await httpClient.GetByteArrayAsync(item.download_url);
                    string filePath = Path.Combine(localPath, item.name);
                    await File.WriteAllBytesAsync(filePath, data);
                }
                else if (item.type == "dir")
                {
                    string newFolder = Path.Combine(localPath, item.name);
                    Directory.CreateDirectory(newFolder);

                    // récursif
                    await DownloadGitHubFolderRecursive(httpClient, item.url, newFolder);
                }
            }
        }
        private async Task CheckAndDownloadHeroesData(string replayVersion)
        {
            using HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    Assembly.GetExecutingAssembly().GetName().Name,
                    Assembly.GetExecutingAssembly().GetName().Version.ToString(2)
                )
            );

            // https://github.com/HeroesToolChest/heroes-data/tree/master/heroesdata
            // Téléchargement des json des héros si besoin
            if (!Directory.Exists($@"{Init.dbDirectory}\{replayVersion}"))
            {
                webView.CoreWebView2.NavigateToString($@"<center>T&eacute;l&eacute;chargement des fichiers json des h&eacute;ros pour la version {replayVersion}...</center>");
                await DownloadHeroesJsonFiles(HttpClient, replayVersion);
            }

            string heroDataJsonPath = Directory.GetFiles($@"{Init.dbDirectory}\{replayVersion}\data\", "herodata_*_localized.json").FirstOrDefault();
            string gameStringsJsonPath = Directory.GetFiles($@"{Init.dbDirectory}\{replayVersion}\gamestrings\", "gamestrings_*_enus.json").FirstOrDefault();

            heroDataDocument = HeroDataDocument.Parse(heroDataJsonPath);

            string gameStringsjson = File.ReadAllText(gameStringsJsonPath);
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
            gameStringsRoot = JsonSerializer.Deserialize<GameStringsRoot>(gameStringsjson, jsonOptions);
            Debug.WriteLine($"GameStrings loaded for version {gameStringsRoot.Meta.Version} - {gameStringsRoot.Meta.Locale}");
        }
        // Sélection d'un replay dans la liste
        private async void ListBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(listBoxHotsReplays.SelectedIndex.ToString());
            Debug.WriteLine(replayList[listBoxHotsReplays.SelectedIndex]);

            try
            {
                hotsReplay = new hotsReplay(replayList[listBoxHotsReplays.SelectedIndex]);
                if (hotsReplay.stormReplay != null)
                {
                    InitTeamDatas(redTeam = new hotsTeam("Red"));
                    InitTeamDatas(blueTeam = new hotsTeam("Blue"));
                    InitPlayersData();

                    await CheckAndDownloadHeroesData(hotsReplay.stormReplay.ReplayVersion.ToString());


                    htmlContent = $@"{HTMLGetHeader()}";
                    htmlContent += $"{HTMLGetHeadTable()}<br /><br />\n";
                    htmlContent += $@"{HTMLGetChatMessages()}<br /><br />";
                    htmlContent += $@"{HTMLGetScoreTable()}<br /><br />";
                    htmlContent += $@"{HTMLGetTalentsTable()}<br /><br />";
                    htmlContent += $@"{HTMLGetFooter()}";
                }
                else
                {
                    htmlContent = $@"<body style=""background: url(app://hotsResources/Welcome.jpg) no-repeat center center; background-size: cover; background-color: black; margin: 0; height: 100%;""></body>";
                }
            }
            catch (Exception exception)
            {
                htmlContent = $@"<body style=""background: url(app://hotsResources/Welcome.jpg) no-repeat center center; background-size: cover; background-color: black; margin: 0; height: 100%;""></body>";
            }
            webView.CoreWebView2.NavigateToString(htmlContent);
        }
        private void BrowseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jsonConfig? jsonConfig = new jsonConfig();
            string jsonFile;
            if (File.Exists($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}"))
            {
                jsonFile = File.ReadAllText($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}");
                jsonConfig = JsonSerializer.Deserialize<jsonConfig>(jsonFile);
                if (Directory.Exists(jsonConfig?.LastBrowseDirectory))
                    folderBrowserDialog.InitialDirectory = jsonConfig.LastBrowseDirectory;
                else
                    folderBrowserDialog.InitialDirectory = hotsReplayFolder;
            }
            else
                folderBrowserDialog.InitialDirectory = hotsReplayFolder;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                jsonConfig.LastBrowseDirectory = folderBrowserDialog.SelectedPath;
                File.WriteAllText($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}", JsonSerializer.Serialize(jsonConfig, new JsonSerializerOptions { WriteIndented = true }));

                hotsReplayFolder = folderBrowserDialog.SelectedPath;
                ListHotsReplays(hotsReplayFolder);
            }
        }
        public string GetNotepadPath()
        {
            string NotepadPPPath = string.Empty;
            using (RegistryKey RegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Notepad++"))
            {
                if (RegKey != null)
                {
                    object value = RegKey.GetValue("");
                    if (value != null)
                    {
                        NotepadPPPath = value.ToString();
                    }
                }
            }
            if (File.Exists($"{NotepadPPPath}\\notepad++.exe"))
            {
                return $"{NotepadPPPath}\\notepad++.exe";
            }
            else
            {
                return "notepad.exe";
            }
        }
        private void SourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = @$"{Environment.GetEnvironmentVariable("TEMP")}\HotsReplayReader.html";
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            using (StreamWriter sw = System.IO.File.CreateText(path))
                sw.Write(htmlContent);

            Process.Start(GetNotepadPath(), path);
        }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HotsReplayReader.Program.ExitApp();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //Debug.WriteLine(keyData.ToString());
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            const int maxRetries = 100;
            const int millisecondsTimeout = 100;

            int retries = 0;
            bool ready = false;
            while (!ready && retries < maxRetries)
            {
                try
                {
                    using (FileStream fs = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        ready = true;
                    }
                }
                catch (IOException)
                {
                    retries++;
                    Thread.Sleep(millisecondsTimeout);
                }
            }
            if (ready)
            {
                this.Invoke(new Action(() =>
                {
                    ListHotsReplays(Path.GetDirectoryName(e.FullPath));
                }));
            }
        }
    }
}
