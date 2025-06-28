namespace HotsReplayReader
{
    partial class hotsReplayWebReader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hotsReplayWebReader));
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            browseToolStripMenuItem = new ToolStripMenuItem();
            sourceToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            accountsToolStripMenuItem = new ToolStripMenuItem();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            listBoxHotsReplays = new ListBox();
            folderBrowserDialog = new FolderBrowserDialog();
            menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, accountsToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1359, 24);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { browseToolStripMenuItem, sourceToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // browseToolStripMenuItem
            // 
            browseToolStripMenuItem.Name = "browseToolStripMenuItem";
            browseToolStripMenuItem.Size = new Size(112, 22);
            browseToolStripMenuItem.Text = "Browse";
            browseToolStripMenuItem.Click += BrowseToolStripMenuItem_Click;
            // 
            // sourceToolStripMenuItem
            // 
            sourceToolStripMenuItem.Name = "sourceToolStripMenuItem";
            sourceToolStripMenuItem.Size = new Size(112, 22);
            sourceToolStripMenuItem.Text = "Source";
            sourceToolStripMenuItem.Click += SourceToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(112, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // accountsToolStripMenuItem
            // 
            accountsToolStripMenuItem.Name = "accountsToolStripMenuItem";
            accountsToolStripMenuItem.Size = new Size(69, 20);
            accountsToolStripMenuItem.Text = "Accounts";
            // 
            // webView
            // 
            webView.AllowExternalDrop = false;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.White;
            webView.Location = new Point(260, 24);
            webView.Name = "webView";
            webView.Size = new Size(1099, 784);
            webView.Source = new Uri("about:blank", UriKind.Absolute);
            webView.TabIndex = 1;
            webView.ZoomFactor = 1D;
            // 
            // listBoxHotsReplays
            // 
            listBoxHotsReplays.FormattingEnabled = true;
            listBoxHotsReplays.Location = new Point(0, 24);
            listBoxHotsReplays.Name = "listBoxHotsReplays";
            listBoxHotsReplays.Size = new Size(254, 784);
            listBoxHotsReplays.TabIndex = 3;
            listBoxHotsReplays.SelectedIndexChanged += ListBoxHotsReplays_SelectedIndexChanged;
            // 
            // hotsReplayWebReader
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1359, 808);
            Controls.Add(listBoxHotsReplays);
            Controls.Add(webView);
            Controls.Add(menuStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = menuStrip;
            Name = "hotsReplayWebReader";
            Text = "Hots Replay Reader";
            Load += HotsReplayWebReader_Load;
            Resize += HotsReplayWebReader_Resize;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem accountsToolStripMenuItem;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private ListBox listBoxHotsReplays;
        private FolderBrowserDialog folderBrowserDialog;
        private ToolStripMenuItem browseToolStripMenuItem;
        private ToolStripMenuItem sourceToolStripMenuItem;
    }
}