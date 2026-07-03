using AIModTranslator.Models;
using AIModTranslator.Services;
using AIModTranslator.Services.Interfaces;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class QAServiceTests
{
    [Fact]
    public void Validate_DoesNotFlagEntry_WhenTechnicalTagsArePreserved()
    {
        var service = new QAService(new StubSettingsService(), new StubGlossaryService());
        var entry = new TranslationEntry
        {
            OriginalText = "§cHello %1$s",
            TranslatedText = "§cПривет %1$s"
        };

        service.Validate(entry);

        entry.HasErrors.Should().BeFalse();
        entry.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Validate_FlagsEntry_WhenMinecraftTagOrPlaceholderIsMissing()
    {
        var service = new QAService(new StubSettingsService(), new StubGlossaryService());
        var entry = new TranslationEntry
        {
            OriginalText = "§cHello %1$s",
            TranslatedText = "Привет"
        };

        service.Validate(entry);

        entry.HasErrors.Should().BeTrue();
        entry.ErrorMessage.Should().Contain("§c");
        entry.ErrorMessage.Should().Contain("%1$s");
    }

    private sealed class StubSettingsService : ISettingsService
    {
        public AppConfig LoadConfig() => new();
        public void SaveConfig(AppConfig config) { }
        public string GetDecryptedApiKey() => string.Empty;
        public void SetAndEncryptApiKey(string rawKey) { }
        public string GetDecryptedGeminiApiKey() => string.Empty;
        public void SetAndEncryptGeminiApiKey(string rawKey) { }
        public string GetDecryptedClaudeApiKey() => string.Empty;
        public void SetAndEncryptClaudeApiKey(string rawKey) { }
    }

    private sealed class StubGlossaryService : IGlossaryService
    {
        public Task<List<AIModTranslator.Data.GlossaryEntry>> GetAllTermsAsync() => Task.FromResult(new List<AIModTranslator.Data.GlossaryEntry>());
        public Task SaveTermsAsync(System.Collections.Generic.IEnumerable<AIModTranslator.Data.GlossaryEntry> terms) => Task.CompletedTask;
    }
}
