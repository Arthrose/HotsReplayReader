using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Heroes.Icons.DataDocument;
using Heroes.Models;
using Heroes.Models.AbilityTalents;
using Heroes.StormReplayParser.Player;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class HotsReplayWebReader : Form
    {
        readonly internal string defaultLangCode = "en-US";
        public Dictionary<string, string> LangCodeList = new()
        {
            ["de-DE"] = "Deutsch",
            ["en-US"] = "English",
            ["es-ES"] = "Español (España)",
            ["es-MX"] = "Español (México)",
            ["fr-FR"] = "Français",
            ["it-IT"] = "Italiano",
            ["ko-KR"] = "한국어",
            ["pl-PL"] = "Polski",
            ["pt-BR"] = "Português",
            ["ru-RU"] = "Русский",
            ["zh-TW"] = "中文"
        };

        readonly bool release = false;

        readonly bool fetchHero = false;
        readonly string fetchedHeroName = "The Lost Vikings";

        private string? hotsReplayFolder;
        internal static string currentAccount = string.Empty;

        private HotsReplay? hotsReplay;
        private HotsTeam? redTeam;
        private HotsTeam? blueTeam;
        private Dictionary<string, string>? hotsParties;
        private HotsPlayer[]? hotsPlayers;

        private readonly string formTitle = "Hots Replay Reader";

        // List ofs replays index and path
        readonly private Dictionary<int, string> replayList;

        // Listen to file system modification's notifications
        private FileSystemWatcher? fileSystemWatcher;
        readonly string tempDataFolder = Path.Combine(Path.GetTempPath(), "HotsReplayReader");
        readonly string webViewDllPath;

        // Dark mode
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

        internal string? htmlContent;

        internal string? dbVersion;
        internal HeroDataDocument? heroDataDocument;
        internal GameStringsRoot? gameStringsRoot;
        internal MatchAwards? matchAwards;

        //readonly string apiKey = "f67e8f89-a1e6-40d0-9f65-df409134342f:fx";

        internal DeepLTranslator? translator;

        readonly private string welcomeHTML = $@"<html>
<head>
<script>
  // Désactive le menu contextuel
  document.addEventListener('DOMContentLoaded', () => {{
    document.addEventListener('contextmenu', (e) => {{
      e.preventDefault()
    }})
  }})

  // Affice la liste des replays
  document.addEventListener(""mousemove"", function (e) {{
    // Détection si la souris est dans les 50px à gauche
    const isHover = e.clientX <= 50;
    // On envoie à C# uniquement quand le statut change
    if (window.__lastHover !== isHover) {{
      console.log(`X: ${{event.clientX}}, Y: ${{event.clientY}}`);
      window.chrome.webview.postMessage({{
        action: ""hoverLeft"",
        isHover: isHover
      }});
      window.__lastHover = isHover;
    }}
  }});
</script>
</head>
<body style=""background: url(app://hotsResources/Welcome.jpg) no-repeat center center; background-size: cover; background-color: black; margin: 0; height: 100%;""></body>
</html>";

        internal Init Init = new();
        public HotsReplayWebReader()
        {
            webViewDllPath = Path.Combine(tempDataFolder, "WebView2Loader.dll");
            byte[] webViewDllBytes = Resources.HotsResources.WebView2Loader;
            Directory.CreateDirectory(tempDataFolder);
            File.WriteAllBytes(webViewDllPath, webViewDllBytes);

            // Charge la dernière langue utilisée
            if (Init.config!.LangCode != null && LangCodeList.ContainsKey(Init.config.LangCode))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Init.config.LangCode);
            }
            else
            {
                Init.config.LangCode = defaultLangCode;
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Init.config.LangCode);
            }

            InitializeComponent();

            if (release)
                sourceToolStripMenuItem.Visible = false;

            if (Init.config.DeepLAPIKey != null)
                translator = new DeepLTranslator(Init.config.DeepLAPIKey);

            replayList = [];

            // Coche la région sélectionnée
            switch (Init.config!.Region)
            {
                case "1":
                    americasRegionToolStripMenuItem.Checked = true;
                    break;
                case "2":
                    europeRegionToolStripMenuItem.Checked = true;
                    break;
                case "3":
                    asiaRegionToolStripMenuItem.Checked = true;
                    break;
                default:
                    europeRegionToolStripMenuItem.Checked = true;
                    break;
            }

            LoadAccountsToolStipMenu();

            ToolStripMenuItem[] languageToolStripMenu = new ToolStripMenuItem[LangCodeList.Count];
            int j = 0;
            foreach (KeyValuePair<string, string> lang in LangCodeList)
            {
                languageToolStripMenu[j] = new ToolStripMenuItem
                {
                    Name = lang.Key,
                    Tag = lang.Key,
                    Text = lang.Value
                };
                languageToolStripMenu[j].Click += new EventHandler(LanguageMenuItemClickHandler);
                languageToolStripMenu[j].CheckOnClick = true;

                if (lang.Key == Init.config.LangCode)
                    languageToolStripMenu[j].Checked = true;

                j++;
            }
            languageToolStripMenuItem.DropDownItems.AddRange(languageToolStripMenu);
        }
        // Dark Mode
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            int useDarkMode = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", -1) == 0) ? 1 : 0;

            // Try latest first
            if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                // Fallback for older Windows 10 builds
                NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }

            if (useDarkMode == 1)
            {
                listBoxHotsReplays.BackColor = Color.FromArgb(30, 30, 30);
                listBoxHotsReplays.ForeColor = Color.FromArgb(200, 200, 200);

                // Mode en sombre de la barre menuStrip
                menuStrip.BackColor = Color.FromArgb(30, 30, 30);
                menuStrip.ForeColor = Color.White;
                menuStrip.Renderer = new DarkModeRenderer();

                // Mode en sombre des menus (File, Edit...)
                foreach (ToolStripMenuItem menuItem in menuStrip.Items)
                {
                    menuItem.BackColor = Color.FromArgb(30, 30, 30);
                    menuItem.ForeColor = Color.White;

                    // Mode en sombre des sous-menu
                    foreach (ToolStripItem subItem in menuItem.DropDownItems)
                    {
                        subItem.BackColor = Color.FromArgb(30, 30, 30);
                        subItem.ForeColor = Color.White;
                    }
                }
            }
        }
        private void InitFileWatcher(string path)
        {
            if (fileSystemWatcher != null)
            {
                // Arrête et libére l'ancien FileSystemWatcher
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Created -= OnFileCreated;
                fileSystemWatcher.Dispose();
            }

            fileSystemWatcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            fileSystemWatcher.Created += OnFileCreated;
        }
        private async void HotsReplayWebReader_Load(object sender, EventArgs e)
        {
            // Ajouter ce dossier au chemin de recherche des DLL natives
            if (!NativeMethods.SetDllDirectory(Path.GetDirectoryName(webViewDllPath)!))
            {
                Debug.WriteLine("Impossible d'ajouter le dossier au chemin des DLL");
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Impossible d'ajouter le dossier au chemin des DLL");
            }

            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, tempDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            Debug.WriteLine("WebView2 Runtime version: " + webView.CoreWebView2.Environment.BrowserVersionString);

            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;

            // Desactivation de la console
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Image);
            webView.CoreWebView2.WebResourceRequested += WebViewWebResourceRequested;

            string appAsetsFolder = @$"{Directory.GetCurrentDirectory()}";
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", appAsetsFolder, CoreWebView2HostResourceAccessKind.Allow);

            // Traite les messages de JavaScript vers C#
            webView.CoreWebView2.WebMessageReceived += async (sender, args) =>
            {
                string json = args.WebMessageAsJson;

                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                // Vérifie si le message contient les propriétés "action"
                if (root.TryGetProperty("action", out JsonElement actionElement))
                {
                    // Récupère les valeurs de "action"
                    string? action = actionElement.GetString();

                    // Vérifie si l'action est "closeMenu"
                    if (action == "closeMenu")
                    {
                        fileToolStripMenuItem.HideDropDown();
                        accountsToolStripMenuItem.HideDropDown();
                        regionToolStripMenuItem.HideDropDown();
                        languageToolStripMenuItem.HideDropDown();
                    }

                    // Vérifie si l'action est "hoverLeft"
                    if (action == "hoverLeft")
                    {
                        bool isHover = root.GetProperty("isHover").GetBoolean();
                        // affiche/masque la listBox
                        listBoxHotsReplays.Visible = isHover;
                    }

                    // Vérifie si l'action est "Translate", si il y a une propriété callbackId et si le message contient "text"
                    if (action == "translate" && root.TryGetProperty("callbackId", out JsonElement callbackIdElement) && root.TryGetProperty("text", out JsonElement textElement))
                    {
                        string? callbackId = callbackIdElement.GetString();
                        // Récupère le texte à traduire
                        string? inputText = textElement.GetString();
                        string translated = string.Empty;

                        try
                        {
                            if (translator != null)
                                translated = await translator.TranslateText(inputText, Resources.Language.i18n.ResourceManager.GetString("DeepLLang")!);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Erreur : " + ex.Message);
                            Console.WriteLine("Erreur : " + ex.Message);
                        }

                        // Sérialise le texte traduit en JSON
                        string returnedText = JsonSerializer.Serialize(translated);

                        // Appelle le callback JavaScript puis nettoie
                        string script = $"window['{callbackId}']({returnedText}); delete window['{callbackId}'];";
                        await webView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                }
            };

            if (Directory.Exists(Init.config!.LastSelectedAccountDirectory))
            {
                ListHotsReplays(Init.config.LastSelectedAccountDirectory);
                if (Init.config!.LastSelectedAccount != null)
                {
                    currentAccount = Init.config!.LastSelectedAccount;
                    this.Text = $"{formTitle} - {currentAccount}";
                }
                else
                {
                    currentAccount = "";
                    this.Text = $"{formTitle}";
                }

                this.Update();

                foreach (ToolStripItem item in accountsToolStripMenuItem.DropDownItems)
                {
                    if (item is ToolStripMenuItem submenu)
                    {
                        if (submenu.Name == currentAccount)
                            submenu.Checked = true;
                        else
                            submenu.Checked = false;
                    }
                }

                this.Invoke(new Action(() =>
                {
                    if (listBoxHotsReplays.Items.Count > 0)
                    {
                        listBoxHotsReplays.SelectedIndex = 0; // sélection du premier élément
                    }
                }));
            }
            else if (accountsToolStripMenuItem.DropDownItems.Count > 0)
            {
                AccountMenuItemClickHandler(accountsToolStripMenuItem.DropDownItems[0], EventArgs.Empty);
            }
            else
            {
                htmlContent = welcomeHTML;

                // Bouton de test pour appeler la fonction translateWithCSharp
                /*
                            htmlContent = @"
                <script>
                    function translateWithCSharp(text) {
                        const callbackId = ""cb_"" + Date.now();
                        window.chrome.webview.postMessage({
                            action: ""Translate"",
                            callbackId: callbackId,
                            text: text
                        });
                        window[callbackId] = function(result) {
                            alert(result);
                        };
                    }
                </script>

                <button onclick = ""translateWithCSharp('Bonjour le monde!')"" >Get a response from C#</button>
                ";
                */
                webView.NavigateToString(htmlContent);
            }
        }
        private void WebViewWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            Uri uri = new(e.Request.Uri);

            // Vérifier si le schéma correspond à celui défini
            if (uri.Scheme == "app")
            {
                // Récupérer le nom du fichier
                string fileName = Path.GetFileName(uri.LocalPath);
                string imageName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                string? actions = null;

                if (!String.IsNullOrEmpty(uri.Query))
                    actions = HttpUtility.ParseQueryString(uri.Query)["actions"];

                // Récupérer l'Image depuis les ressources
                Bitmap? image = new HotsImage(uri.Host, imageName, extension, actions).Bitmap;
                if (image == null) return;

                MemoryStream ms = new();
                // Convertir l'Image en MemoryStream
                if (extension == ".png")
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/png");
                }
                else if (extension == ".jpg")
                {
                    // Suppression du canal Alpha pour ne pas gérer la transparence
                    Bitmap newImage = new(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.Clear(Color.White);
                        g.DrawImage(image, 0, 0, image.Width, image.Height);
                    }
                    newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    ms.Position = 0;
                    e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/jpeg");
                }
                else if (extension == ".gif")
                {
                    // Handle GIF images
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                    ms.Position = 0;
                    e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "Content-Type: image/gif");
                }
            }
        }
        private void LoadAccountsToolStipMenu()
        {
            accountsToolStripMenuItem.DropDownItems.Clear();

            if (Init.hotsLocalAccounts == null) return;

            ToolStripMenuItem[] accountsToolStripMenu = new ToolStripMenuItem[Init.hotsLocalAccounts.Count];
            for (int i = 0; i < accountsToolStripMenu.Length; i++)
            {
                ToolStripMenuItem toolStripMenuItem = new()
                {
                    Name = Init?.hotsLocalAccounts[i].BattleTagName,
                    Tag = "Account",
                    Text = Init?.hotsLocalAccounts[i]?.BattleTagName is string tag && tag.Contains('#')
                        ? tag[..tag.IndexOf('#')]
                        : string.Empty
                };
                accountsToolStripMenu[i] = toolStripMenuItem;
                accountsToolStripMenu[i].Click += new EventHandler(AccountMenuItemClickHandler);
                accountsToolStripMenu[i].CheckOnClick = true;
            }
            accountsToolStripMenuItem.DropDownItems.AddRange(accountsToolStripMenu);
        }
        private void AccountMenuItemClickHandler(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem clickedItem && clickedItem.Tag?.ToString() == "Account" && Init.hotsLocalAccounts != null)
            {
                for (int i = 0; i < Init.hotsLocalAccounts.Count; i++)
                {
                    if (Init.hotsLocalAccounts[i].BattleTagName == clickedItem.Name)
                    {
                        if (clickedItem.Name == null) continue;
                        currentAccount = clickedItem.Name;
                        ListHotsReplays(Init.hotsLocalAccounts[i].FullPath);

                        Init.config!.LastSelectedAccount = clickedItem.Name;
                        Init.config.LastSelectedAccountDirectory = Init.hotsLocalAccounts[i].FullPath;
                    }
                }

                foreach (ToolStripItem item in accountsToolStripMenuItem.DropDownItems)
                {
                    if (item is ToolStripMenuItem submenu)
                    {
                        if (submenu == sender)
                            submenu.Checked = true;
                        else
                            submenu.Checked = false;
                    }
                }

                this.Text = $"{formTitle} - {currentAccount}";
                this.Update();
            }
        }
        private void LanguageMenuItemClickHandler(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem clickedItem)
            {
                Init.config!.LangCode = clickedItem.Tag?.ToString()!;
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Init.config!.LangCode);
            }

            foreach (ToolStripItem item in languageToolStripMenuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem submenu)
                {
                    if (submenu == sender)
                        submenu.Checked = true;
                    else
                        submenu.Checked = false;
                }
            }
            // Met à jour les textes de l'interface
            fileToolStripMenuItem.Text = Resources.Language.i18n.strMenuFile;
            browseToolStripMenuItem.Text = Resources.Language.i18n.strMenuBrowse;
            sourceToolStripMenuItem.Text = Resources.Language.i18n.strMenuSource;
            propertiesToolStripMenuItem.Text = Resources.Language.i18n.strProperties;
            exitToolStripMenuItem.Text = Resources.Language.i18n.strMenuExit;
            accountsToolStripMenuItem.Text = Resources.Language.i18n.strMenuAccounts;
            regionToolStripMenuItem.Text = Resources.Language.i18n.strRegion;
            americasRegionToolStripMenuItem.Text = Resources.Language.i18n.strRegionAmercas;
            europeRegionToolStripMenuItem.Text = Resources.Language.i18n.strRegionEurope;
            asiaRegionToolStripMenuItem.Text = Resources.Language.i18n.strRegionAsia;
            languageToolStripMenuItem.Text = Resources.Language.i18n.strMenuLanguage;

            if (listBoxHotsReplays.Items.Count == 0)
                return;

            // Aucun élément sélectionné : on sélectionne le premier
            if (listBoxHotsReplays.SelectedIndex == -1)
            {
                listBoxHotsReplays.SelectedIndex = 0;
            }
            else
            {
                ListBoxHotsReplays_SelectedIndexChanged(listBoxHotsReplays, EventArgs.Empty);
            }
        }
        private void ListHotsReplays(string? path)
        {
            hotsReplayFolder = path;
            listBoxHotsReplays.Items.Clear();
            replayList.Clear();
            if (Directory.Exists(path))
            {
                // Initie l'observateur de fichiers
                InitFileWatcher(path);

                DirectoryInfo hotsReplayFolder = new(path);
                FileInfo[] replayFiles = hotsReplayFolder.GetFiles("*.StormReplay");
                Array.Reverse(replayFiles);
                string replayDisplayedName = string.Empty;
                int i = 0;
                foreach (FileInfo replayFile in replayFiles)
                {
                    replayDisplayedName = replayFile.Name.ToString().Replace(replayFile.Extension.ToString(), "");
                    replayDisplayedName = MyRegexRenameReplayInList().Replace(replayDisplayedName, "$3/$2/$1 $4:$5 $7");
                    listBoxHotsReplays.Items.Add(replayDisplayedName);

                    replayList.Add(i, replayFile.FullName);
                    i++;
                }

                // Attends que le composant webView2 soit chargé
                if (webView.CoreWebView2 != null && listBoxHotsReplays.Items.Count > 0)
                {
                    this.Invoke(new Action(() =>
                    {
                        listBoxHotsReplays.SelectedIndex = 0;
                    }));
                }
            }
        }
        internal string HTMLGetHeader()
        {
            string css = System.Text.Encoding.UTF8.GetString(Resources.HotsResources.styles);

            if (hotsReplay != null)
                if (hotsReplay.stormReplay?.Owner != null)
                {
                    if (hotsReplay.stormReplay.Owner.IsWinner)
                        css = css.Replace(@"#backColor#", @"#001100");
                    else
                        css = css.Replace(@"#backColor#", @"#110000");
                }
            css = css.Replace(@"#backImg#", $"Map{hotsReplay?.stormReplay?.MapInfo.MapId}");

            string html = $@"<html lang=""{Resources.Language.i18n.ResourceManager.GetString("HTMLLang")!}"">
<head>
<style>
{css}
</style>
<script>
  // Désactive le menu contextuel
  document.addEventListener('DOMContentLoaded', () => {{
    document.addEventListener('contextmenu', (e) => {{
      e.preventDefault()
    }})
  }})

  // Ferme le menu
  document.addEventListener('click', function (e) {{
    window.chrome.webview.postMessage({{
      action: ""closeMenu""
    }});
  }});

  // Affice la liste des replays
  document.addEventListener(""mousemove"", function (e) {{
    // Détection si la souris est dans les 50px à gauche
    const isHover = e.clientX <= 50;
    // On envoie à C# uniquement quand le statut change
    if (window.__lastHover !== isHover) {{
      window.chrome.webview.postMessage({{
        action: ""hoverLeft"",
        isHover: isHover
      }});
      window.__lastHover = isHover;
    }}
  }});

  // Traduit le texte avec C#
  function translateWithCSharp(text) {{
    // Appelle la fonction C# pour traduire le texte
    return new Promise((resolve, reject) => {{
      // Crée un identifiant de rappel unique
      const callbackId = ""cb_"" + Date.now();
      // Envoie le message à C#
      window.chrome.webview.postMessage({{
        action: ""translate"",
        callbackId: callbackId,
        text: text
      }});
      // Définit la fonction de rappel pour traiter la réponse
      window[callbackId] = function(result) {{
        resolve(result);
      }};
    }});
  }}
</script>
</head>
<body>
<br><br><br>
<div class=""parentDiv"">
";
            return html;
        }
        internal static string HTMLGetFooter()
        {
            string html = "</div>\n<br><br><br>\n</body>\n</html>\n";
            return html;
        }
        internal string HTMLGetHeadTable()
        {
            if (blueTeam == null || redTeam == null || hotsReplay == null) return "";
            string isBlueTeamWinner = blueTeam.IsWinner ? Resources.Language.i18n.ResourceManager.GetString("strWinners")! : "&nbsp;";
            string isRedTeamWinner = redTeam.IsWinner ? Resources.Language.i18n.ResourceManager.GetString("strWinners")! : "&nbsp;";
            string winnerTeamClass = blueTeam.IsWinner ? "titleBlue" : "titleRed";

            string? mapName = Resources.Language.i18n.ResourceManager.GetString($"Map{hotsReplay?.stormReplay?.MapInfo.MapId}")
                           ?? hotsReplay?.stormReplay?.MapInfo.MapName;

            string html = "<div class=\"head-container\">\n  <table class=\"headTable\">\n";

            if (hotsReplay?.stormReplay?.ReplayVersion.ToString() != dbVersion)
            {
                html += $@"    <tr>
      <td colspan=""5"">Game Version</td><td>&nbsp;</td><td colspan=""5"">DB Version</td>
    </tr>
    <tr>
      <td colspan=""5"">{hotsReplay?.stormReplay?.ReplayVersion.ToString()}</td><td>&nbsp;</td><td colspan=""5"">{dbVersion}</td>
    </tr>
";
            }

            html += $@"    <tr><td colspan=""11"" class=""{winnerTeamClass}"" title=""{hotsReplay?.stormReplay?.ReplayVersion}"">{mapName}</td></tr>
    <tr>
      <td colspan=""5"" class=""titleBlue"">{isBlueTeamWinner}</td>
      <td></td>
      <td colspan=""5"" class=""titleRed"">{isRedTeamWinner}</td>
    </tr>
    <tr>
";

            if (hotsPlayers != null)
                foreach (HotsPlayer hotsPlayer in hotsPlayers)
                    if (hotsPlayer.Team.ToString() == "Blue")
                        html += HTMLGetHeadTableCell(hotsPlayer);

            html += "      <td width=\"100\"></td>\n";

            if (hotsPlayers != null)
                foreach (HotsPlayer hotsPlayer in hotsPlayers)
                    if (hotsPlayer.Team.ToString() == "Red")
                        html += HTMLGetHeadTableCell(hotsPlayer);

            string replayLength;
            if (hotsReplay?.stormReplay?.ReplayLength.Hours == 0)
                replayLength = $@"{hotsReplay.stormReplay.ReplayLength.ToString()[3..]}";
            else
                replayLength = $@"{hotsReplay?.stormReplay?.ReplayLength}";

            html += "    </tr>\n";

            if (hotsReplay?.stormReplay?.DraftPicks.Count > 0)
            {
                html += "    <tr>\n      <td>&nbsp;</td>\n";
                foreach (Heroes.StormReplayParser.Replay.StormDraftPick draftPick in hotsReplay.stormReplay.DraftPicks)
                    if (draftPick.PickType == Heroes.StormReplayParser.Replay.StormDraftPickType.Banned && draftPick.Team == Heroes.StormReplayParser.Replay.StormTeam.Blue)
                        html += $"      <td class=\"headTableTd\"><img src=\"app://heroesIcon/{Init.HeroNameFromHeroId[draftPick.HeroSelected]}.png\" class=\"bannedHeroIcon\"></td>\n";
                html += $"      <td colspan=\"3\" class=\"titleWhite\" style=\"zoom: 75%;\">{Resources.Language.i18n.strBanned}</td>\n";
                foreach (Heroes.StormReplayParser.Replay.StormDraftPick draftPick in hotsReplay.stormReplay.DraftPicks)
                    if (draftPick.PickType == Heroes.StormReplayParser.Replay.StormDraftPickType.Banned && draftPick.Team == Heroes.StormReplayParser.Replay.StormTeam.Red)
                        html += $"      <td class=\"headTableTd\"><img src=\"app://heroesIcon/{Init.HeroNameFromHeroId[draftPick.HeroSelected]}.png\" class=\"bannedHeroIcon\"></td>\n";
                html += "      <td>&nbsp;</td>\n    </tr>\n";
            }

            html += $@"    <tr>
      <td>&nbsp;</td>
      <td colspan=""3"">
        <span class=""titleBlue"">{blueTeam.TotalKills} <img src=""app://hotsResources/KillsBlue.png"" height=""32""></span><br>
        <span class=""teamLevel"">{Resources.Language.i18n.strLevel} {blueTeam.Level}</span>
      </td>
      <td colspan=""3"" class=""titleWhite"" style=""zoom: 75%;"">{replayLength}</td>
      <td colspan=""3"">
        <span class=""titleRed""><img src=""app://hotsResources/KillsRed.png"" height=""32""> {redTeam.TotalKills}</span><br>
        <span class=""teamLevel"">{Resources.Language.i18n.strLevel} {redTeam.Level}</span>
      </td>
      <td>&nbsp;</td>
    </tr>
  </table>
</div>
<br><br>
";
            return html;
        }
        internal string HTMLGetHeadTableCell(HotsPlayer hotsPlayer)
        {
            if (hotsPlayer == null || hotsPlayer.PlayerHero == null || matchAwards == null || hotsPlayer.MatchAwards == null) return "";

            string playerName;
            string playerID;
            string accountLevel = hotsPlayer.AccountLevel.HasValue ? hotsPlayer.AccountLevel.Value.ToString() : "0";
            string toolTipPosition = hotsPlayer.Team.ToString() == "Blue" ? "Left" : "Right";

            string html = "";

            // Affiche une alerte si le heros joue est celui qu'on veut tester
            if (fetchHero && Init.HeroNameFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId] == fetchedHeroName)
                html += $"      <script> alert('{Init.HeroNameFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]}'); </script>\n";

            html += $"      <td class=\"headTableTd\">\n";
            html += "        <span class=\"tooltip\">\n";
            html += "          <span class=\"heroPortrait\">\n";
            html += $"            <img src=\"app://heroesIcon/{Init.HeroNameFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]}.png\" class=\"heroIcon\">\n"; // heroIconTeam{GetParty(hotsPlayer.BattleTagName)}

            string? party = GetParty(hotsPlayer.BattleTagName);
            if (party != "0")
            {
                string? ressourceName = $"ui_ingame_loadscreen_partylink_{party}.png";
                if (ressourceName != null)
                    ressourceName = ressourceName.Replace("%color%", hotsPlayer.Team.ToString().ToLower());
                html += $"            <img src=\"app://hotsresources/{ressourceName}\" class =\"heroPartyIcon\">\n";
            }

            if (hotsPlayer.IsSilenced == true)
            {
                html += $"            <img src=\"app://hotsresources/isSilenced.png\" class=\"isSilenced\">\n";
            }

            if (hotsPlayer.MatchAwardsCount > 0)
            {
                string? ressourceName = matchAwards[$"{hotsPlayer.MatchAwards[0]}"].MvpScreenIcon;
                if (ressourceName != null)
                    ressourceName = ressourceName.Replace("%color%", hotsPlayer.Team.ToString().ToLower());
                html += $"            <img src=\"app://matchawards/{ressourceName}\" class =\"heroAwardIcon\">\n";
            }

            html += "          </span>\n";
            html += $"          <span class=\"tooltipHero tooltipHero{toolTipPosition}\">\n";

            if (hotsPlayer.MatchAwardsCount > 0)
            {
                html += $"            <center>\n";
                html += $"              <font color=\"#ffd700\">{matchAwards[$"{hotsPlayer.MatchAwards[0]}"].Name}</font><br>\n";
                html += $"              <font color=\"#bfd4fd\" size=\"-1\"><nobr>{matchAwards[$"{hotsPlayer.MatchAwards[0]}"].Description}</nobr></font><br>\n";
                html += $"            </center><br>\n";
            }
            if (hotsPlayer.BattleTagName.IndexOf('#') > 0)
            {
                playerName = hotsPlayer.BattleTagName[..hotsPlayer.BattleTagName.IndexOf('#')];
                playerID = hotsPlayer.BattleTagName[(hotsPlayer.BattleTagName.IndexOf('#') + 1)..];

                // Alignement des donées sur l'intitulé le plus long
                int maxLength = new[] { Resources.Language.i18n.strBattleTag.Length, Resources.Language.i18n.strAccountLevel.Length, Resources.Language.i18n.strHeroLevel.Length }.Max();

                string battleTagLabel = (Resources.Language.i18n.strBattleTag + ":").PadRight(maxLength + 2).Replace(" ", "&nbsp;");
                html += $"            <span class=\"nobr\">{battleTagLabel}<font color=\"#bfd4fd\">{playerName}</font>#{playerID}</span><br>\n";

                string accountLevelLabel = (Resources.Language.i18n.strAccountLevel + ":").PadRight(maxLength + 2).Replace(" ", "&nbsp;");
                html += $"            <span class=\"nobr\">{accountLevelLabel}<font color=\"#bfd4fd\">{accountLevel}</font></span><br>\n";

                string heroLevelLabel = (Resources.Language.i18n.strHeroLevel + ":").PadRight(maxLength + 2).Replace(" ", "&nbsp;");

                if (hotsReplay?.stormReplay?.GameMode.ToString() == "ARAM" || hotsReplay?.stormReplay?.GameMode.ToString() == "Brawl")
                {
                    int tierLevel = hotsPlayer.HeroMasteryTiers.FirstOrDefault(x => x.HeroAttributeId == Init.HeroAttributeIdFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId])?.TierLevel ?? 0;

                    string heroLevel = tierLevel switch
                    {
                        0 => "&lt;&nbsp;15",
                        1 => "15-25",
                        2 => "25-50",
                        3 => "50-75",
                        4 => "75-100",
                        5 => "100+",
                        _ => "&lt;&nbsp;15",
                    };

                    if (tierLevel >= 1)
                        html += $"            <span class=\"nobr\">{heroLevelLabel}<font color=\"#ffd700\">{heroLevel}</font></span><br>\n";
                    else
                        html += $"            <span class=\"nobr\">{heroLevelLabel}<font color=\"#bfd4fd\">{heroLevel}</font></span><br>\n";
                }
                else
                {
                    int tierLevel = hotsPlayer.HeroMasteryTiers.FirstOrDefault(x => x.HeroAttributeId == hotsPlayer.PlayerHero.HeroAttributeId)?.TierLevel ?? 0;

                    if (hotsPlayer.PlayerHero.HeroLevel >= 20)
                    {
                        string heroLevel = tierLevel switch
                        {
                            0 => "&GreaterEqual;&nbsp;20",
                            1 => "20-25",
                            2 => "25-50",
                            3 => "50-75",
                            4 => "75-100",
                            5 => "100+",
                            _ => "&GreaterEqual;&nbsp;20",
                        };
                        html += $"            <span class=\"nobr\">{heroLevelLabel}<font color=\"#ffd700\">{heroLevel}</font></span><br>\n";
                    }
                    else if (hotsPlayer.PlayerHero.HeroLevel >= 15)
                        html += $"            <span class=\"nobr\">{heroLevelLabel}<font color=\"#ffd700\">{hotsPlayer.PlayerHero.HeroLevel}</font></span><br>\n";
                    else
                        html += $"            <span class=\"nobr\">{heroLevelLabel}<font color=\"#bfd4fd\">{hotsPlayer.PlayerHero.HeroLevel}</font></span><br>\n";
                }
            }
            else
            {
                playerName = hotsPlayer.ComputerName!;

                string? computerDifficulty = Resources.Language.i18n.ResourceManager.GetString($"strAI{hotsPlayer.ComputerDifficulty}")
                               ?? hotsPlayer.ComputerDifficulty.ToString();

                html += $"            {Resources.Language.i18n.strAIDifficulty}:&nbsp;<font color=\"#bfd4fd\">{computerDifficulty}</font>\n";
            }

            html += $"          </span>\n";
            html += $"        </span>\n";

            string owner = (hotsReplay?.stormReplay?.Owner?.BattleTagName == hotsPlayer.BattleTagName) ? " owner" : "";
            string partyColor = (party != "0") ? $" team{party}" : "";

            html += $"        <div class=\"battleTag{owner}{partyColor}\">{playerName}</div>\n";
            html += $"      </td>\n";
            return html;
        }
        internal string HTMLGetChatMessages()
        {
            if (hotsReplay == null || hotsPlayers == null || hotsReplay.stormReplay == null) return "";

            List<HotsMessage> hotsMessages = [];
            foreach (Heroes.StormReplayParser.MessageEvent.IStormMessage chatMessage in hotsReplay.stormReplay.ChatMessages)
            {
                string msg = HTMLGetChatMessageEmoticon(((Heroes.StormReplayParser.MessageEvent.ChatMessage)chatMessage).Text);
                if (chatMessage.MessageSender != null && GetHotsPlayer(chatMessage.MessageSender.BattleTagName) != null)
                    hotsMessages.Add(new HotsMessage(GetHotsPlayer(chatMessage.MessageSender.BattleTagName)!, chatMessage.Timestamp, msg));
            }
            foreach (HotsPlayer hotsPlayer in hotsPlayers)
            {
                foreach (PlayerDisconnect playerDisconnect in hotsPlayer.PlayerDisconnects)
                {
                    hotsMessages.Add(new HotsMessage(hotsPlayer, playerDisconnect.From, $"<span class=\"disconnected\">{Resources.Language.i18n.strDisconnected}</span>", false));
                    if (playerDisconnect.To != null)
                        hotsMessages.Add(new HotsMessage(hotsPlayer, playerDisconnect.To.Value, $"<span class=\"reconnected\">{Resources.Language.i18n.strReconnected}</span>", false));
                }
            }
            hotsMessages = [.. hotsMessages.OrderBy(o => o.TotalMilliseconds)];

            if (hotsMessages.Count > 0)
            {
                bool lastMessageAfterAnHour = Convert.ToInt32(hotsMessages.Last().Hours) > 0;

                string html = $@"";
                html += "<div class=\"chat-container\">\n";
                foreach (HotsMessage hotsMessage in hotsMessages)
                {
                    html += HTMLGetChatMessage(hotsMessage, lastMessageAfterAnHour);
                }
                html += "</div>\n";

                html += @"<script>
  // Selectionne tous les elements avec la classe chat-message et ajoute un evenement de clic
  document.querySelectorAll("".chat-message"").forEach(function (element) {
    element.addEventListener(""click"", function () {
      // Récupère le texte du span avec la classe chat-message-corps
      const span = element.querySelector("".chat-message-corps"");
      const currentText = span.textContent;
      // Appelle la fonction translateWithCSharp pour traduire le texte
      translateWithCSharp(currentText)
        // Attends la réponse de la fonction
        .then(translated => {
          // Met à jour le texte du span avec le texte traduit
          span.textContent = translated;
        })
    });
  });
</script>
<br><br>";
                return $"{html}\n";
            }
            else
                return "";
        }
        internal string HTMLGetChatMessage(HotsMessage hotsMessage, bool lastMessageAfterAnHour)
        {
            if (hotsMessage.HotsPlayer.PlayerHero == null) return "";

            string? msgHours = hotsMessage.Hours;
            string? msgMinutes = hotsMessage.Minutes;
            string? msgSeconds = hotsMessage.Seconds;
            string msgSenderName = hotsMessage.HotsPlayer.Name;

            string? heroName = gameStringsRoot?.Gamestrings?.Unit?.Name?[hotsMessage.HotsPlayer.PlayerHero.HeroId];

            string html = "  <div class=\"chat-message\">\n";
            if (lastMessageAfterAnHour)
                html += $"    <span class=\"chat-time\">[{msgHours}:{msgMinutes}:{msgSeconds}]</span>\n";
            else
                html += $"    <span class=\"chat-time\">[{msgMinutes}:{msgSeconds}]</span>\n";
            html += $"    <span class=\"chat-user\"><img src=\"app://minimapicons/{Init.HeroNameFromHeroUnitId[hotsMessage.HotsPlayer.PlayerHero.HeroUnitId]}.png\" class=\"chat-image\" title=\"{heroName}\"></span>\n";

            string owner = (hotsReplay?.stormReplay?.Owner?.BattleTagName == hotsMessage.HotsPlayer.BattleTagName) ? " owner" : "";

            html += $"    <span class=\"team{hotsMessage.HotsPlayer.Party}{owner}\">{msgSenderName}</span>: \n";
            if (hotsMessage.Translate)
                html += $"    <span class=\"chat-message-corps\">{hotsMessage.Message}</span><img class=\"translate-icon\" style=\"float: right\" src=\"app://hotsResources/translate.png\" height=\"24\">\n";
            else
                html += $"    {hotsMessage.Message}\n";
            html += $"  </div>\n";
            return html;
        }
        internal string GetEmoticonImgFromTag(string tag)
        {
            if (Init.hotsEmoticons != null)
            {
                foreach (KeyValuePair<string, HotsEmoticonData> hotsEmoticonData in Init.hotsEmoticons)
                {
                    foreach (string alias in hotsEmoticonData.Value.Aliases)
                    {
                        if (tag == alias && hotsEmoticonData.Value.Image != null)
                        {
                            if (hotsEmoticonData.Value.Image.Contains("storm_emoji_nexus"))
                                return $@"<img src=""app://emoticons/{hotsEmoticonData.Value.Image}"" class=""chat-image"" title=""{hotsEmoticonData.Value.Aliases[0]}"">";
                            else
                                return $@"<img src=""app://emoticons/{hotsEmoticonData.Value.Image}"" class=""chat-image chat-image-emoticon"" title=""{hotsEmoticonData.Value.Aliases[0]}"">";
                        }
                    }
                }
                return tag;
            }
            return "";
        }
        internal string HTMLGetChatMessageEmoticon(string chatMessage)
        {
            string pattern = @"(:\w+:)";
            return Regex.Replace(chatMessage, pattern, match =>
            {
                string emoticonTag = match.Groups[1].Value;
                return GetEmoticonImgFromTag(emoticonTag);
            });
        }
        private string HTMLGetScoreTable()
        {
            if (hotsReplay == null || hotsPlayers == null || blueTeam == null || redTeam == null) return "";
            string html = @$"<table class=""tableScoreAndTalents"">
  <tr class=""freeHeight"">
    <td></td>
    <td></td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreKills.png"">
        <span class=""tooltipHero tooltipScoreHeaderLeft"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreKills")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreAssists.png"">
        <span class=""tooltipHero tooltipScoreHeaderLeft"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreAssists")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreDeaths.png"">
        <span class=""tooltipHero tooltipScoreHeaderLeft"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreDeaths")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreTimeSpentDead.png"">
        <span class=""tooltipHero tooltipScoreHeaderLeft"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreTimeSpentDead")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreSiegeDmg.png"">
        <span class=""tooltipHero tooltipScoreHeaderRight"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreSiegeDmg")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreHeroDmg.png"">
        <span class=""tooltipHero tooltipScoreHeaderRight"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreHeroDmg")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreHealing.png"">
        <span class=""tooltipHero tooltipScoreHeaderRight"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreHealing")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreDmgTaken.png"">
        <span class=""tooltipHero tooltipScoreHeaderRight"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreDmgTaken")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreExp.png"">
        <span class=""tooltipHero tooltipScoreHeaderRight"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreExp")!}</nobr>
        </span>
      </span>
    </td>
    <td class=""teamHeader tdBorders"">
      <span class=""tooltip"">
        <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreMvp.png"">
        <span class=""tooltipHero tooltipScoreHeaderRight"">
          <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreMvp")!}</nobr>
        </span>
      </span>
    </td>
  </tr>
