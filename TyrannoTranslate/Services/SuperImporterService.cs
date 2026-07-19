using System.Xml.Linq;
using TyrannoTranslate.Models;

namespace TyrannoTranslate.Services;

public sealed class TranslationMemoryEntry
{
    public string Original { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
}

public static class SuperImporterService
{
    public static void Export(IEnumerable<TranslationEntry> entries, string filePath)
    {
        var doc = new XDocument(
            new XElement("TranslationMemory",
                entries
                    .Where(e => e.IsTranslated)
                    .Select(e => new XElement("Entry",
                        new XAttribute("Original", e.Original),
                        new XAttribute("Translation", e.Translation)
                    ))
            )
        );

        doc.Save(filePath, SaveOptions.DisableFormatting);
    }

    public static IReadOnlyList<TranslationMemoryEntry> Import(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root;

        if (root == null || root.Name != "TranslationMemory")
            throw new System.IO.InvalidDataException("Invalid translation memory file.");

        return root.Elements("Entry")
            .Select(el => new TranslationMemoryEntry
            {
                Original = el.Attribute("Original")?.Value ?? string.Empty,
                Translation = el.Attribute("Translation")?.Value ?? string.Empty
            })
            .Where(e => !string.IsNullOrEmpty(e.Original))
            .ToList();
    }

    public static int Apply(IEnumerable<TranslationEntry> entries, IReadOnlyList<TranslationMemoryEntry> memory, bool skipFilled)
    {
        var count = 0;

        foreach (var entry in entries)
        {
            if (skipFilled && entry.IsTranslated)
                continue;

            var match = memory.FirstOrDefault(m =>
                string.Equals(entry.Original, m.Original, StringComparison.Ordinal));

            if (match == null)
                continue;

            entry.Translation = match.Translation;
            count++;
        }

        return count;
    }
}
