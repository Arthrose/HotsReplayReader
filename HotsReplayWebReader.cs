// https://github.com/HeroesToolChest/heroes-data
// https://github.com/HeroesToolChest/heroes-images

using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Heroes.StormReplayParser;
using Heroes.StormReplayParser.Decoders;
using Heroes.StormReplayParser.GameEvent;
using Heroes.StormReplayParser.MessageEvent;
using Heroes.StormReplayParser.Player;
using Heroes.StormReplayParser.TrackerEvent;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace HotsReplayReader
{
    public partial class HotsReplayWebReader : Form
    {
        readonly bool release = false;
        readonly internal string defaultLangCode = "en-US";
        readonly List<string> LangCodeList = ["de-DE", "en-US", "es-ES", "es-MX", "fr-FR", "it-IT", "ko-KR", "pl-PL", "pt-BR", "ru-RU", "zh-TW"];

        readonly bool fetchHero = false;
        readonly string fetchedHeroName = "The Lost Vikings";

        private string? hotsReplayFolder;
        internal static string currentAccount = string.Empty;

        private HotsReplay? hotsReplay;
        private HotsTeam? redTeam;
        private HotsTeam? blueTeam;
        private Dictionary<string, string>? hotsParties;
        private HotsPlayer[]? hotsPlayers;

        private TimeSpan timeGateOpen = TimeSpan.Zero;
        private TimeSpan endOfGame = TimeSpan.Zero;

        private string formTitle = "HotS Replay Reader";

        readonly private Dictionary<int, string> replayList;

        private FileSystemWatcher? fileSystemWatcher;
        readonly string tempDataFolder = Path.Combine(Path.GetTempPath(), "HotsReplayReader");
        readonly string webViewDllPath;

        internal string? htmlContent;

        internal string? dbVersion;
        internal Version versionThreshold = new("2.55.16.97039");

        internal HotsData hotsData = new();

        internal DeepLTranslator? translator;
        internal List<DeepLSupportedLanguage>? supportedLanguages;
        internal bool DeepLAPIValid = false;

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

            if (Init.config!.LangCode != null && LangCodeList.Contains(Init.config.LangCode))
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
            {
                translator = new DeepLTranslator(Init.config.DeepLAPIKey);
                if (translator != null)
                {
                    DeepLAPIValid = true;
                    supportedLanguages = translator.GetSupportedLanguages();
                }
            }

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
            foreach (string lang in LangCodeList)
            {
                languageToolStripMenu[j] = new ToolStripMenuItem
                {
                    Name = lang,
                    Tag = lang,
                    Text = Resources.Language.i18n.ResourceManager.GetString("Language", new CultureInfo(lang))
                };
                languageToolStripMenu[j].Click += new EventHandler(LanguageMenuItemClickHandler);
                languageToolStripMenu[j].CheckOnClick = true;

                if (lang == Init.config.LangCode)
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

            // Dark mode
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
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
            if (!release)
                formTitle = $"{formTitle} (v" + Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion + ')';
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
            if (release)
            {
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            }
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

                    //Vérifie si l'action est "copyTextToClipboard"
                    if (action == "copyTextToClipboard")
                    {
                        Debug.WriteLine("copyTextToClipboard: " + root.GetProperty("text").GetString() ?? "");
                        Clipboard.SetText(root.GetProperty("text").GetString() ?? "");
                    }

                    // Vérifie si l'action est "closeMenu"
                    if (action == "closeMenu")
                    {
                        fileToolStripMenuItem.HideDropDown();
                        accountsToolStripMenuItem.HideDropDown();
                        regionToolStripMenuItem.HideDropDown();
                        languageToolStripMenuItem.HideDropDown();
                        aboutToolStripMenuItem.HideDropDown();
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
                        string translatedText = string.Empty;
                        string detectedLanguage = string.Empty;
                        string detectedLanguageName = string.Empty;

                        try
                        {
                            if (translator != null)
                                (translatedText, detectedLanguage) = await translator.TranslateText(inputText, Resources.Language.i18n.ResourceManager.GetString("DeepLLang")!);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Erreur : " + ex.Message);
                            Console.WriteLine("Erreur : " + ex.Message);
                        }

                        DeepLSupportedLanguage? detectedLangInfo = supportedLanguages?.FirstOrDefault(l => string.Equals(l.LanguageCode, detectedLanguage, StringComparison.OrdinalIgnoreCase));
                        if (detectedLangInfo == null)
                        {
                            detectedLanguage = "unknown";
                            detectedLanguageName = "Unknown";
                        }
                        else
                        {
                            detectedLanguageName = detectedLangInfo.LanguageName ?? "Unknown";
                        }
                        var resultObject = new { translatedText, detectedLanguage, detectedLanguageName };
                        // Sérialise le texte traduit et le detected language en JSON
                        string returnedJson = JsonSerializer.Serialize(resultObject);

                        // Appelle le callback JavaScript puis nettoie
                        string script = $"window['{callbackId}']({returnedJson}); delete window['{callbackId}'];";
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
                        if (submenu.Name == currentAccount) submenu.Checked = true;
                        else submenu.Checked = false;
                    }
                }

                this.Invoke(new Action(() =>
                {
                    if (listBoxHotsReplays.Items.Count > 0)
                        listBoxHotsReplays.SelectedIndex = 0; // select first element
                }));
            }
            else if (accountsToolStripMenuItem.DropDownItems.Count > 0)
                AccountMenuItemClickHandler(accountsToolStripMenuItem.DropDownItems[0], EventArgs.Empty);
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
            await CheckAndLaunchUpdateAsync();
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

                if (extension == ".svg")
                {
                    var resourceManager = Resources.Flags.ResourceManager;
                    object? resource = resourceManager.GetObject(imageName);

                    if (resource is byte[] svgBytes)
                    {
                        MemoryStream msSvg = new(svgBytes);
                        e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(msSvg, 200, "OK", "Content-Type: image/svg+xml");
                    }
                    return;
                }

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
            updateToolStripMenuItem.Text = Resources.Language.i18n.strMenuUpdate;
            aboutHotsReplayReaderToolStripMenuItem.Text = Resources.Language.i18n.strMenuAbout;

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

            string bgColor = hotsReplay!.stormReplay!.Owner!.IsWinner ? "#001100" : "#110000";
            string bgImg = $"Map{hotsReplay?.stormReplay?.MapInfo.MapId}";


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

  // Copie le text dans le presse-papier
  function copyTextToClipboard(text) {{
    window.chrome.webview.postMessage({{
      action: ""copyTextToClipboard"",
      text: text
    }});
  }}
</script>
</head>
<body style=""background: {bgColor} url('app://hotsResources/{bgImg}.png') no-repeat center center / cover fixed"">
<div class=""sidebar"">replays</div>
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

            string? mapName = Resources.Language.i18n.ResourceManager.GetString($"Map{hotsReplay?.stormReplay?.MapInfo.MapId}") ?? hotsReplay?.stormReplay?.MapInfo.MapName;
            string bgColor = hotsReplay!.stormReplay!.Owner!.IsWinner ? "#001100" : "#110000";

            string html = $"<div class=\"head-container\" style=\"background-color: {bgColor};\">\n  <table>\n";

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
            if (hotsPlayer == null || hotsPlayer.PlayerHero == null || hotsPlayer.MatchAwards == null) return "";

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

            html += $"            <img src=\"app://heroesIcon/{Init.HeroNameFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]}.png\" class=\"heroIcon\" onclick='copyTextToClipboard({JsonSerializer.Serialize(hotsPlayer.BattleTagName)});'>\n"; // heroIconTeam{GetParty(hotsPlayer.BattleTagName)}

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
                string? ressourceName = hotsData.GetMatchRewardsMvpScreenIcon(hotsPlayer.MatchAwards[0].ToString());
                if (ressourceName != null)
                    ressourceName = ressourceName.Replace("%color%", hotsPlayer.Team.ToString().ToLower());
                html += $"            <img src=\"app://matchawards/{ressourceName}\" class =\"heroAwardIcon\">\n";
            }

            html += "          </span>\n";
            html += $"          <span class=\"tooltipHero tooltipHero{toolTipPosition}\">\n";

            if (hotsPlayer.MatchAwardsCount > 0)
            {
                html += $"            <center>\n";
                html += $"              <font color=\"#ffd700\">{hotsData.GetMatchRewardsName(hotsPlayer.MatchAwards[0].ToString())}</font><br>\n";
                html += $"              <font color=\"#bfd4fd\" size=\"-1\"><nobr>{hotsData.GetMatchRewardsDescription(hotsPlayer.MatchAwards[0].ToString())}</nobr></font><br>\n";
                html += $"            </center><br>\n";
            }
            if (hotsPlayer.BattleTagName.IndexOf('#') > 0)
            {
                playerName = hotsPlayer.BattleTagName[..hotsPlayer.BattleTagName.IndexOf('#')];
                playerID = hotsPlayer.BattleTagName[(hotsPlayer.BattleTagName.IndexOf('#') + 1)..];

                // Alignement des donées sur l'intitulé le plus long
                int maxLength = new[] { Resources.Language.i18n.strBattleTag.Length, Resources.Language.i18n.strAccountLevel.Length, Resources.Language.i18n.strHeroLevel.Length, Resources.Language.i18n.strTimeSpentAFK.Length }.Max();

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

                if (hotsPlayer.TimeSpentAFK != TimeSpan.Zero && hotsPlayer.TimeSpentAFK.TotalSeconds > 1)
                {
                    string AFKLabel = (Resources.Language.i18n.strTimeSpentAFK + ":").PadRight(maxLength + 2).Replace(" ", "&nbsp;");

                    string formattedTimeSpentAFK = hotsPlayer.TimeSpentAFK.Hours > 0
                        ? $"{hotsPlayer.TimeSpentAFK.Hours:D2}:{hotsPlayer.TimeSpentAFK.Minutes:D2}:{hotsPlayer.TimeSpentAFK.Seconds:D2}"
                        : $"{hotsPlayer.TimeSpentAFK.Minutes:D2}:{hotsPlayer.TimeSpentAFK.Seconds:D2}";

                    html += $"            <br>\n            <span class=\"nobr\">{AFKLabel}<font color=\"#bfd4fd\">&#8771; {formattedTimeSpentAFK}</font></span><br>\n";

                    string emptyLabel = "".PadRight(maxLength + 2).Replace(" ", "&nbsp;");
                    foreach (TimeInterval AFKInterval in hotsPlayer.TimeSpentAFKIntervals)
                    {
                        html += $"            <span class=\"nobr\">{emptyLabel}<font size=\"-1\">[{AFKInterval.Start:mm\\:ss} - {AFKInterval.End:mm\\:ss}] <font color=\"#bfd4fd\">{AFKInterval.Duration:mm\\:ss}</font></font></span><br>\n";
                    }
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

            List<StormGameEvent>? STriggerChatMessageEvents = hotsReplay.stormReplay.GameEvents.Where(e => e.GameEventType == StormGameEventType.STriggerChatMessageEvent).ToList();
            foreach (StormGameEvent STriggerChatMessageEvent in STriggerChatMessageEvents)
            {
                string? rawText = STriggerChatMessageEvent.Data?.Structure?.FirstOrDefault()?.Blob;
                if (rawText is null) continue;

                string msg = HTMLGetChatMessageEmoticon(rawText);

                StormPlayer? MessageSender = STriggerChatMessageEvent.MessageSender;
                if (MessageSender is null) continue;

                HotsPlayer? hotsPlayer = GetHotsPlayer(MessageSender.BattleTagName);
                if (hotsPlayer is null) continue;

                string? newRaw = STriggerChatMessageEvent.Data?.Structure?.FirstOrDefault()?.Blob;
                hotsMessages.Add(new HotsMessage(hotsPlayer, STriggerChatMessageEvent.Timestamp, msg, rawText));
            }
            foreach (HotsPlayer hotsPlayer in hotsPlayers)
            {
                foreach (PlayerDisconnect playerDisconnect in hotsPlayer.PlayerDisconnects)
                {
                    hotsMessages.Add(new HotsMessage(hotsPlayer, playerDisconnect.From, $"<span class=\"disconnected\">{Resources.Language.i18n.strDisconnected}</span>", null, false));
                    if (playerDisconnect.To != null)
                        hotsMessages.Add(new HotsMessage(hotsPlayer, playerDisconnect.To.Value, $"<span class=\"reconnected\">{Resources.Language.i18n.strReconnected}</span>", null, false));
                }
            }
            hotsMessages = [.. hotsMessages.OrderBy(o => o.TotalMilliseconds)];

            if (hotsMessages.Count > 0)
            {
                bool lastMessageAfterAnHour = Convert.ToInt32(hotsMessages.Last().Hours) > 0;

                string html = $@"";

                html += "<div class=\"chat-container\" tabindex=\"-1\">\n";

                html += "  <script>\r\n    document.querySelector(\".chat-container\").focus({ preventScroll: true });\r\n  </script>\r\n";

                foreach (HotsMessage hotsMessage in hotsMessages)
                {
                    html += HTMLGetChatMessage(hotsMessage, lastMessageAfterAnHour);
                }
                html += "</div>\n";

                html += @"<script>
  const chatContainer = document.querySelector("".chat-container"");

  chatContainer.addEventListener(""click"", async function (event) {
    const copyIcon = event.target.closest("".copy-icon"");
    const translateIcon = event.target.closest("".translate-icon"");
    if (!copyIcon && !translateIcon) return;

    const message = event.target.closest("".chat-message"");
    if (!message) return;

    const verbatimEl = message.querySelector("".chat-verbatim"");
    const bodyEl = message.querySelector("".chat-message-corps"");

    if (copyIcon) {
      const textToCopy = verbatimEl ? verbatimEl.textContent : (bodyEl ? bodyEl.textContent : """");
      copyTextToClipboard(textToCopy);
      event.stopPropagation();
      return;
    }

    if (translateIcon) {
      if (!bodyEl) return;

      const textToTranslate = bodyEl.textContent;
      const result = await translateWithCSharp(textToTranslate);

      bodyEl.textContent = result.translatedText;

      const flag = document.createElement(""img"");
      flag.className = ""translate-flag"";
      flag.src = ""app://flags/"" + result.detectedLanguage.toLowerCase() + "".svg"";
      flag.width = 24;
      flag.height = 18;
      flag.title = result.detectedLanguageName;

      translateIcon.replaceWith(flag);
      event.stopPropagation();
    }
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

            string? heroName = hotsData.GetHeroNameFromHeroId(hotsMessage.HotsPlayer.PlayerHero.HeroId);

            string teamColor = "";
            if (hotsMessage.HotsPlayer.Team == hotsReplay?.stormReplay?.Owner?.Team)
                teamColor = "blue";
            else
                teamColor = "red";

            string html = "  <div class=\"chat-message\">\n";
            if (hotsMessage.Translate)
                html += $"    <span class=\"chat-verbatim\" style=\"display: none;\">{WebUtility.HtmlEncode(hotsMessage.Verbatim)}</span>\n";
            if (lastMessageAfterAnHour)
                html += $"    <span class=\"chat-time chat-time-{teamColor}\"><span class=\"chat-time-bracket\">[</span>{msgHours}:{msgMinutes}:{msgSeconds}<span class=\"chat-time-bracket\">]</span></span>\n";
            else
                html += $"    <span class=\"chat-time chat-time-{teamColor}\"><span class=\"chat-time-bracket\">[</span>{msgMinutes}:{msgSeconds}<span class=\"chat-time-bracket\">]</span></span>\n";

            html += $"    <span class=\"chat-user\"><img src=\"app://minimapicons/{Init.HeroNameFromHeroUnitId[hotsMessage.HotsPlayer.PlayerHero.HeroUnitId]}.png\" class=\"chat-image\" title=\"{heroName}\"></span>\n";

            string owner = (hotsReplay?.stormReplay?.Owner?.BattleTagName == hotsMessage.HotsPlayer.BattleTagName) ? " owner" : "";

            html += $"    <span class=\"team{hotsMessage.HotsPlayer.Party}{owner}\">{msgSenderName}</span>: \n";
            if (hotsMessage.Translate)
            {
                html += $"    <span class=\"chat-message-corps\">{hotsMessage.Message}</span><span class=\"chat-icons\"><img class=\"copy-icon\" src=\"app://hotsResources/copy.png\" height=\"24\">";
                if (DeepLAPIValid)
                    html += $"<img class=\"translate-icon\" src=\"app://hotsResources/translate.png\" height=\"24\">";
                html += $"</span>\n";
            }
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
            chatMessage = chatMessage.Replace(":@",   ":nexusangry:")
                                     .Replace(":B)",  ":nexuscool:")
                                     .Replace(":^^;", ":nexusoops:")
                                     .Replace(":)",   ":nexushappy:")
                                     .Replace(":*",   ":nexuslove:")
                                     .Replace(":D",   ":nexuslol:")
                                     .Replace(":(",   ":nexussad:")
                                     .Replace(":P",   ":nexussilly:")
                                     .Replace(":|",   ":nexusmeh:")
                                     .Replace(":O",   ":nexuswow:");

            //string pattern = @"(:\w+:)"; // messages from stormReplay.ChatMessages
            string pattern = @"(:\w+:)";
            chatMessage = WebUtility.HtmlEncode(chatMessage);
            return Regex.Replace(chatMessage, pattern, match =>
            {
                string emoticonTag = match.Groups[1].Value;
                return GetEmoticonImgFromTag(emoticonTag);
            });
        }
        private string HTMLGetScoreTable()
        {
            if (hotsReplay == null || hotsPlayers == null || blueTeam == null || redTeam == null) return "";

            string html = @"<script>
  // Trie le tableau des scores
  document.addEventListener(""DOMContentLoaded"", () => {
    const table = document.getElementById(""statsTable"");
    const headers = table.querySelectorAll(""thead th"");
    const tbodyInit = table.querySelector(""tbody"");
    const originalRows = Array.from(tbodyInit.querySelectorAll(""tr""));
    let activeIndex = null;
    function parseValue(text, type) {
      if (!text) return 0;
      text = text.replace(/\u00a0/g, """").trim();
      if (type === ""time"") {
        const parts = text.split("":"").map(Number);
        // hh:mm:ss
        if (parts.length === 3) {
          const [h, m, s] = parts;
          return h * 3600 + m * 60 + s;
        }
        // mm:ss
        if (parts.length === 2) {
          const [m, s] = parts;
          return m * 60 + s;
        }
        return 0;
      }
      if (type === ""number"") {
        const num = parseFloat(text.replace(/\s/g, """").replace("","", "".""));
        return isNaN(num) ? 0 : num;
      }
      return text.toLowerCase();
    }
    headers.forEach((header, index) => {
      header.style.cursor = ""pointer"";
      header.addEventListener(""click"", () => {
        const tbody = table.querySelector(""tbody"");
        const isSorted = activeIndex === index && header.dataset.order === ""desc"";
        if (isSorted) {
          // Deuxieme clic sur la meme colonne : on remet l'ordre d'origine
          header.dataset.order = """";
          activeIndex = null;
          tbody.innerHTML = """";
          originalRows.forEach(r => tbody.appendChild(r));
          table.querySelectorAll("".active-col"").forEach(el => { el.classList.remove(""active-col""); });
          return;
        }
        const type = header.dataset.type || ""string"";
        headers.forEach(h => { if (h !== header) h.dataset.order = """"; });
        header.dataset.order = ""desc"";
        activeIndex = index;
        const rows = Array.from(tbody.querySelectorAll(""tr""));
        rows.sort((a, b) => {
          const A = parseValue(a.children[index]?.innerText, type);
          const B = parseValue(b.children[index]?.innerText, type);
          if (type === ""string"")
            return String(B).localeCompare(String(A));
          return B - A;
        });
        tbody.innerHTML = """";
        rows.forEach(r => tbody.appendChild(r));
        table.querySelectorAll("".active-col"").forEach(el => { el.classList.remove(""active-col""); });
        tbody.querySelectorAll(""tr"").forEach(row => {
          const cell = row.children[index];
          if (cell) cell.classList.add(""active-col"");
        });
      });
    });
  });
</script>
";

            html += @$"<table class=""tableScoreAndTalents"" id=""statsTable"">
  <thead>
    <tr class=""freeHeight"">
      <th></th>
      <th></th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreKills.png"">
          <span class=""tooltipHero tooltipScoreHeaderLeft"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreKills")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreAssists.png"">
          <span class=""tooltipHero tooltipScoreHeaderLeft"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreAssists")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreDeaths.png"">
          <span class=""tooltipHero tooltipScoreHeaderLeft"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreDeaths")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""time"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreTimeSpentDead.png"">
          <span class=""tooltipHero tooltipScoreHeaderLeft"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreTimeSpentDead")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreSiegeDmg.png"">
          <span class=""tooltipHero tooltipScoreHeaderRight"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreSiegeDmg")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreHeroDmg.png"">
          <span class=""tooltipHero tooltipScoreHeaderRight"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreHeroDmg")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreHealing.png"">
          <span class=""tooltipHero tooltipScoreHeaderRight"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreHealing")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreDmgTaken.png"">
          <span class=""tooltipHero tooltipScoreHeaderRight"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreDmgTaken")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreExp.png"">
          <span class=""tooltipHero tooltipScoreHeaderRight"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreExp")!}</nobr>
          </span>
        </span>
      </th>
      <th class=""teamHeader tdBorders"" data-type=""number"">
        <span class=""tooltip"">
          <img class=""scoreHeaderIcon"" src=""app://hotsResources/scoreMvp.png"">
          <span class=""tooltipHero tooltipScoreHeaderRight"">
            <nobr>{Resources.Language.i18n.ResourceManager.GetString("strScoreMvp")!}</nobr>
          </span>
        </span>
      </th>
    </tr>
  </thead>
  <tbody>
";
            foreach (HotsPlayer stormPlayer in hotsPlayers)
                if (stormPlayer.Team.ToString() == "Blue")
                    html += HTMLGetScoreTr(stormPlayer, blueTeam, GetParty(stormPlayer.BattleTagName));
            foreach (HotsPlayer stormPlayer in hotsPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                    html += HTMLGetScoreTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));

            html += "  </tbody>\n</table>\n<br><br>\n";

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

            string? heroName = hotsData.GetHeroNameFromHeroId(Init.HeroIdFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]);

            string timeSpentDead = "&nbsp;";
            if (hotsPlayer.ScoreResult.Deaths > 0)
            {
                if (hotsPlayer.ScoreResult.TimeSpentDead.Hours == 0)
                    timeSpentDead = $@"{hotsPlayer.ScoreResult.TimeSpentDead.ToString()[3..]}";
                else
                    timeSpentDead = $@"{hotsPlayer.ScoreResult.TimeSpentDead}";
            }

            string html = @"";
            html += $"    <tr class=\"team{team.Name}\">\n";
            html += $"      <td class=\"tdBorders\"><img class=\"scoreIcon\" src=\"app://heroesIcon/{Init.HeroNameFromHeroUnitId[hotsPlayer.PlayerHero.HeroUnitId]}.png\"></td>\n";
            html += $"      <td class=\"tdPlayerName team{partyColor} tdBorders\">&nbsp;{heroName}&nbsp;<br><font size=\"-1\">&nbsp;{playerName}</font></td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.SoloKills == team.MaxKills)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.SoloKills}</td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.Assists == team.MaxAssists)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.Assists}</td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.Deaths == team.MaxDeaths)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.Deaths}</td>\n";

            html += $"      <td class=\"tdBorders\">{timeSpentDead}</td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.SiegeDamage == team.MaxSiegeDmg)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.SiegeDamage:n0}</td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.HeroDamage == team.MaxHeroDmg)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.HeroDamage:n0}</td>\n";

            html += "      <td class=\"tdBorders";
            if ((hotsPlayer.ScoreResult.Healing + hotsPlayer.ScoreResult.SelfHealing) == team.MaxTotalHealing)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.Healing + hotsPlayer.ScoreResult.SelfHealing:n0}</td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.DamageTaken == team.MaxDmgTaken)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.DamageTaken:n0}</td>\n";

            html += "      <td class=\"tdBorders";
            if (hotsPlayer.ScoreResult.ExperienceContribution == team.MaxExp)
                html += " teamBestScore";
            html += $"\">{hotsPlayer.ScoreResult.ExperienceContribution:n0}</td>\n";

            // MVP Score with tooltip
            html += "      <td class=\"tooltip-cell tdBorders\">\n";
            html += "        <span class=\"tooltip\">\n          ";
            if (hotsPlayer.MatchAwardsCount > 0 && hotsPlayer.MatchAwards != null)
                if (hotsPlayer.MatchAwards[0].ToString() == "MVP")
                    html += "<span class=\"teamBestScore\">";

            html += $"{Math.Round(hotsPlayer.Mvp!.Score, 2)}";

            if (hotsPlayer.MatchAwardsCount > 0 && hotsPlayer.MatchAwards != null)
                if (hotsPlayer.MatchAwards[0].ToString() == "MVP")
                    html += "</span>";

            html += $"\n          <span class=\"tooltipHeroMvpScore\">\n";

            bool firstLine = true;

            if (hotsPlayer.Mvp.WinningTeam != null)
            {
                html += $"            WinningTeam:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.WinningTeam, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.Kills != null || hotsPlayer.Mvp.Assists != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Takedowns</u><br>\n";
                if (hotsPlayer.Mvp.Kills != null) html += $"            Kills:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.Kills, 2)}<br>\n";
                if (hotsPlayer.Mvp.Assists != null) html += $"            Assists:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.Assists, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.TimeSpentDead != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Deaths</u><br>\n";
                html += $"            TimeSpentDead:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TimeSpentDead, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.TopHeroDamageOnTeam != null || hotsPlayer.Mvp.TopHeroDamage != null || hotsPlayer.Mvp.HeroDamageBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Hero Damage</u><br>\n";
                if (hotsPlayer.Mvp.TopHeroDamage != null) html += $"            TopHeroDamage:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopHeroDamage, 2)}<br>\n";
                if (hotsPlayer.Mvp.TopHeroDamageOnTeam != null) html += $"            TopHeroDamageOnTeam:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopHeroDamageOnTeam, 2)}<br>\n";
                if (hotsPlayer.Mvp.HeroDamageBonus != null) html += $"            HeroDamageBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.HeroDamageBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.TopSiegeDamageOnTeam != null || hotsPlayer.Mvp.TopSiegeDamage != null || hotsPlayer.Mvp.SiegeDamageBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Siege Damage</u><br>\n";
                if (hotsPlayer.Mvp.TopSiegeDamage != null) html += $"            TopSiegeDamage:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopSiegeDamage, 2)}<br>\n";
                if (hotsPlayer.Mvp.TopSiegeDamageOnTeam != null) html += $"            TopSiegeDamageOnTeam:&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopSiegeDamageOnTeam, 2)}<br>\n";
                if (hotsPlayer.Mvp.SiegeDamageBonus != null) html += $"            SiegeDamageBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.SiegeDamageBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.TopDamageTakenOnTeam != null || hotsPlayer.Mvp.TopDamageTaken != null || hotsPlayer.Mvp.DamageTakenBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Damage Taken</u><br>\n";
                if (hotsPlayer.Mvp.TopDamageTaken != null) html += $"            TopDamageTaken:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopDamageTaken, 2)}<br>\n";
                if (hotsPlayer.Mvp.TopDamageTakenOnTeam != null) html += $"            TopDamageTakenOnTeam:&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopDamageTakenOnTeam, 2)}<br>\n";
                if (hotsPlayer.Mvp.DamageTakenBonus != null) html += $"            DamageTakenBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.DamageTakenBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.TopHealing != null || hotsPlayer.Mvp.HealingBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u></u>Healing<br>\n";
                if (hotsPlayer.Mvp.TopHealing != null) html += $"            TopHealing:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopHealing, 2)}<br>\n";
                if (hotsPlayer.Mvp.HealingBonus != null) html += $"            HealingBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.HealingBonus, 2)}<br>\n";
                firstLine = false;
            }

            if (hotsPlayer.Mvp.TopXPContributionOnTeam != null || hotsPlayer.Mvp.TopXPContribution != null || hotsPlayer.Mvp.XPContributionBonus != null)
            {
                if (!firstLine)
                    html += "            <br>\n";
                html += "            <u>Experience</u><br>\n";
                if (hotsPlayer.Mvp.TopXPContribution != null) html += $"            TopXPContribution:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopXPContribution, 2)}<br>\n";
                if (hotsPlayer.Mvp.TopXPContributionOnTeam != null) html += $"            TopXPContributionOnTeam:&nbsp;{Math.Round((double)hotsPlayer.Mvp.TopXPContributionOnTeam, 2)}<br>\n";
                if (hotsPlayer.Mvp.XPContributionBonus != null) html += $"            XPContributionBonus:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{Math.Round((double)hotsPlayer.Mvp.XPContributionBonus, 2)}<br>\n";
            }
            // if (hotsPlayer.ScoreResult.OnFireTimeonFire != null && hotsPlayer.ScoreResult.OnFireTimeonFire.Value.TotalSeconds > 0) html += $"<br>\n            TimeOnFire:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<font color=\"#ffd700\">{hotsPlayer.ScoreResult.OnFireTimeonFire.Value.TotalSeconds} s</font><br>\n";

            html += "          </span>\n";
            html += "        </span>\n";
            html += "      </td>\n";
            html += "    </tr>\n";
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
                    html += HTMLGetAllTalentsTr(stormPlayer, blueTeam, GetParty(stormPlayer.BattleTagName));
                    html += HTMLGetAbilitiesTr(stormPlayer, blueTeam);
                }
            }
            foreach (HotsPlayer stormPlayer in hotsPlayers)
                if (stormPlayer.Team.ToString() == "Red")
                {
                    html += HTMLGetTalentsTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));
                    html += HTMLGetAllTalentsTr(stormPlayer, redTeam, GetParty(stormPlayer.BattleTagName));
                    html += HTMLGetAbilitiesTr(stormPlayer, redTeam);
                }

            html += @"</table>
<script>
  // Renvoie toutes les lignes du groupe qui suivent une ligne trTalents jusqu'à la prochaine trTalents ou la fin du tableau
  function getGroupRows(talentsRow) {
    const rows = [];
    let next = talentsRow.nextElementSibling;
    while (next && !next.classList.contains('trTalents')) {
      rows.push(next);
      next = next.nextElementSibling;
    }
    return rows;
  }

  // Remonte jusqu'à la ligne trTalents ""parente"" d'une ligne du groupe
  function findParentTalents(row) {
    let prev = row.previousElementSibling;
    while (prev && !prev.classList.contains('trTalents')) {
      prev = prev.previousElementSibling;
    }
    return prev;
  }

  // Clic sur trTalents : affiche/cache tout le groupe qui suit
  document.querySelectorAll('.trTalents').forEach(tr => {
    tr.addEventListener('click', function() {
      const groupRows = getGroupRows(this);
      if (groupRows.length === 0) return;
      const isHidden = groupRows[0].style.display === 'none' || groupRows[0].style.display === '';
      groupRows.forEach(row => {
        row.style.display = isHidden ? 'table-row' : 'none';
      });
    });
  });

  // Clic sur trAllTalents ou trAblilities : cache tout le groupe
  document.querySelectorAll('.trAllTalents, .trAblilities').forEach(tr => {
    tr.addEventListener('click', function() {
      const parentTalents = findParentTalents(this);
      if (parentTalents) {
        getGroupRows(parentTalents).forEach(row => row.style.display = 'none');
      } else {
        this.style.display = 'none';
      }
    });

    tr.addEventListener('mouseenter', function() {
      const parentTalents = findParentTalents(this);
      if (parentTalents) parentTalents.classList.add('highlight');
    });
    tr.addEventListener('mouseleave', function() {
      const parentTalents = findParentTalents(this);
      if (parentTalents) parentTalents.classList.remove('highlight');
    });
  });
</script>
";
            return html;
        }
        private string HTMLGetTalentsTr(HotsPlayer stormPlayer, HotsTeam team, string partyColor)
        {
            if (stormPlayer.PlayerHero == null) return "";

            string? heroName = hotsData.GetHeroNameFromHeroId(Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId]);
            string playerName;

            if (stormPlayer.PlayerType == PlayerType.Computer)
                playerName = stormPlayer.ComputerName!;
            else
                playerName = stormPlayer.Name;

            string html = "";
            html += $"  <tr class=\"team{team.Name} trTalents\">\n";
            html += $"    <td class=\"tdBorders\"><img class=\"scoreIcon\" src=\"app://heroesIcon/{Init.HeroNameFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId]}.png\"></td>\n";
            html += $"    <td class=\"tdPlayerName team{partyColor} tdBorders\">&nbsp;{heroName}&nbsp;<br><font size=\"-1\">&nbsp;{playerName}</font></td>\n";

            for (int i = 0; i <= 6; i++)
            {
                int talentEarlierLevel = 0;
                if (stormPlayer.PlayerHero.HeroUnitId == "HeroChromie")
                    talentEarlierLevel = 2;

                if (i < stormPlayer.Talents.Count)
                    html += $"{GetTalentImgString(stormPlayer, i, Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId])}\n";
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
        private string GetTalentImgString(HotsPlayer stormPlayer, int i, string heroId)
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

            HotsTalent? hotsTalent;

            //  hotsPlayer.Talents[0].TalentNameId) renvoie une exception
            if (stormPlayer.Talents[i].TalentNameId != null)
                hotsTalent = hotsData.GetTalentsFromHeroIdAndTalentReferenceId(heroId, stormPlayer.Talents[i].TalentNameId!);
            else
                return "    <td class=\"tdBorders\">&nbsp;</td>";

            if (hotsTalent == null)
                return "    <td class=\"tdBorders\">&nbsp;</td>";

            string iconPath = $@"app://abilityTalents/{hotsTalent.IconFileName}";
            iconPath = iconPath.Replace("kel'thuzad", "kelthuzad");

            string description = "";
            // Si la description est vide, on n'affiche pas le talent
            if (hotsTalent.Full == null || hotsTalent.Full == string.Empty)
            {
                if (hotsTalent.Short == null || hotsTalent.Short == string.Empty)
                    description = "ERROR!";
                else
                    description = "<i>" + hotsTalent.Short + "</i>";
            }
            else
                description = hotsTalent.Full;

            // Affiche le coût en mana si il y en a un
            if (hotsTalent.Energy != null)
                hotsTalent.Energy = MyRegexConvertEnergy().Replace(hotsTalent.Energy, "<font color=\"#${1}\">${2}</font>");
            string abilityManaCost = hotsTalent.Energy != null ? $"<br>\n            {hotsTalent.Energy}" : "";
            // Affiche le cooldown si il y en a un
            string talentCooldown = hotsTalent.Cooldown != null ? $"<br>\n            <font color=\"#bfd4fd\">{hotsTalent.Cooldown}</font>" : "";

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
            <b>{hotsTalent.Name}</b>{abilityManaCost}{talentCooldown}
          </font>
          <br><br>
          {description}
        </span>
      </div>
    </td>";
        }
        private string HTMLGetAllTalentsTr(HotsPlayer stormPlayer, HotsTeam team, string partyColor)
        {
            if (stormPlayer.PlayerHero == null) return "";

            string heroId = Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId];

            List<HotsTalent> talentsLevel1  = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 1);
            List<HotsTalent> talentsLevel4  = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 4);
            List<HotsTalent> talentsLevel7  = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 7);
            List<HotsTalent> talentsLevel10 = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 10);
            List<HotsTalent> talentsLevel13 = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 13);
            List<HotsTalent> talentsLevel16 = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 16);
            List<HotsTalent> talentsLevel20 = hotsData.GetTalentsFromHeroIdAndLevel(heroId, 20);

            string html = "";
            html += $"  <tr class=\"team{team.Name} trAllTalents\">\n";
            html += $"    <td colspan=\"2\" class=\"tdBorders\">&nbsp;</td>\n";

            for (int i = 0; i <= hotsData.GetTalentMaxCountFromHeroId(heroId); i++)
            {
                for (int level = 0; level <= 6; level++)
                {
                    HotsTalent talent;
                    html += $"{GetAllTalentImgString(stormPlayer, level, i, heroId, partyColor)}\n";
                    html += "    <td class=\"tdBorders\">&nbsp;</td>\n";
                }
            }

            html += "  </tr>\n";
            return html;
        }
        private string GetAllTalentImgString(HotsPlayer stormPlayer, int level, int i, string heroId, string partyColor)
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

            HotsTalent? hotsTalent;

            //  hotsPlayer.Talents[0].TalentNameId) renvoie une exception
            if (stormPlayer.Talents[i].TalentNameId != null)
                hotsTalent = hotsData.GetTalentsFromHeroIdAndTalentReferenceId(heroId, stormPlayer.Talents[i].TalentNameId!);
            else
                return "    <td class=\"tdBorders\">&nbsp;</td>";

            if (hotsTalent == null)
                return "    <td class=\"tdBorders\">&nbsp;</td>";

            string iconPath = $@"app://abilityTalents/{hotsTalent.IconFileName}";
            iconPath = iconPath.Replace("kel'thuzad", "kelthuzad");

            string description = "";
            // Si la description est vide, on n'affiche pas le talent
            if (hotsTalent.Full == null || hotsTalent.Full == string.Empty)
            {
                if (hotsTalent.Short == null || hotsTalent.Short == string.Empty)
                    description = "ERROR!";
                else
                    description = "<i>" + hotsTalent.Short + "</i>";
            }
            else
                description = hotsTalent.Full;

            // Affiche le coût en mana si il y en a un
            if (hotsTalent.Energy != null)
                hotsTalent.Energy = MyRegexConvertEnergy().Replace(hotsTalent.Energy, "<font color=\"#${1}\">${2}</font>");
            string abilityManaCost = hotsTalent.Energy != null ? $"<br>\n            {hotsTalent.Energy}" : "";
            // Affiche le cooldown si il y en a un
            string talentCooldown = hotsTalent.Cooldown != null ? $"<br>\n            <font color=\"#bfd4fd\">{hotsTalent.Cooldown}</font>" : "";

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
            <b>{hotsTalent.Name}</b>{abilityManaCost}{talentCooldown}
          </font>
          <br><br>
          {description}
        </span>
      </div>
    </td>";
        }
        private string HTMLGetAbilitiesTr(HotsPlayer stormPlayer, HotsTeam team)
        {
            // https://psionic-storm.com/en/wp-json/psionic/v0/units?region=live
            // https://psionic-storm.com/en/wp-json/psionic/v0
            if (stormPlayer.PlayerHero == null) return "";

            int level = 1;

            string heroId = Init.HeroIdFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId];
            string heroName = Init.HeroNameFromHeroUnitId[stormPlayer.PlayerHero.HeroUnitId];
            if (heroName == "Lucio") heroName = "Lúcio";

            if (Init.PsionicStormUnits == null || Init.PsionicStormUnits[heroName] == null) return "";

            string html = "";
            html += $"  <tr class=\"trAblilities team{team.Name}\">\n";
            html += "    <td colspan=\"9\" class=\"tdBorders\">\n";

            html += "      <table width=\"100%\" rowspan=\"0\">\n";
            html += "        <tr>\n";
            html += "          <td valign=\"top\">\n";

            html += "            <table width=\"315px;\">\n";
            html += "              <tr class=\"stats\">\n";
            html += "                <td class=\"statsHealth\">\n";

            html += "                  <br>\n";
            html += $"                  Health:&nbsp;<font color=\"White\">{hotsData.GetHeroHealthFromHeroUnitId(heroId)}</font><br>\n";
            html += $"                  Regen:&nbsp;&nbsp;<font color=\"White\">{hotsData.GetHeroRegenFromHeroUnitId(heroId)}/s</font>\n";

            html += "                </td>\n";
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

            html += HTMLGetAbilityTd(heroId, HotsAbilityType.Q, team);
            html += HTMLGetAbilityTd(heroId, HotsAbilityType.W, team);
            html += HTMLGetAbilityTd(heroId, HotsAbilityType.E, team);
            html += HTMLGetAbilityTd(heroId, HotsAbilityType.R1, team);
            html += HTMLGetAbilityTd(heroId, HotsAbilityType.R2, team);
            html += HTMLGetAbilityTd(heroId, HotsAbilityType.D, team);
            html += HTMLGetAbilityTd(heroId, HotsAbilityType.Z, team);

            html += "        </tr>\n";
            html += "      </table>\n";

            html += "    </td>\n";
            html += "  </tr>\n";
            return html;
        }
        private string HTMLGetAbilityTd(string heroId, HotsAbilityType hotsAbilityType, HotsTeam team)
        {
            string html = string.Empty;

            string abilityHeader = hotsAbilityType.ToString();
            if (hotsAbilityType == HotsAbilityType.R1 || hotsAbilityType == HotsAbilityType.R2)
                abilityHeader = $"<font color=\"#ffd700\">{abilityHeader}</font>";

            html += "          <td>\n";
            html += $"            <div class=\"abilityHeader\">{abilityHeader}</div>\n";

            List<HotsAbility?>? abilities = hotsData.GetAbilitiesFromHeroIdAndAbilityType(heroId, hotsAbilityType) ?? [];

            bool firstAbility = true;
            foreach (HotsAbility? ability in abilities)
            {
                if (!firstAbility) html += "            <br>\n";
                firstAbility = false;
                html += HTMLGetAbility(ability, team);
            }

            html += "          </td>\n";

            return html;
        }
        private static string HTMLGetAbility(HotsAbility? ability, HotsTeam team)
        {
            string html = string.Empty;

            if (ability != null)
            {
                string actions = string.Empty;
                if (ability.Type == HotsAbilityType.Z)
                    actions = $"?actions=crop:left,4;border:{Uri.EscapeDataString("#000000")},1";

                html += "            <div class=\"tooltip abilityHeaderDiv\">\n";
                html += $"              &nbsp;&nbsp;<div class=\"abilityIconContainer\"><img src=\"app://abilityTalents/{ability.IconFileName}{actions}\" class=\"abilityIcon\"><img src=\"app://hotsResources/abilityIconBorder{team.Name}.png\" class=\"abilityIconBorder\"></div>&nbsp;&nbsp;\n";

                string abilityManaCost = "";
                string abilityName = "";
                string abilityCooldown = "";
                string description = "";
                if (ability.AbilityId != null)
                {
                    if (ability != null)
                    {
                        // Si la description est vide, on n'affiche pas le talent
                        if (ability.Full == null || ability.Full == string.Empty)
                        {
                            if (ability.Short == null || ability.Short == string.Empty)
                                description = "ERROR!";
                            else
                                description = "<i>" + ability.Short + "</i>";
                        }
                        else
                            description = ability.Full;


                        if (ability.Name != null)
                            abilityName = ability.Name;

                        // Affiche le coût en mana si il y en a un
                        if (ability.Energy != null)
                            ability.Energy = MyRegexConvertEnergy().Replace(ability.Energy, "<font color=\"#${1}\">${2}</font>");
                        abilityManaCost = ability.Energy != null ? $"<br>\n                  {ability.Energy}" : "";
                        // Affiche le cooldown si il y en a un
                        abilityCooldown = ability.Cooldown != null ? $"<br>\n                  <font color=\"#bfd4fd\">{ability.Cooldown}</font>" : "";

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
                    if (ability!.Type == HotsAbilityType.Q || ability.Type == HotsAbilityType.W || ability.Type == HotsAbilityType.E)
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

            GetPlayersEvents();
            GetPlayersDeaths();

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
                hotsPlayer.Mvp.Score = GetMvpScore(hotsPlayer);

                // Calculate time spent AFK
                hotsPlayer.TimeSpentAFK = GetTimeSpentAFK(hotsPlayer);

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
        private void GetPlayersEvents()
        {
            if (hotsReplay == null || hotsReplay.stormReplay == null || hotsPlayers == null) return;

            foreach (HotsPlayer hotsPlayer in hotsPlayers)
            {
                IReadOnlyList<PlayerDisconnect> playerDisconnects = hotsPlayer.PlayerDisconnects;
                foreach (StormGameEvent gameEvent in hotsReplay.stormReplay.GameEvents)
                {
                    if (gameEvent.MessageSender != null && gameEvent.MessageSender.BattleTagName != null)
                    {
                        if (gameEvent.MessageSender.BattleTagName == hotsPlayer?.BattleTagName)
                        {
                            // hotsPlayer.UserGameEvents.Add(gameEvent); // Ajoute tous les evements d'un joueur pour debug
                            // Si l'event est pendant une déco, on n'en tient pas compte
                            bool isDuringDisconnect = false;
                            foreach (PlayerDisconnect? disconnect in playerDisconnects)
                            {
                                if (gameEvent.Timestamp >= disconnect.From && gameEvent.Timestamp <= disconnect.To)
                                {
                                    isDuringDisconnect = true;
                                    break; // Quitte au premier intervalle de déconnexion correspondant
                                }
                            }
                            if (isDuringDisconnect)
                                continue; // Si l'event est pendant une déco, on ne met pas à jour lastTimestamp

                            // SCmdEvent -> lance un sort ou auto-attaque ?
                            // SCmdUpdateTargetPointEvent -> Se déplace
                            // SCameraUpdateEvent -> bouge la camera
                            if (
                                (gameEvent.GameEventType == StormGameEventType.SCmdEvent || gameEvent.GameEventType == StormGameEventType.SCmdUpdateTargetPointEvent)
                                && gameEvent.Timestamp > timeGateOpen
                            )
                            {
                                hotsPlayer.UserActionGameEvents.Add(gameEvent);
                            }
                        }
                    }
                }
            }
        }
        private void GetPlayersDeaths()
        {
            if (hotsReplay == null || hotsReplay.stormReplay == null || hotsPlayers == null) return;

            // Add deaths to the hotsPlayer objects
            foreach (StormTrackerEvent trackerEvent in hotsReplay.stormReplay.TrackerEvents
                .Where(trackerEvent =>
                    trackerEvent.TrackerEventType == StormTrackerEventType.StatGameEvent &&
                    trackerEvent.VersionedDecoder?.Structure is { Count: > 2 } structure &&
                    structure[0].Value is byte[] nameBytes &&
                    Encoding.UTF8.GetString(nameBytes) == "PlayerDeath" &&
                    structure[2].OptionalData?.ArrayData != null))
            {
                List<VersionedDecoder>? structure = trackerEvent.VersionedDecoder!.Structure!;
                VersionedDecoder[]? data = structure[2].OptionalData!.ArrayData!;

                int playerID = 0;
                List<HotsPlayer> killers = [];

                foreach (VersionedDecoder? entry in data)
                {
                    string key = Encoding.UTF8.GetString((entry.Structure?[0]?.Structure?[0]?.Value as byte[]) ?? []);

                    if (entry.Structure != null && entry.Structure.Count > 1)
                    {
                        VersionedDecoder? valDecoder = entry.Structure[1];
                        int value = int.Parse(valDecoder.ToString() ?? "0");

                        if (key == "PlayerID")
                            playerID = value;
                        else if (key == "KillingPlayer" && value > 0 && value - 1 >= 0 && value - 1 < hotsPlayers.Length)
                            killers.Add(hotsPlayers[value - 1]);
                    }
                }

                PlayerDeath? death = new()
                {
                    Timestamp = trackerEvent.Timestamp,
                    KillingPlayers = killers
                };

                HotsPlayer? player = null;
                if (playerID > 0 && playerID - 1 >= 0 && playerID - 1 < hotsPlayers.Length)
                    player = hotsPlayers[playerID - 1];

                if (player != null)
                {
                    IReadOnlyList<Heroes.StormReplayParser.Replay.StormTeamLevel>? levels = hotsReplay?.stormReplay.GetTeamLevels(player.Team);

                    if (levels != null)
                        death.Level = levels
                            .Where(l => l.Time <= trackerEvent.Timestamp)
                            .OrderByDescending(l => l.Level)
                            .Select(l => l.Level)
                            .FirstOrDefault();

                    // Calcule le timestamp de res
                    Dictionary<int, int> deathDuration;
                    if (player.PlayerHero?.HeroUnitId == "HeroMurky")
                        deathDuration = new()
                        {
                            { 1, 8}, { 2, 8}, { 3, 8}, { 4, 8}, { 5, 8}, { 6, 8}, { 7, 8}, { 8, 8}, { 9, 8}, {10, 8},
                            {11, 8}, {12, 8}, {13, 8}, {14, 8}, {15, 8}, {16, 8}, {17, 8}, {18, 8}, {19, 8}, {20, 8},
                            {21, 8}, {22, 8}, {23, 8}, {24, 8}, {25, 8}, {26, 8}, {27, 8}, {28, 8}, {29, 8}, {30, 8}
                        };
                    else if (hotsReplay?.stormReplay?.GameMode == Heroes.StormReplayParser.Replay.StormGameMode.ARAM)
                        deathDuration = new()
                        {
                            { 1,  5}, { 2,  5}, { 3,  6}, { 4,  7}, { 5,  8}, { 6,  9}, { 7, 10}, { 8, 12}, { 9, 13}, {10, 15},
                            {11, 17}, {12, 19}, {13, 22}, {14, 24}, {15, 27}, {16, 30}, {17, 33}, {18, 36}, {19, 39}, {20, 42},
                            {21, 42}, {22, 42}, {23, 42}, {24, 42}, {25, 42}, {26, 42}, {27, 42}, {28, 42}, {29, 42}, {30, 42}
                        };
                    else
                        deathDuration = new()
                        {
                            { 1, 15}, { 2, 16}, { 3, 17}, { 4, 18}, { 5, 19}, { 6, 20}, { 7, 21}, { 8, 22}, { 9, 23}, {10, 24},
                            {11, 26}, {12, 29}, {13, 32}, {14, 36}, {15, 40}, {16, 44}, {17, 50}, {18, 56}, {19, 62}, {20, 65},
                            {21, 65}, {22, 65}, {23, 65}, {24, 65}, {25, 65}, {26, 65}, {27, 65}, {28, 65}, {29, 65}, {30, 65}
                        };

                    if (!deathDuration.TryGetValue(death.Level, out int deathSeconds)) continue;
                    death.TimestampRes = death.Timestamp + TimeSpan.FromSeconds(deathSeconds);


                    // Vérifie la dernière mort du joueur
                    PlayerDeath? lastDeath = player.PlayerDeaths.LastOrDefault();
                    // Si la nouvelle mort arrive avant le res de la précédente
                    if (lastDeath != null && death.Timestamp < lastDeath.TimestampRes)
                    {
                        // Le joueur est déjà mort, on étend la durée de la mort précédente
                        if (death.TimestampRes > lastDeath.TimestampRes)
                            lastDeath.TimestampRes = death.TimestampRes;

                        // On fusionne les killers si besoin
                        foreach (var killer in killers)
                        {
                            if (!lastDeath.KillingPlayers.Contains(killer))
                                lastDeath.KillingPlayers.Add(killer);
                        }
                    }
                    // Sinon, on ajoute une nouvelle mort
                    else
                    {
                        player.PlayerDeaths.Add(death);
                    }
                }
            }
        }
        private float GetMvpScore(HotsPlayer hotsPlayer)
        {
            // Ladik's CASC Viewer http://www.zezula.net/en/casc/main.html
            // mods\heroesdata.stormmod\base.stormdata\TriggerLibs\GameLib_h.galaxy
            // mods\heroesdata.stormmod\base.stormdata\TriggerLibs\GameLib.galaxy
            // https://www.reddit.com/r/heroesofthestorm/comments/6hsqcb/current_mvp_algorithm/

            if (hotsPlayer == null || hotsPlayer.PlayerHero == null || hotsPlayer.PlayerTeam == null || hotsPlayer.EnemyTeam == null || hotsReplay == null || hotsReplay.stormReplay == null) return 0f;

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
                hotsPlayer.Mvp.WinningTeam = AwardForWinningTeam;
            }

            // Kills

            int kills = hotsPlayer.ScoreResult?.SoloKills ?? 0;
            if (kills > 0)
            {
                MVPScore += kills * AwardForKill;
                hotsPlayer.Mvp.Kills = kills * AwardForKill;
            }

            if (hotsPlayer.ScoreResult == null) return 0f;

            // Assists (reduced for some heroes)
            if (hotsPlayer.ScoreResult.Assists > 0)
            {
                float assisCoef =
                    (hotsPlayer.PlayerHero.HeroUnitId == "HeroDVaPilot" ||
                     hotsPlayer.PlayerHero.HeroUnitId == "HeroAbathur" ||
                     hotsPlayer.PlayerHero.HeroUnitId == "HeroLostVikingsController")
                    ? 0.75f : AwardForAssist;
                MVPScore += hotsPlayer.ScoreResult.Assists * assisCoef;
                hotsPlayer.Mvp.Assists = hotsPlayer.ScoreResult.Assists * assisCoef;
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
                    hotsPlayer.Mvp.TimeSpentDead = deathRatioPct * deathCoef;
                }
            }

            // Hero damage
            if (hotsPlayer.ScoreResult.HeroDamage >= teamMaxHeroDmg && teamMaxHeroDmg > 0)
            {
                MVPScore += AwardForTopHeroDamageOnTeam;
                hotsPlayer.Mvp.TopHeroDamageOnTeam = AwardForTopHeroDamageOnTeam;
            }
            if (hotsPlayer.ScoreResult.HeroDamage >= maxHeroDmg && maxHeroDmg > 0)
            {
                MVPScore += AwardForTopHeroDamage;
                hotsPlayer.Mvp.TopHeroDamage = AwardForTopHeroDamage;
            }

            // Siege damage
            if (hotsPlayer.ScoreResult.SiegeDamage >= teamMaxSiegeDmg && teamMaxSiegeDmg > 0)
            {
                MVPScore += AwardForTopSiegeDamageOnTeam;
                hotsPlayer.Mvp.TopSiegeDamageOnTeam = AwardForTopSiegeDamageOnTeam;
            }
            if (hotsPlayer.ScoreResult.SiegeDamage >= maxSiegeDmg && maxSiegeDmg > 0)
            {
                MVPScore += AwardForTopSiegeDamage;
                hotsPlayer.Mvp.TopSiegeDamage = AwardForTopSiegeDamage;
            }

            // Damage Taken
            if (isTankOrBruiser)
            {
                if (hotsPlayer.ScoreResult.DamageTaken >= teamMaxDmgTaken && teamMaxDmgTaken > 0)
                {
                    MVPScore += AwardForTopDamageTakenOnTeam;
                    hotsPlayer.Mvp.TopDamageTakenOnTeam = AwardForTopDamageTakenOnTeam;
                }
                if (hotsPlayer.ScoreResult.DamageTaken >= maxDmgTaken && maxDmgTaken > 0)
                {
                    MVPScore += AwardForTopDamageTaken;
                    hotsPlayer.Mvp.TopDamageTaken = AwardForTopDamageTaken;
                }
            }

            // Healing
            if (hotsPlayer.ScoreResult.Healing >= maxHealing && maxHealing > 0)
            {
                MVPScore += AwardForTopHealing;
                hotsPlayer.Mvp.TopHealing = AwardForTopHealing;
            }

            // XP contribution
            if (hotsPlayer.ScoreResult.ExperienceContribution >= teamMaxExp && teamMaxExp > 0)
            {
                MVPScore += AwardForTopXPContributionOnTeam;
                hotsPlayer.Mvp.TopXPContributionOnTeam = AwardForTopXPContributionOnTeam;
            }
            if (hotsPlayer.ScoreResult.ExperienceContribution >= maxExp && maxExp > 0)
            {
                MVPScore += AwardForTopXPContribution;
                hotsPlayer.Mvp.TopXPContribution = AwardForTopXPContribution;
            }

            // Throughput bonus
            if (hotsPlayer.ScoreResult.HeroDamage > 0 && maxHeroDmg > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.HeroDamage / (float)maxHeroDmg);
                hotsPlayer.Mvp.HeroDamageBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.HeroDamage / (float)maxHeroDmg);
            }
            if (hotsPlayer.ScoreResult.SiegeDamage > 0 && maxSiegeDmg > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.SiegeDamage / (float)maxSiegeDmg);
                hotsPlayer.Mvp.SiegeDamageBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.SiegeDamage / (float)maxSiegeDmg);
            }
            if (isHealerOrSupport && hotsPlayer.ScoreResult.Healing > 0 && maxHealing > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.Healing / (float)maxHealing);
                hotsPlayer.Mvp.HealingBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.Healing / (float)maxHealing);
            }
            if (hotsPlayer.ScoreResult.ExperienceContribution > 0 && maxExp > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.ExperienceContribution / (float)maxExp);
                hotsPlayer.Mvp.XPContributionBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.ExperienceContribution / (float)maxExp);
            }
            if (isTankOrBruiser && hotsPlayer.ScoreResult.DamageTaken > 0 && maxDmgTaken > 0)
            {
                MVPScore += ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.DamageTaken / (float)maxDmgTaken) * ExtraStatMultiplierTank;
                hotsPlayer.Mvp.DamageTakenBonus = ThroughputBonusMultiplier * ((float)hotsPlayer.ScoreResult.DamageTaken / (float)maxDmgTaken) * ExtraStatMultiplierTank;
            }

            return MVPScore;
        }
        private TimeSpan GetTimeSpentAFK(HotsPlayer hotsPlayer)
        {
            // https://github.com/Blizzard/heroprotocol
            // There's a known issue where revived units are not tracked, and placeholder units track death but not birth.

            if (hotsPlayer == null || hotsPlayer.PlayerHero == null || hotsPlayer.PlayerHero.HeroName == null)
                return TimeSpan.Zero;

            bool debug = true;

            if (debug) Debug.WriteLine($"End of Game: {endOfGame:mm\\:ss}");

            //string[] buggedHeroes = ["Abathur", "DVa", "Gall", "Rexxar", "LostVikings"];
            string[] buggedHeroes = [];

            if (hotsReplay == null
            || hotsReplay.stormReplay == null
            || hotsPlayer == null
            || hotsPlayer.PlayerType == PlayerType.Computer
            || buggedHeroes.Contains(hotsPlayer.PlayerHero?.HeroId)
            ) return TimeSpan.Zero;

            TimeSpan timeSpentAFK = TimeSpan.Zero;
            TimeSpan lastTimestamp = timeGateOpen;
            TimeSpan AFKThreshold = TimeSpan.FromSeconds(20);

            foreach (StormGameEvent userGameEvent in hotsPlayer.UserActionGameEvents)
            {
                //Debug.WriteLine($"{hotsPlayer?.PlayerHero?.HeroName}: {gameEvent.GameEventType.ToString()} - {gameEvent.Timestamp}");
                if (userGameEvent.Timestamp - lastTimestamp > AFKThreshold)
                {
                    TimeSpan timeSpentAFKSpan = ComputeAFKTimeSpan(lastTimestamp, userGameEvent.Timestamp, AFKThreshold, hotsPlayer.PlayerDeaths, debug, hotsPlayer.PlayerHero?.HeroName!);
                    if (timeSpentAFKSpan > TimeSpan.Zero)
                    {
                        hotsPlayer.TimeSpentAFKIntervals.Add(new TimeInterval { Start = lastTimestamp, End = userGameEvent.Timestamp });
                        timeSpentAFK += timeSpentAFKSpan;
                    }
                }
                lastTimestamp = userGameEvent.Timestamp;
            }

            // End of Game event
            if (endOfGame - lastTimestamp > AFKThreshold)
            {
                TimeSpan timeSpentAFKSpan = ComputeAFKTimeSpan(lastTimestamp, endOfGame, AFKThreshold, hotsPlayer!.PlayerDeaths, debug, hotsPlayer.PlayerHero?.HeroName!);
                if (timeSpentAFKSpan > TimeSpan.Zero)
                {
                    hotsPlayer.TimeSpentAFKIntervals.Add(new TimeInterval { Start = lastTimestamp, End = endOfGame });
                    timeSpentAFK += timeSpentAFKSpan;
                }
            }

            if (timeSpentAFK < TimeSpan.Zero)
                timeSpentAFK = TimeSpan.Zero;

            string formattedTimeSpentAFK = timeSpentAFK.Hours > 0
                ? $"{timeSpentAFK.Hours:D2}:{timeSpentAFK.Minutes:D2}:{timeSpentAFK.Seconds:D2}"
                : $"{timeSpentAFK.Minutes:D2}:{timeSpentAFK.Seconds:D2}";
            if (debug) Debug.WriteLine($"{hotsPlayer?.PlayerHero?.HeroName}: {formattedTimeSpentAFK}\n");

            return timeSpentAFK;
        }
        private static TimeSpan ComputeAFKTimeSpan(TimeSpan from, TimeSpan to, TimeSpan AFKThreshold, IReadOnlyList<PlayerDeath> playerDeaths, bool debug, string heroName)
        {
            if (to <= from || to - from <= AFKThreshold) return TimeSpan.Zero;

            TimeSpan inactiveTimeSpan = TimeSpan.Zero;

            bool hasDeath = false;
            foreach (PlayerDeath? death in playerDeaths)
            {
                TimeSpan deathStart = death.Timestamp;
                TimeSpan deathEnd = death.TimestampRes;
                TimeSpan deathSeconds = death.TimestampRes - death.Timestamp;

                // Mort hors [from, to]
                if (deathEnd <= from || deathStart >= to)
                    continue;

                hasDeath = true;

                // Coupe l'intervalle en : avant mort, mort, après mort
                TimeSpan beforeStart = from;
                TimeSpan beforeEnd = deathStart < from ? from : deathStart;
                TimeSpan afterStart = deathEnd > to ? to : deathEnd;
                TimeSpan afterEnd = to;

                TimeSpan before = beforeEnd > beforeStart ? beforeEnd - beforeStart : TimeSpan.Zero;
                TimeSpan deathTimeSpan = (deathEnd > from && deathStart < to)
                                         ? ((deathEnd < to ? deathEnd : to) - (deathStart > from ? deathStart : from))
                                         : TimeSpan.Zero;
                TimeSpan after = afterEnd > afterStart ? afterEnd - afterStart : TimeSpan.Zero;

                // Si (avant + après) dépasse le seuil, on compte avant + mort + après
                if (before + after > AFKThreshold)
                {
                    inactiveTimeSpan = before + deathTimeSpan + after;
                    if (debug)
                    {
                        Debug.WriteLine($"{heroName}: [DEATH]  Level:  {death.Level:D2} - Duration:   {deathSeconds.TotalSeconds}");
                        Debug.WriteLine($"{heroName}:          From:   {from:mm\\:ss} - To:    {to:mm\\:ss}");
                        Debug.WriteLine($"{heroName}:          Start:  {deathStart:mm\\:ss} - End:   {deathEnd:mm\\:ss}");
                        Debug.WriteLine($"{heroName}:          Before: {before:mm\\:ss} - After: {after:mm\\:ss}");
                        Debug.WriteLine($"{heroName}:          inactiveTimeSpan:      {before + deathTimeSpan + after:mm\\:ss}");
                    }
                }
                // Une seule mort par intervalle
                break;
            }

            // Si pas de mort
            if (!hasDeath)
            {
                inactiveTimeSpan = to - from;
                if (debug && inactiveTimeSpan > AFKThreshold)
                {
                    Debug.WriteLine($"{heroName}: [AFK]    From:   {from:mm\\:ss} - To:    {to:mm\\:ss}");
                    Debug.WriteLine($"{heroName}:          inactiveTimeSpan:      {to - from:mm\\:ss}");
                }
            }

            if (inactiveTimeSpan > AFKThreshold)
            {
                if (debug) Debug.WriteLine($"{heroName}: [ADDING] {inactiveTimeSpan:mm\\:ss}");
                return inactiveTimeSpan;
            }
            return TimeSpan.Zero;
        }
        private async Task CheckAndDownloadHeroesData(string replayVersion, bool firstPass)
        {
            dbVersion = null;
            if (Directory.Exists(Path.Combine(Init.DbDirectory!, replayVersion)))
                dbVersion = replayVersion;
            else {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(
                        Assembly.GetExecutingAssembly().GetName().Name ?? "HotsReplayReader",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "1.0"
                    )
                );
    
                dbVersion = await GitHubDownloader.DownloadHeroesDataAsync(httpClient, replayVersion, Init.DbDirectory!, webView.CoreWebView2);
            }
            // Seek high version in APPDATA
            dbVersion ??=
                Directory.GetDirectories(Init.DbDirectory!)
                    .Select(dirPath => new DirectoryInfo(dirPath))
                    .Select(dirInfo => new
                    {
                        Info = dirInfo,
                        Success = Version.TryParse(dirInfo.Name, out Version? v),
                        Version = v
                    })
                    .Where(x => x.Success)
                    .OrderByDescending(x => x.Version)
                    .Select(x => x.Info.Name)
                    .FirstOrDefault();
            if (dbVersion == null)
                return;

            try
            {
                string? heroDataJsonPath = Directory.GetFiles($@"{Init.DbDirectory}\{dbVersion}\data\", "herodata_*.json").FirstOrDefault();
                string? matchAwardsJsonPath = Directory.GetFiles($@"{Init.DbDirectory}\{dbVersion}\data\", "matchawarddata_*.json").FirstOrDefault();
                string? gameStringsJsonPath = Directory.GetFiles($@"{Init.DbDirectory}\{dbVersion}\gamestrings\", $"gamestrings_*_{Init.config!.LangCode?.ToLower().Replace("-", "")}.json").FirstOrDefault();

                Debug.WriteLine($"heroDataJsonPath: {heroDataJsonPath}");
                Debug.WriteLine($"matchAwardsJsonPath: {matchAwardsJsonPath}");
                Debug.WriteLine($"gameStringsJsonPath: {gameStringsJsonPath}");

                if (heroDataJsonPath == null || matchAwardsJsonPath == null || gameStringsJsonPath == null) return;

                List<string> matchAwardsList = [];
                foreach (StormPlayer player in hotsReplay!.stormPlayers!)
                    if (player.MatchAwards?.Count > 0)
                        matchAwardsList.Add(player.MatchAwards[0].ToString());

                hotsData.Parse(heroDataJsonPath, gameStringsJsonPath, matchAwardsJsonPath, Version.Parse(dbVersion), [.. hotsReplay!.stormPlayers!.Select(p => p.PlayerHero!.HeroUnitId)], matchAwardsList);
            }
            catch
            {
                if (firstPass)
                {
                    Directory.Delete(Path.Combine(Init.DbDirectory!, dbVersion), true);
                    await CheckAndDownloadHeroesData(hotsReplay!.stormReplay!.ReplayVersion.ToString(), false);
                }
            }
        }
        // Sélection d'un replay dans la liste
        internal async void ListBoxHotsReplays_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                hotsReplay = new HotsReplay(replayList[listBoxHotsReplays.SelectedIndex]);
                if (hotsReplay.stormReplay != null)
                {
                    // Get GatesOpen Timestamp
                    StormTrackerEvent gatesOpenEvent = hotsReplay.stormReplay.TrackerEvents
                        .FirstOrDefault(trackerEvent =>
                            trackerEvent.TrackerEventType == StormTrackerEventType.StatGameEvent &&
                            trackerEvent.VersionedDecoder != null &&
                            trackerEvent.VersionedDecoder.Structure != null &&
                            trackerEvent.VersionedDecoder.Structure.Any(decoder => decoder.Value != null &&
                                Encoding.UTF8.GetString(decoder.Value) == "GatesOpen"));
                    if (gatesOpenEvent != null)
                        timeGateOpen = gatesOpenEvent.Timestamp;

                    // Get EndOfGame Timestamp
                    TimeSpan timeEndOfGameXPBreakdown;
                    StormTrackerEvent endOfGameXPBreakdown = hotsReplay.stormReplay.TrackerEvents
                        .FirstOrDefault(trackerEvent =>
                            trackerEvent.TrackerEventType == StormTrackerEventType.StatGameEvent &&
                            trackerEvent.VersionedDecoder != null &&
                            trackerEvent.VersionedDecoder.Structure != null &&
                            trackerEvent.VersionedDecoder.Structure.Any(decoder => decoder.Value != null &&
                                Encoding.UTF8.GetString(decoder.Value) == "EndOfGameXPBreakdown"));
                    if (endOfGameXPBreakdown != null)
                    {
                        timeEndOfGameXPBreakdown = endOfGameXPBreakdown.Timestamp;
                        StormTrackerEvent previousEvent = hotsReplay.stormReplay.TrackerEvents
                            .Where(trackerEvent => trackerEvent.Timestamp < timeEndOfGameXPBreakdown)
                            .OrderByDescending(trackerEvent => trackerEvent.Timestamp)
                            .FirstOrDefault();
                        endOfGame = previousEvent.Timestamp;
                    }

                    InitTeamDatas(redTeam = new HotsTeam("Red"));
                    InitTeamDatas(blueTeam = new HotsTeam("Blue"));
                    InitPlayersData();

                    await CheckAndDownloadHeroesData(hotsReplay.stormReplay.ReplayVersion.ToString(), true);
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
                    htmlContent = welcomeHTML;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
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
            PropertiesForm propertiesForm = new(this) { Location = new System.Drawing.Point(this.Location.X + 150, this.Location.Y + 150) };
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
        private enum UpdateCheckStatus
        {
            UpdateLaunched,       // Mise à jour disponible, utilisateur a accepté, updater lancé
            UpdateDeclined,       // Mise à jour disponible, utilisateur a refusé
            NoUpdateAvailable,    // Déjà à jour
            NoReleaseFound,       // Aucune release trouvée sur GitHub
            NoExeAssetFound,      // Release trouvée mais aucun .exe dedans
            VersionCompareError,  // Impossible de comparer les versions
            ConnectionError       // Erreur réseau / exception
        }
        private async void UpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateCheckStatus status = await CheckAndLaunchUpdateAsync();

            switch (status)
            {
                case UpdateCheckStatus.NoUpdateAvailable:
                    MessageBox.Show($"{Resources.Language.i18n.strUpdateUpToDate}", $"{Resources.Language.i18n.strUpdateNoUpdate}",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case UpdateCheckStatus.NoReleaseFound:
                case UpdateCheckStatus.NoExeAssetFound:
                    MessageBox.Show($"{Resources.Language.i18n.strUpdateNoVersionFound}", $"{Resources.Language.i18n.strUpdateError}",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;

                case UpdateCheckStatus.VersionCompareError:
                    MessageBox.Show($"{Resources.Language.i18n.strUpdateImpossibleToCompareA}{Resources.Language.i18n.strUpdateImpossibleToCompareB}",
                        $"{Resources.Language.i18n.strUpdateError}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;

                case UpdateCheckStatus.ConnectionError:
                    MessageBox.Show($"{Resources.Language.i18n.strUpdateNoVersionFound}", $"{Resources.Language.i18n.strUpdateConnectionError}",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;

                case UpdateCheckStatus.UpdateLaunched:
                case UpdateCheckStatus.UpdateDeclined:
                    // le dialogue Oui/Non déjà affiché dans CheckAndLaunchUpdateAsync
                    break;
            }
        }
        private static async Task<UpdateCheckStatus> CheckAndLaunchUpdateAsync()
        {
            // Récupération de la version locale (et nettoyage du hash Git si présent)
            string? versionBrute = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string versionLocaleClean = versionBrute?.Split('+')[0] ?? "0.1.0";

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Assembly.GetExecutingAssembly().GetName().Name ?? "HotsReplayReader", versionLocaleClean));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            try
            {
                // Requête vers l'API GitHub — endpoint "releases" (liste) pour inclure les pré-releases
                string url = "https://api.github.com/repos/Arthrose/HotsReplayReader/releases";
                using JsonDocument? doc = await httpClient.GetFromJsonAsync<JsonDocument>(url);

                if (doc == null || doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                    return UpdateCheckStatus.NoReleaseFound;

                // Extraction dynamique du premier élément (le plus récent, GitHub trie par created_at desc)
                JsonElement latestRelease = doc.RootElement[0];
                string? tagElement = latestRelease.GetProperty("tag_name").GetString();

                if (string.IsNullOrEmpty(tagElement))
                    return UpdateCheckStatus.NoReleaseFound;

                string versionGitHubClean = tagElement.TrimStart('v', 'V');

                // Comparaison des versions (parsing sans suffixes -beta, -rc, etc.)
                if (!TryParseGitHubVersion(versionLocaleClean, out Version? localVersion) || !TryParseGitHubVersion(versionGitHubClean, out Version? githubVersion))
                    return UpdateCheckStatus.VersionCompareError;

                //if (false)
                if (githubVersion! <= localVersion!)
                    return UpdateCheckStatus.NoUpdateAvailable;

                DialogResult result = MessageBox.Show(
                    $"{Resources.Language.i18n.strUpdateNewVersionAvailableA}({versionGitHubClean}){Resources.Language.i18n.strUpdateNewVersionAvailableB}\n{Resources.Language.i18n.strUpdateDoYouWantToUpdate}",
                    $"{Resources.Language.i18n.strUpdateAvailable}",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result != DialogResult.Yes)
                    return UpdateCheckStatus.UpdateDeclined;

                // Récupère l'URL du fichier .exe
                string? exeDownloadUrl = null;
                if (latestRelease.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string? name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                        if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            exeDownloadUrl = asset.TryGetProperty("browser_download_url", out var urlProp) ? urlProp.GetString() : null;
                    }
                }

                if (string.IsNullOrEmpty(exeDownloadUrl))
                    return UpdateCheckStatus.NoExeAssetFound;

                // Lancement de HotsReplayReader.Updater.exe
                System.Reflection.Assembly currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                ExtractResourceToTempFolder(currentAssembly, "HotsReplayReader.Updater.exe");
                ExtractResourceToTempFolder(currentAssembly, "HotsReplayReader.Updater.dll");
                ExtractResourceToTempFolder(currentAssembly, "HotsReplayReader.Updater.runtimeconfig.json");
                ExtractResourceToTempFolder(currentAssembly, "HotsReplayReader.Updater.deps.json");

                string hotsReplayReaderExePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory;

                System.Diagnostics.ProcessStartInfo startInfo = new()
                {
                    FileName = Path.Combine(Path.Combine(Path.GetTempPath(), "HotsReplayReaderUpdater"), "HotsReplayReader.Updater.exe"),
                    Arguments = $"\"{hotsReplayReaderExePath}\" \"{exeDownloadUrl}\"",
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);

                Application.Exit();

                return UpdateCheckStatus.UpdateLaunched;
            }
            catch
            {
                return UpdateCheckStatus.ConnectionError;
            }
        }
        private static bool TryParseGitHubVersion(string input, out Version? version)
        {
            string cleaned = input.Split('-')[0].Trim();
            return Version.TryParse(cleaned, out version);
        }
        private static void ExtractResourceToTempFolder(System.Reflection.Assembly assembly, string resourceName)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "HotsReplayReaderUpdater");
            Directory.CreateDirectory(tempDir);
            using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"Ressource introuvable : {resourceName}");
            using FileStream fileStream = new(Path.Combine(tempDir, resourceName), FileMode.Create, FileAccess.Write);
            stream.CopyTo(fileStream);
        }
        private void AboutHotsReplayReaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new() { Location = new System.Drawing.Point(this.Location.X + 150, this.Location.Y + 150) };
            aboutForm.ShowDialog(this);
            aboutForm.Dispose();
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
