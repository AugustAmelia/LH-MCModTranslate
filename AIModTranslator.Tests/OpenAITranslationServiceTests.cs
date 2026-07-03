using System.Net;
using System.Text;
using AIModTranslator.Data;
using AIModTranslator.Models;
using AIModTranslator.Services;
using AIModTranslator.Services.Interfaces;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class OpenAITranslationServiceTests
{
    [Fact]
    public async Task TranslateBatchAsync_ReturnsOriginalsAfterInvalidResponses()
    {
        var handler = new QueueHttpMessageHandler(
            ChatResponse("""not json"""),
            ChatResponse("""["too few"]"""));
        var service = CreateService(handler, new AppConfig { Provider = "Ollama", MaxRetries = 2 });

        var result = await service.TranslateBatchAsync(new[] { "§cHello %1$s", "World" });

        result.Should().Equal("§cHello %1$s", "World");
        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task TranslateBatchAsync_RetriesWrongLengthAndUsesValidResponse()
    {
        var handler = new QueueHttpMessageHandler(
            ChatResponse("""["too few"]"""),
            ChatResponse("""["§cПривет %1$s","Мир"]"""));
        var service = CreateService(handler, new AppConfig { Provider = "Ollama", MaxRetries = 2 });

        var result = await service.TranslateBatchAsync(new[] { "§cHello %1$s", "World" });

        result.Should().Equal("§cПривет %1$s", "Мир");
        handler.RequestCount.Should().Be(2);
    }

    private static OpenAITranslationService CreateService(QueueHttpMessageHandler handler, AppConfig config)
    {
        return new OpenAITranslationService(
            new HttpClient(handler),
            new StubGlossaryService(),
            new StubSettingsService(config),
            new LogService());
    }

    private static string ChatResponse(string content)
    {
        return $$"""
            {
              "choices": [
                {
                  "message": {
                    "content": {{System.Text.Json.JsonSerializer.Serialize(content)}}
                  }
                }
              ]
            }
            """;
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;
        public int RequestCount { get; private set; }

        public QueueHttpMessageHandler(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var content = _responses.Count > 0 ? _responses.Dequeue() : ChatResponse("""[]""");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class StubSettingsService(AppConfig config) : ISettingsService
    {
        public AppConfig LoadConfig() => config;
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
        public Task<List<GlossaryEntry>> GetAllTermsAsync() => Task.FromResult(new List<GlossaryEntry>());
        public Task SaveTermsAsync(IEnumerable<GlossaryEntry> terms) => Task.CompletedTask;
    }
}
