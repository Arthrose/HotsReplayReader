using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class PropertiesForm : Form
    {
        readonly HotsReplayWebReader hotsReplayWebReader;
        // Dark mode
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        public PropertiesForm(HotsReplayWebReader hotsReplayWebReader)
        {
            InitializeComponent();
            this.hotsReplayWebReader = hotsReplayWebReader;

            this.Text = Resources.Language.i18n.strProperties;
            deepLLabel.Text = Resources.Language.i18n.strPropertiesDeepLAPIKey;
            deepLLinkLabel.Text = Resources.Language.i18n.strPropertiesVisitDeepLWebsite;
            testButton.Text = Resources.Language.i18n.strPropertiesTest;
            groupBoxDisplay.Text = Resources.Language.i18n.strPropertiesDisplay;
            checkBoxDisplayGameMode.Text = Resources.Language.i18n.strPropertiesDisplayGameMode;
            checkBoxDisplayDate.Text = Resources.Language.i18n.strPropertiesDisplayDate;
            checkBoxDisplayReplaySideBar.Text = Resources.Language.i18n.strPropertiesDisplayReplaySidebar;
            checkBoxDisplayPingButton.Text = Resources.Language.i18n.strPropertiesDisplayPingButton;
            checkBoxDisplayDraftOrder.Text = Resources.Language.i18n.strPropertiesDisplayDraftOrder;

            OKButton.Text = Resources.Language.i18n.strPropertiesOK;

            if (this.hotsReplayWebReader.Init.config != null)
            {
                deepLTextBox.Text = this.hotsReplayWebReader.Init.config.DeepLAPIKey;
                checkBoxDisplayGameMode.Checked = this.hotsReplayWebReader.Init.config.DisplayGameMode;
                checkBoxDisplayDate.Checked = this.hotsReplayWebReader.Init.config.DisplayDate;
                checkBoxDisplayReplaySideBar.Checked = this.hotsReplayWebReader.Init.config.DisplayReplaySideBar;
                checkBoxDisplayPingButton.Checked = this.hotsReplayWebReader.Init.config.DisplayPingButton;
                checkBoxDisplayDraftOrder.Checked = this.hotsReplayWebReader.Init.config.DisplayDraftOrder;
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
                this.hotsReplayWebReader.Init.config.DisplayPingButton = checkBoxDisplayPingButton.Checked;
                this.hotsReplayWebReader.Init.config.DisplayDraftOrder = checkBoxDisplayDraftOrder.Checked;

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
            hotsReplayWebReader.ListBoxHotsReplays_SelectedIndexChanged(hotsReplayWebReader, EventArgs.Empty);
            this.Close();
        }
        private async void TestButton_Click(object sender, EventArgs e)
        {
            DeepLTranslator translator = new(deepLTextBox.Text);
            if (translator != null)
            {
                bool isValid = await translator.CheckApiKeyValidity();
                if (isValid)
                    MessageBox.Show(Resources.Language.i18n.ResourceManager.GetString("strPropertiesValidAPIKey"));
                else
                    MessageBox.Show(Resources.Language.i18n.ResourceManager.GetString("strPropertiesInvalidAPIKey"));
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
            if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref this.hotsReplayWebReader.useDarkMode, sizeof(int)) != 0)
            {
                // Fallback for older Windows 10 builds
                NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref this.hotsReplayWebReader.useDarkMode, sizeof(int));
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
                testButton.BackColor = buttonBackColor;
                testButton.FlatStyle = FlatStyle.Flat;
                testButton.FlatAppearance.MouseOverBackColor = buttonMouseOverColor;
                testButton.FlatAppearance.BorderColor = buttonBorderColor;
                OKButton.BackColor = buttonBackColor;
                OKButton.FlatStyle = FlatStyle.Flat;
                OKButton.FlatAppearance.MouseOverBackColor = buttonMouseOverColor;
                OKButton.FlatAppearance.BorderColor = buttonBorderColor;
            }
        }
        private void PropertiesForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}