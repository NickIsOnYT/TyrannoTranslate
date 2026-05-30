using TyrannoTranslate.Models;

namespace TyrannoTranslate.Services;

public sealed class KsFileDocument
{
    public required string[] Lines { get; init; }
    public required IReadOnlyList<TranslationEntry> Entries { get; init; }
}

public static class KsParser
{
    public static KsFileDocument Parse(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var entries = new List<TranslationEntry>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!KsBracketHelper.HasTranslatableContent(line))
                continue;

            var kind = DetectKind(line);
            entries.Add(new TranslationEntry
            {
                LineNumber = entries.Count + 1,
                FileLineIndex = i,
                Original = line,
                Kind = kind,
                ProtectedTags = KsBracketHelper.ExtractTags(line),
                Translation = string.Empty
            });
        }

        return new KsFileDocument { Lines = lines, Entries = entries };
    }

    private static KsLineKind DetectKind(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('#') && trimmed.Length > 1 && !trimmed.Contains('['))
            return KsLineKind.CharacterName;
        return KsLineKind.Translatable;
    }
}
