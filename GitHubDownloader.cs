using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace HotsReplayReader;

internal static class GitHubDownloader
{
    private static readonly Version VersionThreshold = new("2.55.16.97039");

    private readonly record struct ResolvedVersion(Version Version, bool IsPtr);

    // GitHub versions cache
    private static readonly ConcurrentDictionary<Version, ResolvedVersion> ResolvedVersionCache = new();

    public static async Task<string?> DownloadHeroesDataAsync(HttpClient httpClient, string replayVersion, string dbDirectory, CoreWebView2 coreWebView)
    {
        if (!Version.TryParse(replayVersion, out Version? requestedVersion)) return null;

        bool useNewRepository = requestedVersion >= VersionThreshold;

        // Cached version
        if (ResolvedVersionCache.TryGetValue(requestedVersion, out ResolvedVersion cached))
        {
            string cachedFolder = Path.Combine(dbDirectory, cached.Version.ToString());
            if (Directory.Exists(cachedFolder)) return cached.Version.ToString();

            return await DownloadAndExtractAsync(httpClient, cached, useNewRepository, dbDirectory, coreWebView);
        }

        // Downloaded version
        string exactFolder = Path.Combine(dbDirectory, requestedVersion.ToString());
        if (Directory.Exists(exactFolder))
        {
            ResolvedVersionCache[requestedVersion] = new ResolvedVersion(requestedVersion, false);
            return requestedVersion.ToString();
        }

        ResolvedVersion versionToUse;

        // Exact GiHub release version
        string exactNormalUrl = BuildExactDownloadUrl(requestedVersion, useNewRepository, isPtr: false);
        if (await UrlExistsAsync(httpClient, exactNormalUrl))
        {
            versionToUse = new ResolvedVersion(requestedVersion, false);
        }
        else
        {
            // Exact GiHub ptr version
            string exactPtrUrl = BuildExactDownloadUrl(requestedVersion, useNewRepository, isPtr: true);
            if (await UrlExistsAsync(httpClient, exactPtrUrl))
            {
                versionToUse = new ResolvedVersion(requestedVersion, true);
            }
            else
            {
                // None: get closest version
                ResolvedVersion? closest = await GetClosestAvailableVersionAsync(httpClient, requestedVersion, useNewRepository);
                if (closest == null)
                    return null;
                versionToUse = closest.Value;
            }
        }

        ResolvedVersionCache[requestedVersion] = versionToUse;

        string destinationFolder = Path.Combine(dbDirectory, versionToUse.Version.ToString());
        if (Directory.Exists(destinationFolder)) return versionToUse.Version.ToString();

        return await DownloadAndExtractAsync(httpClient, versionToUse, useNewRepository, dbDirectory, coreWebView, destinationFolder);
    }

    private static async Task<string?> DownloadAndExtractAsync(HttpClient httpClient, ResolvedVersion versionToUse, bool useNewRepository, string dbDirectory, CoreWebView2 coreWebView, string? destinationFolder = null)
    {
        destinationFolder ??= Path.Combine(dbDirectory, versionToUse.Version.ToString());
        string downloadUrl = BuildExactDownloadUrl(versionToUse.Version, useNewRepository, versionToUse.IsPtr);

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
<div class=""gameVersion"">{versionToUse.Version}{(versionToUse.IsPtr ? " [PTR]" : "")}<br><br></div>
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
            return versionToUse.Version.ToString();
        }
        finally
        {
            if (File.Exists(tempZipFile)) File.Delete(tempZipFile);
        }
    }

    private static string BuildExactDownloadUrl(Version version, bool useNewRepository, bool isPtr)
    {
        string v = version.ToString();

        if (useNewRepository)
        {
            return isPtr
                ? $"https://github.com/HeroesToolChest/heroes-data2/releases/download/v{v}_ptr/heroes-data-{v}_ptr.zip"
                : $"https://github.com/HeroesToolChest/heroes-data2/releases/download/v{v}/heroes-data-no-maps-{v}.zip";
        }

        return isPtr
            ? $"https://github.com/HeroesToolChest/heroes-data/releases/download/v{v}_ptr/heroes-data-{v}_ptr_last.zip"
            : $"https://github.com/HeroesToolChest/heroes-data/releases/download/v{v}/heroes-data-{v}_last.zip";
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

    private static async Task<ResolvedVersion?> GetClosestAvailableVersionAsync(HttpClient httpClient, Version requestedVersion, bool useNewRepository)
    {
        string repo = useNewRepository ? "heroes-data2" : "heroes-data";

        int page = 1;
        const int perPage = 30;

        try
        {
            while (true)
            {
                string apiUrl = $"https://api.github.com/repos/HeroesToolChest/{repo}/releases?per_page={perPage}&page={page}";

                using HttpRequestMessage request = new(HttpMethod.Get, apiUrl);
                request.Headers.UserAgent.ParseAdd("HotsReplayReader");

                using HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using Stream stream = await response.Content.ReadAsStreamAsync();
                using JsonDocument doc = await JsonDocument.ParseAsync(stream);

                JsonElement releases = doc.RootElement;
                if (releases.ValueKind != JsonValueKind.Array || releases.GetArrayLength() == 0)
                    return null;

                foreach (JsonElement release in releases.EnumerateArray())
                {
                    if (!release.TryGetProperty("tag_name", out JsonElement tagNameProp))
                        continue;

                    string? tagName = tagNameProp.GetString();
                    if (string.IsNullOrEmpty(tagName))
                        continue;

                    string raw = tagName.StartsWith('v') ? tagName[1..] : tagName;

                    bool isPtr = raw.EndsWith("_ptr", StringComparison.OrdinalIgnoreCase);
                    string versionString = isPtr ? raw[..^"_ptr".Length] : raw;

                    if (!Version.TryParse(versionString, out Version? releaseVersion))
                        continue;

                    if (releaseVersion <= requestedVersion)
                        return new ResolvedVersion(releaseVersion, isPtr);
                }

                if (releases.GetArrayLength() < perPage)
                    return null;

                page++;
            }
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