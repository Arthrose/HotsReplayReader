using System.IO.Compression;
using Microsoft.Web.WebView2.Core;

namespace HotsReplayReader;

internal static class GitHubDownloader
{
    private static readonly Version VersionThreshold = new("2.55.16.97039");

    public static async Task<string?> DownloadHeroesDataAsync(HttpClient httpClient, string replayVersion, string dbDirectory, CoreWebView2 coreWebView)
    {
        if (!Version.TryParse(replayVersion, out Version? requestedVersion)) return null;

        bool useNewRepository = requestedVersion >= VersionThreshold;
        string downloadUrl = BuildExactDownloadUrl(requestedVersion, useNewRepository);
        Version? versionToUse = requestedVersion;

        // La version exacte n'existe pas
        if (!await UrlExistsAsync(httpClient, downloadUrl))
        {
            versionToUse = await GetLatestVersionAsync(httpClient, useNewRepository);
            if (versionToUse == null)
                return null;
            downloadUrl = BuildExactDownloadUrl(versionToUse, useNewRepository);
        }

        string destinationFolder = Path.Combine(dbDirectory, versionToUse.ToString());

        // Déjà présente localement
        if (Directory.Exists(destinationFolder)) return versionToUse.ToString();

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
<div class=""header"">{Resources.Language.i18n.ResourceManager.GetString("DownloadingGameData")!}</div>
<div class=""gameVersion"">{versionToUse}<br><br></div>
<div class=""loader""></div>
</div>
</div>
</body>
</html>
";
        coreWebView.NavigateToString(html);

        Directory.CreateDirectory(destinationFolder);
        string tempZipFile = Path.Combine(Path.GetTempPath(), $"heroes-data-{Guid.NewGuid():N}.zip");

        try
        {
            await DownloadFileAsync(httpClient, downloadUrl, tempZipFile);
            if (!useNewRepository) ZipFile.ExtractToDirectory(tempZipFile, dbDirectory, true);
            else ZipFile.ExtractToDirectory(tempZipFile, destinationFolder, true);
            return versionToUse.ToString();
        }
        finally
        {
            if (File.Exists(tempZipFile)) File.Delete(tempZipFile);
        }
    }

    private static string BuildExactDownloadUrl(Version version, bool useNewRepository)
    {
        string v = version.ToString();
        if (useNewRepository) return $"https://github.com/HeroesToolChest/heroes-data2/releases/download/v{v}/heroes-data-no-maps-{v}.zip";
        return $"https://github.com/HeroesToolChest/heroes-data/releases/download/v{v}/heroes-data-{v}_last.zip";
    }

    private static async Task<bool> UrlExistsAsync(HttpClient httpClient, string url)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Head, url);
            using HttpResponseMessage response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<Version?> GetLatestVersionAsync(HttpClient httpClient, bool useNewRepository)
    {
        try
        {
            string latestUrl = useNewRepository ? "https://github.com/HeroesToolChest/heroes-data2/releases/latest" : "https://github.com/HeroesToolChest/heroes-data/releases/latest";
            using HttpResponseMessage response = await httpClient.GetAsync(latestUrl);

            string finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? throw new Exception("Unable to determine latest release.");

            // Exemple :
            // https://github.com/HeroesToolChest/heroes-data2/releases/tag/v2.55.17.97605

            int index = finalUrl.LastIndexOf("/v", StringComparison.OrdinalIgnoreCase);
            if (index < 0) throw new Exception($"Unexpected release URL : {finalUrl}");
            string versionString = finalUrl[(index + 2)..];
            return Version.Parse(versionString);
        }
        catch
        {
            return null;
        }
    }

    private static async Task DownloadFileAsync(HttpClient httpClient, string url, string destinationFile)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using Stream input = await response.Content.ReadAsStreamAsync();
        await using FileStream output = File.Create(destinationFile);
        await input.CopyToAsync(output);
    }
}
