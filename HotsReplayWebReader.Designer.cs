namespace HotsReplayReader
{
    partial class HotsReplayWebReader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HotsReplayWebReader));
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            propertiesToolStripMenuItem = new ToolStripMenuItem();
            browseToolStripMenuItem = new ToolStripMenuItem();
            sourceToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            regionToolStripMenuItem = new ToolStripMenuItem();
            americasRegionToolStripMenuItem = new ToolStripMenuItem();
            europeRegionToolStripMenuItem = new ToolStripMenuItem();
            asiaRegionToolStripMenuItem = new ToolStripMenuItem();
            accountsToolStripMenuItem = new ToolStripMenuItem();
            languageToolStripMenuItem = new ToolStripMenuItem();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            listBoxHotsReplays = new ListBox();
            folderBrowserDialog = new FolderBrowserDialog();
            menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, regionToolStripMenuItem, accountsToolStripMenuItem, languageToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1359, 24);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { browseToolStripMenuItem, sourceToolStripMenuItem, propertiesToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = Resources.Language.i18n.strMenuFile;
            // 
            // propertiesToolStripMenuItem
            // 
            propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            propertiesToolStripMenuItem.Size = new Size(180, 22);
            propertiesToolStripMenuItem.Text = Resources.Language.i18n.strProperties;
            propertiesToolStripMenuItem.Click += PropertiesToolStripMenuItem_Click;
            // 
            // browseToolStripMenuItem
            // 
            browseToolStripMenuItem.Name = "browseToolStripMenuItem";
            browseToolStripMenuItem.Size = new Size(180, 22);
            browseToolStripMenuItem.Text = Resources.Language.i18n.strMenuBrowse;
            browseToolStripMenuItem.Click += BrowseToolStripMenuItem_Click;
            // 
            // sourceToolStripMenuItem
            // 
            sourceToolStripMenuItem.Name = "sourceToolStripMenuItem";
            sourceToolStripMenuItem.Size = new Size(180, 22);
            sourceToolStripMenuItem.Text = Resources.Language.i18n.strMenuSource;
            sourceToolStripMenuItem.Click += SourceToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 22);
            exitToolStripMenuItem.Text = Resources.Language.i18n.strMenuExit;
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // regionToolStripMenuItem
            // 
            regionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { americasRegionToolStripMenuItem, europeRegionToolStripMenuItem, asiaRegionToolStripMenuItem });
            regionToolStripMenuItem.Name = "regionToolStripMenuItem";
            regionToolStripMenuItem.Size = new Size(56, 20);
            regionToolStripMenuItem.Text = Resources.Language.i18n.strRegion;
            // 
            // americasRegionToolStripMenuItem
            // 
            americasRegionToolStripMenuItem.CheckOnClick = true;
            americasRegionToolStripMenuItem.Name = "americasRegionToolStripMenuItem";
            americasRegionToolStripMenuItem.Size = new Size(180, 22);
            americasRegionToolStripMenuItem.Tag = "1";
            americasRegionToolStripMenuItem.Text = Resources.Language.i18n.strRegionAmercas;
            americasRegionToolStripMenuItem.Click += RegionToolStripMenuItem_Click;
            // 
            // europeRegionToolStripMenuItem
            // 
            europeRegionToolStripMenuItem.CheckOnClick = true;
            europeRegionToolStripMenuItem.Name = "europeRegionToolStripMenuItem";
            europeRegionToolStripMenuItem.Size = new Size(180, 22);
            europeRegionToolStripMenuItem.Tag = "2";
            europeRegionToolStripMenuItem.Text = Resources.Language.i18n.strRegionEurope;
            europeRegionToolStripMenuItem.Click += RegionToolStripMenuItem_Click;
            // 
            // asiaRegionToolStripMenuItem
            // 
            asiaRegionToolStripMenuItem.CheckOnClick = true;
            asiaRegionToolStripMenuItem.Name = "asiaRegionToolStripMenuItem";
            asiaRegionToolStripMenuItem.Size = new Size(180, 22);
            asiaRegionToolStripMenuItem.Tag = "3";
            asiaRegionToolStripMenuItem.Text = Resources.Language.i18n.strRegionAsia;
            asiaRegionToolStripMenuItem.Click += RegionToolStripMenuItem_Click;
            // 
            // accountsToolStripMenuItem
            // 
            accountsToolStripMenuItem.Name = "accountsToolStripMenuItem";
            accountsToolStripMenuItem.Size = new Size(69, 20);
            accountsToolStripMenuItem.Text = Resources.Language.i18n.strMenuAccounts;
            // 
            // languageToolStripMenuItem
            // 
            languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            languageToolStripMenuItem.Size = new Size(71, 20);
            languageToolStripMenuItem.Text = Resources.Language.i18n.strMenuLanguage;
            // 
            // webView
            // 
            webView.AllowExternalDrop = false;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.White;
            webView.Dock = DockStyle.Fill;
            webView.Location = new Point(0, 24);
            webView.Name = "webView";
            webView.Size = new Size(1359, 784);
            webView.TabIndex = 3;
            webView.ZoomFactor = 1D;
            // 
            // listBoxHotsReplays
            // 
            listBoxHotsReplays.BackColor = SystemColors.ControlDarkDark;
            listBoxHotsReplays.Dock = DockStyle.Left;
            listBoxHotsReplays.ForeColor = SystemColors.Window;
            listBoxHotsReplays.FormattingEnabled = true;
            listBoxHotsReplays.IntegralHeight = false;
            listBoxHotsReplays.Location = new Point(0, 24);
            listBoxHotsReplays.Name = "listBoxHotsReplays";
            listBoxHotsReplays.Size = new Size(254, 784);
            listBoxHotsReplays.TabIndex = 1;
            listBoxHotsReplays.Visible = false;
            listBoxHotsReplays.SelectedIndexChanged += ListBoxHotsReplays_SelectedIndexChanged;
            // 
            // HotsReplayWebReader
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
            MinimumSize = new Size(1041, 490);
            Name = "HotsReplayWebReader";
            Text = "Hots Replay Reader";
            FormClosed += HotsReplayWebReader_FormClosed;
            Load += HotsReplayWebReader_Load;
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
        private ToolStripMenuItem languageToolStripMenuItem;
        private ToolStripMenuItem regionToolStripMenuItem;
        private ToolStripMenuItem americasRegionToolStripMenuItem;
        private ToolStripMenuItem europeRegionToolStripMenuItem;
        private ToolStripMenuItem asiaRegionToolStripMenuItem;
        private ToolStripMenuItem propertiesToolStripMenuItem;
    }
}