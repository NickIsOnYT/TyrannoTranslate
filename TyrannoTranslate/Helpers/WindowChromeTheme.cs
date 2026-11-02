using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace TyrannoTranslate.Helpers;

public static class WindowChromeTheme
{
    private const int UseImmersiveDarkModeBeforeWindows11 = 19;
    private const int UseImmersiveDarkMode = 20;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    public static void Attach(Window window)
    {
        window.SourceInitialized += OnSourceInitialized;
    }

    public static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var background = GetColor("BgDark", Color.FromRgb(30, 30, 46));
        var text = GetColor("TextPrimary", Color.FromRgb(232, 232, 240));
        var border = GetColor("BorderColor", Color.FromRgb(61, 61, 92));
        var darkMode = (background.R * 299 + background.G * 587 + background.B * 114) / 1000 < 128 ? 1 : 0;
        var darkModeAttribute = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
            ? UseImmersiveDarkMode
            : UseImmersiveDarkModeBeforeWindows11;
        SetAttribute(window, handle, darkModeAttribute, darkMode, nameof(UseImmersiveDarkMode));

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return;

        SetAttribute(window, handle, CaptionColor, ToColorRef(background), nameof(CaptionColor));
        SetAttribute(window, handle, TextColor, ToColorRef(text), nameof(TextColor));
        SetAttribute(window, handle, BorderColor, ToColorRef(border), nameof(BorderColor));
    }

    private static Color GetColor(string key, Color fallback) =>
        Application.Current.Resources[key] is SolidColorBrush brush ? brush.Color : fallback;

    private static int ToColorRef(Color color) =>
        color.R | color.G << 8 | color.B << 16;

    private static void SetAttribute(Window window, IntPtr handle, int attribute, int value, string name)
    {
        var result = DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
        if (result != 0)
            Trace.TraceWarning($"Could not apply dark window chrome attribute {name} (HRESULT 0x{result:X8}).");
    }

    private static void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        window.SourceInitialized -= OnSourceInitialized;
        Apply(window);
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
}
