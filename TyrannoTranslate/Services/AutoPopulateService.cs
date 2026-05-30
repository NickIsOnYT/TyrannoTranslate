using TyrannoTranslate.Models;

namespace TyrannoTranslate.Services;

public static class AutoPopulateService
{
    public static int CountMatches(IEnumerable<TranslationEntry> entries, string find, bool matchCase)
    {
        if (string.IsNullOrEmpty(find))
            return 0;

        return entries.Count(e => Contains(e.Original, find, matchCase));
    }

    public static int Apply(IEnumerable<TranslationEntry> entries, string find, string replace, bool matchCase)
    {
        if (string.IsNullOrEmpty(find))
            return 0;

        var count = 0;
        foreach (var entry in entries)
        {
            if (!Contains(entry.Original, find, matchCase))
                continue;

            entry.Translation = ReplaceAll(entry.Original, find, replace ?? string.Empty, matchCase);
            count++;
        }

        return count;
    }

    private static bool Contains(string text, string find, bool matchCase) =>
        text.Contains(find, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

    private static string ReplaceAll(string text, string find, string replace, bool matchCase) =>
        text.Replace(find, replace, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
}
