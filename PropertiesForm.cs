using System.Diagnostics;

namespace HotsReplayReader
{
    public partial class PropertiesForm : Form
    {
        readonly HotsReplayWebReader hotsReplayWebReader;
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
    }
}