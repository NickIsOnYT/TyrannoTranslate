using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TyrannoTranslate.Services;

public sealed class LibreTranslateProvider : ITranslationProvider
{
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public string Name => "LibreTranslate";
    public string Description => "Self-hosted (free, data stays local, requires your own server)";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServerUrl);

    public string ServerUrl { get; set; } = "http://localhost:5000";
    public bool RequiresApiKey { get; set; } = false;
    public string ApiKey { get; set; } = "";

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("LibreTranslate URL not configured.");

        var baseUrl = ServerUrl.TrimEnd('/') + "/translate";

        var semaphore = new SemaphoreSlim(4, 4);
        var results = new string[texts.Count];

        var tasks = new List<Task>();
        for (int i = 0; i < texts.Count; i++)
        {
            var idx = i;
            var text = texts[i];
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var request = new Dictionary<string, object>
                    {
                        ["q"] = text,
                        ["source"] = sourceLang,
                        ["target"] = targetLang,
                    };
                    if (!string.IsNullOrWhiteSpace(ApiKey))
                        request["api_key"] = ApiKey;

                    var requestJson = JsonSerializer.Serialize(request);
                    var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

                    var response = await _client.PostAsync(baseUrl, content, ct);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync(ct);
                    var result = JsonSerializer.Deserialize<LibreResponse>(responseJson);
                    results[idx] = result?.TranslatedText ?? "";
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
        return results;
    }

    private sealed class LibreResponse
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}
