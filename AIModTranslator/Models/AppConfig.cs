namespace AIModTranslator.Models;

public class AppConfig
{
    public string EncryptedOpenAIApiKey { get; set; } = string.Empty;
    public string EncryptedGeminiApiKey { get; set; } = string.Empty;
    public string EncryptedClaudeApiKey { get; set; } = string.Empty;
    public string Provider { get; set; } = "OpenAI";
    public string CustomModel { get; set; } = "llama3";
    public string CustomBaseUrl { get; set; } = "http://localhost:11434/v1/chat/completions";
    public System.Collections.Generic.List<string> CustomRegexes { get; set; } = new();
    public bool IsDarkMode { get; set; } = true;
    public string TargetLanguage { get; set; } = "ru";
    public string TranslationStyle { get; set; } = "Vanilla";
    public string GitHubToken { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public int MaxParallelRequests { get; set; } = 1;
    public bool UseHybridMode { get; set; } = false;
    public string HybridCloudProvider { get; set; } = "OpenAI";
    public int HybridWordThreshold { get; set; } = 30;
    
    // UI Theme Settings
    public string ThemePalette { get; set; } = "Neon Cyberpunk";
    public double UIBackgroundOpacity { get; set; } = 0.8;
    public double UICornerRadius { get; set; } = 12.0;
}
