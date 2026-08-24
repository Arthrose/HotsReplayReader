namespace HotsReplayReader
{
    partial class AboutForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            gitHubLinkLabel = new LinkLabel();
            OKButton = new Button();
            programVersionLabel = new Label();
            authorLabel = new Label();
            authorLinkLabel = new LinkLabel();
            SuspendLayout();
            // 
            // gitHubLinkLabel
            // 
            gitHubLinkLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            gitHubLinkLabel.Location = new Point(139, 53);
            gitHubLinkLabel.Name = "gitHubLinkLabel";
            gitHubLinkLabel.Size = new Size(200, 15);
            gitHubLinkLabel.TabIndex = 4;
            gitHubLinkLabel.TabStop = true;
            gitHubLinkLabel.Text = "GitHub Repository";
            gitHubLinkLabel.TextAlign = ContentAlignment.TopRight;
            gitHubLinkLabel.LinkClicked += GitHubLinkLabel_LinkClicked;
            // 
            // OKButton
            // 
            OKButton.Location = new Point(264, 76);
            OKButton.Name = "OKButton";
            OKButton.Size = new Size(75, 23);
            OKButton.TabIndex = 1;
            OKButton.Text = "OK";
            OKButton.UseVisualStyleBackColor = true;
            OKButton.Click += OKButton_Click;
            // 
            // programVersionLabel
            // 
            programVersionLabel.AutoSize = true;
            programVersionLabel.Location = new Point(12, 9);
            programVersionLabel.Name = "programVersionLabel";
            programVersionLabel.Size = new Size(146, 15);
            programVersionLabel.TabIndex = 5;
            programVersionLabel.Text = "HotS Replay Reader v 0.1.1";
            // 
            // authorLabel
            // 
            authorLabel.AutoSize = true;
            authorLabel.Location = new Point(12, 34);
            authorLabel.Name = "authorLabel";
            authorLabel.Size = new Size(47, 15);
            authorLabel.TabIndex = 6;
            authorLabel.Text = "Author:";
            // 
            // authorLinkLabel
            // 
            authorLinkLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            authorLinkLabel.Location = new Point(54, 34);
            authorLinkLabel.Name = "authorLinkLabel";
            authorLinkLabel.Size = new Size(200, 15);
            authorLinkLabel.TabIndex = 7;
            authorLinkLabel.TabStop = true;
            authorLinkLabel.Text = "u/Arthrose";
            authorLinkLabel.LinkClicked += Author_LinkClicked;
            // 
            // AboutForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(351, 111);
            Controls.Add(authorLinkLabel);
            Controls.Add(authorLabel);
            Controls.Add(programVersionLabel);
            Controls.Add(OKButton);
            Controls.Add(gitHubLinkLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Properties";
            KeyDown += AboutForm_KeyDown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private LinkLabel gitHubLinkLabel;
        private Button OKButton;
        private Label programVersionLabel;
        private Label authorLabel;
        private LinkLabel authorLinkLabel;
    }
}