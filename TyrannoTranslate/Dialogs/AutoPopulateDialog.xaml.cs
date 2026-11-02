using System.Windows;
using TyrannoTranslate.Helpers;
using TyrannoTranslate.Models;
using TyrannoTranslate.Services;

namespace TyrannoTranslate.Dialogs;

public partial class AutoPopulateDialog : Window
{
    private readonly IReadOnlyList<TranslationEntry> _entries;
    private bool _isInitialized;

    public AutoPopulateDialog(Window owner, IEnumerable<TranslationEntry> entries)
    {
        WindowChromeTheme.Attach(this);
        Owner = owner;
        _entries = entries.ToList();
        InitializeComponent();
        _isInitialized = true;
        UpdateMatchCount();
    }

    private void FindBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isInitialized)
            UpdateMatchCount();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
            UpdateMatchCount();
    }

    private void UpdateMatchCount()
    {
        var find = FindBox.Text;
        var count = AutoPopulateService.CountMatches(
            _entries,
            find,
            MatchCaseBox.IsChecked == true,
            ExactMatchBox.IsChecked == true,
            SkipFilledBox.IsChecked == true);
        MatchCountText.Text = string.IsNullOrEmpty(find)
            ? "Enter text to search."
            : $"{count} row(s) will be updated.";
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var find = FindBox.Text;
        if (string.IsNullOrEmpty(find))
        {
            DarkMessageBox.Show(this, "Enter text to find.", "Auto-populate", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var count = AutoPopulateService.Apply(_entries, find, ReplaceBox.Text, MatchCaseBox.IsChecked == true, ExactMatchBox.IsChecked == true, SkipFilledBox.IsChecked == true);
        DialogResult = true;
        Close();

        DarkMessageBox.Show(Owner, $"Updated {count} row(s).", "Auto-populate", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
