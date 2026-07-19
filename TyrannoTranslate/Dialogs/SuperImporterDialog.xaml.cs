using System.Windows;
using Microsoft.Win32;
using TyrannoTranslate.Models;
using TyrannoTranslate.Services;

namespace TyrannoTranslate.Dialogs;

public partial class SuperImporterDialog : Window
{
    private readonly IReadOnlyList<TranslationEntry> _entries;
    private IReadOnlyList<TranslationMemoryEntry>? _loadedMemory;

    public SuperImporterDialog(Window owner, IEnumerable<TranslationEntry> entries)
    {
        Owner = owner;
        _entries = entries.ToList();
        InitializeComponent();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Translation memory (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Export translation memory"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            SuperImporterService.Export(_entries, dialog.FileName);
            MessageBox.Show(this, $"Exported {_entries.Count(e => e.IsTranslated)} translation(s) to:\n{dialog.FileName}",
                            "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Translation memory (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Import translation memory"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _loadedMemory = SuperImporterService.Import(dialog.FileName);
            SourceFileText.Text = $"Loaded: {System.IO.Path.GetFileName(dialog.FileName)} ({_loadedMemory.Count} entries)";

            var matchCount = _loadedMemory.Count(m =>
                _entries.Any(e => string.Equals(e.Original, m.Original, StringComparison.Ordinal)));

            var skipFilled = SkipFilledBox.IsChecked == true;
            var willUpdate = _loadedMemory.Count(m =>
                _entries.Any(e =>
                    string.Equals(e.Original, m.Original, StringComparison.Ordinal) &&
                    (!skipFilled || !e.IsTranslated)));

            if (willUpdate > 0)
            {
                var result = MessageBox.Show(this,
                    $"{willUpdate} row(s) will be updated from the imported file.\n\nApply now?",
                    "SuperImporter",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var count = SuperImporterService.Apply(_entries, _loadedMemory, skipFilled);
                    SourceFileText.Text = $"Imported: {count} row(s) updated.";
                    MatchCountText.Text = $"{count} row(s) updated.";
                }
                else
                {
                    MatchCountText.Text = $"{willUpdate} row(s) ready. Click Import again to apply.";
                }
            }
            else
            {
                MatchCountText.Text = "No matching originals found.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