";
            foreach (HotsPlayer stormPlayer in hotsPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetScoreTr(stormPlayer, blueTeam, GetParty(stormPlayer.BattleTagName));
            foreach (HotsPlayer stormPlayer in hotsPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetScoreTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));

            html += "</table>\n<br><br>\n";

            return html;
        }
        private string HTMLGetScoreTr(HotsPlayer hotsPlayer, HotsTeam team, string partyColor)
        {
            if (hotsPlayer.ScoreResult == null || hotsPlayer.PlayerHero == null) return "";

            string playerName;

            if (hotsPlayer.PlayerType == PlayerType.Computer)
                playerName = hotsPlayer.ComputerName!;
            else
                playerName = hotsPlayer.Name;

            string? heroName = gameStringsRoot?.Gamestrings?.Unit?.Name?[Init.HeroIdFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]];

            string timeSpentDead = "&nbsp;";
            if (hotsPlayer.ScoreResult.Deaths > 0)
            {
                if (hotsPlayer.ScoreResult.TimeSpentDead.Hours == 0)
                    timeSpentDead = $@"{hotsPlayer.ScoreResult.TimeSpentDead.ToString()[3..]}";
                else
                    timeSpentDead = $@"{hotsPlayer.ScoreResult.TimeSpentDead}";
            }

            string html = @"";
            html += $"  <tr class=\"team{team.Name}\">\n";
            html += $"    <td class=\"tdBorders\"><img class=\"scoreIcon\" src=\"app://heroesIcon/{Init.HeroNameFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]}.png\"></td>\n";
            html += $"    <td class=\"tdPlayerName team{partyColor} tdBorders\">&nbsp;{heroName}<br><font size=\"-1\">&nbsp;{playerName}</font></td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.SoloKills == team.MaxKills)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.SoloKills}</td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.Assists == team.MaxAssists)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.Assists}</td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.Deaths == team.MaxDeaths)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.Deaths}</td>\n";

            html += $"    <td class=\"tdBorders\">{timeSpentDead}</td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.SiegeDamage == team.MaxSiegeDmg)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.SiegeDamage:n0}</td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.HeroDamage == team.MaxHeroDmg)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.HeroDamage:n0}</td>\n";

            html += "    <td class=\"tdBorders";
            if ((hotsPlayer.ScoreResult.Healing + hotsPlayer.ScoreResult.SelfHealing) == team.MaxTotalHealing)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.Healing + hotsPlayer.ScoreResult.SelfHealing:n0}</td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.DamageTaken == team.MaxDmgTaken)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.DamageTaken:n0}</td>\n";

            html += "    <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.ExperienceContribution == team.MaxExp)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.ExperienceContribution:n0}</td>\n";

            // MVP Score with tooltip
            html += "    <td class=\"tooltip-cell tdBorders\">\n";
            html += "      <span class=\"tooltip\">\n        ";
            if (hotsPlayer.MatchAwardsCount > 0 && hotsPlayer.MatchAwards != null)
                if (hotsPlayer.MatchAwards[0].ToString() == "MVP")
                    html += "<span class=\"teamBestScore\">";

            html += $"{Math.Round(hotsPlayer.MvpScore, 2)}";

            if (hotsPlayer.MatchAwardsCount > 0 && hotsPlayer.MatchAwards != null)
                if (hotsPlayer.MatchAwards[0].ToString() == "MVP")
                    html += "</span>";

            html += $"\n          <span class=\"tooltipHeroMvpScore\">\n";

            bool firstLine = true;

            if (hotsPlayer.MvpScoreWinningTeam != null)
            {
                html += $"            WinningTeam:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreWinningTeam, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreKills != null || hotsPlayer.MvpScoreAssists != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Takedowns</u><br>\n";
                if (hotsPlayer.MvpScoreKills != null) html += $"            Kills:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreKills, 2)}<br>\n";
                if (hotsPlayer.MvpScoreAssists != null) html += $"            Assists:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreAssists, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreTimeSpentDead != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Deaths</u><br>\n";
                html += $"            TimeSpentDead:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTimeSpentDead, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreTopHeroDamageOnTeam != null || hotsPlayer.MvpScoreTopHeroDamage != null || hotsPlayer.MvpScoreHeroDamageBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Hero Damage</u><br>\n";
                if (hotsPlayer.MvpScoreTopHeroDamage != null) html += $"            TopHeroDamage:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopHeroDamage, 2)}<br>\n";
                if (hotsPlayer.MvpScoreTopHeroDamageOnTeam != null) html += $"            TopHeroDamageOnTeam:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopHeroDamageOnTeam, 2)}<br>\n";
                if (hotsPlayer.MvpScoreHeroDamageBonus != null) html += $"            HeroDamageBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreHeroDamageBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreTopSiegeDamageOnTeam != null || hotsPlayer.MvpScoreTopSiegeDamage != null || hotsPlayer.MvpScoreSiegeDamageBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Siege Damage</u><br>\n";
                if (hotsPlayer.MvpScoreTopSiegeDamage != null) html += $"            TopSiegeDamage:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopSiegeDamage, 2)}<br>\n";
                if (hotsPlayer.MvpScoreTopSiegeDamageOnTeam != null) html += $"            TopSiegeDamageOnTeam:&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopSiegeDamageOnTeam, 2)}<br>\n";
                if (hotsPlayer.MvpScoreSiegeDamageBonus != null) html += $"            SiegeDamageBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreSiegeDamageBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreTopDamageTakenOnTeam != null || hotsPlayer.MvpScoreTopDamageTaken != null || hotsPlayer.MvpScoreDamageTakenBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Damage Taken</u><br>\n";
                if (hotsPlayer.MvpScoreTopDamageTaken != null) html += $"            TopDamageTaken:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopDamageTaken, 2)}<br>\n";
                if (hotsPlayer.MvpScoreTopDamageTakenOnTeam != null) html += $"            TopDamageTakenOnTeam:&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopDamageTakenOnTeam, 2)}<br>\n";
                if (hotsPlayer.MvpScoreDamageTakenBonus != null) html += $"            DamageTakenBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreDamageTakenBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreTopHealing != null || hotsPlayer.MvpScoreHealingBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u></u>Healing<br>\n";
                if (hotsPlayer.MvpScoreTopHealing != null) html += $"            TopHealing:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopHealing, 2)}<br>\n";
                if (hotsPlayer.MvpScoreHealingBonus != null) html += $"            HealingBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreHealingBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.MvpScoreTopXPContributionOnTeam != null || hotsPlayer.MvpScoreTopXPContribution != null || hotsPlayer.MvpScoreXPContributionBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Experience</u><br>\n";
                if (hotsPlayer.MvpScoreTopXPContribution != null) html += $"            TopXPContribution:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopXPContribution, 2)}<br>\n";
                if (hotsPlayer.MvpScoreTopXPContributionOnTeam != null) html += $"            TopXPContributionOnTeam:&nbsp;{Math.Round((double)hotsPlayer.MvpScoreTopXPContributionOnTeam, 2)}<br>\n";
                if (hotsPlayer.MvpScoreXPContributionBonus != null) html += $"            XPContributionBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.MvpScoreXPContributionBonus, 2)}<br>\n";
            }
            // if (hotsPlayer.ScoreResult.OnFireTimeonFire != null && hotsPlayer.ScoreResult.OnFireTimeonFire.Value.TotalSeconds > 0) html += $"<br>\n            TimeOnFire:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<font color=\"#ffd700\">{hotsPlayer.ScoreResult.OnFireTimeonFire.Value.TotalSeconds} s</font><br>\n";

            html += "          </span>\n";
            html += "      </span>\n";
            html += "    </td>\n";
            html += "  </tr>\n";
            return html;
        }
        private string HTMLGetTalentsTable()
        {
            string html = @$"<table class=""tableScoreAndTalents tableTalents"">
  <tr class=""freeHeight"">
    <td></td>
    <td></td>
    <td class=""teamScoreHeader tdBorders"">1</td>
    <td class=""teamScoreHeader tdBorders"">4</td>
    <td class=""teamScoreHeader tdBorders"">7</td>
    <td class=""teamScoreHeader tdBorders""><font color=""#ffd700"">10</font></td>
    <td class=""teamScoreHeader tdBorders"">13</td>
    <td class=""teamScoreHeader tdBorders"">16</td>
    <td class=""teamScoreHeader tdBorders"">20</td>
  </tr>
";
            if (hotsReplay == null || hotsPlayers == null || blueTeam == null || redTeam == null) return "";

            foreach (HotsPlayer stormPlayer in hotsPlayers)
            {
                if (stormPlayer.Team.ToString() == "Blue")
                {
                    html += HTMLGetTalentsTr(stormPlayer, blueTeam, GetParty(stormPlayer.BattleTagName));
                    html += HTMLGetAbilitiesTr(stormPlayer, blueTeam);
                }
            }
            foreach (HotsPlayer stormPlayer in hotsPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                {
                    html += HTMLGetTalentsTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));
                    html += HTMLGetAbilitiesTr(stormPlayer, redTeam);
                }

            html += @"</table>
