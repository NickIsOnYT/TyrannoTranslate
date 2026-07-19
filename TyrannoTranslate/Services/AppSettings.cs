using System.IO;
using System.Text.Json;

namespace TyrannoTranslate.Services;

public sealed class AppSettings
{
    private static readonly string _folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TyrannoTranslate");

    private static readonly string _filePath = Path.Combine(_folder, "settings.json");

    public string SelectedProvider { get; set; } = "Argos Translate";
    public string ArgosModelPath { get; set; } = "";
    public string GoogleApiKey { get; set; } = "";
    public bool GoogleUseFreeEndpoint { get; set; } = true;
    public string DeepLApiKey { get; set; } = "";
    public bool DeepLUseFreeApi { get; set; } = true;
    public string LibreTranslateUrl { get; set; } = "http://localhost:5000";
    public string LibreTranslateApiKey { get; set; } = "";
    public bool LibreTranslateRequiresApiKey { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new AppSettings();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_folder);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
        }
    }
}
