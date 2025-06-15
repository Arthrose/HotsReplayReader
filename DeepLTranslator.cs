using System.Text.Json;

namespace HotsReplayReader
{
    internal class DeepLTranslator
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public DeepLTranslator(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }

        public async Task<string> TranslateText(string text, string targetLang)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate");

            request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("target_lang", targetLang)
            });

            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"DeepL API error: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var translatedText = doc.RootElement
                                    .GetProperty("translations")[0]
                                    .GetProperty("text")
                                    .GetString();

            return translatedText;
        }
    }
}
