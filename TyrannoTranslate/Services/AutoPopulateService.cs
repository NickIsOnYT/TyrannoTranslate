using TyrannoTranslate.Models;

namespace TyrannoTranslate.Services;

public static class AutoPopulateService
{
    public static int CountMatches(IEnumerable<TranslationEntry> entries, string find, bool matchCase, bool exactMatch)
    {
        if (string.IsNullOrEmpty(find))
            return 0;

        return entries.Count(e => Matches(e.Original, find, matchCase, exactMatch));
    }

    public static int Apply(IEnumerable<TranslationEntry> entries, string find, string replace, bool matchCase, bool exactMatch, bool skipFilled)
    {
        if (string.IsNullOrEmpty(find))
            return 0;

        var count = 0;
        foreach (var entry in entries)
        {
            if (skipFilled && entry.IsTranslated)
                continue;

            if (!Matches(entry.Original, find, matchCase, exactMatch))
                continue;

            entry.Translation = exactMatch ? replace : ReplaceAll(entry.Original, find, replace, matchCase);
            count++;
        }

        return count;
    }

    private static bool Matches(string text, string find, bool matchCase, bool exactMatch)
    {
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return exactMatch
            ? text.Equals(find, comparison)
            : text.Contains(find, comparison);
    }

    private static string ReplaceAll(string text, string find, string replace, bool matchCase) =>
        text.Replace(find, replace, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
}
