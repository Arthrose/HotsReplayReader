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
            OKButton.Location = new Point(264, 71);
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
            deepLTextBox.Location = new Point(12, 42);
            deepLTextBox.Name = "deepLTextBox";
            deepLTextBox.Size = new Size(246, 23);
            deepLTextBox.TabIndex = 2;
            // 
            // testButton
            // 
            testButton.Location = new Point(264, 42);
            testButton.Name = "testButton";
            testButton.Size = new Size(75, 23);
            testButton.TabIndex = 3;
            testButton.Text = "Test";
            testButton.UseVisualStyleBackColor = true;
            testButton.Click += TestButton_Click;
            // 
            // PropertiesForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(351, 104);
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
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label deepLLabel;
        private LinkLabel deepLLinkLabel;
        private Button OKButton;
        private TextBox deepLTextBox;
        private Button testButton;
    }
}