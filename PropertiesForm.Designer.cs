namespace HotsReplayReader
{
    partial class PropertiesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertiesForm));
            deepLLabel = new Label();
            deepLLinkLabel = new LinkLabel();
            OKButton = new Button();
            deepLTextBox = new TextBox();
            testButton = new Button();
            groupBoxDisplay = new GroupBox();
            checkedListBoxDisplayReplaySideBar = new CheckedListBox();
            checkedListBoxDisplayPingButton = new CheckedListBox();
            groupBoxDisplay.SuspendLayout();
            SuspendLayout();
            // 
            // deepLLabel
            // 
            deepLLabel.AutoSize = true;
            deepLLabel.Location = new Point(12, 9);
            deepLLabel.Name = "deepLLabel";
            deepLLabel.Size = new Size(82, 15);
            deepLLabel.TabIndex = 0;
            deepLLabel.Text = "DeepL API key";
            // 
            // deepLLinkLabel
            // 
            deepLLinkLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            deepLLinkLabel.Location = new Point(139, 9);
            deepLLinkLabel.Name = "deepLLinkLabel";
            deepLLinkLabel.Size = new Size(200, 15);
            deepLLinkLabel.TabIndex = 4;
            deepLLinkLabel.TabStop = true;
            deepLLinkLabel.Text = "Visit DeepL website";
            deepLLinkLabel.TextAlign = ContentAlignment.TopRight;
            deepLLinkLabel.LinkClicked += DeepLLinkLabel_LinkClicked;
            // 
            // OKButton
            // 
            OKButton.Location = new Point(264, 142);
            OKButton.Name = "OKButton";
            OKButton.Size = new Size(75, 23);
            OKButton.TabIndex = 1;
            OKButton.Text = "OK";
            OKButton.UseVisualStyleBackColor = true;
            OKButton.Click += OKButton_Click;
            // 
            // deepLTextBox
            // 
            deepLTextBox.BorderStyle = BorderStyle.FixedSingle;
            deepLTextBox.Location = new Point(12, 27);
            deepLTextBox.Name = "deepLTextBox";
            deepLTextBox.Size = new Size(246, 23);
            deepLTextBox.TabIndex = 2;
            // 
            // testButton
            // 
            testButton.Location = new Point(264, 27);
            testButton.Name = "testButton";
            testButton.Size = new Size(75, 23);
            testButton.TabIndex = 3;
            testButton.Text = "Test";
            testButton.UseVisualStyleBackColor = true;
            testButton.Click += TestButton_Click;
            // 
            // groupBoxDisplay
            // 
            groupBoxDisplay.Controls.Add(checkedListBoxDisplayPingButton);
            groupBoxDisplay.Controls.Add(checkedListBoxDisplayReplaySideBar);
            groupBoxDisplay.Location = new Point(12, 56);
            groupBoxDisplay.Name = "groupBoxDisplay";
            groupBoxDisplay.Size = new Size(327, 80);
            groupBoxDisplay.TabIndex = 5;
            groupBoxDisplay.TabStop = false;
            groupBoxDisplay.Text = "Display";
            // 
            // checkedListBoxDisplayReplaySideBar
            // 
            checkedListBoxDisplayReplaySideBar.FormattingEnabled = true;
            checkedListBoxDisplayReplaySideBar.Location = new Point(6, 22);
            checkedListBoxDisplayReplaySideBar.Name = "checkedListBoxDisplayReplaySideBar";
            checkedListBoxDisplayReplaySideBar.Size = new Size(315, 22);
            checkedListBoxDisplayReplaySideBar.TabIndex = 0;
            // 
            // checkedListBoxDisplayPingButton
            // 
            checkedListBoxDisplayPingButton.FormattingEnabled = true;
            checkedListBoxDisplayPingButton.Location = new Point(6, 50);
            checkedListBoxDisplayPingButton.Name = "checkedListBoxDisplayPingButton";
            checkedListBoxDisplayPingButton.Size = new Size(315, 22);
            checkedListBoxDisplayPingButton.TabIndex = 1;
            // 
            // PropertiesForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(351, 175);
            Controls.Add(groupBoxDisplay);
            Controls.Add(testButton);
            Controls.Add(deepLTextBox);
            Controls.Add(OKButton);
            Controls.Add(deepLLinkLabel);
            Controls.Add(deepLLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PropertiesForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Properties";
            KeyDown += PropertiesForm_KeyDown;
            groupBoxDisplay.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label deepLLabel;
        private LinkLabel deepLLinkLabel;
        private Button OKButton;
        private TextBox deepLTextBox;
        private Button testButton;
        private GroupBox groupBoxDisplay;
        private CheckedListBox checkedListBoxDisplayPingButton;
        private CheckedListBox checkedListBoxDisplayReplaySideBar;
    }
}