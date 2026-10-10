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
            toolStripMenuItemFile = new ToolStripMenuItem();
            toolStripMenuItemBrowse = new ToolStripMenuItem();
            toolStripMenuItemSource = new ToolStripMenuItem();
            toolStripMenuItemExit = new ToolStripMenuItem();
            toolStripMenuItemRegion = new ToolStripMenuItem();
            toolStripMenuItemRegionAmericas = new ToolStripMenuItem();
            toolStripMenuItemRegionEurope = new ToolStripMenuItem();
            toolStripMenuItemRegionAsia = new ToolStripMenuItem();
            toolStripMenuItemRegionPTR = new ToolStripMenuItem();
            toolStripMenuItemAccounts = new ToolStripMenuItem();
            toolStripMenuItemOptions = new ToolStripMenuItem();
            toolStripMenuItemLanguage = new ToolStripMenuItem();
            toolStripMenuItemClearCache = new ToolStripMenuItem();
            toolStripMenuItemPreferences = new ToolStripMenuItem();
            toolStripMenuItemAbout = new ToolStripMenuItem();
            toolStripMenuItemUpdate = new ToolStripMenuItem();
            toolStripMenuItemAboutHotsReplayReader = new ToolStripMenuItem();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            listBoxHotsReplays = new ListBox();
            folderBrowserDialog = new FolderBrowserDialog();
            menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { toolStripMenuItemFile, toolStripMenuItemAccounts, toolStripMenuItemRegion, toolStripMenuItemOptions, toolStripMenuItemAbout });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1359, 24);
            menuStrip.TabIndex = 0;
            // 
            // toolStripMenuItemFile
            // 
            toolStripMenuItemFile.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItemBrowse, toolStripMenuItemSource, toolStripMenuItemExit });
            toolStripMenuItemFile.Name = "toolStripMenuItemFile";
            toolStripMenuItemFile.Size = new Size(37, 20);
            toolStripMenuItemFile.Text = Resources.Language.i18n.strMenuFile;
            // 
            // toolStripMenuItemBrowse
            // 
            toolStripMenuItemBrowse.Name = "toolStripMenuItemBrowse";
            toolStripMenuItemBrowse.Size = new Size(112, 22);
            toolStripMenuItemBrowse.Text = Resources.Language.i18n.strMenuBrowse;
            toolStripMenuItemBrowse.Click += BrowseToolStripMenuItem_Click;
            // 
            // toolStripMenuItemSource
            // 
            toolStripMenuItemSource.Name = "toolStripMenuItemSource";
            toolStripMenuItemSource.Size = new Size(112, 22);
            toolStripMenuItemSource.Text = Resources.Language.i18n.strMenuSource;
            toolStripMenuItemSource.Click += SourceToolStripMenuItem_Click;
            // 
            // toolStripMenuItemExit
            // 
            toolStripMenuItemExit.Name = "toolStripMenuItemExit";
            toolStripMenuItemExit.Size = new Size(112, 22);
            toolStripMenuItemExit.Text = Resources.Language.i18n.strMenuExit;
            toolStripMenuItemExit.Click += ExitToolStripMenuItem_Click;
            // 
            // toolStripMenuItemRegion
            // 
            toolStripMenuItemRegion.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItemRegionAmericas, toolStripMenuItemRegionEurope, toolStripMenuItemRegionAsia, toolStripMenuItemRegionPTR });
            toolStripMenuItemRegion.Name = "toolStripMenuItemRegion";
            toolStripMenuItemRegion.Size = new Size(56, 20);
            toolStripMenuItemRegion.Text = Resources.Language.i18n.strRegion;
            // 
            // toolStripMenuItemRegionAmericas
            // 
            toolStripMenuItemRegionAmericas.CheckOnClick = true;
            toolStripMenuItemRegionAmericas.Name = "toolStripMenuItemRegionAmericas";
            toolStripMenuItemRegionAmericas.Size = new Size(180, 22);
            toolStripMenuItemRegionAmericas.Tag = "1";
            toolStripMenuItemRegionAmericas.Text = Resources.Language.i18n.strRegionAmercas;
            toolStripMenuItemRegionAmericas.Click += RegionToolStripMenuItem_Click;
            // 
            // toolStripMenuItemRegionEurope
            // 
            toolStripMenuItemRegionEurope.CheckOnClick = true;
            toolStripMenuItemRegionEurope.Name = "toolStripMenuItemRegionEurope";
            toolStripMenuItemRegionEurope.Size = new Size(180, 22);
            toolStripMenuItemRegionEurope.Tag = "2";
            toolStripMenuItemRegionEurope.Text = Resources.Language.i18n.strRegionEurope;
            toolStripMenuItemRegionEurope.Click += RegionToolStripMenuItem_Click;
            // 
            // toolStripMenuItemRegionAsia
            // 
            toolStripMenuItemRegionAsia.CheckOnClick = true;
            toolStripMenuItemRegionAsia.Name = "toolStripMenuItemRegionAsia";
            toolStripMenuItemRegionAsia.Size = new Size(180, 22);
            toolStripMenuItemRegionAsia.Tag = "3";
            toolStripMenuItemRegionAsia.Text = Resources.Language.i18n.strRegionAsia;
            toolStripMenuItemRegionAsia.Click += RegionToolStripMenuItem_Click;
            // 
            // toolStripMenuItemRegionPTR
            // 
            toolStripMenuItemRegionPTR.CheckOnClick = true;
            toolStripMenuItemRegionPTR.Name = "toolStripMenuItemRegionPTR";
            toolStripMenuItemRegionPTR.Size = new Size(180, 22);
            toolStripMenuItemRegionPTR.Tag = "98";
            toolStripMenuItemRegionPTR.Text = "PTR";
            toolStripMenuItemRegionPTR.Click += RegionToolStripMenuItem_Click;
            // 
            // toolStripMenuItemAccounts
            // 
            toolStripMenuItemAccounts.Name = "toolStripMenuItemAccounts";
            toolStripMenuItemAccounts.Size = new Size(69, 20);
            toolStripMenuItemAccounts.Text = Resources.Language.i18n.strMenuAccounts;
            // 
            // toolStripMenuItemOptions
            // 
            toolStripMenuItemOptions.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItemLanguage, toolStripMenuItemClearCache, toolStripMenuItemPreferences });
            toolStripMenuItemOptions.Name = "toolStripMenuItemOptions";
            toolStripMenuItemOptions.Size = new Size(61, 20);
            toolStripMenuItemOptions.Text = Resources.Language.i18n.strMenuSettings;
            // 
            // toolStripMenuItemLanguage
            // 
            toolStripMenuItemLanguage.Name = "toolStripMenuItemLanguage";
            toolStripMenuItemLanguage.Size = new Size(135, 22);
            toolStripMenuItemLanguage.Text = Resources.Language.i18n.strMenuLanguage;
            // 
            // toolStripMenuItemClearCache
            // 
            toolStripMenuItemClearCache.Name = "toolStripMenuItemClearCache";
            toolStripMenuItemClearCache.Size = new Size(135, 22);
            toolStripMenuItemClearCache.Text = Resources.Language.i18n.strMenuClearCache;
            toolStripMenuItemClearCache.Click += ClearCacheToolStripMenuItem_Click;
            // 
            // toolStripMenuItemProperties
            // 
            toolStripMenuItemPreferences.Name = "toolStripMenuItemProperties";
            toolStripMenuItemPreferences.Size = new Size(135, 22);
            toolStripMenuItemPreferences.Text = Resources.Language.i18n.strPreferences;
            toolStripMenuItemPreferences.Click += PreferencesToolStripMenuItem_Click;
            // 
            // toolStripMenuItemAbout
            // 
            toolStripMenuItemAbout.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItemUpdate, toolStripMenuItemAboutHotsReplayReader });
            toolStripMenuItemAbout.Name = "toolStripMenuItemAbout";
            toolStripMenuItemAbout.Size = new Size(24, 20);
            toolStripMenuItemAbout.Text = "?";
            // 
            // toolStripMenuItemUpdate
            // 
            toolStripMenuItemUpdate.Name = "toolStripMenuItemUpdate";
            toolStripMenuItemUpdate.Size = new Size(116, 22);
            toolStripMenuItemUpdate.Text = Resources.Language.i18n.strMenuUpdate;
            toolStripMenuItemUpdate.Click += UpdateToolStripMenuItem_Click;
            // 
            // toolStripMenuItemAboutHotsReplayReader
            // 
            toolStripMenuItemAboutHotsReplayReader.Name = "toolStripMenuItemAboutHotsReplayReader";
            toolStripMenuItemAboutHotsReplayReader.Size = new Size(116, 22);
            toolStripMenuItemAboutHotsReplayReader.Text = Resources.Language.i18n.strMenuAbout;
            toolStripMenuItemAboutHotsReplayReader.Click += AboutHotsReplayReaderToolStripMenuItem_Click;
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
            MinimumSize = new Size(1331, 490);
            Name = "HotsReplayWebReader";
            Text = "HotS Replay Reader";
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
        private ToolStripMenuItem toolStripMenuItemFile;
        private ToolStripMenuItem toolStripMenuItemExit;
        private ToolStripMenuItem toolStripMenuItemAccounts;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private ListBox listBoxHotsReplays;
        private FolderBrowserDialog folderBrowserDialog;
        private ToolStripMenuItem toolStripMenuItemBrowse;
        private ToolStripMenuItem toolStripMenuItemSource;
        private ToolStripMenuItem toolStripMenuItemLanguage;
        private ToolStripMenuItem toolStripMenuItemRegion;
        private ToolStripMenuItem toolStripMenuItemRegionAmericas;
        private ToolStripMenuItem toolStripMenuItemRegionEurope;
        private ToolStripMenuItem toolStripMenuItemRegionAsia;
        private ToolStripMenuItem toolStripMenuItemPreferences;
        private ToolStripMenuItem toolStripMenuItemAbout;
        private ToolStripMenuItem toolStripMenuItemAboutHotsReplayReader;
        private ToolStripMenuItem toolStripMenuItemUpdate;
        private ToolStripMenuItem toolStripMenuItemClearCache;
        private ToolStripMenuItem toolStripMenuItemOptions;
        private ToolStripMenuItem toolStripMenuItemRegionPTR;
    }
}