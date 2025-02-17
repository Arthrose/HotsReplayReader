using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Web.WebView2.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Player;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Printing;
using System.Security.Policy;
using System.Diagnostics;

namespace HotsReplayReader
{
    public partial class hotsReplayWebReader : Form
    {
        private Rectangle originalHotsReplayWebReaderSize;
        private Rectangle listBoxHotsReplaysOriginalRectangle;
        private Rectangle webViewOriginalRectangle;

        private string? hotsReplayFolder;

        hotsReplay hotsReplay;
        hotsTeam redTeam;
        hotsTeam blueTeam;
        hotsPlayer[] hotsplayers;

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
            //webView.CoreWebView2.NavigateToString(htmlContent);
            webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Image);
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            //htmlContent += $@"<body style=""background-color: black; margin: 0;""><img style=""width: 100%; height: 100%;"" src=""app://heroesIcon.local/Alarak.png"" /></body>";
            htmlContent = $@"<body style=""background-color: black; margin: 0;""><img style=""width: 100%; height: 100%;"" src=""app://hotsImages/Welcome.jpg"" /></body>";
            //htmlContent += $@"<body style=""background-color: black; margin: 0;""><img style=""width: 100%; height: 100%;"" src=""app://hotsImages/Kael'thas.png"" /></body>";
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
                string resourceName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string Extension = System.IO.Path.GetExtension(fileName);

