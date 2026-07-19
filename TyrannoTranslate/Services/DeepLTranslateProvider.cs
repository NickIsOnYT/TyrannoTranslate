using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TyrannoTranslate.Services;

public sealed class DeepLTranslateProvider : ITranslationProvider
{
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public string Name => "DeepL";
    public string Description => "Free: 500k chars/month, Pro: paid, excellent JA→EN quality";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public string ApiKey { get; set; } = "";
    public bool UseFreeApi { get; set; } = true;

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("DeepL API key not configured.");

        var baseUrl = UseFreeApi
            ? "https://api-free.deepl.com/v2/translate"
            : "https://api.deepl.com/v2/translate";

        var langMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "en", "EN" }, { "es", "ES" }, { "fr", "FR" }, { "de", "DE" },
            { "it", "IT" }, { "pt", "PT" }, { "pt-br", "PT-BR" }, { "ru", "RU" },
            { "ja", "JA" }, { "zh", "ZH" }, { "ko", "KO" }, { "ar", "AR" },
            { "nl", "NL" }, { "pl", "PL" }, { "sv", "SV" }, { "tr", "TR" },
            { "cs", "CS" }, { "da", "DA" }, { "el", "EL" }, { "fi", "FI" },
            { "hu", "HU" }, { "nb", "NB" }, { "ro", "RO" }, { "uk", "UK" },
            { "vi", "VI" }, { "id", "ID" }, { "th", "TH" },
        };

        var src = langMap.TryGetValue(sourceLang, out var mappedSrc) ? mappedSrc : sourceLang.ToUpperInvariant();
        var tgt = langMap.TryGetValue(targetLang, out var mappedTgt) ? mappedTgt : targetLang.ToUpperInvariant();

        var results = new List<string>();

        for (int i = 0; i < texts.Count; i += 50)
        {
            var batch = texts.Skip(i).Take(50).ToList();

            var request = new Dictionary<string, object>
            {
                ["text"] = batch,
                ["source_lang"] = src,
                ["target_lang"] = tgt
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            content.Headers.ContentType!.CharSet = "utf-8";

            var message = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            message.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", ApiKey);
            message.Content = content;

            var response = await _client.SendAsync(message, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<DeepLResponse>(responseJson);

            if (result?.Translations == null)
                throw new InvalidOperationException("DeepL returned an unexpected response.");

            results.AddRange(result.Translations.Select(t => t.Text ?? ""));
        }

        return results;
    }

    private sealed class DeepLResponse
    {
        [JsonPropertyName("translations")]
        public List<DeepLTranslation>? Translations { get; set; }
    }

    private sealed class DeepLTranslation
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("detected_source_language")]
        public string? DetectedSourceLanguage { get; set; }
    }
}
