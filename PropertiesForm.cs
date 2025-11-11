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
            if (this.hotsReplayWebReader.Init.config != null)
                deepLTextBox.Text = this.hotsReplayWebReader.Init.config.DeepLAPIKey;
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
        private void OKButton_Click(object sender, EventArgs e)
        {
            if (this.hotsReplayWebReader.Init.config != null)
                hotsReplayWebReader.Init.config.DeepLAPIKey = deepLTextBox.Text;
            this.Close();
        }
        private async void TestButton_Click(object sender, EventArgs e)
        {
            DeepLTranslator translator = new(deepLTextBox.Text);
            if (translator != null)
            {
                bool isValid = await translator.CheckApiKeyValidity();
                if (isValid)
                    MessageBox.Show("La clé API est valide.");
                else
                    MessageBox.Show("La clé API est invalide.");
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            int useDarkMode = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", -1) == 0) ? 1 : 0;

            // Try latest first
            if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                // Fallback for older Windows 10 builds
                NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }

            if (useDarkMode == 1)
            {
                this.BackColor = Color.FromArgb(32, 32, 32);
                this.ForeColor = Color.White;

                deepLLinkLabel.LinkColor = Color.FromArgb(86, 156, 214);
                deepLTextBox.BackColor = Color.FromArgb(56, 56, 56);
                deepLTextBox.ForeColor = Color.White;

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
    }
}