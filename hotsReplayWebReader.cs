using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using System.Text.RegularExpressions;
using Heroes.StormReplayParser.Player;
using Microsoft.Web.WebView2.Core;

namespace HotsReplayReader
{
    public partial class hotsReplayWebReader : Form
    {
        private Rectangle originalHotsReplayWebReaderSize;
        private Rectangle listBoxHotsReplaysOriginalRectangle;
        private Rectangle webViewOriginalRectangle;

        private string? hotsReplayFolder;
        private string jsonConfigFile = "HotsReplayReader.json";

        hotsReplay? hotsReplay;
        hotsTeam? redTeam;
        hotsTeam? blueTeam;
        private hotsPlayer[]? hotsPlayers;

        internal string? htmlContent;

        Init Init = new Init();
        public hotsReplayWebReader()
        {
            InitializeComponent();

            ToolStripMenuItem[] accountsToolStipMenu = new ToolStripMenuItem[Init.hotsLocalAccounts.Count];
            for (int i = 0; i < accountsToolStipMenu.Length; i++)
            {
                accountsToolStipMenu[i] = new ToolStripMenuItem();
                accountsToolStipMenu[i].Name = Init.hotsLocalAccounts[i].BattleTagName;
                accountsToolStipMenu[i].Tag = "Account";
                accountsToolStipMenu[i].Text = Init.hotsLocalAccounts[i].BattleTagName.Remove(Init.hotsLocalAccounts[i].BattleTagName.IndexOf(@"#"));
                accountsToolStipMenu[i].Click += new EventHandler(MenuItemClickHandler);
            }
            accountsToolStripMenuItem.DropDownItems.AddRange(accountsToolStipMenu);
        }
        private async void hotsReplayWebReader_Load(object sender, EventArgs e)
        {
            originalHotsReplayWebReaderSize = new Rectangle(this.Location.X, this.Location.Y, this.Size.Width, this.Size.Height);
            listBoxHotsReplaysOriginalRectangle = new Rectangle(listBoxHotsReplays.Location.X, listBoxHotsReplays.Location.Y, listBoxHotsReplays.Width, listBoxHotsReplays.Height);
            webViewOriginalRectangle = new Rectangle(webViewOriginalRectangle.Location.X, webViewOriginalRectangle.Location.Y, webViewOriginalRectangle.Width, webViewOriginalRectangle.Height);

            if (Directory.Exists(Init.lastReplayFilePath))
                listHotsReplays(Init.lastReplayFilePath);
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Image);
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            string appAsetsFolder = @$"{Directory.GetCurrentDirectory()}";
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", appAsetsFolder, CoreWebView2HostResourceAccessKind.Allow);

            htmlContent = $@"<body style=""background-color: black; margin: 0;""><img style=""width: 100%; height: 100%;"" src=""app://hotsResources/Welcome.jpg"" /></body>";
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
                // On suppose que le nom de la ressource est le nom du fichier sans l'extension
                string imageName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string Extension = System.IO.Path.GetExtension(fileName);

