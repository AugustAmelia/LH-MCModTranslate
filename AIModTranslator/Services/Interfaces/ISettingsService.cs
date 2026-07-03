using AIModTranslator.Models;

namespace AIModTranslator.Services.Interfaces;

public interface ISettingsService
{
    AppConfig LoadConfig();
    void SaveConfig(AppConfig config);
    string GetDecryptedApiKey();
    void SetAndEncryptApiKey(string rawKey);
    string GetDecryptedGeminiApiKey();
    void SetAndEncryptGeminiApiKey(string rawKey);
    string GetDecryptedClaudeApiKey();
    void SetAndEncryptClaudeApiKey(string rawKey);
}