<script>
  document.querySelectorAll('.trTalents').forEach(tr => {
    tr.addEventListener('click', function() {
      const next = this.nextElementSibling;
      if (next && next.classList.contains('trAblilities')) {
        next.style.display = (next.style.display === 'none' || next.style.display === '') ? 'table-row' : 'none';
      }
    });
  });
  document.querySelectorAll('.trAblilities').forEach(tr => {
    tr.addEventListener('click', function() {
      this.style.display = 'none';
    });
  });
  document.querySelectorAll('tr.trAblilities').forEach(tr => {
    tr.addEventListener('mouseenter', () => {
      const prev = tr.previousElementSibling;
      if (prev && prev.classList.contains('trTalents')) {
        prev.classList.add('highlight');
      }
    });
    tr.addEventListener('mouseleave', () => {
      const prev = tr.previousElementSibling;
      if (prev && prev.classList.contains('trTalents')) {
        prev.classList.remove('highlight');
      }
    });
  });
</script>
";
            return html;
        }
        private string HTMLGetTalentsTr(HotsPlayer stormPlayer, HotsTeam team, string partyColor)
        {
            if (stormPlayer.PlayerHero == null || heroDataDocument == null) return "";

            string? heroName = gameStringsRoot?.Gamestrings?.Unit?.Name?[Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId]];

            Hero heroData = heroDataDocument.GetHeroById(Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId], true, true, true, true);

            string playerName;

            if (stormPlayer.PlayerType == PlayerType.Computer)
                playerName = stormPlayer.ComputerName!;
            else
                playerName = stormPlayer.Name;

            string html = "";
            html += $"  <tr class=\"team{team.Name} trTalents\">\n";
            html += $"    <td class=\"tdBorders\"><img class=\"scoreIcon\" src=\"app://heroesIcon/{Init.HeroNameFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId]}.png\"></td>\n";
            html += $"    <td class=\"tdPlayerName team{partyColor} tdBorders\">&nbsp;{heroName}<br><font size=\"-1\">&nbsp;{playerName}</font></td>\n";

            for (int i = 0; i <= 6; i++)
            {
                int talentEarlierLevel = 0;
                if (stormPlayer.PlayerHero.HeroUnitId == "HeroChromie")
                    talentEarlierLevel = 2;

                if (i < stormPlayer.Talents.Count)
                    html += $"{GetTalentImgString(stormPlayer, heroData, i)}\n";
                else
                {
                    // Qustion mark for unselected talents
                    if (i == 0 || (i == 1 && team.Level >= 4 - talentEarlierLevel) || (i == 2 && team.Level >= 7 - talentEarlierLevel) || (i == 3 && team.Level >= 10 - talentEarlierLevel) || (i == 4 && team.Level >= 13 - talentEarlierLevel) || (i == 5 && team.Level >= 16 - talentEarlierLevel) || (i == 6 && team.Level >= 20 - talentEarlierLevel))
                    {
                        string imgTalentBorderClass;
                        if (i == 3 || i == 6)
                            imgTalentBorderClass = "imgTalent10Border";
                        else
                            imgTalentBorderClass = "imgTalentBorder";
                        html += $"    <td class=\"tdBorders\"><img src=\"app://hotsResources/noTalent.png\" class=\"heroTalentIcon {imgTalentBorderClass}\"></td>\n";
                    }
                    else
                        html += "    <td class=\"tdBorders\">&nbsp;</td>\n";
                }
            }

            html += "  </tr>\n";
            return html;
        }
        private string HTMLGetAbilitiesTr(HotsPlayer stormPlayer, HotsTeam team)
        {
            // https://psionic-storm.com/en/wp-json/psionic/v0/units?region=live
            // https://psionic-storm.com/en/wp-json/psionic/v0
            if (stormPlayer.PlayerHero == null || heroDataDocument == null) return "";

            int level = 1;

            Hero heroData = heroDataDocument.GetHeroById(Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId], true, true, true, true);

            string playerName;

            if (stormPlayer.PlayerType == PlayerType.Computer)
                playerName = stormPlayer.ComputerName!;
            else
                playerName = stormPlayer.Name;

            string heroName = Init.HeroNameFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId];
            if (heroName == "Lucio") heroName = "Lúcio";

            if (Init.PsionicStormUnits == null || Init.PsionicStormUnits[heroName] == null) return "";

            string html = "";
            html += $"  <tr class=\"trAblilities team{team.Name}\">\n";
            html += "    <td colspan=\"9\" class=\"tdBorders\">\n";

            html += "      <table width=\"100%\">\n";
            html += "        <tr>\n";
            html += "          <td valign=\"top\">\n";

            html += "            <table width=\"315px;\">\n";
            html += "              <tr class=\"stats\">\n";
            html += "                <td class=\"statsHealth\">\n";

            html += "                  <br>\n";
            html += $"                  Health:&nbsp;<font color=\"White\">{Math.Ceiling(heroData.Life.LifeMax * Math.Pow((1 + heroData.Life.LifeScaling), level))}</font><br>\n";
            html += $"                  Regen:&nbsp;&nbsp;<font color=\"White\">{Math.Round(heroData.Life.LifeRegenerationRate * Math.Pow((1 + heroData.Life.LifeRegenerationRateScaling), level), 2)}/s</font>\n";

            html += "                </td>\n";

            /*
            string mana = "";
            string manaRegen = "";
            if (Init.PsionicStormUnits[heroName].ManaBase == 500)
            {
                // Start with 500 Mana at level 1, except Probius who starts with 600 Mana, and gain 10 maximum Mana every level onwards
                mana = Math.Round(heroData.Energy.EnergyMax + ((level - 1) * 10), 0).ToString();
                // Start with 3 Mana per second at level 1 and gain 0.0975 Mana per second every level
                manaRegen = Math.Round(3 + (level - 1) * 0.0975, 2).ToString();
                html += $"      Mana: {mana}<br>\n";
                html += $"      Regen: {manaRegen}/s<br><br>\n";
            }
            else if (Init.PsionicStormUnits[heroName].ManaBase > 0)
            {
                html += $"      Mana: {Init.PsionicStormUnits[heroName].ManaBase}<br>";
                if (Init.PsionicStormUnits[heroName].ManaRegenBase > 0)
                    html += $"\n      Regen: {Init.PsionicStormUnits[heroName].ManaRegenBase}/s<br>";
                html += "<br>\n";
            }
            */

            html += "                <td class=\"statsDamage\">\n";
            html += "                  <br>\n";

            double aaDmg = Math.Round(Init.PsionicStormUnits[heroName].AaDmgBase * Math.Pow((1 + Init.PsionicStormUnits[heroName].AaDmgScaling), level), 1);
            html += $"                  Damage:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<font color=\"White\">{aaDmg}</font><br>\n";
            html += $"                  Attack&nbsp;speed:&nbsp;<font color=\"White\">{Init.PsionicStormUnits[heroName].AaSpeed}/s</font><br>\n";
            html += $"                  Dps:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<font color=\"White\">{Math.Round(aaDmg * Init.PsionicStormUnits[heroName].AaSpeed, 1)}</font><br><br>\n";
            html += $"                  <font color=\"#31ccff\">Attack range:</font>&nbsp;<font color=\"White\">{Init.PsionicStormUnits[heroName].AaRange}</font><br>\n";

            html += "                </td>\n";
            html += "              </tr>\n";
            html += "            </table>\n";

            html += "          </td>\n";
            html += "          <td width=\"100%\">&nbsp;</td>\n";

            html += HTMLGetAbilityTd(heroData, AbilityTypes.Q, team);
            html += HTMLGetAbilityTd(heroData, AbilityTypes.W, team);
            html += HTMLGetAbilityTd(heroData, AbilityTypes.E, team);
            html += HTMLGetAbilityTd(heroData, AbilityTypes.Heroic, team);
            html += HTMLGetAbilityTd(heroData, AbilityTypes.Heroic, team, 2);
            html += HTMLGetAbilityTd(heroData, AbilityTypes.Trait, team);
            html += HTMLGetAbilityTd(heroData, AbilityTypes.Z, team);

            html += "        </tr>\n";
            html += "      </table>\n";

            html += "    </td>\n";
            html += "  </tr>\n";
            return html;
        }
        private string HTMLGetAbilityTd(Hero heroData, AbilityTypes abilityType, HotsTeam team, int heroicNumber = 1)
        {
            string html = string.Empty;

            string abilityHeader = abilityType.ToString();

            switch (abilityType)
            {
                case AbilityTypes.Heroic:
                    abilityHeader = $"<font color=\"#ffd700\">R{heroicNumber}</font>";
                    break;
                case AbilityTypes.Trait:
                    abilityHeader = "D";
                    break;
            }

            html += "          <td>\n";
            html += $"            <div class=\"abilityHeader\">{abilityHeader}</div>\n";

            Ability? ability = null;

            if (heroData.Id == "LostVikings" && (abilityType == AbilityTypes.Q || abilityType == AbilityTypes.W || abilityType == AbilityTypes.E))
            {
                switch (abilityType)
                {
                    case AbilityTypes.Q:
                        ability = heroData.Abilities.FirstOrDefault(a => a.AbilityTalentId.AbilityType == AbilityTypes.Active);
                        break;
                    case AbilityTypes.W:
                        ability = heroData.Abilities.Where(a => a.AbilityTalentId.AbilityType == AbilityTypes.Active).Skip(1).FirstOrDefault();
                        break;
                    case AbilityTypes.E:
                        ability = heroData.Abilities.Where(a => a.AbilityTalentId.AbilityType == AbilityTypes.Active).Skip(2).FirstOrDefault();
                        break;
                }
            }
            else
            {
                if (heroicNumber == 1)
                    ability = heroData.Abilities.FirstOrDefault(a => a.AbilityTalentId.AbilityType == abilityType);
                else
                    ability = heroData.Abilities.Where(a => a.AbilityTalentId.AbilityType == abilityType).Skip(1).FirstOrDefault();
            }

            html += HTMLGetAbility(heroData, team, ability);

            // Displays Abathur, Alexstrasza, D.Va and Ragnaros other abilities
            if (heroData.Id != "Chen" && heroData.Id != "LostVikings" && heroData.Id != "Rexxar")
            {
                foreach (Hero heroUnit in heroData.HeroUnits)
                {
                    Ability? unitAbility;
                    if (heroicNumber == 1)
                        unitAbility = heroUnit.Abilities.FirstOrDefault(a => a.AbilityTalentId.AbilityType == abilityType);
                    else
                        unitAbility = heroUnit.Abilities.Where(a => a.AbilityTalentId.AbilityType == abilityType).Skip(1).FirstOrDefault();

                    html += "            <br>\n";
                    html += HTMLGetAbility(heroData, team, unitAbility);
                }
            }

            html += "          </td>\n";

            return html;
        }
        private string HTMLGetAbility(Hero heroData, HotsTeam team, Ability? ability)
        {
            string html = string.Empty;

            if (ability != null)
            {
                string? iconPath = ability.IconFileName;
                iconPath = iconPath?.Replace("kel'thuzad", "kelthuzad");
                iconPath = iconPath?.Replace("storm_ui_icon_tracer_blink_empty.png", "storm_ui_icon_tracer_blink.png");

                string actions = string.Empty;
                if (ability.AbilityTalentId.AbilityType == AbilityTypes.Z)
                    actions = $"?actions=crop:left,4;border:{Uri.EscapeDataString("#000000")},1";

                html += "            <div class=\"tooltip abilityHeaderDiv\">\n";
                html += $"              &nbsp;&nbsp;<div class=\"abilityIconContainer\"><img src=\"app://abilityTalents/{iconPath}{actions}\" class=\"abilityIcon\"><img src=\"app://hotsResources/abilityIconBorder{team.Name}.png\" class=\"abilityIconBorder\"></div>&nbsp;&nbsp;\n";

                string abilityManaCost = "";
                string abilityName = "";
                string abilityCooldown = "";
                string description = "";
                AbilTalentEntry AbilTalentEntry;
                if (ability.AbilityTalentId.ReferenceId != null)
                {
                    AbilTalentEntry = GetAbilTalent(heroData, ability.AbilityTalentId.ReferenceId!, ability.AbilityTalentId.Id);
                    if (AbilTalentEntry != null)
                    {
                        // Si la description est vide, on n'affiche pas le talent
                        if (AbilTalentEntry.Full == null || AbilTalentEntry.Full == string.Empty)
                        {
                            if (AbilTalentEntry.Short == null || AbilTalentEntry.Short == string.Empty)
                                description = "ERROR!";
                            else
                                description = "<i>" + AbilTalentEntry.Short + "</i>";
                        }
                        else
                            description = AbilTalentEntry.Full;


                        if (AbilTalentEntry.Name != null)
                            abilityName = AbilTalentEntry.Name;

                        // Affiche le coût en mana si il y en a un
                        if (AbilTalentEntry.Energy != null)
                            AbilTalentEntry.Energy = MyRegexConvertEnergy().Replace(AbilTalentEntry.Energy, "<font color=\"#${1}\">${2}</font>");
                        abilityManaCost = AbilTalentEntry.Energy != null ? $"<br>\n                  {AbilTalentEntry.Energy}" : "";
                        // Affiche le cooldown si il y en a un
                        abilityCooldown = AbilTalentEntry.Cooldown != null ? $"<br>\n                  <font color=\"#bfd4fd\">{AbilTalentEntry.Cooldown}</font>" : "";

                        // Suppression des balises <img> dans la description
                        description = MyRegexRemoveImg().Replace(description, string.Empty);

                        // Bug FR talent GreymaneLordofHisPack
                        description = description.Replace("\"#ColorViolet »>", "\"d65cff\">");

                        // Remplace <c val="color">text</c> par du texte coloré
                        description = MyRegexConvertColor().Replace(description, "<font color=\"#${1}\">${2}</font>");
                        description = MyRegexStandardTooltipDetails().Replace(description, "<font color=\"#${1}\">${2}</font>");
                        description = MyRegexStandardTooltipHeader().Replace(description, "<font color=\"#${1}\"><b>${2}</b></font>");

                        description = MyRegexConvertPercentPerLevel().Replace(description, match =>
                        {
                            // Conversion du nombre capturé
                            double value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                            // Conversion en pourcentage (4% pour 0.04)
                            int percent = (int)Math.Round(value * 100);
                            // Mise en forme du texte final
                            string replacement = "";
                            if (Resources.Language.i18n.strPerLevelBefore == "false")
                                replacement = $" (<font color=\"#bfd4fd\">+{percent}%</font> {Resources.Language.i18n.strPerLevel})";
                            else
                                replacement = $" ({Resources.Language.i18n.strPerLevel} <font color=\"#bfd4fd\">+{percent}%</font>) ";

                            // Si la balise </font> était présente, la déplacer avant le texte remplacé
                            if (match.Groups[2].Success)
                                return $"{match.Groups[2].Value}{replacement}";
                            else
                                return replacement;
                        });

                        // Remplace <n/> par un saut de ligne <br>
                        description = MyRegexNewLine().Replace(description, "<br>");
                    }
                }

                if (description != "")
                {
                    html += "              <span class=\"tooltipAbilityText ";
                    if (ability.Tier.ToString() == "Basic")
                        html += "tooltipAbilityTextRight";
                    else
                        html += "tooltipAbilityTextLeft";
                    html += "\">\n";

                    html += @$"                <font color=""White"">
                  <b>{abilityName}</b>{abilityManaCost}{abilityCooldown}
                </font>
                <br><br>
                {description}";

                    html += "\n              </span>\n";
                }

                html += "            </div>\n";
            }
            else
                html += $"            &nbsp;&nbsp;<div class=\"abilityIconContainer\"><img src=\"app://hotsResources/noAbility.png\" class=\"abilityIcon\"><img src=\"app://hotsResources/abilityIconBorder{team.Name}.png\" class=\"abilityIconBorder\"></div>&nbsp;&nbsp;\n";

            return html;
        }
        private string GetTalentImgString(HotsPlayer stormPlayer, Hero heroData, int i)
        {
            if (stormPlayer == null) return "    <td>&nbsp;</td>";

            int tier = 0;
            switch (i)
            {
                case 0:
                    tier = 1;
                    break;
                case 1:
                    tier = 4;
                    break;
                case 2:
                    tier = 7;
                    break;
                case 3:
                    tier = 10;
                    break;
                case 4:
                    tier = 13;
                    break;
                case 5:
                    tier = 16;
                    break;
                case 6:
                    tier = 20;
                    break;
            }

            AbilTalentEntry AbilTalentEntry;

            //  hotsPlayer.Talents[0].TalentNameId) renvoie une exception
            if (stormPlayer.Talents[i].TalentNameId != null)
                AbilTalentEntry = GetAbilTalent(heroData, stormPlayer.Talents[i].TalentNameId!); // ! Assure au compilateur que TalentNameId n'est pas null
            else
                return "    <td class=\"tdBorders\">&nbsp;</td>";

            if (AbilTalentEntry == null)
                return "    <td class=\"tdBorders\">&nbsp;</td>";

            string iconPath = $@"app://abilityTalents/{AbilTalentEntry.IconFileName}";
            iconPath = iconPath.Replace("kel'thuzad", "kelthuzad");

            string description = "";
            // Si la description est vide, on n'affiche pas le talent
            if (AbilTalentEntry.Full == null || AbilTalentEntry.Full == string.Empty)
            {
                if (AbilTalentEntry.Short == null || AbilTalentEntry.Short == string.Empty)
                    description = "ERROR!";
                else
                    description = "<i>" + AbilTalentEntry.Short + "</i>";
            }
            else
                description = AbilTalentEntry.Full;

            // Affiche le coût en mana si il y en a un
            if (AbilTalentEntry.Energy != null)
                AbilTalentEntry.Energy = MyRegexConvertEnergy().Replace(AbilTalentEntry.Energy, "<font color=\"#${1}\">${2}</font>");
            string abilityManaCost = AbilTalentEntry.Energy != null ? $"<br>\n            {AbilTalentEntry.Energy}" : "";
            // Affiche le cooldown si il y en a un
            string talentCooldown = AbilTalentEntry.Cooldown != null ? $"<br>\n            <font color=\"#bfd4fd\">{AbilTalentEntry.Cooldown}</font>" : "";

            // Suppression des balises <img> dans la description
            description = MyRegexRemoveImg().Replace(description, string.Empty);

            // Bug FR talent GreymaneLordofHisPack
            description = description.Replace("\"#ColorViolet »>", "\"d65cff\">");

            // Remplace <c val="color">text</c> par du texte coloré
            description = MyRegexConvertColor().Replace(description, "<font color=\"#${1}\">${2}</font>");

            description = MyRegexConvertPercentPerLevel().Replace(description, match =>
            {
                // Conversion du nombre capturé
                double value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                // Conversion en pourcentage (4% pour 0.04)
                int percent = (int)Math.Round(value * 100);
                // Mise en forme du texte final
                string replacement = "";
                if (Resources.Language.i18n.strPerLevelBefore == "false")
                    replacement = $" (<font color=\"#bfd4fd\">+{percent}%</font> {Resources.Language.i18n.strPerLevel})";
                else
                    replacement = $" ({Resources.Language.i18n.strPerLevel} <font color=\"#bfd4fd\">+{percent}%</font>) ";

                // Si la balise </font> était présente, la déplacer avant le texte remplacé
                if (match.Groups[2].Success)
                    return $"{match.Groups[2].Value}{replacement}";
                else
                    return replacement;
            });

            // Remplace <n/> par un saut de ligne <br>
            description = MyRegexNewLine().Replace(description, "<br>");

            // Place le tooltip a gauche ou a droite de l'icône
            string toolTipPosition = tier > 10 ? "Left" : "Right";
            // Met une bordure sur les Talents de niveau 10 et 20
            string imgTalentBorderClass;
            if (tier == 10 || tier == 20)
                imgTalentBorderClass = "imgTalent10Border";
            else
                imgTalentBorderClass = "imgTalentBorder";
            return @$"    <td class=""tdBorders"">
      <div class=""tooltip"">
        <img src=""{iconPath}"" class=""heroTalentIcon {imgTalentBorderClass}"">
        <span class=""tooltiptext tooltiptext{toolTipPosition}"">
          <font color=""White"">
            <b>{AbilTalentEntry.Name}</b>{abilityManaCost}{talentCooldown}
          </font>
          <br><br>
          {description}
        </span>
      </div>
    </td>";
        }
        private AbilTalentEntry GetAbilTalent(Hero heroData, string TalentNameId, string? TalentId = null)
        {
            AbilTalentEntry abilTalentEntry = new()
            {
                HeroId = heroData.CHeroId,
                AbilityId = TalentNameId,

                IconFileName =
                    heroData.Talents
                        .FirstOrDefault(t =>
                            t.AbilityTalentId.ToString().Split('|')[0]
                             .Equals(TalentNameId, StringComparison.OrdinalIgnoreCase))
                        ?.IconFileName
                    ?? string.Empty
            };

            bool MatchPrefix(string key) => key.Split('|')[0].Equals(TalentNameId, StringComparison.OrdinalIgnoreCase);

            if (TalentId != null)
            {
                if (gameStringsRoot?.Gamestrings?.AbilTalent?.Cooldown != null && gameStringsRoot.Gamestrings.AbilTalent.Cooldown.TryGetValue(TalentId, out string? talentValue))
                    abilTalentEntry.Cooldown = talentValue;
                if (gameStringsRoot?.Gamestrings?.AbilTalent?.Energy != null && gameStringsRoot.Gamestrings.AbilTalent.Energy.TryGetValue(TalentId, out talentValue))
                    abilTalentEntry.Energy = talentValue;
                if (gameStringsRoot?.Gamestrings?.AbilTalent?.Full != null && gameStringsRoot.Gamestrings.AbilTalent.Full.TryGetValue(TalentId, out talentValue))
                    abilTalentEntry.Full = talentValue;
                if (gameStringsRoot?.Gamestrings?.AbilTalent?.Life != null && gameStringsRoot.Gamestrings.AbilTalent.Life.TryGetValue(TalentId, out talentValue))
                    abilTalentEntry.Life = talentValue;
                if (gameStringsRoot?.Gamestrings?.AbilTalent?.Name != null && gameStringsRoot.Gamestrings.AbilTalent.Name.TryGetValue(TalentId, out talentValue))
                    abilTalentEntry.Name = talentValue;
                if (gameStringsRoot?.Gamestrings?.AbilTalent?.Short != null && gameStringsRoot.Gamestrings.AbilTalent.Short.TryGetValue(TalentId, out talentValue))
                    abilTalentEntry.Short = talentValue;

                return abilTalentEntry;
            }

            abilTalentEntry.Cooldown = gameStringsRoot?.Gamestrings?.AbilTalent?.Cooldown?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Energy = gameStringsRoot?.Gamestrings?.AbilTalent?.Energy?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Full = gameStringsRoot?.Gamestrings?.AbilTalent?.Full?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Life = gameStringsRoot?.Gamestrings?.AbilTalent?.Life?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Name = gameStringsRoot?.Gamestrings?.AbilTalent?.Name?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;
            abilTalentEntry.Short = gameStringsRoot?.Gamestrings?.AbilTalent?.Short?.FirstOrDefault(kv => MatchPrefix(kv.Key)).Value;

            return abilTalentEntry;
        }
        private string GetParty(string playerBattleTag)
        {
            if (hotsPlayers != null)
                foreach (HotsPlayer hotsPlayer in hotsPlayers)
                {
                    if (hotsPlayer.BattleTagName == playerBattleTag && hotsPlayer.Party != null)
                    {
                        return hotsPlayer.Party;
                    }
                }
            return "0";
        }
        private HotsPlayer? GetHotsPlayer(string playerBattleTag)
        {
            if (hotsPlayers != null)
                foreach (HotsPlayer hotsPlayer in hotsPlayers)
                {
                    if (hotsPlayer.BattleTagName == playerBattleTag)
                    {
                        return hotsPlayer;
                    }
                }
            return null;
        }
        private void InitTeamDatas(HotsTeam team)
        {
            if (hotsReplay == null || hotsReplay.stormPlayers == null || hotsReplay.stormReplay == null) return;

            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
            {
                if (stormPlayer.Team.ToString() == team.Name && stormPlayer.ScoreResult != null && stormPlayer.PlayerHero != null)
                {
                    if (stormPlayer.ScoreResult.SoloKills >= team.MaxKills)
                        team.MaxKills = stormPlayer.ScoreResult.SoloKills;
                    if (stormPlayer.ScoreResult.Assists >= team.MaxAssists)
                        team.MaxAssists = stormPlayer.ScoreResult.Assists;
                    if (stormPlayer.ScoreResult.Deaths <= team.MaxDeaths)
                        team.MaxDeaths = stormPlayer.ScoreResult.Deaths;
                    if (stormPlayer.ScoreResult.SiegeDamage >= team.MaxSiegeDmg)
                        team.MaxSiegeDmg = stormPlayer.ScoreResult.SiegeDamage;
                    if (stormPlayer.ScoreResult.HeroDamage >= team.MaxHeroDmg)
                        team.MaxHeroDmg = stormPlayer.ScoreResult.HeroDamage;
                    if (stormPlayer.ScoreResult.Healing >= team.MaxHealing)
                        team.MaxHealing = stormPlayer.ScoreResult.Healing;
                    if (stormPlayer.ScoreResult.SelfHealing >= team.MaxSelfHealing)
                        team.MaxSelfHealing = stormPlayer.ScoreResult.SelfHealing;
                    if (stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing >= team.MaxTotalHealing)
                        team.MaxTotalHealing = stormPlayer.ScoreResult.Healing + stormPlayer.ScoreResult.SelfHealing;
                    if (stormPlayer.ScoreResult.DamageTaken >= team.MaxDmgTaken)
                        team.MaxDmgTaken = stormPlayer.ScoreResult.DamageTaken;
                    if (stormPlayer.ScoreResult.ExperienceContribution >= team.MaxExp)
                        team.MaxExp = stormPlayer.ScoreResult.ExperienceContribution;
                    if (stormPlayer.ScoreResult.Level >= team.Level)
                        team.Level = stormPlayer.ScoreResult.Level;
                    team.TotalDeath += stormPlayer.ScoreResult.Deaths;
                    team.TotalKills += stormPlayer.ScoreResult.SoloKills;
                }
            }
            if (team.Name == hotsReplay.stormReplay.WinningTeam.ToString())
                team.IsWinner = true;
        }
        private void InitPlayersData()
        {
            if (hotsReplay == null || hotsReplay.stormPlayers == null || hotsReplay.stormReplay == null) return;

            hotsPlayers = null;
            hotsPlayers = new HotsPlayer[10];
            hotsParties = new Dictionary<string, string>()
            {
                { "1", "" },
                { "2", "" },
                { "3", "" },
                { "4", "" }
            };

            int i = 0;
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
            {
                if (stormPlayer.Team.ToString() == "Blue")
                {
                    InitPlayerData(stormPlayer, i);
                    i++;
                }
            }
            foreach (StormPlayer stormPlayer in hotsReplay.stormPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                {
                    InitPlayerData(stormPlayer, i);
                    i++;
                }

            int ComputerID = 0;
            foreach (HotsPlayer hotsPlayer in hotsPlayers)
            {
                // Assign party number
                if (hotsPlayer.PartyValue != null)
                {
                    string? partyValue = hotsPlayer.PartyValue.ToString();
                    for (int j = 1; j <= 4; j++)
                    {
                        if (hotsParties[$"{j}"] == partyValue)
                        {
                            hotsPlayer.Party = j.ToString();
                            break;
                        }
                        else if (hotsParties[$"{j}"] == "")
                        {
                            hotsParties[$"{j}"] = partyValue!;
                            hotsPlayer.Party = j.ToString();
                            break;
                        }
                    }
                }

                // Calculate MVP score
                hotsPlayer.MvpScore = GetMvpScore(hotsPlayer);

                // i18n AI player name
                if (hotsPlayer.PlayerType == PlayerType.Computer)
                {
                    ComputerID++;
                    hotsPlayer.ComputerName = $"{Resources.Language.i18n.ResourceManager.GetString("strPlayer")} {ComputerID} ({Resources.Language.i18n.ResourceManager.GetString("strAI")})";
                }
            }
        }
        private void InitPlayerData(StormPlayer stormPlayer, int id)
        {
            if (hotsPlayers != null && hotsReplay != null && hotsReplay?.stormReplay?.Owner != null)
            {
                hotsPlayers[id] = new HotsPlayer(stormPlayer)
                {
                    Party = "0",
                    TeamColor = stormPlayer.Team.ToString()
                };

                if (hotsPlayers[id].TeamColor == "Blue")
                {
                    hotsPlayers[id].PlayerTeam = blueTeam;
                    hotsPlayers[id].EnemyTeam = redTeam;
                }
                else
                {
                    hotsPlayers[id].PlayerTeam = redTeam;
                    hotsPlayers[id].EnemyTeam = blueTeam;
                }
            }
        }
        private float GetMvpScore(HotsPlayer hotsPlayer)
        {
            // Ladik's CASC Viewer http://www.zezula.net/en/casc/main.html
            // mods\heroesdata.stormmod\base.stormdata\TriggerLibs\GameLib_h.galaxy
            // mods\heroesdata.stormmod\base.stormdata\TriggerLibs\GameLib.galaxy
            // https://www.reddit.com/r/heroesofthestorm/comments/6hsqcb/current_mvp_algorithm/

            if (hotsPlayer == null || hotsPlayer.ScoreResult == null || hotsPlayer.PlayerHero == null || hotsPlayer.PlayerTeam == null || hotsPlayer.EnemyTeam == null || hotsReplay == null || hotsReplay.stormReplay == null) return 0f;

            const float AwardForKill = 1.0f;
            const float AwardForAssist = 1.0f;
            const float AwardForTimeSpentDead = -0.5f;
            const float AwardForWinningTeam = 2.0f;
            const float AwardForTopHeroDamage = 1.0f;
            const float AwardForTopSiegeDamage = 1.0f;
            const float AwardForTopHealing = 1.0f;
            const float AwardForTopXPContribution = 1.0f;
            const float AwardForTopDamageTaken = 1.0f;

            const float AwardForTopHeroDamageOnTeam = 1.0f;
            const float AwardForTopSiegeDamageOnTeam = 1.0f;
            const float AwardForTopXPContributionOnTeam = 1.0f;
            const float AwardForTopDamageTakenOnTeam = 0.5f;

            const float ThroughputBonusMultiplier = 2.0f;
            const float ExtraStatMultiplierTank = 0.5f;

            int teamMaxHeroDmg = hotsPlayer.PlayerTeam.MaxHeroDmg;
            int teamMaxSiegeDmg = hotsPlayer.PlayerTeam.MaxSiegeDmg;
            int teamMaxHealing = hotsPlayer.PlayerTeam.MaxHealing;
            int teamMaxDmgTaken = hotsPlayer.PlayerTeam.MaxDmgTaken;
            int teamMaxExp = hotsPlayer.PlayerTeam.MaxExp;

            int enemyMaxHeroDmg = hotsPlayer.EnemyTeam.MaxHeroDmg;
            int enemyMaxSiegeDmg = hotsPlayer.EnemyTeam.MaxSiegeDmg;
            int enemyMaxHealing = hotsPlayer.EnemyTeam.MaxHealing;
            int enemyMaxDmgTaken = hotsPlayer.EnemyTeam.MaxDmgTaken;
            int enemyMaxExp = hotsPlayer.EnemyTeam.MaxExp;

            int maxHeroDmg = Math.Max(teamMaxHeroDmg, enemyMaxHeroDmg);
            int maxSiegeDmg = Math.Max(teamMaxSiegeDmg, enemyMaxSiegeDmg);
            int maxDmgTaken = Math.Max(teamMaxDmgTaken, enemyMaxDmgTaken);
            int maxHealing = Math.Max(teamMaxHealing, enemyMaxHealing);
            int maxExp = Math.Max(teamMaxExp, enemyMaxExp);

            string role = Init.HeroRoleFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId];
            bool isTankOrBruiser = (role == "Tank" || role == "Bruiser");
            bool isHealerOrSupport = (role == "Healer" || role == "Support");

            float MVPScore = 0f;

            // Winning team bonus
            if (hotsPlayer.IsWinner)
            {
                MVPScore += AwardForWinningTeam;
                hotsPlayer.MvpScoreWinningTeam = AwardForWinningTeam;
            }

            // Kills
            if (hotsPlayer.Kills > 0)
            {
                MVPScore += hotsPlayer.Kills * AwardForKill;
                hotsPlayer.MvpScoreKills = hotsPlayer.Kills * AwardForKill;
            }

            // Assists (reduced for some heroes)
            if (hotsPlayer.ScoreResult.Assists > 0)
            {
                float assisCoef =
                    (hotsPlayer.PlayerHero.HeroUnitId == "HeroDVaPilot" ||
                     hotsPlayer.PlayerHero.HeroUnitId == "HeroAbathur" ||
                     hotsPlayer.PlayerHero.HeroUnitId == "HeroLostVikingsController")
                    ? 0.75f : AwardForAssist;
                MVPScore += hotsPlayer.ScoreResult.Assists * assisCoef;
                hotsPlayer.MvpScoreAssists = hotsPlayer.ScoreResult.Assists * assisCoef;
            }

            // Time spent dead (increased for some heroes)
            if (hotsPlayer.ScoreResult.Deaths > 0)
            {
                float deathCoef = AwardForTimeSpentDead;
                if (hotsPlayer.PlayerHero.HeroUnitId == "HeroMurky" || hotsPlayer.PlayerHero.HeroUnitId == "HeroGall") deathCoef = -1.0f;
                else if (hotsPlayer.PlayerHero.HeroUnitId == "HeroCho") deathCoef = -0.85f;

                if (hotsReplay.stormReplay.ReplayLength.TotalSeconds > 0)
                {
                    float deathRatioPct = (float)(hotsPlayer.ScoreResult.TimeSpentDead.TotalSeconds / hotsReplay.stormReplay.ReplayLength.TotalSeconds) * 100.0f;
                    MVPScore += deathRatioPct * deathCoef;
                    hotsPlayer.MvpScoreTimeSpentDead = deathRatioPct * deathCoef;
                }
            }

            // Hero damage
            if (hotsPlayer.ScoreResult.HeroDamage >= teamMaxHeroDmg && teamMaxHeroDmg > 0)
            {
                MVPScore += AwardForTopHeroDamageOnTeam;
                hotsPlayer.MvpScoreTopHeroDamageOnTeam = AwardForTopHeroDamageOnTeam;
            }
            if (hotsPlayer.ScoreResult.HeroDamage >= maxHeroDmg && maxHeroDmg > 0)
            {
                MVPScore += AwardForTopHeroDamage;
                hotsPlayer.MvpScoreTopHeroDamage = AwardForTopHeroDamage;
            }

            // Siege damage
            if (hotsPlayer.ScoreResult.SiegeDamage >= teamMaxSiegeDmg && teamMaxSiegeDmg > 0)
            {
                MVPScore += AwardForTopSiegeDamageOnTeam;
                hotsPlayer.MvpScoreTopSiegeDamageOnTeam = AwardForTopSiegeDamageOnTeam;
            }
            if (hotsPlayer.ScoreResult.SiegeDamage >= maxSiegeDmg && maxSiegeDmg > 0)
            {
                MVPScore += AwardForTopSiegeDamage;
                hotsPlayer.MvpScoreTopSiegeDamage = AwardForTopSiegeDamage;
            }

            // Damage Taken
            if (isTankOrBruiser)
            {
                if (hotsPlayer.ScoreResult.DamageTaken >= teamMaxDmgTaken && teamMaxDmgTaken > 0)
                {
                    MVPScore += AwardForTopDamageTakenOnTeam;
                    hotsPlayer.MvpScoreTopDamageTakenOnTeam = AwardForTopDamageTakenOnTeam;
                }
                if (hotsPlayer.ScoreResult.DamageTaken >= maxDmgTaken && maxDmgTaken > 0)
                {
                    MVPScore += AwardForTopDamageTaken;
                    hotsPlayer.MvpScoreTopDamageTaken = AwardForTopDamageTaken;
                }
            }

            // Healing
            if (hotsPlayer.ScoreResult.Healing >= maxHealing && maxHealing > 0)
            {
                MVPScore += AwardForTopHealing;
                hotsPlayer.MvpScoreTopHealing = AwardForTopHealing;
            }

            // XP contribution
            if (hotsPlayer.ScoreResult.ExperienceContribution >= teamMaxExp && teamMaxExp > 0)
            {
                MVPScore += AwardForTopXPContributionOnTeam;
                hotsPlayer.MvpScoreTopXPContributionOnTeam = AwardForTopXPContributionOnTeam;
            }
            if (hotsPlayer.ScoreResult.ExperienceContribution >= maxExp && maxExp > 0)
            {
                MVPScore += AwardForTopXPContribution;
                hotsPlayer.MvpScoreTopXPContribution = AwardForTopXPContribution;
            }

            // Throughput bonus
            if (hotsPlayer.ScoreResult.HeroDamage > 0 && maxHeroDmg > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.HeroDamage / (float)maxHeroDmg);
                hotsPlayer.MvpScoreHeroDamageBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.HeroDamage / (float)maxHeroDmg);
            }
            if (hotsPlayer.ScoreResult.SiegeDamage > 0 && maxSiegeDmg > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.SiegeDamage / (float)maxSiegeDmg);
                hotsPlayer.MvpScoreSiegeDamageBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.SiegeDamage / (float)maxSiegeDmg);
            }
            if (isHealerOrSupport && hotsPlayer.ScoreResult.Healing > 0 && maxHealing > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.Healing / (float)maxHealing);
                hotsPlayer.MvpScoreHealingBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.Healing / (float)maxHealing);
            }
            if (hotsPlayer.ScoreResult.ExperienceContribution > 0 && maxExp > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.ExperienceContribution / (float)maxExp);
                hotsPlayer.MvpScoreXPContributionBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.ExperienceContribution / (float)maxExp);
            }
            if (isTankOrBruiser && hotsPlayer.ScoreResult.DamageTaken > 0 && maxDmgTaken > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.DamageTaken / (float)maxDmgTaken) * ExtraStatMultiplierTank;
                hotsPlayer.MvpScoreDamageTakenBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.DamageTaken / (float)maxDmgTaken) * ExtraStatMultiplierTank;
            }

            return MVPScore;
        }
        public static async Task<string?> FindVersionGitHubFolder(HttpClient httpClient, string replayVersion)
        {
            Debug.WriteLine($"FindVersionGitHubFolder {replayVersion}");
            string url = "https://api.github.com/repositories/214500273/contents/heroesdata";

            Debug.WriteLine($"GetAsync {url}");
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(json);
            List<string> folders = [];

            foreach (JsonElement item in doc.RootElement.EnumerateArray())
            {
                if (item.GetProperty("type").GetString() == "dir")
                {
                    folders.Add(item.GetProperty("name").GetString() ?? "");
                }
            }

            bool isPtr = replayVersion.EndsWith("_ptr", StringComparison.OrdinalIgnoreCase);

            // --- Étape 1 : Chercher la correspondance exacte ---
            string replayVersionShort = replayVersion[(replayVersion.LastIndexOf('.') + 1)..];
            string? exact = folders
                .FirstOrDefault(f => f.EndsWith(replayVersionShort, StringComparison.OrdinalIgnoreCase)
                                     && (!isPtr || f.EndsWith("_ptr", StringComparison.OrdinalIgnoreCase)));

            if (exact != null)
                return exact;

            // --- Étape 2 : Chercher la version précédente ---
            // On retire _ptr uniquement pour pouvoir parser le numéro
            string cleanReplay = replayVersion.Replace("_ptr", "", StringComparison.OrdinalIgnoreCase);

            if (!Version.TryParse(cleanReplay, out Version? targetVersion))
                return null;

            var previousCandidates = folders
                .Where(f => isPtr || !f.EndsWith("_ptr", StringComparison.OrdinalIgnoreCase)) // si PTR, on garde tout, sinon on exclut les PTR
                .Select(f => new { Name = f, Version = Version.TryParse(f.Replace("_ptr", ""), out var v) ? v : null })
                .Where(x => x.Version != null)
                .OrderBy(x => x.Version)
                .LastOrDefault(x => x.Version < targetVersion);

            return previousCandidates?.Name;
        }
        private async Task<string?> DownloadHeroesJsonFiles(HttpClient httpClient, string version)
        {
            Debug.WriteLine($"DownloadHeroesJsonFiles {version}");
            // Recherche du dossier GitHub correspondant à la version du replay
            string? versionGitHubFolder = await FindVersionGitHubFolder(httpClient, version);

            Debug.WriteLine($"versionGitHubFolder {versionGitHubFolder}");
            if (versionGitHubFolder == null)
            {
                Debug.WriteLine($"No GitHub folder found for version {versionGitHubFolder}");
                return null;
            }

            string rootFolder = $@"{Init.DbDirectory}\{versionGitHubFolder}";

            string gitHubApiUrl = $@"https://api.github.com/repos/HeroesToolChest/heroes-data/contents/heroesdata/{versionGitHubFolder}";

            if (!Directory.Exists(Init.DbDirectory) && Init.DbDirectory != null)
                Directory.CreateDirectory(Init.DbDirectory);

            if (!Directory.Exists(rootFolder))
                Directory.CreateDirectory(rootFolder);
            else
            {
                Debug.WriteLine($"Older GitHub folder found for version {versionGitHubFolder}");
                return versionGitHubFolder;
            }

            Debug.WriteLine($"Downloading heroes' Json files version {versionGitHubFolder}...");
            Debug.WriteLine($"{gitHubApiUrl}");

            string html = $@"
<head>
<script>
  // Désactive le menu contextuel
  document.addEventListener('DOMContentLoaded', () => {{
    document.addEventListener('contextmenu', (e) => {{
      e.preventDefault()
    }})
  }})

  // Affice la liste des replays
  document.addEventListener(""mousemove"", function (e) {{
    // Détection si la souris est dans les 50px à gauche
    const isHover = e.clientX <= 50;
    // On envoie à C# uniquement quand le statut change
    if (window.__lastHover !== isHover) {{
      console.log(`X: ${{event.clientX}}, Y: ${{event.clientY}}`);
      window.chrome.webview.postMessage({{
        action: ""hoverLeft"",
        isHover: isHover
      }});
      window.__lastHover = isHover;
    }}
  }});
</script>
<style>
.body-div {{
  display: flex;
  justify-content: center; /* centre horizontalement */
  align-items: center;     /* centre verticalement */
  height: 100vh;           /* occupe toute la hauteur de la fenêtre */
}}
.parent {{
  width: 900px;
  overflow-y: auto;
  text-align: left;
  margin: 0 auto;
  background-color: #000000;
  border-radius: 10px;
  padding: 20px;
}}
.header {{
  font-family: Calibri;
  font-size: 250%;
  text-align: center;
  color: White;
}}
.gameVersion {{
  font-family: Calibri;
  font-size: 150%;
  text-align: center;
  color: #ef8030;
}}
.loader {{
  width: 800px;
  height: 30px;
  border-radius: 40px;
  color: #ef8030;
  border: 2px solid;
  position: relative;
  margin: 30 auto;
}}
.loader::before {{
  content: """";
  position: absolute;
  margin: 2px;
  width: 25%;
  top: 0;
  bottom: 0;
  left: 0;
  border-radius: inherit;
  background: currentColor;
  animation: l3 3s infinite linear;
}}
@keyframes l3 {{
  50% {{left:100%;transform: translateX(calc(-100% - 4px))}}
}}
</style>
</head>
<body style=""background: url(app://hotsResources/DownloadingBG.jpg) no-repeat center center; background-size: cover; background-color: black; margin: 0; height: 100%;""></body>
<br><br>
<div class=""body-div"">
<div class=""parent"">
<div class=""header"">Downloading game datas</div>
<div class=""gameVersion"">{versionGitHubFolder}<br><br></div>
<div class=""loader""></div>
</div>
</div>
</body>
</html>
";

            webView.CoreWebView2.NavigateToString(html);

            await DownloadGitHubFolderRecursive(httpClient, gitHubApiUrl, rootFolder);
            return versionGitHubFolder;
        }
        private static async Task DownloadGitHubFolderRecursive(HttpClient httpClient, string apiUrl, string localPath)
        {
            string json = await httpClient.GetStringAsync(apiUrl);
            GitHubFileInfo[]? items = JsonSerializer.Deserialize<GitHubFileInfo[]>(json);

            if (items == null) return;

            foreach (GitHubFileInfo? item in items)
            {
                if (item.Name == null) continue;

                if (item.Type == "file")
                {
                    Console.WriteLine($"Téléchargement {item.Path}...");
                    byte[] data = await httpClient.GetByteArrayAsync(item.DownloadURL);
                    string filePath = Path.Combine(localPath, item.Name);
                    await File.WriteAllBytesAsync(filePath, data);
                }
                else if (item.Type == "dir" && item.URL != null)
                {
                    string newFolder = Path.Combine(localPath, item.Name);
                    Directory.CreateDirectory(newFolder);

                    // récursif
                    await DownloadGitHubFolderRecursive(httpClient, item.URL, newFolder);
                }
            }
        }
        private async Task CheckAndDownloadHeroesData(string replayVersion)
        {
            dbVersion = null;

            using HttpClient HttpClient = new();
            HttpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    Assembly.GetExecutingAssembly().GetName().Name ?? "HotsReplayReader",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "1.0"
                )
            );

            // https://github.com/HeroesToolChest/heroes-data/tree/master/heroesdata
            // Téléchargement des json des héros si besoin
            if (!Directory.Exists($@"{Init.DbDirectory}\{replayVersion}"))
                dbVersion = await DownloadHeroesJsonFiles(HttpClient, replayVersion);
            else
                dbVersion = replayVersion;

            if (dbVersion == null) return;

            string? heroDataJsonPath = Directory.GetFiles($@"{Init.DbDirectory}\{dbVersion}\data\", "herodata_*_localized.json").FirstOrDefault();
            string? matchAwardsJsonPath = Directory.GetFiles($@"{Init.DbDirectory}\{dbVersion}\data\", "matchawarddata_*_localized.json").FirstOrDefault();
            string? gameStringsJsonPath = Directory.GetFiles($@"{Init.DbDirectory}\{dbVersion}\gamestrings\", $"gamestrings_*_{Init.config!.LangCode?.ToLower().Replace("-", "")}.json").FirstOrDefault();

            Debug.WriteLine($"heroDataJsonPath: {heroDataJsonPath}");
            Debug.WriteLine($"matchAwardsJsonPath: {matchAwardsJsonPath}");
            Debug.WriteLine($"gameStringsJsonPath: {gameStringsJsonPath}");

            if (heroDataJsonPath == null || matchAwardsJsonPath == null || gameStringsJsonPath == null)
            {
                return;
            }

            heroDataDocument = HeroDataDocument.Parse(heroDataJsonPath);

            JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };

            string gameStringsjson = File.ReadAllText(gameStringsJsonPath);
            gameStringsRoot = JsonSerializer.Deserialize<GameStringsRoot>(gameStringsjson, jsonOptions);

            if (gameStringsRoot?.Meta == null || gameStringsRoot.Gamestrings?.Award?.Name == null || gameStringsRoot.Gamestrings.Award.Description == null) return;

            Debug.WriteLine($"GameStrings loaded for version {gameStringsRoot.Meta.Version} - {gameStringsRoot.Meta.Locale}");

            string matchAwardsJson = File.ReadAllText(matchAwardsJsonPath);
            matchAwards = JsonSerializer.Deserialize<MatchAwards>(matchAwardsJson, jsonOptions);
            if (matchAwards == null) return;

            foreach (KeyValuePair<string, string> Award in gameStringsRoot.Gamestrings.Award.Description)
            {
                matchAwards[Award.Key].Description = Award.Value;
            }
            foreach (KeyValuePair<string, string> Award in gameStringsRoot.Gamestrings.Award.Name)
            {
                matchAwards[Award.Key].Name = Award.Value;
            }
        }
        // Sélection d'un replay dans la liste
        private async void ListBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                hotsReplay = new HotsReplay(replayList[listBoxHotsReplays.SelectedIndex]);
                if (hotsReplay.stormReplay != null)
                {
                    InitTeamDatas(redTeam = new HotsTeam("Red"));
                    InitTeamDatas(blueTeam = new HotsTeam("Blue"));
                    InitPlayersData();

                    await CheckAndDownloadHeroesData(hotsReplay.stormReplay.ReplayVersion.ToString());
                    //await CheckAndDownloadHeroesData("2.55.13.95170");

                    htmlContent = $"{HTMLGetHeader()}";
                    htmlContent += $"{HTMLGetHeadTable()}";
                    htmlContent += $"{HTMLGetChatMessages()}";
                    htmlContent += $"{HTMLGetScoreTable()}";
                    htmlContent += $"{HTMLGetTalentsTable()}";
                    htmlContent += $"{HTMLGetFooter()}";

                    this.Text = $"{formTitle} - {hotsReplay?.stormReplay?.Owner?.BattleTagName}";
                }
                else
                {
                    htmlContent = welcomeHTML;
                }
            }
            catch (Exception)
            {
                htmlContent = welcomeHTML;
            }
            webView.CoreWebView2.NavigateToString(htmlContent);
        }
        private void BrowseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(Init.config!.LastBrowseDirectory))
                folderBrowserDialog.InitialDirectory = Init.config.LastBrowseDirectory;
            else
                folderBrowserDialog.InitialDirectory = hotsReplayFolder ?? "";

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK && Init.config != null)
            {
                Init.config.LastBrowseDirectory = folderBrowserDialog.SelectedPath;

                hotsReplayFolder = folderBrowserDialog.SelectedPath;
                ListHotsReplays(hotsReplayFolder);
            }
        }
        public static string GetNotepadPath()
        {
            string? NotepadPPPath = string.Empty;

            using (RegistryKey? RegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Notepad++"))
            {
                if (RegKey != null)
                {
                    object? value = RegKey.GetValue("");
                    if (value != null)
                    {
                        NotepadPPPath = value.ToString();
                    }
                }
            }
            if (File.Exists($"{NotepadPPPath}\\notepad++.exe"))
            {
                return $"{NotepadPPPath}\\notepad++.exe";
            }
            else
            {
                return "notepad.exe";
            }
        }
        private void SourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = @$"{Environment.GetEnvironmentVariable("TEMP")}\HotsReplayReader.html";
            if (File.Exists(path))
                File.Delete(path);
            using (StreamWriter sw = File.CreateText(path))
                sw.Write(htmlContent);

            Process.Start(GetNotepadPath(), path);
        }
        private void PropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PropertiesForm propertiesForm = new(this) { Location = new Point(this.Location.X + 150, this.Location.Y + 150) };
            propertiesForm.ShowDialog(this);
            propertiesForm.Dispose();
            if (Init.config != null)
            {
                Init.config.DeepLAPIKey ??= "";
                translator = new DeepLTranslator(Init.config.DeepLAPIKey);
            }
        }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HotsReplayReader.Program.ExitApp();
        }
        private void RegionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            americasRegionToolStripMenuItem.Checked = false;
            europeRegionToolStripMenuItem.Checked = false;
            asiaRegionToolStripMenuItem.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;
            if (((ToolStripMenuItem)sender)?.Tag != null)
            {
                Init.config!.Region = ((ToolStripMenuItem)sender)?.Tag?.ToString();

                Init.ListHotsAccounts();
                LoadAccountsToolStipMenu();

                if (accountsToolStripMenuItem.DropDownItems.Count > 0)
                    accountsToolStripMenuItem.DropDownItems[0].PerformClick();
            }
        }
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            const int maxRetries = 100;
            const int millisecondsTimeout = 100;

            int retries = 0;
            bool ready = false;
            while (!ready && retries < maxRetries)
            {
                try
                {
                    using FileStream fs = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                    ready = true;
                }
                catch (IOException)
                {
                    retries++;
                    Thread.Sleep(millisecondsTimeout);
                }
            }
            if (ready)
            {
                this.Invoke(new Action(() => { ListHotsReplays(Path.GetDirectoryName(e.FullPath)); }));
            }
        }
        private void HotsReplayWebReader_FormClosed(object sender, FormClosedEventArgs e)
        {
            Init.config!.Save();
            try
            {
                webView.Dispose();
                if (Directory.Exists(tempDataFolder))
                {
                    Directory.Delete(tempDataFolder, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        // Colorie l'energie
        [GeneratedRegex(@"<s\s+val=""(.*?)""[^>]*>(.*?)</s>")]
        private static partial Regex MyRegexConvertEnergy();

        // Retire les images
        [GeneratedRegex(@"<img\s.*?\/>")]
        private static partial Regex MyRegexRemoveImg();

        // Converti les couleurs
        [GeneratedRegex(@"<c\s+val=""(.*?)"">(.*?)</c>")]
        private static partial Regex MyRegexConvertColor();

        // Converti les TooltipDetails (par ex: ToolTip Mout)
        [GeneratedRegex(@"<s\s+val=""(.*?)""\s+name=""StandardTooltipDetails"">(.*?)<\/s>")]
        private static partial Regex MyRegexStandardTooltipDetails();

        // Converti les TooltipHeader (par ex: Ana's trait)
        [GeneratedRegex(@"<s\s+val=""(.*?)""\s+name=""StandardTooltipHeader"">(.*?)<\/s>")]
        private static partial Regex MyRegexStandardTooltipHeader();

        // Affiche (+x% per level)
        [GeneratedRegex(@"\~\~([0-9.]+)\~\~(</font>)?")]
        private static partial Regex MyRegexConvertPercentPerLevel();

        // Sauts de ligne
        [GeneratedRegex(@"<n/>")]
        private static partial Regex MyRegexNewLine();

        // Renomme les replays dans la liste
        [GeneratedRegex(@"(\d{4})-(\d{2})-(\d{2}) (\d{2}).(\d{2}).(\d{2}) (.*)")]
        private static partial Regex MyRegexRenameReplayInList();
    }

    // Override des couleurs pour le mode sombre
    public class DarkModeColorTable : ProfessionalColorTable
    {
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(61, 61, 61);    // Mouseover menu top
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(61, 61, 61);      // Mouseover menu bottom
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(61, 61, 61);   // Mouseover sub-menu top
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(61, 61, 61);     // Mouseover sub-menu bottom

        public override Color MenuItemBorder => Color.FromArgb(112, 112, 112);               // Border mouseover item

        private readonly Color borderColor = Color.FromArgb(61, 61, 61);
        public override Color ToolStripDropDownBackground => borderColor;                    // Bordure sub-menu
        public override Color ImageMarginGradientBegin => borderColor;                       // Bordure sub-menu
        public override Color ImageMarginGradientEnd => borderColor;                         // Bordure sub-menu
    }
    public class DarkModeRenderer : ToolStripProfessionalRenderer
    {
        public DarkModeRenderer() : base(new DarkModeColorTable()) { }
    }

    // Used to load WebView2Loader.dll from the specified folder
    internal static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", EntryPoint = "SetDllDirectoryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetDllDirectory(string lpPathName);

        [LibraryImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", SetLastError = true)]
        internal static partial int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
