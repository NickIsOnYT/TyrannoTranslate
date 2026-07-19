using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using TyrannoTranslate.Models;
using TyrannoTranslate.Services;

namespace TyrannoTranslate.Dialogs;

public sealed class TranslateItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _translated = string.Empty;

    public int LineNumber { get; set; }
    public string Original { get; set; } = string.Empty;
    public TranslationEntry SourceEntry { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string Translated
    {
        get => _translated;
        set
        {
            if (_translated == value) return;
            _translated = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public partial class TranslateDialog : Window
{
    public static readonly IReadOnlyList<LanguageInfo> Languages = new List<LanguageInfo>
    {
        new() { Code = "en", Name = "English" },
        new() { Code = "es", Name = "Spanish" },
        new() { Code = "fr", Name = "French" },
        new() { Code = "de", Name = "German" },
        new() { Code = "it", Name = "Italian" },
        new() { Code = "pt", Name = "Portuguese" },
        new() { Code = "ru", Name = "Russian" },
        new() { Code = "ja", Name = "Japanese" },
        new() { Code = "zh", Name = "Chinese (Simplified)" },
        new() { Code = "zh-TW", Name = "Chinese (Traditional)" },
        new() { Code = "ko", Name = "Korean" },
        new() { Code = "ar", Name = "Arabic" },
        new() { Code = "nl", Name = "Dutch" },
        new() { Code = "el", Name = "Greek" },
        new() { Code = "hi", Name = "Hindi" },
        new() { Code = "pl", Name = "Polish" },
        new() { Code = "sv", Name = "Swedish" },
        new() { Code = "tr", Name = "Turkish" },
        new() { Code = "vi", Name = "Vietnamese" },
        new() { Code = "th", Name = "Thai" },
        new() { Code = "id", Name = "Indonesian" },
        new() { Code = "ro", Name = "Romanian" },
        new() { Code = "cs", Name = "Czech" },
        new() { Code = "da", Name = "Danish" },
        new() { Code = "fi", Name = "Finnish" },
        new() { Code = "hu", Name = "Hungarian" },
        new() { Code = "nb", Name = "Norwegian" },
        new() { Code = "uk", Name = "Ukrainian" },
    };

    private readonly TranslationEntry[] _allEntries;
    private readonly AppSettings _settings;
    private readonly List<ITranslationProvider> _providers;
    private HashSet<string> _installedModels = new();
    private bool _initialized;

    public ObservableCollection<TranslateItem> Items { get; } = new();

    public TranslateDialog(Window owner, IEnumerable<TranslationEntry> entries)
    {
        Owner = owner;
        _allEntries = entries.ToArray();

        _settings = AppSettings.Load();

        _providers =
        [
            new ArgosTranslateProvider { ModelPath = _settings.ArgosModelPath },
            new GoogleTranslateProvider
            {
                ApiKey = _settings.GoogleApiKey,
                UseFreeEndpoint = _settings.GoogleUseFreeEndpoint
            },
            new DeepLTranslateProvider
            {
                ApiKey = _settings.DeepLApiKey,
                UseFreeApi = _settings.DeepLUseFreeApi
            },
            new LibreTranslateProvider
            {
                ServerUrl = _settings.LibreTranslateUrl,
                RequiresApiKey = _settings.LibreTranslateRequiresApiKey,
                ApiKey = _settings.LibreTranslateApiKey
            },
        ];

        InitializeComponent();
        EntriesGrid.ItemsSource = Items;
        SourceLangCombo.ItemsSource = Languages;
        TargetLangCombo.ItemsSource = Languages;

        SourceLangCombo.SelectedIndex = 7;
        TargetLangCombo.SelectedIndex = 0;

        ProviderCombo.ItemsSource = _providers;
        ProviderCombo.SelectedIndex = FindProviderIndex(_settings.SelectedProvider);

        SourceLangCombo.SelectionChanged += (_, _) => UpdateModelStatus();
        TargetLangCombo.SelectionChanged += (_, _) => UpdateModelStatus();

        foreach (var entry in _allEntries)
        {
            Items.Add(new TranslateItem
            {
                LineNumber = entry.LineNumber,
                Original = entry.Original,
                SourceEntry = entry,
                IsSelected = !entry.IsTranslated,
            });
        }

        foreach (var item in Items)
            item.PropertyChanged += (_, _) => UpdateSelectedCount();

        UpdateSelectedCount();
        ApplySettingsToUI();
        SwitchProviderPanel();
        _ = CheckModelsAsync();
        _initialized = true;
    }

    private int FindProviderIndex(string name)
    {
        for (int i = 0; i < _providers.Count; i++)
            if (_providers[i].Name == name)
                return i;
        return 0;
    }

    private void ApplySettingsToUI()
    {
        ModelPathBox.Text = _settings.ArgosModelPath;
        GoogleApiKeyBox.Password = _settings.GoogleApiKey;
        GoogleFreeCheck.IsChecked = _settings.GoogleUseFreeEndpoint;
        DeepLApiKeyBox.Password = _settings.DeepLApiKey;
        DeepLFreeCheck.IsChecked = _settings.DeepLUseFreeApi;
        LibreUrlBox.Text = _settings.LibreTranslateUrl;
        LibreRequiresKeyCheck.IsChecked = _settings.LibreTranslateRequiresApiKey;
        LibreApiKeyBox.Password = _settings.LibreTranslateApiKey;
        UpdateLibreKeyVisibility();
    }

    private void SwitchProviderPanel()
    {
        if (ArgosSettings == null) return;
        ArgosSettings.Visibility = Visibility.Collapsed;
        GoogleSettings.Visibility = Visibility.Collapsed;
        DeepLSettings.Visibility = Visibility.Collapsed;
        LibreSettings.Visibility = Visibility.Collapsed;

        var provider = ProviderCombo.SelectedItem as ITranslationProvider;
        if (provider == null) return;

        ProviderDescText.Text = provider.Description;

        switch (provider.Name)
        {
            case "Argos Translate":
                ArgosSettings.Visibility = Visibility.Visible;
                break;
            case "Google Translate":
                GoogleSettings.Visibility = Visibility.Visible;
                break;
            case "DeepL":
                DeepLSettings.Visibility = Visibility.Visible;
                break;
            case "LibreTranslate":
                LibreSettings.Visibility = Visibility.Visible;
                break;
        }
    }

    private void SaveSettings()
    {
        if (!_initialized) return;
        _settings.SelectedProvider = (ProviderCombo.SelectedItem as ITranslationProvider)?.Name ?? "Argos Translate";
        _settings.Save();
    }

    private async Task CheckModelsAsync()
    {
        var provider = ProviderCombo.SelectedItem as ArgosTranslateProvider;
        if (provider == null)
        {
            ModelStatusText.Text = "";
            return;
        }

        var pythonExe = FindPython();
        if (pythonExe == null)
        {
            ModelStatusText.Text = "Python not found";
            return;
        }

        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "argos_check_models.py");
        if (!File.Exists(scriptPath))
        {
            ModelStatusText.Text = "Check script not found";
            return;
        }

        try
        {
            var input = System.Text.Json.JsonSerializer.Serialize(new { packages_dir = ModelPathBox.Text.Trim() });

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardInputEncoding = new System.Text.UTF8Encoding(false)
            };
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            using var process = new System.Diagnostics.Process { StartInfo = psi };
            process.Start();
            process.StandardInput.Write(input);
            process.StandardInput.Close();

            var jsonOutput = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(jsonOutput))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonOutput);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    var installed = root.GetProperty("installed");
                    _installedModels = new HashSet<string>(
                        installed.EnumerateObject().Select(p => p.Name)
                    );
                }
            }
        }
        catch
        {
        }

        UpdateModelStatus();
    }

    private void UpdateModelStatus()
    {
        if (ModelStatusText == null) return;
        var provider = ProviderCombo.SelectedItem as ITranslationProvider;
        if (provider is ArgosTranslateProvider)
        {
            var srcCode = (SourceLangCombo.SelectedItem as LanguageInfo)?.Code;
            var tgtCode = (TargetLangCombo.SelectedItem as LanguageInfo)?.Code;

            if (srcCode == null || tgtCode == null)
            {
                ModelStatusText.Text = "";
                return;
            }

            var key = $"{srcCode}->{tgtCode}";
            ModelStatusText.Text = _installedModels.Contains(key)
                ? "Model installed ✓"
                : "Model not installed (will download on translate)";
        }
        else
        {
            ModelStatusText.Text = provider?.IsConfigured == true ? "Configured ✓" : "";
        }
    }

    private void UpdateSelectedCount()
    {
        var count = Items.Count(i => i.IsSelected);
        SelectedCountText.Text = $"{count} selected";
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in Items)
            item.IsSelected = true;
    }

    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in Items)
            item.IsSelected = false;
    }

    private void ProviderCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SwitchProviderPanel();
        SaveSettings();
        UpdateModelStatus();
        _ = CheckModelsAsync();
    }

    private void BrowseModelPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select model packages directory",
            FileName = "folder_select",
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = false
        };

        if (dialog.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                ModelPathBox.Text = dir;
                _settings.ArgosModelPath = dir;
                SaveSettings();
                _ = CheckModelsAsync();
            }
        }
    }

    private void UpdateLibreKeyVisibility()
    {
        LibreApiKeyBox.Visibility = LibreRequiresKeyCheck.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GoogleFreeCheck_Changed(object sender, RoutedEventArgs e)
    {
        _settings.GoogleUseFreeEndpoint = GoogleFreeCheck.IsChecked == true;
        SaveSettings();
        if (_providers[1] is GoogleTranslateProvider google)
            google.UseFreeEndpoint = GoogleFreeCheck.IsChecked == true;
        UpdateModelStatus();
    }

    private void LibreRequiresKeyCheck_Changed(object sender, RoutedEventArgs e)
    {
        _settings.LibreTranslateRequiresApiKey = LibreRequiresKeyCheck.IsChecked == true;
        SaveSettings();
        if (_providers[3] is LibreTranslateProvider libre)
            libre.RequiresApiKey = LibreRequiresKeyCheck.IsChecked == true;
        UpdateLibreKeyVisibility();
    }

    private void ModelPathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _settings.ArgosModelPath = ModelPathBox.Text;
        SaveSettings();
        if (_providers[0] is ArgosTranslateProvider argos)
            argos.ModelPath = ModelPathBox.Text;
    }

    private void GoogleApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _settings.GoogleApiKey = GoogleApiKeyBox.Password;
        SaveSettings();
        if (_providers[1] is GoogleTranslateProvider google)
            google.ApiKey = GoogleApiKeyBox.Password;
        UpdateModelStatus();
    }

    private void DeepLApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _settings.DeepLApiKey = DeepLApiKeyBox.Password;
        SaveSettings();
        if (_providers[2] is DeepLTranslateProvider deepl)
            deepl.ApiKey = DeepLApiKeyBox.Password;
        UpdateModelStatus();
    }

    private void DeepLFreeCheck_Changed(object sender, RoutedEventArgs e)
    {
        _settings.DeepLUseFreeApi = DeepLFreeCheck.IsChecked == true;
        SaveSettings();
        if (_providers[2] is DeepLTranslateProvider deepl)
            deepl.UseFreeApi = DeepLFreeCheck.IsChecked == true;
    }

    private void LibreUrlBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _settings.LibreTranslateUrl = LibreUrlBox.Text;
        SaveSettings();
        if (_providers[3] is LibreTranslateProvider libre)
            libre.ServerUrl = LibreUrlBox.Text;
        UpdateModelStatus();
    }

    private void LibreApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _settings.LibreTranslateApiKey = LibreApiKeyBox.Password;
        SaveSettings();
        if (_providers[3] is LibreTranslateProvider libre)
            libre.ApiKey = LibreApiKeyBox.Password;
    }

    private static string? FindPython()
    {
        foreach (var name in new[] { "python", "python3", "py" })
        {
            try
            {
                using var proc = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                proc.WaitForExit(2000);
                if (proc.ExitCode == 0)
                    return name;
            }
            catch { }
        }
        return null;
    }

    private void SetControlsEnabled(bool enabled)
    {
        TranslateButton.IsEnabled = enabled;
        SelectAllButton.IsEnabled = enabled;
        DeselectAllButton.IsEnabled = enabled;
        SourceLangCombo.IsEnabled = enabled;
        TargetLangCombo.IsEnabled = enabled;
        ProviderCombo.IsEnabled = enabled;
    }

    private async void Translate_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = Items.Where(i => i.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            MessageBox.Show(this, "Select at least one line to translate.", "Translate",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var provider = ProviderCombo.SelectedItem as ITranslationProvider;
        if (provider == null)
        {
            MessageBox.Show(this, "No translation provider selected.", "Translate",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!provider.IsConfigured)
        {
            MessageBox.Show(this, $"{provider.Name} is not configured.\n{provider.Description}",
                            "Translate", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sourceLang = (SourceLangCombo.SelectedItem as LanguageInfo)?.Code
                         ?? SourceLangCombo.SelectedValue as string;
        var targetLang = (TargetLangCombo.SelectedItem as LanguageInfo)?.Code
                         ?? TargetLangCombo.SelectedValue as string;

        if (string.IsNullOrEmpty(sourceLang) || string.IsNullOrEmpty(targetLang))
        {
            MessageBox.Show(this, "Select source and target languages.", "Translate",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (sourceLang == targetLang)
        {
            MessageBox.Show(this, "Source and target languages must be different.", "Translate",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetControlsEnabled(false);
        ProgressBar.Visibility = Visibility.Visible;
        StatusText.Text = $"Translating {selectedItems.Count} line(s) via {provider.Name}...";

        try
        {
            // Split each line into text segments and tags, collect all plain-text segments
            var allSegments = new List<List<TextSegment>>();
            var allPlainTexts = new List<string>();

            foreach (var item in selectedItems)
            {
                var segments = TranslationTagHelper.SplitText(item.Original);
                allSegments.Add(segments);
                foreach (var seg in segments.Where(s => !s.IsTag))
                    allPlainTexts.Add(seg.Text);
            }

            // Translate all plain-text segments in one batch
            var translations = await provider.TranslateAsync(allPlainTexts, sourceLang, targetLang);

            // Rejoin segments with translations, write back to items
            var ti = 0;
            for (int i = 0; i < selectedItems.Count; i++)
            {
                var segments = allSegments[i];
                var segTranslations = new List<string>();
                foreach (var seg in segments)
                {
                    if (!seg.IsTag)
                        segTranslations.Add(translations[ti++]);
                }
                var restored = TranslationTagHelper.Rejoin(segments, segTranslations);
                selectedItems[i].Translated = restored;
                selectedItems[i].SourceEntry.Translation = restored;
            }

            StatusText.Text = $"Done! Translated {selectedItems.Count} line(s).";
            if (provider is ArgosTranslateProvider)
                _ = CheckModelsAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Translation failed: {ex.Message}";
        }
        finally
        {
            SetControlsEnabled(true);
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
