using System.Text.RegularExpressions;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Replay;
using Heroes.StormReplayParser.MessageEvent;
//using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
//using System.Windows.Forms;
//using System.Drawing.Drawing2D;
//using System.Drawing.Imaging;
//using System.Resources;
using Heroes.StormReplayParser.Player;

namespace HotsReplayReader
{
    public partial class hotsReplayReaderForm : Form
    {
        private Rectangle originalHotsReplayReaderFormSize;
        private Rectangle richTextBoxHotsReplayMessagesOriginalRectangle;
        private Rectangle listBoxHotsReplaysOriginalRectangle;
        private Rectangle dataGridViewHeroStatsOriginalRectangle;

        private const int heroImageBorderThickness = 3;
        private ButtonBorderStyle heroImageBorderStyle = ButtonBorderStyle.Solid;
        private Color heroImageBorderColor = Color.Black;

        private string userDocumentsFolder;

        private string hotsVariablesFile;
        List<hotsLocalAccount> hotsLocalAccounts;
        private StormReplay hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.stormPlayer> hotsPlayers;
        long? opponentsFirstParty;

        public hotsReplayReaderForm()
        {
            InitializeComponent();
        }

        private void hotsReplayReaderForm_Load(object sender, EventArgs e)
        {
            originalHotsReplayReaderFormSize = new Rectangle(this.Location.X, this.Location.Y, this.Size.Width, this.Size.Height);
            richTextBoxHotsReplayMessagesOriginalRectangle = new Rectangle(richTextBoxHotsReplayMessages.Location.X, richTextBoxHotsReplayMessages.Location.Y, richTextBoxHotsReplayMessages.Width, richTextBoxHotsReplayMessages.Height);
            listBoxHotsReplaysOriginalRectangle = new Rectangle(listBoxHotsReplays.Location.X, listBoxHotsReplays.Location.Y, listBoxHotsReplays.Width, listBoxHotsReplays.Height);
            dataGridViewHeroStatsOriginalRectangle = new Rectangle(dataGridViewHeroStats.Location.X, dataGridViewHeroStats.Location.Y, dataGridViewHeroStats.Width, dataGridViewHeroStats.Height);
            labelHotsReplayFolder.Text = getLastReplayFilePath();
            listHotsAccounts();
            listHotsReplays();
            foreach (hotsLocalAccount hotsLocalAccount in hotsLocalAccounts)
            {
                if (hotsLocalAccount.FullPath == labelHotsReplayFolder.Text)
                {
                    foreach (var Item in comboBoxHotsAccounts.Items)
                    {
                        if (hotsLocalAccount.BattleTagName == Item.ToString())
                            comboBoxHotsAccounts.SelectedItem = Item;
                    }
                }
            }
        }

        private string getLastReplayFilePath()
        {
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

        private void listHotsAccounts()
        {
            string[] accountsDirs;
            comboBoxHotsAccounts.Items.Clear();
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
                                    comboBoxHotsAccounts.Items.Add(hotsReplay.Owner.BattleTagName);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void listHotsReplays()
        {
            listBoxHotsReplays.Items.Clear();
            if (Directory.Exists(labelHotsReplayFolder.Text))
            {
                DirectoryInfo hotsReplayFolder = new(labelHotsReplayFolder.Text);
                FileInfo[] replayFiles = hotsReplayFolder.GetFiles(@"*.StormReplay");
                Array.Reverse(replayFiles);
                foreach (FileInfo replayFile in replayFiles)
                {
                    listBoxHotsReplays.Items.Add(@replayFile.Name.ToString().Replace(@replayFile.Extension.ToString(), @""));
                }
            }
        }

        private void buttonHotsReplayFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.InitialDirectory = labelHotsReplayFolder.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                labelHotsReplayFolder.Text = folderBrowserDialog.SelectedPath;
                listHotsReplays();
            }
        }

