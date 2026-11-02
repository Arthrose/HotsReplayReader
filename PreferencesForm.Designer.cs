namespace HotsReplayReader
{
    partial class PreferencesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PreferencesForm));
            deepLLabel = new Label();
            deepLLinkLabel = new LinkLabel();
            OKButton = new Button();
            deepLTextBox = new TextBox();
            deepLTestButton = new Button();
            groupBoxDisplay = new GroupBox();
            checkBoxMessagesShow = new CheckBox();
            checkBoxMessagesEnnemyChat = new CheckBox();
            checkBoxMessagesFriendlyChat = new CheckBox();
            checkBoxMessagesPingButton = new CheckBox();
            checkBoxDisplayGameMode = new CheckBox();
            checkBoxDisplayDate = new CheckBox();
            checkBoxDisplayDraftOrder = new CheckBox();
            checkBoxDisplayReplaySideBar = new CheckBox();
            groupBoxDarkMode = new GroupBox();
            radioButtonDarkModeAutomatic = new RadioButton();
            radioButtonDarkModeDark = new RadioButton();
            radioButtonDarkModeLight = new RadioButton();
            groupBoxDisplay.SuspendLayout();
            groupBoxDarkMode.SuspendLayout();
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
            deepLLinkLabel.Location = new Point(189, 9);
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
            OKButton.Location = new Point(315, 263);
            OKButton.Name = "OKButton";
            OKButton.Size = new Size(75, 23);
            OKButton.TabIndex = 1;
            OKButton.Text = Resources.Language.i18n.strPreferencesOK;
            OKButton.UseVisualStyleBackColor = true;
            OKButton.Click += OKButton_Click;
            // 
            // deepLTextBox
            // 
            deepLTextBox.BorderStyle = BorderStyle.FixedSingle;
            deepLTextBox.Location = new Point(12, 27);
            deepLTextBox.Name = "deepLTextBox";
            deepLTextBox.Size = new Size(297, 23);
            deepLTextBox.TabIndex = 2;
            // 
            // deepLTestButton
            // 
            deepLTestButton.Location = new Point(314, 27);
            deepLTestButton.Name = "deepLTestButton";
            deepLTestButton.Size = new Size(75, 23);
            deepLTestButton.TabIndex = 3;
            deepLTestButton.Text = Resources.Language.i18n.strPreferencesTest;
            deepLTestButton.UseVisualStyleBackColor = true;
            deepLTestButton.Click += TestButton_Click;
            // 
            // groupBoxDisplay
            // 
            groupBoxDisplay.Controls.Add(checkBoxMessagesShow);
            groupBoxDisplay.Controls.Add(checkBoxMessagesEnnemyChat);
            groupBoxDisplay.Controls.Add(checkBoxMessagesFriendlyChat);
            groupBoxDisplay.Controls.Add(checkBoxMessagesPingButton);
            groupBoxDisplay.Controls.Add(checkBoxDisplayGameMode);
            groupBoxDisplay.Controls.Add(checkBoxDisplayDate);
            groupBoxDisplay.Controls.Add(checkBoxDisplayDraftOrder);
            groupBoxDisplay.Controls.Add(checkBoxDisplayReplaySideBar);
            groupBoxDisplay.Location = new Point(18, 60);
            groupBoxDisplay.Name = "groupBoxDisplay";
            groupBoxDisplay.Size = new Size(194, 226);
            groupBoxDisplay.TabIndex = 5;
            groupBoxDisplay.TabStop = false;
            groupBoxDisplay.Text = "Display";
            // 
            // checkBoxMessagesShow
            // 
            checkBoxMessagesShow.AutoSize = true;
            checkBoxMessagesShow.Location = new Point(6, 122);
            checkBoxMessagesShow.Name = "checkBoxMessagesShow";
            checkBoxMessagesShow.Size = new Size(94, 19);
            checkBoxMessagesShow.TabIndex = 8;
            checkBoxMessagesShow.Text = Resources.Language.i18n.strPreferencesMessagesFriendly;
            checkBoxMessagesShow.UseVisualStyleBackColor = true;
            checkBoxMessagesShow.CheckedChanged += CheckBoxMessagesShow_CheckedChanged;
            // 
            // checkBoxMessagesEnnemyChat
            // 
            checkBoxMessagesEnnemyChat.AutoSize = true;
            checkBoxMessagesEnnemyChat.Location = new Point(28, 172);
            checkBoxMessagesEnnemyChat.Name = "checkBoxMessagesEnnemyChat";
            checkBoxMessagesEnnemyChat.Size = new Size(88, 19);
            checkBoxMessagesEnnemyChat.TabIndex = 7;
            checkBoxMessagesEnnemyChat.Text = Resources.Language.i18n.strPreferencesMessagesEnnemy;
            checkBoxMessagesEnnemyChat.UseVisualStyleBackColor = true;
            // 
            // checkBoxMessagesFriendlyChat
            // 
            checkBoxMessagesFriendlyChat.AutoSize = true;
            checkBoxMessagesFriendlyChat.Location = new Point(28, 147);
            checkBoxMessagesFriendlyChat.Name = "checkBoxMessagesFriendlyChat";
            checkBoxMessagesFriendlyChat.Size = new Size(94, 19);
            checkBoxMessagesFriendlyChat.TabIndex = 6;
            checkBoxMessagesFriendlyChat.Text = Resources.Language.i18n.strPreferencesMessagesFriendly;
            checkBoxMessagesFriendlyChat.UseVisualStyleBackColor = true;
            // 
            // checkBoxMessagesPingButton
            // 
            checkBoxMessagesPingButton.AutoSize = true;
            checkBoxMessagesPingButton.Location = new Point(28, 197);
            checkBoxMessagesPingButton.Name = "checkBoxMessagesPingButton";
            checkBoxMessagesPingButton.Size = new Size(89, 19);
            checkBoxMessagesPingButton.TabIndex = 5;
            checkBoxMessagesPingButton.Text = Resources.Language.i18n.strPreferencesDisplayPingButton;
            checkBoxMessagesPingButton.UseVisualStyleBackColor = true;
            // 
            // checkBoxDisplayGameMode
            // 
            checkBoxDisplayGameMode.AutoSize = true;
            checkBoxDisplayGameMode.Location = new Point(6, 22);
            checkBoxDisplayGameMode.Name = "checkBoxDisplayGameMode";
            checkBoxDisplayGameMode.Size = new Size(91, 19);
            checkBoxDisplayGameMode.TabIndex = 4;
            checkBoxDisplayGameMode.Text = Resources.Language.i18n.strPreferencesDisplayGameMode;
            checkBoxDisplayGameMode.UseVisualStyleBackColor = true;
            // 
            // checkBoxDisplayDate
            // 
            checkBoxDisplayDate.AutoSize = true;
            checkBoxDisplayDate.Location = new Point(6, 47);
            checkBoxDisplayDate.Name = "checkBoxDisplayDate";
            checkBoxDisplayDate.Size = new Size(50, 19);
            checkBoxDisplayDate.TabIndex = 3;
            checkBoxDisplayDate.Text = Resources.Language.i18n.strPreferencesDisplayDate;
            checkBoxDisplayDate.UseVisualStyleBackColor = true;
            // 
            // checkBoxDisplayDraftOrder
            // 
            checkBoxDisplayDraftOrder.AutoSize = true;
            checkBoxDisplayDraftOrder.Location = new Point(6, 97);
            checkBoxDisplayDraftOrder.Name = "checkBoxDisplayDraftOrder";
            checkBoxDisplayDraftOrder.Size = new Size(83, 19);
            checkBoxDisplayDraftOrder.TabIndex = 2;
            checkBoxDisplayDraftOrder.Text = Resources.Language.i18n.strPreferencesDisplayDraftOrder;
            checkBoxDisplayDraftOrder.UseVisualStyleBackColor = true;
            // 
            // checkBoxDisplayReplaySideBar
            // 
            checkBoxDisplayReplaySideBar.AutoSize = true;
            checkBoxDisplayReplaySideBar.Location = new Point(6, 72);
            checkBoxDisplayReplaySideBar.Name = "checkBoxDisplayReplaySideBar";
            checkBoxDisplayReplaySideBar.Size = new Size(102, 19);
            checkBoxDisplayReplaySideBar.TabIndex = 0;
            checkBoxDisplayReplaySideBar.Text = Resources.Language.i18n.strPreferencesDisplayReplaySidebar;
            checkBoxDisplayReplaySideBar.UseVisualStyleBackColor = true;
            // 
            // groupBoxDarkMode
            // 
            groupBoxDarkMode.Controls.Add(radioButtonDarkModeAutomatic);
            groupBoxDarkMode.Controls.Add(radioButtonDarkModeDark);
            groupBoxDarkMode.Controls.Add(radioButtonDarkModeLight);
            groupBoxDarkMode.Location = new Point(218, 60);
            groupBoxDarkMode.Name = "groupBoxDarkMode";
            groupBoxDarkMode.Size = new Size(172, 100);
            groupBoxDarkMode.TabIndex = 6;
            groupBoxDarkMode.TabStop = false;
            groupBoxDarkMode.Text = "Dark Mode";
            // 
            // radioButtonDarkModeAutomatic
            // 
            radioButtonDarkModeAutomatic.AutoSize = true;
            radioButtonDarkModeAutomatic.Location = new Point(16, 71);
            radioButtonDarkModeAutomatic.Name = "radioButtonDarkModeAutomatic";
            radioButtonDarkModeAutomatic.Size = new Size(81, 19);
            radioButtonDarkModeAutomatic.TabIndex = 2;
            radioButtonDarkModeAutomatic.TabStop = true;
            radioButtonDarkModeAutomatic.Text = Resources.Language.i18n.strPreferencesDarkModeAutomatic;
            radioButtonDarkModeAutomatic.UseVisualStyleBackColor = true;
            // 
            // radioButtonDarkModeDark
            // 
            radioButtonDarkModeDark.AutoSize = true;
            radioButtonDarkModeDark.Location = new Point(16, 47);
            radioButtonDarkModeDark.Name = "radioButtonDarkModeDark";
            radioButtonDarkModeDark.Size = new Size(83, 19);
            radioButtonDarkModeDark.TabIndex = 1;
            radioButtonDarkModeDark.TabStop = true;
            radioButtonDarkModeDark.Text = Resources.Language.i18n.strPreferencesDarkModeDarkMode;
            radioButtonDarkModeDark.UseVisualStyleBackColor = true;
            // 
            // radioButtonDarkModeLight
            // 
            radioButtonDarkModeLight.AutoSize = true;
            radioButtonDarkModeLight.Location = new Point(16, 22);
            radioButtonDarkModeLight.Name = "radioButtonDarkModeLight";
            radioButtonDarkModeLight.Size = new Size(86, 19);
            radioButtonDarkModeLight.TabIndex = 0;
            radioButtonDarkModeLight.TabStop = true;
            radioButtonDarkModeLight.Text = Resources.Language.i18n.strPreferencesDarkModeLightMode;
            radioButtonDarkModeLight.UseVisualStyleBackColor = true;
            // 
            // PreferencesForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(401, 297);
            Controls.Add(groupBoxDarkMode);
            Controls.Add(groupBoxDisplay);
            Controls.Add(deepLTestButton);
            Controls.Add(deepLTextBox);
            Controls.Add(OKButton);
            Controls.Add(deepLLinkLabel);
            Controls.Add(deepLLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PreferencesForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Preferences";
            KeyDown += PreferencesForm_KeyDown;
            groupBoxDisplay.ResumeLayout(false);
            groupBoxDisplay.PerformLayout();
            groupBoxDarkMode.ResumeLayout(false);
            groupBoxDarkMode.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label deepLLabel;
        private LinkLabel deepLLinkLabel;
        private Button OKButton;
        private TextBox deepLTextBox;
        private Button deepLTestButton;
        private GroupBox groupBoxDisplay;
        private CheckBox checkBoxDisplayReplaySideBar;
        private CheckBox checkBoxDisplayDraftOrder;
        private CheckBox checkBoxDisplayGameMode;
        private CheckBox checkBoxDisplayDate;
        private GroupBox groupBoxDarkMode;
        private RadioButton radioButtonDarkModeAutomatic;
        private RadioButton radioButtonDarkModeDark;
        private RadioButton radioButtonDarkModeLight;
        private CheckBox checkBoxMessagesShow;
        private CheckBox checkBoxMessagesEnnemyChat;
        private CheckBox checkBoxMessagesFriendlyChat;
        private CheckBox checkBoxMessagesPingButton;
    }
}