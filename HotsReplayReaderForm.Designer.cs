namespace HotsReplayReader
{
    partial class hotsReplayReaderForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hotsReplayReaderForm));
            folderBrowserDialog = new FolderBrowserDialog();
            buttonHotsReplayFolder = new Button();
            listBoxHotsReplays = new ListBox();
            labelHotsReplayFolder = new Label();
            richTextBoxHotsReplayMessages = new RichTextBox();
            comboBoxHotsAccounts = new ComboBox();
            SuspendLayout();
            // 
            // buttonHotsReplayFolder
            // 
            buttonHotsReplayFolder.Location = new Point(12, 12);
            buttonHotsReplayFolder.Name = "buttonHotsReplayFolder";
            buttonHotsReplayFolder.Size = new Size(75, 23);
            buttonHotsReplayFolder.TabIndex = 1;
            buttonHotsReplayFolder.Text = "Browse";
            buttonHotsReplayFolder.UseVisualStyleBackColor = true;
            buttonHotsReplayFolder.Click += buttonHotsReplayFolder_Click;
            // 
            // listBoxHotsReplays
            // 
            listBoxHotsReplays.FormattingEnabled = true;
            listBoxHotsReplays.Location = new Point(12, 41);
            listBoxHotsReplays.Name = "listBoxHotsReplays";
            listBoxHotsReplays.Size = new Size(342, 679);
            listBoxHotsReplays.TabIndex = 2;
            listBoxHotsReplays.SelectedIndexChanged += listBoxHotsReplays_SelectedIndexChanged;
            // 
            // labelHotsReplayFolder
            // 
            labelHotsReplayFolder.AutoSize = true;
            labelHotsReplayFolder.Location = new Point(280, 16);
            labelHotsReplayFolder.Name = "labelHotsReplayFolder";
            labelHotsReplayFolder.Size = new Size(23, 15);
            labelHotsReplayFolder.TabIndex = 3;
            labelHotsReplayFolder.Text = "C:\\";
            // 
            // richTextBoxHotsReplayMessages
            // 
            richTextBoxHotsReplayMessages.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBoxHotsReplayMessages.Location = new Point(360, 41);
            richTextBoxHotsReplayMessages.Name = "richTextBoxHotsReplayMessages";
            richTextBoxHotsReplayMessages.Size = new Size(640, 679);
            richTextBoxHotsReplayMessages.TabIndex = 4;
            richTextBoxHotsReplayMessages.Text = "";
            // 
            // comboBoxHotsAccounts
            // 
            comboBoxHotsAccounts.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxHotsAccounts.FormattingEnabled = true;
            comboBoxHotsAccounts.Location = new Point(93, 12);
            comboBoxHotsAccounts.Name = "comboBoxHotsAccounts";
            comboBoxHotsAccounts.Size = new Size(181, 23);
            comboBoxHotsAccounts.TabIndex = 5;
            comboBoxHotsAccounts.SelectedIndexChanged += comboBoxHotsAccounts_SelectedIndexChanged;
            // 
            // hotsReplayReaderForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1012, 729);
            Controls.Add(comboBoxHotsAccounts);
            Controls.Add(richTextBoxHotsReplayMessages);
            Controls.Add(labelHotsReplayFolder);
            Controls.Add(listBoxHotsReplays);
            Controls.Add(buttonHotsReplayFolder);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "hotsReplayReaderForm";
            Text = "Hots Replay Reader";
            Load += hotsReplayReaderForm_Load;
            KeyUp += hotsReplayReaderForm_KeyUp;
            Resize += hotsReplayReaderForm_Resize;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FolderBrowserDialog folderBrowserDialog;
        private Button buttonHotsReplayFolder;
        private ListBox listBoxHotsReplays;
        private Label labelHotsReplayFolder;
        private RichTextBox richTextBoxHotsReplayMessages;
        private ComboBox comboBoxHotsAccounts;
    }
}
