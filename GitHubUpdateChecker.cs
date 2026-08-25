using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class GitHubUpdateChecker
{
    private static readonly HttpClient _httpClient = new();

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public required string TagName { get; set; }
    }

    public static async Task<string> GetLatestReleaseVersionAsync()
    {
        try
        {
            // Obligatoire pour l'API GitHub
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HotsReplayReader-Updater");

            // URL qui liste TOUTES les releases (Stables et Pre-releases mélangées)
            string url = "https://github.com";

            // On récupère le tableau de résultats
            var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(url);

            // Si le dépôt a au moins une release, la première [0] est la plus récente
            if (releases != null && releases.Count > 0)
            {
                return releases[0].TagName.TrimStart('v');
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Impossible de vérifier les mises à jour : {ex.Message}");
        }

        return null;
    }
}
