namespace HotsReplayReader.Updater
{
    public partial class HotsReplayReaderUpdater : Form
    {
        public HotsReplayReaderUpdater(string[] args)
        {
            InitializeComponent();
            string message = args.Length > 0
                ? string.Join(Environment.NewLine, args)
                : "Aucun argument reçu.";

            MessageBox.Show(message, "Arguments reçus", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
