using TyrannoTranslate.Models;

namespace TyrannoTranslate.Services;

public static class KsWriter
{
    public static string BuildContent(string[] lines, IEnumerable<TranslationEntry> entries, bool ignoreValidation = false)
    {
        var output = (string[])lines.Clone();
        var errors = new List<string>();

        foreach (var entry in entries)
        {
            if (!ignoreValidation && entry.HasError)
            {
                errors.Add($"Line {entry.FileLineIndex + 1}: {entry.ValidationMessage}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Translation))
                continue;

            output[entry.FileLineIndex] = entry.Translation;
        }

        if (!ignoreValidation && errors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        return string.Join(Environment.NewLine, output);
    }
}
