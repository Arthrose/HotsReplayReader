using System.Text.RegularExpressions;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Replay;
using Heroes.StormReplayParser.MessageEvent;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class hotsReplayReaderForm : Form
    {
        private Rectangle richTextBoxHotsReplayMessagesOriginalRectangle;
        private Rectangle originalHotsReplayReaderFormSize;
        private Rectangle listBoxHotsReplaysOriginalRectangle;

        private string hotsVariablesFile;
        List<hotsLocalAccount> hotsLocalAccounts;
        private StormReplay hotsReplay;
        IEnumerable<Heroes.StormReplayParser.Player.StormPlayer> hotsPlayers;

        public hotsReplayReaderForm()
        {
            InitializeComponent();
        }

        private void hotsReplayReaderForm_Load(object sender, EventArgs e)
        {
            originalHotsReplayReaderFormSize = new Rectangle(this.Location.X, this.Location.Y, this.Size.Width, this.Size.Height);
            richTextBoxHotsReplayMessagesOriginalRectangle = new Rectangle(richTextBoxHotsReplayMessages.Location.X, richTextBoxHotsReplayMessages.Location.Y, richTextBoxHotsReplayMessages.Width, richTextBoxHotsReplayMessages.Height);
            listBoxHotsReplaysOriginalRectangle = new Rectangle(listBoxHotsReplays.Location.X, listBoxHotsReplays.Location.Y, listBoxHotsReplays.Width, listBoxHotsReplays.Height);
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
            string userDocumentsFolder = RegKey.GetValue("Personal", "").ToString();

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
            comboBoxHotsAccounts.Items.Clear();
            hotsLocalAccounts = new List<hotsLocalAccount>();
            string[] accountsDirs = Directory.GetDirectories(Path.GetDirectoryName(hotsVariablesFile) + @"\Accounts");
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
                    listBoxHotsReplays.Items.Add(@replayFile.Name);
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

                foreach (Heroes.StormReplayParser.Player.StormPlayer hotsPlayer in hotsPlayers)
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

        private void listBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBoxHotsReplayMessages.Clear();
            string hotsReplayFilePath = labelHotsReplayFolder.Text + @"\" + listBoxHotsReplays.Text;
            if (StormReplayParse(hotsReplayFilePath)) richTextBoxHotsReplayMessagesRenew();
        }

        private void resizeControl(Rectangle r, Control c, bool growWidth)
        {
            float xRatio = (float)(this.Width) / (float)(originalHotsReplayReaderFormSize.Width);
            float yRatio = (float)(this.Height) / (float)(originalHotsReplayReaderFormSize.Height);

            int newX = (int)(r.Width * xRatio);
            int newY = (int)(r.Height * yRatio);

            // int newWidth = (int)(r.Width * xRatio);
            int newWidth;
            if (growWidth)
                newWidth = (int)(this.Width - 384);
            else
                newWidth = c.Width;

            // int newHeight = (int)(r.Height * yRatio);
            int newHeight = (int)(this.Height - 89);

            // c.Location = new System.Drawing.Point(newX, newY);
            c.Size = new System.Drawing.Size(newWidth, newHeight);
        }

        private void hotsReplayReaderForm_Resize(object sender, EventArgs e)
        {
            resizeControl(richTextBoxHotsReplayMessagesOriginalRectangle, richTextBoxHotsReplayMessages, true);
            resizeControl(listBoxHotsReplaysOriginalRectangle, listBoxHotsReplays, false);
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
