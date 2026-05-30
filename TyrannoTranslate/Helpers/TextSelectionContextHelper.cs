using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TyrannoTranslate.Helpers;

/// <summary>
/// Preserves TextBox selection when right-clicking (including empty cell areas)
/// so the context menu Copy command still works.
/// </summary>
public static class TextSelectionContextHelper
{
    private static TextBox? _activeTextBox;
    private static int _savedSelectionStart;
    private static int _savedSelectionLength;

    public static void PrepareContextMenu(TextBox textBox, MouseButtonEventArgs e)
    {
        _activeTextBox = textBox;
        _savedSelectionStart = textBox.SelectionStart;
        _savedSelectionLength = textBox.SelectionLength;
        textBox.Focus();
        e.Handled = true;
    }

    public static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (textBox.SelectionLength == 0 && _savedSelectionLength > 0 && ReferenceEquals(_activeTextBox, textBox))
        {
            var start = Math.Clamp(_savedSelectionStart, 0, textBox.Text.Length);
            var length = Math.Clamp(_savedSelectionLength, 0, textBox.Text.Length - start);
            textBox.SelectionStart = start;
            textBox.SelectionLength = length;
        }
    }

    public static TextBox? FindTextBoxInCell(DependencyObject? source)
    {
        if (source is TextBox direct)
            return direct;

        var cell = FindAncestor<DataGridCell>(source);
        return cell != null ? FindVisualChild<TextBox>(cell) : null;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var found = FindVisualChild<T>(child);
            if (found != null)
                return found;
        }

        return null;
    }
}
