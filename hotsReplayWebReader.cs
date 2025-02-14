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

namespace HotsReplayReader
{
    public partial class hotsReplayWebReader : Form
    {
        private Rectangle originalHotsReplayWebReaderSize;
        private Rectangle listBoxHotsReplaysOriginalRectangle;
        private Rectangle webViewOriginalRectangle;

        private string? hotsReplayFolder;

        hotsReplay hotsReplay;

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


            //htmlContent += $@"<img src='data:image/png;base64,{new hotsImage("Welcome").Base64String}' />";
            //webView.CoreWebView2.NavigateToString(htmlContent);

            await webView.EnsureCoreWebView2Async();
            htmlContent += $@"<body style=""background-color: black;""><img style=""width: 100%; height: 100%;"" src=""data:image/png;base64,{new hotsImage("Welcome").Base64String}"" /></body>";
            webView.CoreWebView2.NavigateToString(htmlContent);
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
                backColor = @"#003300"; //@"#DDFFDD";
            else
                backColor = @"#330000";

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
                .heroIcon {{
                  border: 3px solid white;
                  border-radius: 100%;
                  width: 80px;
                  height: 80px;
                }}
                .heroIcon:hover {{
                  filter: brightness(125%);
                }}
                .battleTag {{
                  font-size: 80%;
                }}
                .team1 {{
                  border: 3px solid red;
                }}
                .team2 {{
                  border: 3px solid blue;
                }}
                .team3 {{
                  border: 3px solid green;
                }}
                .team4 {{
                  border: 3px solid gold;
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
                </style>
                </head>
                <body>
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
            string html = $@"<table>
            <tr>
            <td colspan=""5"">TEAM 1</td>
            <td></td>
            <td colspan=""5"">TEAM 2</td>
            </tr>
            <tr>
            ";
            long? opponentsFirstParty = null;
            foreach (StormPlayer StormPlayer in hotsReplay.stormPlayers)
            {
                if (StormPlayer.Team.ToString() == "Red")
                {
                    playerName = StormPlayer.BattleTagName.Remove(StormPlayer.BattleTagName.IndexOf("#"));
                    html += $@"<td><img src='data:image/png;base64,{new hotsImage(StormPlayer.PlayerHero.HeroName).Base64String}' class='heroIcon";
                    if (StormPlayer.Team == hotsReplay.stormReplay.Owner.Team)
                    {
                        if (hotsReplay.stormReplay.Owner.BattleTagName == StormPlayer.BattleTagName)
                            html += $@" team1";
                        else if ((StormPlayer.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (StormPlayer.PartyValue != null))
                            html += $@" team1";
                        else if (StormPlayer.PartyValue != null)
                            html += $@" team2";
                    }
                    else
                    {
                        if ((StormPlayer.PartyValue != null) && ((opponentsFirstParty == null) || (opponentsFirstParty == StormPlayer.PartyValue)))
                        {
                            opponentsFirstParty = StormPlayer.PartyValue;
                            html += $@" team3";
                        }
                        else if ((StormPlayer.PartyValue != null) && (opponentsFirstParty != StormPlayer.PartyValue))
                            html += $@" team4";
                    }
                    html += $@"' /><br><div class='battleTag'>{playerName}</div></td>";
                }
            }
            html += $@"<td width=""100""></td>";
            //opponentsFirstParty = null;
            foreach (StormPlayer StormPlayer in hotsReplay.stormPlayers)
            {
                if (StormPlayer.Team.ToString() == "Blue")
                {
                    playerName = StormPlayer.BattleTagName.Remove(StormPlayer.BattleTagName.IndexOf("#"));
                    html += $@"<td><img src='data:image/png;base64,{new hotsImage(StormPlayer.PlayerHero.HeroName).Base64String}' class='heroIcon";
                    if (StormPlayer.Team == hotsReplay.stormReplay.Owner.Team)
                    {
                        if (hotsReplay.stormReplay.Owner.BattleTagName == StormPlayer.BattleTagName)
                            html += $@" team1";
                        else if ((StormPlayer.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (StormPlayer.PartyValue != null))
                            html += $@" team1";
                        else if (StormPlayer.PartyValue != null)
                            html += $@" team2";
                    }
                    else
                    {
                        if ((StormPlayer.PartyValue != null) && ((opponentsFirstParty == null) || (opponentsFirstParty == StormPlayer.PartyValue)))
                        {
                            opponentsFirstParty = StormPlayer.PartyValue;
                            html += $@" team3";
                        }
                        else if ((StormPlayer.PartyValue != null) && (opponentsFirstParty != StormPlayer.PartyValue))
                            html += $@" team4";
                    }
                    html += $@"' /><br><div class='battleTag'>{playerName}</div></td>";
                }
            }
            html += $@"
            </tr>
            </table>
            </body>
            </html>
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
                foreach (Heroes.StormReplayParser.Player.StormPlayer hotsPlayer in hotsReplay.stormPlayers)
                    if (hotsPlayer.BattleTagName == chatMessage.MessageSender.BattleTagName)
                        msgCharacter = hotsPlayer.PlayerHero.HeroName;
                html += $@"<td class=""messages"">[" + msgHours + ":" + msgMinutes + ":" + msgSeconds + ":" + msgMilliseconds + "]&nbsp;&nbsp;</td>";
                html += $@"<td class=""messages""><b>";
                if (chatMessage.MessageSender.BattleTagName == hotsReplay.stormReplay.Owner.BattleTagName)
                    html += $@"<class style=""color: red"">{msgSenderName}</class>";
                else if ((chatMessage.MessageSender.PartyValue == hotsReplay.stormReplay.Owner.PartyValue) && (chatMessage.MessageSender.PartyValue != null))
                    html += $@"<class style=""color: red"">{msgSenderName}</class>";
                else if (chatMessage.MessageSender.PartyValue != null)
                    html += $@"<class style=""color: lightblue"">{msgSenderName}</class>";
                else
                    html += $@"<class>{msgSenderName}</class>";
                html += $@"</b>";
                html += $@"&nbsp;({msgSenderAccountLevel})&nbsp;&nbsp;</td>";
                html += $@"<td class=""messages"">{msgCharacter}&nbsp;&nbsp;</td>";
                html += $@"<td width=""100%"" class=""messages"">{((Heroes.StormReplayParser.MessageEvent.ChatMessage)chatMessage).Text}</td>";
                html += $@"</tr>";
            }
            html += $@"</table>";
            html += $@"</div>";
            return html;
        }
        private void listBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            //htmlContent += $@"<img src='http://hotsreplayreader.local/{StormPlayer.PlayerHero.HeroName}.png' class='heroIcon' /> - {StormPlayer.PlayerHero.HeroName}<br />";
            //webView.CoreWebView2.SetVirtualHostNameToFolderMapping("hotsreplayreader.local", "icons/heroes", CoreWebView2HostResourceAccessKind.Allow);

            hotsReplay = new hotsReplay(hotsReplayFolder + "\\" + listBoxHotsReplays.Text + ".stormreplay");
            htmlContent = $@"{HTMLGetHeader()}";
            htmlContent += $@"{HTMLGetHeadTable()}<br /><br />";
            htmlContent += $@"{HTMLGetChatMessage()}";
            htmlContent += $@"{HTMLGetFooter()}";
            webView.CoreWebView2.NavigateToString(htmlContent);
        }
        private void sourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(htmlContent);
            string path = "C:\\Users\\Thom\\Downloads\\html.html";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write(htmlContent);
                }
            }

        }
    }
}
