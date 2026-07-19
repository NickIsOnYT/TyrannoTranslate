using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TyrannoTranslate.Services;

public sealed class GoogleTranslateProvider : ITranslationProvider
{
    private static readonly HttpClient _client = new();

    public string Name => "Google Translate";
    public string Description => UseFreeEndpoint
        ? "Free (unofficial endpoint, no key needed, rate-limited)"
        : "Paid ($20/million chars, free tier available), high quality";

    public bool IsConfigured => UseFreeEndpoint || !string.IsNullOrWhiteSpace(ApiKey);

    public string ApiKey { get; set; } = "";
    public bool UseFreeEndpoint { get; set; } = true;

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Google Translate is not configured.");

        return UseFreeEndpoint
            ? await TranslateFreeAsync(texts, sourceLang, targetLang, ct)
            : await TranslateApiAsync(texts, sourceLang, targetLang, ct);
    }

    private async Task<IReadOnlyList<string>> TranslateFreeAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct)
    {
        var results = new string[texts.Count];
        var semaphore = new SemaphoreSlim(8, 8);

        var tasks = texts.Select(async (text, i) =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var url = "https://translate.googleapis.com/translate_a/single"
                    + $"?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t"
                    + "&q=" + Uri.EscapeDataString(text);

                var response = await _client.GetStringAsync(url, ct);

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                {
                    results[i] = "";
                    return;
                }

                var sentences = root[0];
                if (sentences.ValueKind != JsonValueKind.Array)
                {
                    results[i] = "";
                    return;
                }

                var sb = new System.Text.StringBuilder();
                for (int j = 0; j < sentences.GetArrayLength(); j++)
                {
                    var entry = sentences[j];
                    if (entry.ValueKind == JsonValueKind.Array && entry.GetArrayLength() > 0)
                        sb.Append(entry[0].GetString() ?? "");
                }
                results[i] = sb.ToString();
            }
            catch (Exception ex)
            {
                results[i] = $"[error: {ex.Message}]";
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    private async Task<IReadOnlyList<string>> TranslateApiAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct)
    {
        var results = new List<string>();

        for (int i = 0; i < texts.Count; i += 128)
        {
            var batch = texts.Skip(i).Take(128).ToList();

            var request = new GoogleApiRequest
            {
                Q = batch,
                Source = sourceLang,
                Target = targetLang,
                Format = "text"
            };

            var url = $"https://translation.googleapis.com/language/translate/v2?key={ApiKey}";

            var response = await _client.PostAsJsonAsync(url, request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GoogleApiResponse>(cancellationToken: ct);

            if (result?.Data?.Translations == null)
                throw new InvalidOperationException("Google Translate returned an unexpected response.");

            results.AddRange(result.Data.Translations.Select(t => t.TranslatedText ?? ""));
        }

        return results;
    }

    private sealed class GoogleApiRequest
    {
        [JsonPropertyName("q")]
        public List<string> Q { get; set; } = new();

        [JsonPropertyName("source")]
        public string Source { get; set; } = "";

        [JsonPropertyName("target")]
        public string Target { get; set; } = "";

        [JsonPropertyName("format")]
        public string Format { get; set; } = "text";
    }

    private sealed class GoogleApiResponse
    {
        [JsonPropertyName("data")]
        public GoogleApiData? Data { get; set; }
    }

    private sealed class GoogleApiData
    {
        [JsonPropertyName("translations")]
        public List<GoogleApiTranslation>? Translations { get; set; }
    }

    private sealed class GoogleApiTranslation
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}
