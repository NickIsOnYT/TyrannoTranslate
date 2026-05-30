using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TyrannoTranslate.Converters;
using TyrannoTranslate.Models;
using TyrannoTranslate.Services;

namespace TyrannoTranslate;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TranslationEntry> _allEntries = new();
    private readonly ICollectionView _entriesView;
    private KsFileDocument? _document;
    private string? _currentFilePath;
    private bool _showUntranslatedOnly;
    private bool _backupsEnabled = true;
    private bool _progressBackupsEnabled = true;

    public MainWindow()
    {
        Resources.Add("LineDisplayConverter", new LineDisplayConverter());
        InitializeComponent();

        _entriesView = CollectionViewSource.GetDefaultView(_allEntries);
        _entriesView.Filter = EntryFilter;
        TranslationGrid.ItemsSource = _entriesView;

        foreach (var entry in _allEntries)
            entry.PropertyChanged += OnEntryChanged;

        _allEntries.CollectionChanged += (_, e) =>
        {
            if (e.NewItems == null) return;
            foreach (TranslationEntry item in e.NewItems)
                item.PropertyChanged += OnEntryChanged;
        };

        InputBindings.Add(new KeyBinding(new RelayCommand(OpenFile), Key.O, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(SaveFile), Key.S, ModifierKeys.Control));

        UpdateStatus();
    }

    private bool EntryFilter(object obj)
    {
        if (!_showUntranslatedOnly) return true;
        return obj is TranslationEntry e && !e.IsTranslated;
    }

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TranslationEntry.Translation) or nameof(TranslationEntry.ValidationMessage))
            UpdateStatus();
    }

    private void LoadDocument(KsFileDocument doc, string? path)
    {
        _document = doc;
        _currentFilePath = path;
        _allEntries.Clear();
        foreach (var entry in doc.Entries)
            _allEntries.Add(entry);

        FilePathText.Text = path ?? "(unsaved)";
        SaveMenuItem.IsEnabled = path != null;
        Title = path != null ? $"TyrannoTranslate — {Path.GetFileName(path)}" : "TyrannoTranslate";
        _entriesView.Refresh();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var total = _allEntries.Count;
        var translated = _allEntries.Count(e => e.IsTranslated && !e.HasError);
        var errors = _allEntries.Count(e => e.HasError);

        RowCountRun.Text = total.ToString();
        TranslatedCountRun.Text = $"{translated}/{total}";

        if (_currentFilePath == null)
            StatusText.Text = "Open a TyranoScript .ks file to begin.";
        else if (errors > 0)
            StatusText.Text = $"{errors} row(s) have bracket mismatches — fix before saving.";
        else if (_showUntranslatedOnly)
            StatusText.Text = "Showing untranslated rows only.";
        else
        {
            var parts = new List<string>();
            if (_backupsEnabled)
                parts.Add(".bak (original, once)");
            if (_progressBackupsEnabled)
                parts.Add(".baktl (progress, each save)");
            StatusText.Text = parts.Count > 0
                ? $"Ready. On save: {string.Join("; ", parts)}."
                : "Ready. All save backups disabled.";
        }
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e) => OpenFile();

    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "TyranoScript files (*.ks)|*.ks|All files (*.*)|*.*",
            Title = "Open TyranoScript scenario"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var content = File.ReadAllText(dialog.FileName);
            LoadDocument(KsParser.Parse(content), dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e) => SaveFile();

    private void SaveFile()
    {
        if (_document == null)
        {
            MessageBox.Show(this, "Open a file first.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_currentFilePath == null)
        {
            SaveAsFile();
            return;
        }

        SaveToPath(_currentFilePath);
    }

    private void SaveAsFile_Click(object sender, RoutedEventArgs e) => SaveAsFile();

    private void SaveAsFile()
    {
        if (_document == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "TyranoScript files (*.ks)|*.ks|All files (*.*)|*.*",
            Title = "Save translated scenario",
            FileName = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "translated.ks"
        };

        if (dialog.ShowDialog() != true) return;
        SaveToPath(dialog.FileName);
        _currentFilePath = dialog.FileName;
        FilePathText.Text = dialog.FileName;
        Title = $"TyrannoTranslate — {Path.GetFileName(dialog.FileName)}";
        SaveMenuItem.IsEnabled = true;
    }

    private void SaveToPath(string path)
    {
        if (_document == null) return;

        try
        {
            var content = KsWriter.BuildContent(_document.Lines, _allEntries);
            var details = new List<string>();

            if (_backupsEnabled && File.Exists(path))
            {
                var bakPath = KsBackupService.GetOriginalBackupPath(path);
                if (KsBackupService.TryCreateOriginalBackup(path))
                    details.Add($"Original backup created:\n{bakPath}");
                else if (File.Exists(bakPath))
                    details.Add($"Original backup unchanged:\n{bakPath}");
            }

            File.WriteAllText(path, content);

            if (_progressBackupsEnabled)
            {
                KsBackupService.SaveProgressSnapshot(path, content);
                details.Add($"Progress snapshot updated:\n{KsBackupService.GetProgressBackupPath(path)}");
            }

            StatusText.Text = details.Count > 0
                ? $"Saved to {path}"
                : $"Saved to {path}";

            var message = details.Count > 0
                ? "File saved successfully.\n\n" + string.Join("\n\n", details)
                : "File saved successfully.";
            MessageBox.Show(this, message, "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OriginalText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox)
            textBox.Focus();
    }

    private void CopyOriginal_Click(object sender, RoutedEventArgs e)
    {
        foreach (TranslationEntry entry in TranslationGrid.SelectedItems)
            entry.Translation = entry.Original;
        UpdateStatus();
    }

    private void ToggleFilter_Click(object sender, RoutedEventArgs e)
    {
        _showUntranslatedOnly = !_showUntranslatedOnly;
        FilterMenuItem.IsChecked = _showUntranslatedOnly;
        _entriesView.Refresh();
        UpdateStatus();
    }

    private void ToggleBackup_Click(object sender, RoutedEventArgs e)
    {
        _backupsEnabled = BackupMenuItem.IsChecked;
        UpdateStatus();
    }

    private void ToggleProgressBackup_Click(object sender, RoutedEventArgs e)
    {
        _progressBackupsEnabled = ProgressBackupMenuItem.IsChecked;
        UpdateStatus();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "TyrannoTranslate\n\nTranslates TyranoScript .ks scenario files.\n" +
            "• Left column: original text\n" +
            "• Right column: your English translation\n" +
            "• Content inside [brackets] must remain unchanged\n\n" +
            "Inspired by Translator++ for RPG Maker.",
            "About TyrannoTranslate",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}

internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