                // Récupérer l'image depuis les ressources (adapté selon vos ressources)
                Bitmap image = new hotsImage(resourceName).Bitmap;
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
        private static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
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
                    listBoxHotsReplays.Items.Add(@replayFile.Name.ToString().Replace(@replayFile.Extension.ToString(), @""));
            }
        }
        private void browseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.InitialDirectory = hotsReplayFolder;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                hotsReplayFolder = folderBrowserDialog.SelectedPath;
                listHotsReplays(hotsReplayFolder);
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HotsReplayReader.Program.ExitApp();
        }
        internal string HTMLGetHeader()
        {
            string backColor = @"#0C0318";
            if (hotsReplay.stormReplay.Owner.IsWinner)
                backColor = @"#001100";
            else
                backColor = @"#110000";

            string html = $@"
                <html>
                <head>
                <style>
                body {{
                  color: white;
                  background-color: {backColor};
                  text-align: center;
                }}
                table {{
                  margin-left: auto;
                  margin-right: auto;
                }}
                tr {{
                  text-align: center;
                }}
				.headTableTd {{
                  padding: 5px;
				}}
                .titleBlueTeam {{
                  color: deepskyblue;
                  font-size: 300%;
                  font-weight: bold;
                  font-family: Consolas, Lucida Console, Courier New;
                }}
                .titleRedTeam {{
                  color: crimson;
                  font-size: 300%;
                  font-weight: bold;
                  font-family: Consolas, Lucida Console, Courier New;
                }}
                .heroIcon {{
                  /*border: 3px solid gainsboro;*/
                  box-shadow: 0px 0px 4px 4px gainsboro;
                  border-radius: 100%;
                  width: 80px;
                  height: 80px;
                }}
                .heroIcon:hover {{
                  filter: brightness(125%);
                }}
                .battleTag {{
                  font-size: 80%;
                  font-family: Consolas, Lucida Console, Courier New;
                  line-height: 250%;
                }}
                .heroIconTeam1 {{
                  box-shadow: 0px 0px 4px 4px crimson;
                }}
                .heroIconTeam2 {{
                  box-shadow: 0px 0px 4px 4px deepskyblue;
                }}
                .heroIconTeam3 {{
                  box-shadow: 0px 0px 4px 3px green;
                }}
                .heroIconTeam4 {{
                  box-shadow: 0px 0px 4px 4px goldenrod;
                }}
                .team1 {{
                  color: crimson;
                }}
                .team2 {{
                  color: deepskyblue;
                }}
                .team3 {{
                  color: green;
                }}
                .team4 {{
                  color: goldenrod;
                }}
                .teamHeader {{
                  background-color: #000000;
                }}
                .teamBlue {{
                  background-color: #17203D;
                }}
                .teamRed {{
                  background-color: #300F22;
                }}
                .tdPlayerName {{
                  text-align: left;
                  vertical-align: center;
                  font-family: Consolas, Lucida Console, Courier New;
                }}
                .chatMessages {{
                  background-color: #272A34;
                  margin-left: auto;
                  margin-right: auto;
                  width: 800px;
                  height: 300px;
                  overflow-y: scroll;
                }}
                .messages {{
                  text-align: left;
                  vertical-align: top;
                  font-family: Consolas, Lucida Console, Courier New;
                }}
                .nonBreakingText {{
                  white-space: nowrap;
                }}
                .tableScore {{
                  text-align: center;
                  vertical-align: center;
                  font-family: Consolas, Lucida Console, Courier New;
                }}
                .scoreIcon {{
                  width: 100px;
                  height: 50px;
                  object-fit: cover;
                  object-position: 100% 50;
                }}
                .teamBestScore {{
				  text-shadow: 2px 2px 15px lightBlue, 2px -2px 15px lightBlue, -2px -2px 15px lightBlue, -2px 2px 15px lightBlue;
                }}
                </style>
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
            string playerName;
            string html = $@"<table class=""headTable"">
            <tr>
            <td colspan=""5"" class=""titleBlueTeam"">Blue Team</td>
            <td></td>
            <td colspan=""5"" class=""titleRedTeam"">Red Team</td>
            </tr>
            <tr>
            ";
            long? opponentsFirstParty = null;
            hotsplayers = new hotsPlayer[10];
            int i = 0;

            foreach (stormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                {
                    html += HTMLGetHeadTableCell(stormPlayer, ref opponentsFirstParty, i);
                    i++;
                }

            html += $@"<td width=""100""></td>";

            foreach (stormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                {
                    html += HTMLGetHeadTableCell(stormPlayer, ref opponentsFirstParty, i);
                    i++;
                }

            html += $@"
            </tr>
            </table>
            </body>
            </html>
            ";
            return html;
        }
        internal string HTMLGetHeadTableCell(stormPlayer stormPlayer, ref long? opponentsFirstParty, int id)
        {
            string html = $@"";
            string playerName;

            hotsplayers[id] = new hotsPlayer();
            hotsplayers[id].BattleTag = stormPlayer.BattleTagName;
            hotsplayers[id].Party = "0";
            hotsplayers[id].Team = stormPlayer.Team.ToString();

            playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")) : stormPlayer.Name + " (AI)";
            //playerName = stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#"));
            //html += $@"<td><img src='data:image/png;base64,{new hotsImage(StormPlayer.PlayerHero.HeroName).Base64String}' class='heroIcon";
            html += $@"<td class=""headTableTd""><img src=""app://heroesIcon/{stormPlayer.PlayerHero.HeroName}.png"" class='heroIcon";
            if (stormPlayer.Team == hotsReplay.stormReplay.Owner.Team)
            {
                if (hotsReplay.stormReplay.Owner.BattleTagName == stormPlayer.BattleTagName)
                {
                    html += $@" heroIconTeam1";
                    hotsplayers[id].Party = "1";
                }
                else if ((stormPlayer.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (stormPlayer.PartyValue != null))
                {
                    html += $@" heroIconTeam1";
                    hotsplayers[id].Party = "1";
                }
                else if (stormPlayer.PartyValue != null)
                {
                    html += $@" heroIconTeam2";
                    hotsplayers[id].Party = "2";
                }
            }
            else
            {
                if ((stormPlayer.PartyValue != null) && ((opponentsFirstParty == null) || (opponentsFirstParty == stormPlayer.PartyValue)))
                {
                    opponentsFirstParty = stormPlayer.PartyValue;
                    html += $@" heroIconTeam3";
                    hotsplayers[id].Party = "3";
                }
                else if ((stormPlayer.PartyValue != null) && (opponentsFirstParty != stormPlayer.PartyValue))
                {
                    html += $@" heroIconTeam4";
                    hotsplayers[id].Party = "4";
                }
            }
            html += $@"' /><div class=""battleTag"">{playerName}</div></td>
            ";
            return html;
        }
        internal string HTMLGetChatMessage()
        {
            string html = $@"";
            html += $@"<div class=""chatMessages"">";
            html += $@"<table>";
            //html += $@"<tr><td class=""messages"">Time</td><td class=""messages"">Account</td><td class=""messages"">Character</td><td width=""100%"" class=""messages"">Message</td></tr>";
            foreach (Heroes.StormReplayParser.MessageEvent.IStormMessage chatMessage in hotsReplay.stormReplay.ChatMessages)
            {
                html += $@"<tr>";
                string msgHours = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Hours < 10 ? "0" + ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Hours.ToString() : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Hours.ToString();
                string msgMinutes = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Minutes < 10 ? "0" + ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Minutes.ToString() : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Minutes.ToString();
                string msgSeconds = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Seconds < 10 ? "0" + ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Seconds.ToString() : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Seconds.ToString();
                string msgMilliseconds = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds < 10 ? ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds.ToString() + "00" : (((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds < 100 ? ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds.ToString() + "0" : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds.ToString());
                string msgSenderName = chatMessage.MessageSender.Name;
                int? msgSenderAccountLevel = chatMessage.MessageSender.AccountLevel;
                string msgBattleTagName = chatMessage.MessageSender.BattleTagName;
                string msgCharacter = "";
                foreach (Heroes.StormReplayParser.Player.stormPlayer hotsPlayer in hotsReplay.stormPlayers)
                    if (hotsPlayer.BattleTagName == chatMessage.MessageSender.BattleTagName)
                    {
                        msgCharacter = hotsPlayer.PlayerHero.HeroName;
                        msgCharacter = msgCharacter.Replace(" ", "&nbsp;");
                    }
                html += $@"<td class=""messages"">[" + msgHours + ":" + msgMinutes + ":" + msgSeconds + ":" + msgMilliseconds + "]&nbsp;&nbsp;</td>";
                html += $@"<td class=""messages""><b>";
                if (chatMessage.MessageSender.BattleTagName == hotsReplay.stormReplay.Owner.BattleTagName)
                    html += $@"<class style=""color: crimson"">{msgSenderName}</class>";
                else if ((chatMessage.MessageSender.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (chatMessage.MessageSender.PartyValue != null))
                    html += $@"<class style=""color: crimson"">{msgSenderName}</class>";
                else if (chatMessage.MessageSender.PartyValue != null)
                    html += $@"<class style=""color: deepskyblue"">{msgSenderName}</class>";
                else
                    html += $@"<class style=""color: gainsboro"">{msgSenderName}</class>";
                html += $@"</b>";
                html += $@"&nbsp;({msgSenderAccountLevel})&nbsp;&nbsp;</td>";
                html += $@"<td class=""messages nonBreakingText""><nobr>{msgCharacter}</nobr>&nbsp;&nbsp;</td>";
                html += $@"<td width=""100%"" class=""messages"">{((Heroes.StormReplayParser.MessageEvent.ChatMessage)chatMessage).Text}</td>";
                html += "</tr>\r\n";
            }
            html += $@"</table>";
            html += $@"</div>";
            return html;
        }
        private string HTMLGetScoreTable()
        {
            string playerName;
            string html = @$"
              <table class=""tableScore"">
                <tr class=""teamHeader"">
                <td>&nbsp;&nbsp;Icone&nbsp;&nbsp;&nbsp;</td>
                <td>&nbsp;&nbsp;&nbsp;Name&nbsp;&nbsp;&nbsp;</td>
                <td>&nbsp;&nbsp;Kills&nbsp;&nbsp;&nbsp;</td>
                <td>&nbsp;Takedown&nbsp;</td>
                <td>&nbsp;&nbsp;Deaths&nbsp;</td>
                <td>Siege Dmge</td>
                <td>&nbsp;Hero Dmg&nbsp;</td>
                <td>&nbsp;Healing&nbsp;&nbsp;</td>
                <td>Dmg Taken&nbsp;</td>
                <td>&nbsp;&nbsp;&nbsp;Exp&nbsp;&nbsp;&nbsp;&nbsp;</td>
                </tr>
            ";

            foreach (stormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetScoreTr(stormPlayer, blueTeam, getParty(stormPlayer.BattleTagName));
            foreach (stormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetScoreTr(stormPlayer, redTeam, getParty(stormPlayer.BattleTagName));

            html += @$"</table>";
            return html;
        }
        private string getParty(string playerBattleTag)
        {
            foreach (hotsPlayer hotsPlayer in hotsplayers)
            {
                if (hotsPlayer.BattleTag == playerBattleTag)
                {
                    return hotsPlayer.Party;
                }
            }
            return "0";
        }
        private string HTMLGetScoreTr(stormPlayer stormPlayer, hotsTeam team, string partyColor)
        {
            string playerName = stormPlayer.BattleTagName.IndexOf("#") > 0 ? stormPlayer.BattleTagName.Remove(stormPlayer.BattleTagName.IndexOf("#")): stormPlayer.Name + " (AI)";
            string html = @"";
            html += @$"<tr class=""team{team.Name}"">";
            //html += @$"<td><img class=""scoreIcon"" src=""data:image/png;base64,{new hotsImage(stormPlayer.PlayerHero.HeroName).Base64String}"" /></td>";
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

            html += "</tr>\r\n";
            return html;
        }
        private void initTeamDatas(hotsTeam team)
        {
            foreach (stormPlayer stormPlayer in hotsReplay.stormPlayers)
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
        private void listBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            //htmlContent += $@"<img src='http://hotsreplayreader.local/{StormPlayer.PlayerHero.HeroName}.png' class='heroIcon' /> - {StormPlayer.PlayerHero.HeroName}<br />";
            //webView.CoreWebView2.SetVirtualHostNameToFolderMapping("hotsreplayreader.local", "icons/heroes", CoreWebView2HostResourceAccessKind.Allow);
            hotsReplay = new hotsReplay(hotsReplayFolder + "\\" + listBoxHotsReplays.Text + ".stormreplay");
            if (hotsReplay.stormReplay != null)
            {
                initTeamDatas(redTeam = new hotsTeam("Red"));
                initTeamDatas(blueTeam = new hotsTeam("Blue"));
                htmlContent = $@"{HTMLGetHeader()}";
                htmlContent += $@"{HTMLGetHeadTable()}<br /><br />";
                htmlContent += $@"{HTMLGetChatMessage()}<br /><br />";
                htmlContent += $@"{HTMLGetScoreTable()}<br /><br />";
                htmlContent += $@"{HTMLGetFooter()}";
            }
            else
            {
                htmlContent = $@"<body style=""background-color: black; margin: 0;""><img style=""width: 100%; height: 100%;"" src=""app://hotsImages/Welcome.jpg"" /></body>";
            }
            webView.CoreWebView2.NavigateToString(htmlContent);
        }
        private void sourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = "C:\\Users\\Thom\\Downloads\\html.html";
            //MessageBox.Show($@"Souces enrgistrées dans le fichier {path}");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.Write(htmlContent);
            }
            Process.Start("notepad.exe", path);
        }
    }
}
