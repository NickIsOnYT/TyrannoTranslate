using System.ComponentModel;
using System.Runtime.CompilerServices;
using TyrannoTranslate.Services;

namespace TyrannoTranslate.Models;

public enum KsLineKind
{
    Translatable,
    CharacterName,
    ContextOnly
}

public sealed class TranslationEntry : INotifyPropertyChanged
{
    private string _translation = string.Empty;
    private string? _validationMessage;

    public int LineNumber { get; init; }
    public int FileLineIndex { get; init; }
    public string Original { get; init; } = string.Empty;
    public KsLineKind Kind { get; init; }
    public IReadOnlyList<string> ProtectedTags { get; init; } = Array.Empty<string>();

    public string Translation
    {
        get => _translation;
        set
        {
            if (_translation == value) return;
            _translation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTranslated));
            OnPropertyChanged(nameof(Status));
            ValidateBrackets();
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value) return;
            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(Status));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ValidationMessage);
    public bool IsTranslated => !string.IsNullOrWhiteSpace(Translation);
    public string Status => HasError ? "!" : IsTranslated ? "✓" : "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ClearValidation() => ValidationMessage = null;

    public void ValidateBrackets()
    {
        if (string.IsNullOrWhiteSpace(Translation))
        {
            ValidationMessage = null;
            return;
        }

        var origTags = KsBracketHelper.ExtractTags(Original);
        var transTags = KsBracketHelper.ExtractTags(Translation);
        if (!origTags.SequenceEqual(transTags))
        {
            ValidationMessage = "Bracket tags must match the original (order and content).";
            return;
        }

        ValidationMessage = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
