using System;
using System.IO;
using System.Text.Json;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace AIModTranslator.Services;

public class SettingsService : ISettingsService
{
    private readonly string _configFilePath;
    private readonly IDataProtector _protector;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "AIModTranslator");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        _configFilePath = Path.Combine(appFolder, "config.json");

        // Setup cross-platform Data Protection
        var keysFolder = Path.Combine(appFolder, "Keys");
        if (!Directory.Exists(keysFolder))
        {
            Directory.CreateDirectory(keysFolder);
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
            .SetApplicationName("AIModTranslator");

        var services = serviceCollection.BuildServiceProvider();
        _protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("AIModTranslator.ApiKey");
    }

    public AppConfig LoadConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configFilePath, json);
    }

    public string GetDecryptedApiKey()
    {
        var config = LoadConfig();
        if (string.IsNullOrEmpty(config.EncryptedOpenAIApiKey))
        {
            return string.Empty;
        }

        try
        {
            return _protector.Unprotect(config.EncryptedOpenAIApiKey);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetAndEncryptApiKey(string rawKey)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            config.EncryptedOpenAIApiKey = string.Empty;
        }
        else
        {
            config.EncryptedOpenAIApiKey = _protector.Protect(rawKey);
        }
        SaveConfig(config);
    }

    public string GetDecryptedGeminiApiKey()
    {
        var config = LoadConfig();
        if (string.IsNullOrEmpty(config.EncryptedGeminiApiKey))
        {
            return string.Empty;
        }
        try { return _protector.Unprotect(config.EncryptedGeminiApiKey); }
        catch { return string.Empty; }
    }

    public void SetAndEncryptGeminiApiKey(string rawKey)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(rawKey))
            config.EncryptedGeminiApiKey = string.Empty;
        else
            config.EncryptedGeminiApiKey = _protector.Protect(rawKey);
        SaveConfig(config);
    }

    public string GetDecryptedClaudeApiKey()
    {
        var config = LoadConfig();
        if (string.IsNullOrEmpty(config.EncryptedClaudeApiKey))
        {
            return string.Empty;
        }
        try { return _protector.Unprotect(config.EncryptedClaudeApiKey); }
        catch { return string.Empty; }
    }

    public void SetAndEncryptClaudeApiKey(string rawKey)
    {
        var config = LoadConfig();
        if (string.IsNullOrWhiteSpace(rawKey))
            config.EncryptedClaudeApiKey = string.Empty;
        else
            config.EncryptedClaudeApiKey = _protector.Protect(rawKey);
        SaveConfig(config);
    }
}
