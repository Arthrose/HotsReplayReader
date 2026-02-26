using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotsReplayReader
{
    internal class DeepLTranslator(string apiKey)
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _apiKey = apiKey;

        public async Task<List<DeepLSupportedLanguage>?> GetSupportedLanguages()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api-free.deepl.com/v2/languages?type=target");
            request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"DeepL API error: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            // Désérialisation de la réponse JSON directement dans une liste d'objets SupportedLanguage
            var supportedLanguages = JsonSerializer.Deserialize<List<DeepLSupportedLanguage>>(json);

            return supportedLanguages;
        }

        public async Task<(string translatedText, string detectedLanguage)> TranslateText(string? text, string targetLang)
        {
            if (text != null)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate");

                request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");

                var content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("target_lang", targetLang)
                ]);

                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"DeepL API error: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var translatedText   = doc.RootElement
                                          .GetProperty("translations")[0]
                                          .GetProperty("text")
                                          .GetString();

                var detectedLanguage = doc.RootElement
                                          .GetProperty("translations")[0]
                                          .GetProperty("detected_source_language")
                                          .GetString();

                if (translatedText != null)
                    return (translatedText, detectedLanguage ?? "");
                else
                    return ("", "");
            }
            else
                return ("", "");
        }
        public async Task<bool> CheckApiKeyValidity()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api-free.deepl.com/v2/usage");
            request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }

    internal class DeepLSupportedLanguage
    {
        [JsonPropertyName("language")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("name")]
        public string? LanguageName { get; set; }

        [JsonPropertyName("supports_formality")]
        public bool SupportsFormality { get; set; }
    }
}
