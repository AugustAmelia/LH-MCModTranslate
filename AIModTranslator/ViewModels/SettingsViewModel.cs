using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIModTranslator.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
// MaterialDesignThemes removed for Avalonia

namespace AIModTranslator.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _openAIApiKey = string.Empty;

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    [ObservableProperty]
    private string _claudeApiKey = string.Empty;

    [ObservableProperty]
    private string _provider = "OpenAI";

    [ObservableProperty]
    private string _customModel = "llama3";

    [ObservableProperty]
    private string _customBaseUrl = "http://localhost:11434/v1/chat/completions";

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _customRegexes = new();

    [ObservableProperty]
    private string _newRegex = string.Empty;

    [ObservableProperty]
    private bool _isDarkMode = true;

    [ObservableProperty]
    private string _targetLanguage = "ru";

    [ObservableProperty]
    private string _translationStyle = "Vanilla";

    [ObservableProperty]
    private int _batchSize = 10;

    [ObservableProperty]
    private int _maxRetries = 3;

    [ObservableProperty]
    private int _maxParallelRequests = 1;

    [ObservableProperty]
    private bool _useHybridMode;

    [ObservableProperty]
    private string _hybridCloudProvider = "OpenAI";

    [ObservableProperty]
    private int _hybridWordThreshold = 30;

    [ObservableProperty]
    private string _themePalette = "Neon Cyberpunk";

    [ObservableProperty]
    private double _uiBackgroundOpacity = 0.8;

    [ObservableProperty]
    private double _uiCornerRadius = 12.0;

    public List<string> ThemePalettes { get; } = new()
    {
        "Vanilla",
        "Deep Blue",
        "Emerald",
        "Amber",
        "Neon Cyberpunk",
        "Monochrome",
        "Dracula",
        "Nord",
        "Solarized",
        "Sakura",
        "Cyberpunk Edge",
        "Forest Oasis",
        "Midnight Neon",
        "Royal Crimson"
    };

    public List<string> Providers { get; } = new() { "OpenAI", "Gemini", "Claude", "Ollama", "LM Studio" };

    public List<LanguageOption> AvailableLanguages { get; } = new()
    {
        new("ru", "🇷🇺 Русский"),
        new("es", "🇪🇸 Español"),
        new("fr", "🇫🇷 Français"),
        new("de", "🇩🇪 Deutsch"),
        new("zh", "🇨🇳 中文"),
        new("ja", "🇯🇵 日本語"),
        new("ko", "🇰🇷 한국어"),
        new("pt", "🇧🇷 Português"),
        new("it", "🇮🇹 Italiano"),
        new("pl", "🇵🇱 Polski"),
        new("tr", "🇹🇷 Türkçe"),
        new("nl", "🇳🇱 Nederlands"),
        new("uk", "🇺🇦 Українська"),
        new("cs", "🇨🇿 Čeština"),
        new("ar", "🇸🇦 العربية"),
    };

    public List<string> TranslationStyles { get; } = new()
    {
        "Vanilla",
        "Sci-Fi",
        "Детский / Смешной",
        "Литературный",
        "Формальный"
    };

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var config = _settingsService.LoadConfig();
        OpenAIApiKey = _settingsService.GetDecryptedApiKey();
        GeminiApiKey = _settingsService.GetDecryptedGeminiApiKey();
        ClaudeApiKey = _settingsService.GetDecryptedClaudeApiKey();
        Provider = config.Provider;
        CustomModel = config.CustomModel;
        CustomBaseUrl = config.CustomBaseUrl;
        TargetLanguage = config.TargetLanguage;
        TranslationStyle = config.TranslationStyle;
        BatchSize = config.BatchSize;
        MaxRetries = config.MaxRetries;
        MaxParallelRequests = config.MaxParallelRequests;
        UseHybridMode = config.UseHybridMode;
        HybridCloudProvider = config.HybridCloudProvider;
        HybridWordThreshold = config.HybridWordThreshold;

        CustomRegexes.Clear();
        foreach (var regex in config.CustomRegexes ?? new List<string>())
        {
            CustomRegexes.Add(regex);
        }
        
        IsDarkMode = config.IsDarkMode;
        ThemePalette = config.ThemePalette ?? "Vanilla";
        UiBackgroundOpacity = config.UIBackgroundOpacity;
        UiCornerRadius = config.UICornerRadius;
    }

    private void ApplyCurrentThemeLive()
    {
        if (Avalonia.Application.Current is App app)
        {
            var config = _settingsService.LoadConfig(); // Get base config
            config.IsDarkMode = IsDarkMode;
            config.ThemePalette = ThemePalette;
            config.UIBackgroundOpacity = UiBackgroundOpacity;
            config.UICornerRadius = UiCornerRadius;
            app.ApplyTheme(config);
        }
    }

    partial void OnIsDarkModeChanged(bool value) => ApplyCurrentThemeLive();
    partial void OnThemePaletteChanged(string value) => ApplyCurrentThemeLive();
    partial void OnUiBackgroundOpacityChanged(double value) => ApplyCurrentThemeLive();
    partial void OnUiCornerRadiusChanged(double value) => ApplyCurrentThemeLive();

    partial void OnProviderChanged(string value)
    {
        if (value == "Ollama")
        {
            CustomBaseUrl = "http://localhost:11434/v1/chat/completions";
        }
        else if (value == "LM Studio")
        {
            CustomBaseUrl = "http://localhost:1234/v1/chat/completions";
        }
        else if (value == "Gemini" && string.IsNullOrWhiteSpace(CustomModel))
        {
            CustomModel = "gemini-1.5-flash";
        }
        else if (value == "Claude" && string.IsNullOrWhiteSpace(CustomModel))
        {
            CustomModel = "claude-3-5-sonnet-20240620";
        }
        
        OnPropertyChanged(nameof(IsOpenAISelected));
        OnPropertyChanged(nameof(IsGeminiSelected));
        OnPropertyChanged(nameof(IsClaudeSelected));
        OnPropertyChanged(nameof(IsLocalSelected));
    }

    public bool IsOpenAISelected => Provider == "OpenAI";
    public bool IsGeminiSelected => Provider == "Gemini";
    public bool IsClaudeSelected => Provider == "Claude";
    public bool IsLocalSelected => Provider == "Ollama" || Provider == "LM Studio";

    [RelayCommand]
    private void AddRegex()
    {
        if (!string.IsNullOrWhiteSpace(NewRegex) && !CustomRegexes.Contains(NewRegex))
        {
            CustomRegexes.Add(NewRegex);
            NewRegex = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveRegex(string regex)
    {
        if (CustomRegexes.Contains(regex))
        {
            CustomRegexes.Remove(regex);
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.SetAndEncryptApiKey(OpenAIApiKey);
        _settingsService.SetAndEncryptGeminiApiKey(GeminiApiKey);
        _settingsService.SetAndEncryptClaudeApiKey(ClaudeApiKey);
        
        var config = _settingsService.LoadConfig();
        config.Provider = Provider;
        config.CustomModel = CustomModel;
        config.CustomBaseUrl = CustomBaseUrl;
        config.CustomRegexes = new List<string>(CustomRegexes);
        config.IsDarkMode = IsDarkMode;
        config.TargetLanguage = TargetLanguage;
        config.TranslationStyle = TranslationStyle;
        config.BatchSize = BatchSize;
        config.MaxRetries = Math.Max(1, MaxRetries);
        config.MaxParallelRequests = Math.Clamp(MaxParallelRequests, 1, 5);
        config.UseHybridMode = UseHybridMode;
        config.HybridCloudProvider = HybridCloudProvider;
        config.HybridWordThreshold = HybridWordThreshold;
        
        config.ThemePalette = ThemePalette;
        config.UIBackgroundOpacity = UiBackgroundOpacity;
        config.UICornerRadius = UiCornerRadius;
        
        _settingsService.SaveConfig(config);

        App.Current?.Services?.GetRequiredService<IDialogService>()?.ShowMessageAsync("Успех", "Настройки успешно сохранены.");
    }
}

public record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
