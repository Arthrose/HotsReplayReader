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

namespace HotsReplayReader
{
    public partial class hotsReplayWebReader : Form
    {
        private Rectangle originalHotsReplayWebReaderSize;
        private Rectangle listBoxHotsReplaysOriginalRectangle;
        private Rectangle webViewOriginalRectangle;

        private string? hotsReplayFolder;

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
        private void hotsReplayWebReader_Load(object sender, EventArgs e)
        {
            originalHotsReplayWebReaderSize = new Rectangle(this.Location.X, this.Location.Y, this.Size.Width, this.Size.Height);
            listBoxHotsReplaysOriginalRectangle = new Rectangle(listBoxHotsReplays.Location.X, listBoxHotsReplays.Location.Y, listBoxHotsReplays.Width, listBoxHotsReplays.Height);
            webViewOriginalRectangle = new Rectangle(webViewOriginalRectangle.Location.X, webViewOriginalRectangle.Location.Y, webViewOriginalRectangle.Width, webViewOriginalRectangle.Height);

            if (Directory.Exists(Init.lastReplayFilePath))
                listHotsReplays(Init.lastReplayFilePath);
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
            string html = $@"
            <html>
            <head>
            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
            <style>
            .heroIcon {{
              border-radius: 100%;
              width: 80px;
              height: 80px;
            }}
            .heroIcon:hover {{
              filter: brightness(125%);
            }}
            </style>
            </head>";
            return html;
        }

        private void listBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            hotsReplay hotsReplay = new hotsReplay(hotsReplayFolder + "\\" + listBoxHotsReplays.Text + ".stormreplay");

            byte[] imageBytes = (byte[])heroesIcon.Xul;
            string base64Image = Convert.ToBase64String(imageBytes);
            htmlContent = $@"
            {HTMLGetHeader()}
            <body>
                <p>{hotsReplayFolder}\{listBoxHotsReplays.Text}.stormreplay</p>";
            foreach (StormPlayer StormPlayer in hotsReplay.stormPlayers)
            {
                htmlContent += StormPlayer.PlayerHero.HeroName + "<br />";
            }
            htmlContent += $@"
                <img src='data:image/png;base64,{base64Image}' class='heroIcon' />
            </body>
            </html>";
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
