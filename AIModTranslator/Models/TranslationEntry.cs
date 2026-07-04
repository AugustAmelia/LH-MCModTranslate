using CommunityToolkit.Mvvm.ComponentModel;

namespace AIModTranslator.Models;

public partial class TranslationEntry : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _originalText = string.Empty;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private string _aiSuggestion = string.Empty;

    [ObservableProperty]
    private string _status = "Untranslated"; // e.g., Untranslated, Translated, AutoTranslated

    [ObservableProperty]
    private string _comment = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _relativeResourcePath = string.Empty;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasWarnings;

    [ObservableProperty]
    private string _warningMessage = string.Empty;
}
