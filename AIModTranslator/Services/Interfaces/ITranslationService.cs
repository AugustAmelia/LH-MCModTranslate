using AIModTranslator.Models;

namespace AIModTranslator.Services.Interfaces;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage = "ru", string? providerOverride = null);
    Task<string[]> TranslateBatchAsync(string[] texts, string targetLanguage = "ru", string? providerOverride = null);
}
