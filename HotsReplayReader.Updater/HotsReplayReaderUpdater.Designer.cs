namespace HotsReplayReader.Updater
{
    partial class HotsReplayReaderUpdater
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblStatus = new Label();
            progressBar = new ProgressBar();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(20, 20);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(360, 23);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Text = "Initialisation...";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(20, 55);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(360, 23);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            // 
            // HotsReplayReaderUpdater
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 100);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Name = "HotsReplayReaderUpdater";
            Text = "Mise à jour de HotsReplayReader";
            Load += HotsReplayReaderUpdater_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblStatus;
        private ProgressBar progressBar;
    }
}