                // Récupérer l'image depuis les ressources (adapté selon vos ressources)
                Bitmap image = new hotsImage(uri.Host, imageName).Bitmap;
                if (image != null)
                {
                    // Convertir l'image en MemoryStream
                    if (Extension == ".png")
                    {
                        MemoryStream ms = new MemoryStream();
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;
                        e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/png");
                    }
                    else if (Extension == ".jpg")
                    {
                        // Suppression du canal Alpha pour ne pas gérer la transparence
                        Bitmap newImage = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        using (Graphics g = Graphics.FromImage(newImage))
                        {
                            g.Clear(Color.White);
                            g.DrawImage(image, 0, 0, image.Width, image.Height);
                        }
                        MemoryStream ms = new MemoryStream();
                        newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Position = 0;
                        e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/jpeg");
                    }
                }
            }
        }
        private void resizeControl(Rectangle r, Control c, bool growWidth)
        {
            int newWidth;
            if (growWidth)
                newWidth = (int)(this.Width - 364); //384
            else
                newWidth = c.Width;
            int newHeight = (int)(this.Height - 63);
            c.Size = new Size(newWidth, newHeight);
        }
        private void hotsReplayWebReader_Resize(object sender, EventArgs e)
        {
            resizeControl(listBoxHotsReplaysOriginalRectangle, listBoxHotsReplays, false);
            resizeControl(webViewOriginalRectangle, webView, true);
        }
        private void MenuItemClickHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            if (clickedItem.Tag.ToString() == "Account")
                for (int i = 0; i < Init.hotsLocalAccounts.Count; i++)
                    if (Init.hotsLocalAccounts[i].BattleTagName == clickedItem.Name)
                        listHotsReplays(Init.hotsLocalAccounts[i].FullPath);
        }
        private void listHotsReplays(string path)
        {
            hotsReplayFolder = path;
            listBoxHotsReplays.Items.Clear();
            if (Directory.Exists(path))
            {
                DirectoryInfo hotsReplayFolder = new(path);
                FileInfo[] replayFiles = hotsReplayFolder.GetFiles(@"*.StormReplay");
                Array.Reverse(replayFiles);
                foreach (FileInfo replayFile in replayFiles)
                {
                    listBoxHotsReplays.Items.Add(@replayFile.Name.ToString().Replace(@replayFile.Extension.ToString(), @""));
                }
            }
        }
        internal string HTMLGetHeader()
        {
            string contenu = System.Text.Encoding.UTF8.GetString(hotsResources.styles);

            if (hotsReplay.stormReplay.Owner.IsWinner)
                contenu = contenu.Replace(@"#backColor#", @"#001100");
            else
                contenu = contenu.Replace(@"#backColor#", @"#110000");

            string html = $@"
                <html>
                <head>
                {contenu}
                </head>
                <body>
                <p>&nbsp;</p>
                ";

            return html;
        }
        internal string HTMLGetFooter()
        {
            string html = $@"
            </body>
            </html>";
            return html;
        }
        internal string HTMLGetHeadTable()
        {
            string html = $@"<table class=""headTable"">
            <tr>
            <td colspan=""5"" class=""titleBlueTeam"">Blue Team</td>
            <td></td>
            <td colspan=""5"" class=""titleRedTeam"">Red Team</td>
            </tr>
            <tr>
            ";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetHeadTableCell(stormPlayer);

            html += $@"<td width=""100""></td>";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetHeadTableCell(stormPlayer);

            html += $@"
            </tr>
            </table>
            </body>
            </html>
            ";
            return html;
        }
        internal string HTMLGetHeadTableCell(StormPlayer stormPlayer)
        {
            string html = $@"";
            string playerName;
/*
            if (stormPlayer.PlayerHero.HeroName == "Varian")
                MessageBox.Show(stormPlayer.PlayerHero.HeroName);
*/
            playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            html += $@"<td class=""headTableTd""><img src=""app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png"" class='heroIcon";
            html += $@" heroIconTeam{getParty(stormPlayer.BattleTagName)}";

            html += $@"' /><div class=""battleTag"">{playerName}</div></td>
            ";
            return html;
        }
        internal string HTMLGetChatMessages()
        {
            List<hotsMessage> hotsMessages = new List<hotsMessage>();
            foreach (Heroes.StormReplayParser.MessageEvent.IStormMessage chatMessage in hotsReplay.stormReplay.ChatMessages)
                hotsMessages.Add(new hotsMessage(getHotsPlayer(chatMessage.MessageSender.BattleTagName), chatMessage.Timestamp, ((Heroes.StormReplayParser.MessageEvent.ChatMessage)chatMessage).Text));
            foreach (hotsPlayer hotsPlayer in hotsPlayers)
            {
                foreach (PlayerDisconnect playerDisconnect in hotsPlayer.playerDisconnects)
                {
                    hotsMessages.Add(new hotsMessage(hotsPlayer, playerDisconnect.From, "<span class='disconnected'>Disconnected</span>"));
                    if (playerDisconnect.To != null)
                        hotsMessages.Add(new hotsMessage(hotsPlayer, playerDisconnect.To.Value, "<span class='reconnected'>Reconnected</span>"));
                }
            }
            hotsMessages = hotsMessages.OrderBy(o => o.TotalMilliseconds).ToList();

            string html = $@"";
            html += $@"<div class=""chatMessages"">";
            html += $@"<table>";
            foreach (hotsMessage hotsMessage in hotsMessages)
            {
                html += HTMLGetChatMessage(hotsMessage);
            }
            html += $@"</table>";
            html += $@"</div>";
            return html;
        }
        internal string HTMLGetChatMessage(hotsMessage hotsMessage)
        {
            string html = $@"<tr>";
            string msgHours = hotsMessage.Hours;
            string msgMinutes = hotsMessage.Minutes;
            string msgSeconds = hotsMessage.Seconds;
            string msgMilliseconds = hotsMessage.MilliSeconds;
            string msgSenderName = hotsMessage.HotsPlayer.Name;

            int? msgSenderAccountLevel = hotsMessage.HotsPlayer.AccountLevel;
            string msgBattleTagName = hotsMessage.HotsPlayer.BattleTagName;
            string msgCharacter = (hotsMessage.HotsPlayer.PlayerHero.HeroName).Replace(" ", "&nbsp;");

            html += $@"<td class=""messages"">[" + msgHours + ":" + msgMinutes + ":" + msgSeconds + ":" + msgMilliseconds + "]&nbsp;&nbsp;</td>";
            html += $@"<td class=""messages""><b>";
            if (msgBattleTagName == hotsReplay.stormReplay.Owner.BattleTagName)
                html += $@"<class style=""color: crimson"">{msgSenderName}</class>";
            else if ((hotsMessage.HotsPlayer.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (hotsMessage.HotsPlayer.PartyValue != null))
                html += $@"<class style=""color: crimson"">{msgSenderName}</class>";
            else if (hotsMessage.HotsPlayer.PartyValue != null)
                html += $@"<class style=""color: deepskyblue"">{msgSenderName}</class>";
            else
                html += $@"<class style=""color: gainsboro"">{msgSenderName}</class>";
            html += $@"</b>";
            html += $@"&nbsp;({msgSenderAccountLevel})&nbsp;&nbsp;</td>";
            html += $@"<td class=""messages nonBreakingText""><nobr>{msgCharacter}</nobr>&nbsp;&nbsp;</td>";
            html += $@"<td width=""100%"" class=""messages"">{hotsMessage.Message}</td>";
            html += "</tr>\r\n";
            return html;
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
                <td>Siege Dmge</td>
                <td>&nbsp;Hero Dmg&nbsp;</td>
                <td>&nbsp;Healing&nbsp;&nbsp;</td>
                <td>Dmg Taken&nbsp;</td>
                <td>&nbsp;&nbsp;&nbsp;Exp&nbsp;&nbsp;&nbsp;&nbsp;</td>
                <td>MVP</td>
                </tr>
            ";

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetScoreTr(stormPlayer, blueTeam, getParty(stormPlayer.BattleTagName));
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetScoreTr(stormPlayer, redTeam, getParty(stormPlayer.BattleTagName));

            html += @$"</table>";
            return html;
        }
        private string HTMLGetScoreTr(StormPlayer stormPlayer, hotsTeam team, string partyColor)
        {
            string playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            string html = @"";
            html += @$"<tr class=""team{team.Name}"">";
            html += @$"<td><img class=""scoreIcon"" src=""app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png"" /></td>";
            html += @$"<td class=""tdPlayerName {team.Name} team{partyColor}"">{stormPlayer.PlayerHero.HeroName}<br /><font size=""-1"">{playerName}</font></td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.SoloKills == team.maxKills)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.SoloKills}</td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.Takedowns == team.maxTakedowns)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.Takedowns}</td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.Deaths == team.maxDeaths)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.Deaths}</td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.SiegeDamage == team.maxSiegeDmg)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.SiegeDamage:n0}</td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.HeroDamage == team.maxHeroDmg)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.HeroDamage:n0}</td>";

            html += @$"<td";
            if ((stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing) == team.maxHealing)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing:n0}</td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.DamageTaken == team.maxDmgTaken)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.DamageTaken:n0}</td>";

            html += @$"<td";
            if (stormPlayer.ScoreResult.ExperienceContribution == team.maxExp)
                html += $@" class = teamBestScore";
            html += @$">{stormPlayer.ScoreResult.ExperienceContribution:n0}</td>";

            html += @$"<td";
            if (stormPlayer.MatchAwardsCount > 0)
                if (stormPlayer.MatchAwards[0].ToString() == "MVP")
                    html += $@" class = teamBestScore";
            html += @$">{Math.Round(getHotsPlayer(stormPlayer.BattleTagName).mvpScore, 2)}</td>";

            html += "</tr>\r\n";
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
                <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1&nbsp;&nbsp;&nbsp;&nbsp;&nbsp</td>
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
                    html += HTMLGetTalentsTr(stormPlayer, blueTeam, getParty(stormPlayer.BattleTagName));
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetTalentsTr(stormPlayer, redTeam, getParty(stormPlayer.BattleTagName));

            html += @$"</table>";
            return html;
        }
        private string HTMLGetTalentsTr(StormPlayer stormPlayer, hotsTeam team, string partyColor)
        {
            hotsHero? hotsHero;
            string json;
            string resourceName = getHeroJsonFileName(stormPlayer.PlayerHero.HeroName);
            Type resourcesType = typeof(heroesJson);
            PropertyInfo propertyInfo = resourcesType.GetProperty(resourceName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            byte[] resourceData = (byte[])propertyInfo.GetValue(null, null);
            using (var stream = new MemoryStream(resourceData))
            using (var reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }

            hotsHero = JsonSerializer.Deserialize<hotsHero>(json);
            string playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            string html = @"";
            html += @$"<tr class=""team{team.Name}"">";
            html += @$"<td><img class=""scoreIcon"" src=""app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png"" /></td>";
            html += @$"<td class=""tdPlayerName {team.Name} team{partyColor}"">{stormPlayer.PlayerHero.HeroName}<br /><font size=""-1"">{playerName}</font></td>";
            for (int i = 0; i <= 6; i++)
            {
                if (i < stormPlayer.Talents.Count)
                    html += @$"{getTalentImgString(stormPlayer, hotsHero, i)}";
                else
                    html += @$"<td>&nbsp;</td>";
            }
            html += "</tr>\r\n";
            return html;
        }
        private string getHeroJsonFileName(string heroName)
        {
            heroName = heroName.ToLower();
            heroName = heroName.Replace(".", "");
            heroName = heroName.Replace("'", "");
            heroName = heroName.Replace("-", "");
            heroName = heroName.Replace(" ", "");
            heroName = heroName.Replace("cho", "chogall");
            heroName = heroName.Replace("lúcio", "lucio");
            heroName = heroName.Replace("thelostvikings", "lostvikings");
            return heroName;
        }
        private string getTalentImgString(StormPlayer stormPlayer, hotsHero hotsHero, int i)
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
            string iconPath = $@"https://appassets/images/abilitytalents/{getHotsHeroTalent(hotsHero, tier, stormPlayer.Talents[i].TalentNameId).icon}";
            iconPath = iconPath.Replace("kelthuzad", "kel'thuzad");
            iconPath = $@"app://abilityTalents/{getHotsHeroTalent(hotsHero, tier, stormPlayer.Talents[i].TalentNameId).icon}";
            string description = getHotsHeroTalent(hotsHero, tier, stormPlayer.Talents[i].TalentNameId).description;
            description = description.Replace("  ", "<br />");
            // Saute une ligne si il y a plusieurs quetes
            description = Regex.Replace(description, @". Quest:", "<br :>Quest:");
            // Colore les chiffres et les % en blanc
            description = Regex.Replace(description, @"([0-9]|%)", "<font color='White'>$1</font>");
            // Saute une ligne avant "Reward:"
            description = description.Replace("Reward:", "<br />Reward:");
            // Saute une ligne avant "Quest:" si elle n'est pas la premiere ligne de la descritpion.
            // Ex : Extended Lightning de Alarak
            description = Regex.Replace(description, @"(?<!^)(Quest:)", "<br />Quest:");
            // Colorie Quest et Reward en jaune
            description = Regex.Replace(description, @"(Quest:|Reward:)", "<font color='#D7BA3A'>$1</font>");

            string imgTalentBorderClass;
            if (tier == 10 | tier == 20)
                imgTalentBorderClass = "imgTalent10Border";
            else
                imgTalentBorderClass = "imgTalentBorder";
            return @$"
             <td>
              <div class=""tooltip"">
                <img src=""{iconPath}"" class=""heroTalentIcon {imgTalentBorderClass}"" />
                <span class=""tooltiptext"">
                  <b><font color='White'>{getHotsHeroTalent(hotsHero, tier, stormPlayer.Talents[i].TalentNameId).name}</font></b><br /><br />
                  {description}
                </span>
              </div>
             </td>";
        }
        private hotsHeroTalent getHotsHeroTalent(hotsHero hotsHero, int tier, string talentTreeId)
        {
            List<hotsHeroTalent> hotsHeroTalents;
            if (hotsHero.talents.TryGetValue(tier.ToString(), out hotsHeroTalents))
            {
                foreach (hotsHeroTalent HeroTalent in hotsHeroTalents)
                {
                    if (HeroTalent.talentTreeId == talentTreeId)
                        return HeroTalent;
                }
            }
            hotsHeroTalent hotsHeroTalent = new hotsHeroTalent();
            return hotsHeroTalent;
        }
        private string getParty(string playerBattleTag)
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
        private hotsPlayer getHotsPlayer(string playerBattleTag)
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
        private void initTeamDatas(hotsTeam team)
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
                }
            }
        }
        private void initPlayersData()
        {
            long? opponentsFirstParty = null;
            hotsPlayers = null;
            hotsPlayers = new hotsPlayer[10];
            int i = 0;
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                {
                    initPlayerData(stormPlayer, ref opponentsFirstParty, i);
                    i++;
                }
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                {
                    initPlayerData(stormPlayer, ref opponentsFirstParty, i);
                    i++;
                }

            i = 0;
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
            {
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

                        if (getHeroIdRole(stormPlayer.PlayerHero.HeroUnitId) == "Tank" || getHeroIdRole(stormPlayer.PlayerHero.HeroUnitId) == "Bruiser")
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
        private void initPlayerData(StormPlayer stormPlayer, ref long? opponentsFirstParty, int id)
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
        public string getHeroIdRole(string HeroId)
        {
            List<string> Tanks = new List<string>(new string[] {
                "HeroAnubarak",
                "HeroArthas",
                "HeroFirebat",
                "HeroCho",
                "HeroDiablo",
                "HeroL90ETC",
                "HeroGarrosh",
                "HeroCrusader",
                "HeroMalGanis",
                "HeroMeiOW",
                "HeroMuradin",
                "HeroStitches",
                "HeroTyrael"
            });
            List<string> Bruisers = new List<string>(new string[] {
                "HeroArtanis",
                "HeroChen",
                "HeroDeathwing",
                "DeathwingDragonflightUnit",
                "HeroDehaka",
                "HeroDVaPilot",
                "HeroTinker",
                "HeroHogger",
                "HeroImperius",
                "HeroLeoric",
                "HeroMalthael",
                "HeroRagnaros",
                "HeroRexxar",
                "HeroBarbarian",
                "HeroThrall",
                "HeroVarian",
                "HeroNecromancer",
                "HeroYrel"
            });
            List<string> Rangeds = new List<string>(new string[] {
                "HeroAzmodan",
                "HeroAmazon",
                "HeroChromie",
                "HeroFalstad",
                "HeroFenix",
                "HeroGall",
                "HeroGenji",
                "HeroGreymane",
                "HeroGuldan",
                "HeroHanzo",
                "HeroJaina",
                "HeroJunkrat",
                "HeroKaelthas",
                "HeroKelThuzad",
                "HeroWizard",
                "HeroDryad",
                "HeroMephisto",
                "HeroWitchDoctor",
                "HeroNova",
                "HeroOrphea",
                "HeroProbius",
                "HeroRaynor",
                "HeroSgtHammer",
                "HeroSylvanas",
                "HeroTassadar",
                "HeroTracer",
                "HeroTychus",
                "HeroDemonHunter",
                "HeroZagara",
                "HeroZuljin"
            });
            List<string> Melees = new List<string>(new string[] {
                "HeroAlarak",
                "HeroIllidan",
                "HeroKerrigan",
                "HeroMaiev",
                "HeroMurky",
                "HeroNexusHunter",
                "HeroSamuro",
                "HeroButcher",
                "HeroValeera",
                "HeroZeratul"
            });
            List<string> Healers = new List<string>(new string[] {
                "HeroAlexstrasza",
                "HeroAlexstraszaDragon",
                "HeroAna",
                "HeroAnduin",
                "HeroAuriel",
                "HeroFaerieDragon",
                "HeroDeckard",
                "HeroMonk",
                "HeroLiLi",
                "HeroMedic",
                "HeroLucio",
                "HeroMalfurion",
                "HeroRehgar",
                "HeroStukov",
                "HeroTyrande",
                "HeroUther",
                "HeroWhitemane"
            });
            List<string> Supports = new List<string>(new string[] {
                "HeroAbathur",
                "HeroMedivh",
                "HeroMedivhRaven",
                "HeroLostVikingsController",
                "HeroZarya"
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
        private void listBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            hotsReplay = new hotsReplay(hotsReplayFolder + "\\" + listBoxHotsReplays.Text + ".stormreplay");
            if (hotsReplay.stormReplay != null)
            {
                initTeamDatas(redTeam = new hotsTeam("Red"));
                initTeamDatas(blueTeam = new hotsTeam("Blue"));
                initPlayersData();
                htmlContent = $@"{HTMLGetHeader()}";
                htmlContent += $@"{HTMLGetHeadTable()}<br /><br />";
                htmlContent += $@"{HTMLGetChatMessages()}<br /><br />";
                htmlContent += $@"{HTMLGetScoreTable()}<br /><br />";
                htmlContent += $@"{HTMLGetTalentsTable()}<br /><br />";
                htmlContent += $@"{HTMLGetFooter()}";
            }
            else
            {
                htmlContent = $@"<body style=""background-color: black; margin: 0;""><img style=""width: 100%; height: 100%;"" src=""app://hotsResources/Welcome.jpg"" /></body>";
            }
            webView.CoreWebView2.NavigateToString(htmlContent);
        }
        private void browseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}"))
            {
                var json = System.IO.File.ReadAllText($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}");
                jsonConfig? jsonConfig = JsonSerializer.Deserialize<jsonConfig>(json);
                if (Directory.Exists(jsonConfig?.LastBrowseDirectory))
                    folderBrowserDialog.InitialDirectory = jsonConfig.LastBrowseDirectory;
                else
                    folderBrowserDialog.InitialDirectory = hotsReplayFolder;
            }
            else
                folderBrowserDialog.InitialDirectory = hotsReplayFolder;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText($@"{Directory.GetCurrentDirectory()}\{jsonConfigFile}", JsonSerializer.Serialize(new jsonConfig { LastBrowseDirectory = folderBrowserDialog.SelectedPath }));
                hotsReplayFolder = folderBrowserDialog.SelectedPath;
                listHotsReplays(hotsReplayFolder);
            }
        }
        private void sourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = @$"{Environment.GetEnvironmentVariable("TEMP")}\HotsReplayReader.html";
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            using (StreamWriter sw = System.IO.File.CreateText(path))
                sw.Write(htmlContent);
            Process.Start("notepad.exe", path);
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HotsReplayReader.Program.ExitApp();
        }
    }
}
