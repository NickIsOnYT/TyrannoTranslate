using System.Text.RegularExpressions;

namespace TyrannoTranslate.Services;

public static partial class TranslationTagHelper
{
    [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    /// <summary>
    /// Splits text into segments alternating between plain text and tags.
    /// Tags are returned separately so they can be re-inserted after translation.
    /// </summary>
    public static List<TextSegment> SplitText(string text)
    {
        var segments = new List<TextSegment>();
        var start = 0;

        foreach (Match m in TagRegex().Matches(text))
        {
            if (m.Index > start)
                segments.Add(new TextSegment(text[start..m.Index], isTag: false));
            segments.Add(new TextSegment(m.Value, isTag: true));
            start = m.Index + m.Length;
        }

        if (start < text.Length)
            segments.Add(new TextSegment(text[start..], isTag: false));

        return segments;
    }

    /// <summary>
    /// Applies translations to the text segments, keeping tags unchanged.
    /// </summary>
    public static string Rejoin(List<TextSegment> segments, IReadOnlyList<string> translations)
    {
        var ti = 0;
        var sb = new System.Text.StringBuilder();
        foreach (var seg in segments)
        {
            sb.Append(seg.IsTag ? seg.Text : translations[ti++]);
        }
        return sb.ToString();
    }
}

public sealed class TextSegment
{
    public string Text { get; }
    public bool IsTag { get; }

    public TextSegment(string text, bool isTag)
    {
        Text = text;
        IsTag = isTag;
    }
}
