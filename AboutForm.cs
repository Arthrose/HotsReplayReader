using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class AboutForm : Form
    {
        // Dark mode
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        public AboutForm()
        {
            InitializeComponent();
            programVersionLabel.Text = "HotS Replay Reader v" + Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
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

                authorLinkLabel.LinkColor = Color.FromArgb(86, 156, 214);
                gitHubLinkLabel.LinkColor = Color.FromArgb(86, 156, 214);

                Color buttonBackColor = Color.FromArgb(51, 51, 51);
                Color buttonBorderColor = Color.FromArgb(139, 139, 139);
                Color buttonMouseOverColor = Color.FromArgb(69, 69, 69);
                OKButton.BackColor = buttonBackColor;
                OKButton.FlatStyle = FlatStyle.Flat;
                OKButton.FlatAppearance.MouseOverBackColor = buttonMouseOverColor;
                OKButton.FlatAppearance.BorderColor = buttonBorderColor;
            }
        }
        private void OKButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void AboutForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }
        private void Author_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "https://www.reddit.com/user/Arthrose/",
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
        private void GitHubLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "https://github.com/Arthrose/HotsReplayReader/releases/",
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
    }
}