        private void hotsReplayReaderForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HotsReplayReader.Program.ExitApp();
            }
        }

        private bool StormReplayParse(string hotsReplayFilePath)
        {
            opponentsFirstParty = null;

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

        private void richTextBoxHotsReplayMessagesRenew()
        {
            if (hotsReplay.Owner.IsWinner)
                richTextBoxHotsReplayMessages.BackColor = ColorTranslator.FromHtml(@"#DDFFDD");
            else
                richTextBoxHotsReplayMessages.BackColor = ColorTranslator.FromHtml(@"#FFDDDD");

            foreach (Heroes.StormReplayParser.MessageEvent.IStormMessage chatMessage in hotsReplay.ChatMessages)
            {
                string msgHours = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Hours < 10 ? "0" + ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Hours.ToString() : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Hours.ToString();
                string msgMinutes = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Minutes < 10 ? "0" + ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Minutes.ToString() : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Minutes.ToString();
                string msgSeconds = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Seconds < 10 ? "0" + ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Seconds.ToString() : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Seconds.ToString();
                string msgMilliseconds = ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds < 10 ? ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds.ToString() + "00" : (((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds < 100 ? ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds.ToString() + "0" : ((Heroes.StormReplayParser.MessageEvent.StormMessageBase)chatMessage).Timestamp.Milliseconds.ToString());
                string msgSenderName = chatMessage.MessageSender.Name;
                int? msgSenderAccountLevel = chatMessage.MessageSender.AccountLevel;
                string msgBattleTagName = chatMessage.MessageSender.BattleTagName;
                string msgCharacter = "";

                foreach (Heroes.StormReplayParser.Player.stormPlayer hotsPlayer in hotsPlayers)
                    if (hotsPlayer.BattleTagName == chatMessage.MessageSender.BattleTagName)
                        msgCharacter = hotsPlayer.PlayerHero.HeroName;

                richTextBoxHotsReplayMessages.AppendText("[" + msgHours + ":" + msgMinutes + ":" + msgSeconds + ":" + msgMilliseconds + "] ");

                richTextBoxHotsReplayMessages.SelectionFont = new Font(richTextBoxHotsReplayMessages.Font, FontStyle.Bold);
                if (chatMessage.MessageSender.BattleTagName == hotsReplay.Owner.BattleTagName)
                    richTextBoxHotsReplayMessages.AppendText(msgSenderName, Color.Red);
                else if ((chatMessage.MessageSender.PartyValue == hotsReplay.Owner.PartyValue) && (chatMessage.MessageSender.PartyValue != null))
                    richTextBoxHotsReplayMessages.AppendText(msgSenderName, Color.Red);
                else if (chatMessage.MessageSender.PartyValue != null)
                    richTextBoxHotsReplayMessages.AppendText(msgSenderName, Color.Blue);
                else
                    richTextBoxHotsReplayMessages.AppendText(msgSenderName);
                richTextBoxHotsReplayMessages.SelectionFont = new Font(richTextBoxHotsReplayMessages.Font, FontStyle.Regular);
                richTextBoxHotsReplayMessages.AppendText(" (" + msgSenderAccountLevel + ")");

                if (msgSenderName.Length + msgSenderAccountLevel.ToString().Length < 10)
                    richTextBoxHotsReplayMessages.AppendText("\t");

                richTextBoxHotsReplayMessages.AppendText("\t");
                richTextBoxHotsReplayMessages.AppendText(msgCharacter + "\t");
                if (msgCharacter.Length < 7)
                    richTextBoxHotsReplayMessages.AppendText("\t");

                richTextBoxHotsReplayMessages.AppendText(((Heroes.StormReplayParser.MessageEvent.ChatMessage)chatMessage).Text);
                richTextBoxHotsReplayMessages.AppendText(Environment.NewLine);
            }
        }

        private void setimageHeroAndBorder(stormPlayer hotsPlayer, PictureBox pictureBoxHero)
        {
            hotsImage heroImage = new hotsImage();
            pictureBoxHero.Image = heroImage.getBitmap(hotsPlayer.PlayerHero.HeroName);
            pictureBoxHero.Tag = null;

            if (hotsPlayer.Team == hotsReplay.Owner.Team)
            {
                if (hotsReplay.Owner.BattleTagName == hotsPlayer.BattleTagName)
                    pictureBoxHero.Tag = Color.Red;
                else if ((hotsPlayer.PartyValue == hotsReplay.Owner.PartyValue) && (hotsPlayer.PartyValue != null))
                    pictureBoxHero.Tag = Color.Red;
                else if (hotsPlayer.PartyValue != null)
                    pictureBoxHero.Tag = Color.Blue;
            }
            else
            {
                if ((hotsPlayer.PartyValue != null) && ((opponentsFirstParty == null) || (opponentsFirstParty == hotsPlayer.PartyValue)))
                {
                    opponentsFirstParty = hotsPlayer.PartyValue;
                    pictureBoxHero.Tag = Color.Green;
                }
                else if ((hotsPlayer.PartyValue != null) && (opponentsFirstParty != hotsPlayer.PartyValue))
                    pictureBoxHero.Tag = Color.Gold;
            }
            pictureBoxHero.Refresh();
        }

        private void imageHeroesRenew()
        {
            hotsImage heroImage = new hotsImage();
            int i = 0;
            foreach (Heroes.StormReplayParser.Player.stormPlayer hotsPlayer in hotsPlayers)
            {
                switch (i)
                {
                    case 0:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero0);
                        break;
                    case 1:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero1);
                        break;
                    case 2:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero2);
                        break;
                    case 3:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero3);
                        break;
                    case 4:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero4);
                        break;
                    case 5:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero5);
                        break;
                    case 6:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero6);
                        break;
                    case 7:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero7);
                        break;
                    case 8:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero8);
                        break;
                    case 9:
                        setimageHeroAndBorder(hotsPlayer, pictureBoxHero9);
                        break;
                }
                i++;
            }
        }

        private void dataGridViewHeroStatsDefault()
        {
            dataGridViewHeroStats.Rows.Clear();

            dataGridViewHeroStats.ColumnCount = 5;

            dataGridViewHeroStats.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
            dataGridViewHeroStats.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridViewHeroStats.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridViewHeroStats.Font, FontStyle.Bold);

            dataGridViewHeroStats.Name = "songsDataGridView";
            //dataGridViewHeroStats.Location = new System.Drawing.Point(8, 8);
            //dataGridViewHeroStats.Size = new Size(500, 250);
            dataGridViewHeroStats.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            dataGridViewHeroStats.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewHeroStats.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dataGridViewHeroStats.GridColor = Color.Black;
            dataGridViewHeroStats.RowHeadersVisible = false;

            //dataGridViewHeroStats.Width = 100;


            dataGridViewHeroStats.Columns[0].Name = "Kills";
            dataGridViewHeroStats.Columns[1].Name = "Takedowns";
            dataGridViewHeroStats.Columns[2].Name = "Deaths";
            dataGridViewHeroStats.Columns[3].Name = "Siege Dmg";
            dataGridViewHeroStats.Columns[4].Name = "Hero Dmg";
            dataGridViewHeroStats.Columns[4].DefaultCellStyle.Font = new Font(dataGridViewHeroStats.DefaultCellStyle.Font, FontStyle.Italic);

            dataGridViewHeroStats.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewHeroStats.MultiSelect = false;
            //dataGridViewHeroStats.Dock = DockStyle.Fill;

            //dataGridViewHeroStats.CellFormatting += new DataGridViewCellFormattingEventHandler(dataGridViewHeroStats_CellFormatting);
        }

        private void listBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBoxHotsReplayMessages.Clear();
            string hotsReplayFilePath = labelHotsReplayFolder.Text + @"\" + listBoxHotsReplays.Text + @".stormreplay";
            if (StormReplayParse(hotsReplayFilePath))
            {
                richTextBoxHotsReplayMessagesRenew();
                imageHeroesRenew();
                dataGridViewHeroStatsDefault();
            }
        }

        private void resizeControl(Rectangle r, Control c, bool growWidth, bool fixedYOriginOnly, int fixedHeight)
        {
            float xRatio = (float)(this.Width) / (float)(originalHotsReplayReaderFormSize.Width);
            float yRatio = (float)(this.Height) / (float)(originalHotsReplayReaderFormSize.Height);

            int newX = (int)(r.Width * xRatio);
            int newY = (int)(r.Height * yRatio);

            // int newWidth = (int)(r.Width * xRatio);
            int newWidth;
            if (growWidth)
                newWidth = (int)(this.Width - 850); //384
            else
                newWidth = c.Width;

            // int newHeight = (int)(r.Height * yRatio);
            int newHeight = (int)(this.Height - 89 - fixedHeight);

            // c.Location = new System.Drawing.Point(newX, newY);
            if (!fixedYOriginOnly) c.Location = new System.Drawing.Point(this.Width - r.Width - 25, r.Y);
            c.Size = new System.Drawing.Size(newWidth, newHeight);
        }

        private void hotsReplayReaderForm_Resize(object sender, EventArgs e)
        {
            resizeControl(listBoxHotsReplaysOriginalRectangle, listBoxHotsReplays, false, true, 0);
            resizeControl(richTextBoxHotsReplayMessagesOriginalRectangle, richTextBoxHotsReplayMessages, true, true, 70);
            resizeControl(dataGridViewHeroStatsOriginalRectangle, dataGridViewHeroStats, false, false, 70);
        }

        private void comboBoxHotsAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (hotsLocalAccount hotsLocalAccount in hotsLocalAccounts)
            {
                if (hotsLocalAccount.BattleTagName == comboBoxHotsAccounts.Text)
                {
                    labelHotsReplayFolder.Text = hotsLocalAccount.FullPath;
                }
            }
            listHotsReplays();
        }

        private void pictureBoxHero0_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero0.Tag == null) { pictureBoxHero0.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero0.ClientRectangle, (Color)pictureBoxHero0.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero0.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero0.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero0.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero1.Tag == null) { pictureBoxHero1.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero1.ClientRectangle, (Color)pictureBoxHero1.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero1.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero1.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero1.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero2_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero2.Tag == null) { pictureBoxHero2.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero2.ClientRectangle, (Color)pictureBoxHero2.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero2.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero2.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero2.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero3_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero3.Tag == null) { pictureBoxHero3.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero3.ClientRectangle, (Color)pictureBoxHero3.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero3.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero3.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero3.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero4_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero4.Tag == null) { pictureBoxHero4.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero4.ClientRectangle, (Color)pictureBoxHero4.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero4.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero4.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero4.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero5_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero5.Tag == null) { pictureBoxHero5.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero5.ClientRectangle, (Color)pictureBoxHero5.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero5.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero5.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero5.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero6_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero6.Tag == null) { pictureBoxHero6.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero6.ClientRectangle, (Color)pictureBoxHero6.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero6.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero6.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero6.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero7_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero7.Tag == null) { pictureBoxHero7.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero7.ClientRectangle, (Color)pictureBoxHero7.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero7.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero7.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero7.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero8_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero8.Tag == null) { pictureBoxHero8.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero8.ClientRectangle, (Color)pictureBoxHero8.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero8.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero8.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero8.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }

        private void pictureBoxHero9_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxHero9.Tag == null) { pictureBoxHero9.Tag = heroImageBorderColor; }
            ControlPaint.DrawBorder(e.Graphics, pictureBoxHero9.ClientRectangle, (Color)pictureBoxHero9.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero9.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero9.Tag, heroImageBorderThickness, heroImageBorderStyle, (Color)pictureBoxHero9.Tag, heroImageBorderThickness, heroImageBorderStyle);
        }
    }
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}
