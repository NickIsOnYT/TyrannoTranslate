using System.Text.RegularExpressions;

namespace TyrannoTranslate.Services;

public static partial class KsBracketHelper
{
    [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    public static List<string> ExtractTags(string line) =>
        TagRegex().Matches(line).Select(m => m.Value).ToList();

    public static string StripTags(string line) =>
        TagRegex().Replace(line, string.Empty);

    public static bool IsTagOnlyLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0) return true;
        if (trimmed.StartsWith('*')) return true;
        if (trimmed is "#" or "[s]" or "[cm]") return true;
        if (!trimmed.Contains('[')) return false;

        var withoutTags = StripTags(trimmed).Trim();
        return string.IsNullOrEmpty(withoutTags);
    }

    public static bool HasTranslatableContent(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0) return false;
        if (trimmed.StartsWith('*')) return false;
        if (trimmed is "#") return false;
        if (IsTagOnlyLine(trimmed)) return false;

        if (trimmed.StartsWith('#') && trimmed.Length > 1)
            return true;

        var text = StripTags(trimmed).Trim();
        return text.Length > 0;
    }
}
