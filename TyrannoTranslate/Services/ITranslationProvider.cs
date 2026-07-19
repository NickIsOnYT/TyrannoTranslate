namespace TyrannoTranslate.Services;

public interface ITranslationProvider
{
    string Name { get; }
    string Description { get; }
    bool IsConfigured { get; }
    Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct = default);
}
