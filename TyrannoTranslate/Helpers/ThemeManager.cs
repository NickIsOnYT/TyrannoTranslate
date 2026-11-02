using System.Windows;
using System.Windows.Media;
using TyrannoTranslate.Services;

namespace TyrannoTranslate.Helpers;

public static class ThemeManager
{
    private sealed record Palette(
        string BgDark, string BgPanel, string BgRowAlt, string BgRow,
        string BorderColor, string TextPrimary, string TextMuted, string Accent,
        string HeaderBg, string TagHighlight, string SelectionBackground,
        string SelectionForeground, string ButtonBackground, string ButtonHover,
        string ButtonPressed, string ErrorText);

    private static readonly IReadOnlyDictionary<string, Palette> Palettes =
        new Dictionary<string, Palette>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"] = new(
                "#1E1E2E", "#252536", "#2A2A3C", "#232334", "#3D3D5C",
                "#E8E8F0", "#9090A8", "#7C9EFF", "#2D2D44", "#4A5568",
                "#3D3D58", "#FFFFFF", "#353548", "#454560", "#2A2A3E", "#FF6B6B"),
            ["Light"] = new(
                "#F3F4F6", "#E5E7EB", "#F8FAFC", "#FFFFFF", "#C5CBD3",
                "#20232A", "#5E6672", "#315FA8", "#E4E8EF", "#DDE4EE",
                "#D6E4F0", "#20232A", "#E1E5EA", "#D4DBE4", "#C6CDD6", "#C62828"),
            ["Darker"] = new(
                "#151719", "#1B1E21", "#222629", "#1D2023", "#3A4045",
                "#D6D9DC", "#929A9F", "#9AB8D0", "#272B2F", "#41474D",
                "#343A3F", "#E8EAEC", "#30353A", "#41474A", "#222629", "#FF7777")
        };

    public static string CurrentTheme { get; private set; } = "Dark";

    public static void Apply(string? themeName)
    {
        var theme = Palettes.Keys.FirstOrDefault(key =>
            string.Equals(key, themeName, StringComparison.OrdinalIgnoreCase)) ?? "Dark";
        var palette = Palettes[theme];
        CurrentTheme = theme;

        SetBrush("BgDark", palette.BgDark);
        SetBrush("BgPanel", palette.BgPanel);
        SetBrush("BgRowAlt", palette.BgRowAlt);
        SetBrush("BgRow", palette.BgRow);
        SetBrush("BorderColor", palette.BorderColor);
        SetBrush("TextPrimary", palette.TextPrimary);
        SetBrush("TextMuted", palette.TextMuted);
        SetBrush("Accent", palette.Accent);
        SetBrush("HeaderBg", palette.HeaderBg);
        SetBrush("TagHighlight", palette.TagHighlight);
        SetBrush("SelectionBackground", palette.SelectionBackground);
        SetBrush("SelectionForeground", palette.SelectionForeground);
        SetBrush("ButtonBackground", palette.ButtonBackground);
        SetBrush("ButtonHover", palette.ButtonHover);
        SetBrush("ButtonPressed", palette.ButtonPressed);
        SetBrush("ErrorText", palette.ErrorText);

        SetBrush(SystemColors.MenuBrushKey, palette.BgPanel);
        SetBrush(SystemColors.MenuTextBrushKey, palette.TextPrimary);
        SetBrush(SystemColors.MenuHighlightBrushKey, palette.SelectionBackground);
        SetBrush(SystemColors.HighlightBrushKey, palette.SelectionBackground);
        SetBrush(SystemColors.HighlightTextBrushKey, palette.SelectionForeground);
        SetBrush(SystemColors.ControlBrushKey, palette.ButtonBackground);
        SetBrush(SystemColors.ControlTextBrushKey, palette.TextPrimary);
        SetBrush(SystemColors.WindowBrushKey, palette.BgPanel);
        SetBrush(SystemColors.WindowTextBrushKey, palette.TextPrimary);

        foreach (Window window in Application.Current.Windows)
            WindowChromeTheme.Apply(window);
    }

    private static void SetBrush(object key, string hex)
    {
        var color = ParseColor(hex);
        if (Application.Current.Resources[key] is SolidColorBrush brush)
        {
            if (brush.IsFrozen)
            {
                brush = brush.Clone();
                brush.Color = color;
                Application.Current.Resources[key] = brush;
            }
            else
                brush.Color = color;
        }
        else
            Application.Current.Resources[key] = new SolidColorBrush(color);
    }

    private static Color ParseColor(string hex) =>
        Color.FromRgb(
            Convert.ToByte(hex.Substring(1, 2), 16),
            Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16));
}
