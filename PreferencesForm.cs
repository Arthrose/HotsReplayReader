using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class PreferencesForm : Form
    {
        readonly HotsReplayWebReader hotsReplayWebReader;
        readonly string lasReplayFilePath = "";
        // Dark mode
        public PreferencesForm(HotsReplayWebReader hotsReplayWebReader, string lasReplayFilePath)
        {
            this.lasReplayFilePath = lasReplayFilePath;
            InitializeComponent();
            this.hotsReplayWebReader = hotsReplayWebReader;

            this.Text = Resources.Language.i18n.strPreferences;

            deepLLabel.Text = Resources.Language.i18n.strPreferencesDeepLAPIKey;
            deepLLinkLabel.Text = Resources.Language.i18n.strPreferencesVisitDeepLWebsite;
            deepLTestButton.Text = Resources.Language.i18n.strPreferencesTest;

            groupBoxDisplay.Text = Resources.Language.i18n.strPreferencesDisplay;
            checkBoxDisplayGameMode.Text = Resources.Language.i18n.strPreferencesDisplayGameMode;
            checkBoxDisplayDate.Text = Resources.Language.i18n.strPreferencesDisplayDate;
            checkBoxDisplayReplaySideBar.Text = Resources.Language.i18n.strPreferencesDisplayReplaySidebar;
            checkBoxDisplayDraftOrder.Text = Resources.Language.i18n.strPreferencesDisplayDraftOrder;

            checkBoxMessagesShow.Text = Resources.Language.i18n.strPreferencesMessages;
            checkBoxMessagesFriendlyChat.Text = Resources.Language.i18n.strPreferencesMessagesFriendly;
            checkBoxMessagesEnnemyChat.Text = Resources.Language.i18n.strPreferencesMessagesEnnemy;
            checkBoxMessagesPingButton.Text = Resources.Language.i18n.strPreferencesDisplayPingButton;

            groupBoxDarkMode.Text = Resources.Language.i18n.strPreferencesDarkMode;
            radioButtonDarkModeLight.Text = Resources.Language.i18n.strPreferencesDarkModeLightMode;
            radioButtonDarkModeDark.Text = Resources.Language.i18n.strPreferencesDarkModeDarkMode;
            radioButtonDarkModeAutomatic.Text = Resources.Language.i18n.strPreferencesDarkModeAutomatic;

            OKButton.Text = Resources.Language.i18n.strPreferencesOK;

            if (this.hotsReplayWebReader.Init.config != null)
            {
                deepLTextBox.Text = this.hotsReplayWebReader.Init.config.DeepLAPIKey;
                checkBoxDisplayGameMode.Checked = this.hotsReplayWebReader.Init.config.DisplayGameMode;
                checkBoxDisplayDate.Checked = this.hotsReplayWebReader.Init.config.DisplayDate;
                checkBoxDisplayReplaySideBar.Checked = this.hotsReplayWebReader.Init.config.DisplayReplaySideBar;
                checkBoxDisplayDraftOrder.Checked = this.hotsReplayWebReader.Init.config.DisplayDraftOrder;

                checkBoxMessagesShow.Checked = this.hotsReplayWebReader.Init.config.DisplayMessages;
                checkBoxMessagesFriendlyChat.Checked = this.hotsReplayWebReader.Init.config.DisplayFriendlyMessages;
                checkBoxMessagesEnnemyChat.Checked = this.hotsReplayWebReader.Init.config.DisplayEnnemyMessages;
                checkBoxMessagesPingButton.Checked = this.hotsReplayWebReader.Init.config.DisplayPingButton;

                CheckBoxMessagesShow_CheckedChanged(this, EventArgs.Empty);

                switch (this.hotsReplayWebReader.Init.config.DarkMode)
                {
                    case Config.DarkModeType.Automatic:
                        radioButtonDarkModeAutomatic.Checked = true;
                        break;
                    case Config.DarkModeType.Light:
                        radioButtonDarkModeLight.Checked = true;
                        break;
                    case Config.DarkModeType.Dark:
                        radioButtonDarkModeDark.Checked = true;
                        break;
                }
            }
        }
        private void CheckBoxMessagesShow_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = checkBoxMessagesShow.Checked;

            Color disabledColor = this.hotsReplayWebReader.useDarkMode == 1
                ? Color.FromArgb(128, 128, 128)
                : SystemColors.GrayText;

            foreach (CheckBox? checkBox in new[] { checkBoxMessagesEnnemyChat, checkBoxMessagesFriendlyChat, checkBoxMessagesPingButton })
            {
                checkBox.AutoCheck = enabled;
                checkBox.TabStop = enabled;
                checkBox.ForeColor = enabled ? Color.Empty : disabledColor;
            }
        }
        private void DeepLLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "https://developers.deepl.com/docs/getting-started/managing-api-keys",
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
        private async void OKButton_Click(object sender, EventArgs e)
        {
            if (this.hotsReplayWebReader.Init.config != null)
            {
                hotsReplayWebReader.Init.config.DeepLAPIKey = deepLTextBox.Text;
                this.hotsReplayWebReader.Init.config.DisplayGameMode = checkBoxDisplayGameMode.Checked;
                this.hotsReplayWebReader.Init.config.DisplayDate = checkBoxDisplayDate.Checked;
                this.hotsReplayWebReader.Init.config.DisplayReplaySideBar = checkBoxDisplayReplaySideBar.Checked;
                this.hotsReplayWebReader.Init.config.DisplayDraftOrder = checkBoxDisplayDraftOrder.Checked;

                this.hotsReplayWebReader.Init.config.DisplayMessages = checkBoxMessagesShow.Checked;
                this.hotsReplayWebReader.Init.config.DisplayFriendlyMessages = checkBoxMessagesFriendlyChat.Checked;
                this.hotsReplayWebReader.Init.config.DisplayEnnemyMessages = checkBoxMessagesEnnemyChat.Checked;
                this.hotsReplayWebReader.Init.config.DisplayPingButton = checkBoxMessagesPingButton.Checked;

                if (radioButtonDarkModeLight.Checked)
                    this.hotsReplayWebReader.Init.config.DarkMode = Config.DarkModeType.Light;
                else if (radioButtonDarkModeDark.Checked)
                    this.hotsReplayWebReader.Init.config.DarkMode = Config.DarkModeType.Dark;
                else if (radioButtonDarkModeAutomatic.Checked)
                    this.hotsReplayWebReader.Init.config.DarkMode = Config.DarkModeType.Automatic;
            }

            DeepLTranslator translator = new(deepLTextBox.Text);
            if (translator != null)
                hotsReplayWebReader.DeepLAPIValid = await translator.CheckApiKeyValidity();

            this.hotsReplayWebReader.ApplyTheme();

            // Recharge le dernier replay
            this.Close();
            await hotsReplayWebReader.LoadReplay(this.lasReplayFilePath);
        }
        private async void TestButton_Click(object sender, EventArgs e)
        {
            DeepLTranslator translator = new(deepLTextBox.Text);
            if (translator != null)
            {
                bool isValid = await translator.CheckApiKeyValidity();
                if (isValid)
                    MessageBox.Show(Resources.Language.i18n.strPreferencesValidAPIKey);
                else
                    MessageBox.Show(Resources.Language.i18n.strPreferencesInvalidAPIKey);
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
            int useDarkMode = this.hotsReplayWebReader.useDarkMode;
            if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                // Fallback for older Windows 10 builds
                NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }

            if (this.hotsReplayWebReader.useDarkMode == 1)
            {
                this.BackColor = Color.FromArgb(32, 32, 32);
                this.ForeColor = Color.White;

                deepLLinkLabel.LinkColor = Color.FromArgb(86, 156, 214);
                deepLTextBox.BackColor = Color.FromArgb(56, 56, 56);
                deepLTextBox.ForeColor = Color.White;

                groupBoxDisplay.ForeColor = Color.White;
                groupBoxDarkMode.ForeColor = Color.White;

                Color buttonBackColor = Color.FromArgb(51, 51, 51);
                Color buttonBorderColor = Color.FromArgb(139, 139, 139);
                Color buttonMouseOverColor = Color.FromArgb(69, 69, 69);
                deepLTestButton.BackColor = buttonBackColor;
                deepLTestButton.FlatStyle = FlatStyle.Flat;
                deepLTestButton.FlatAppearance.MouseOverBackColor = buttonMouseOverColor;
                deepLTestButton.FlatAppearance.BorderColor = buttonBorderColor;
                OKButton.BackColor = buttonBackColor;
                OKButton.FlatStyle = FlatStyle.Flat;
                OKButton.FlatAppearance.MouseOverBackColor = buttonMouseOverColor;
                OKButton.FlatAppearance.BorderColor = buttonBorderColor;
            }
        }
        private void PreferencesForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}