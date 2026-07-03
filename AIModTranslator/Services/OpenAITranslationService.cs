using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.Services;

public class OpenAITranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IGlossaryService _glossaryService;
    private readonly ISettingsService _settingsService;
    private readonly LogService _logService;

    public OpenAITranslationService(HttpClient httpClient, IGlossaryService glossaryService, ISettingsService settingsService, LogService logService)
    {
        _httpClient = httpClient;
        _glossaryService = glossaryService;
        _settingsService = settingsService;
        _logService = logService;
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage = "ru", string? providerOverride = null)
    {
        var result = await TranslateBatchAsync(new[] { text }, targetLanguage, providerOverride);
        return result.FirstOrDefault() ?? string.Empty;
    }

    public async Task<string[]> TranslateBatchAsync(string[] texts, string targetLanguage = "ru", string? providerOverride = null)
    {
        if (texts == null || texts.Length == 0) return Array.Empty<string>();

        const int maxChunkSize = 50; // Prevent context size exceeded errors
        var results = new List<string>();

        for (int i = 0; i < texts.Length; i += maxChunkSize)
        {
            var chunk = texts.Skip(i).Take(maxChunkSize).ToArray();
            var chunkResult = await TranslateChunkAsync(chunk, targetLanguage, providerOverride);
            results.AddRange(chunkResult);
        }

        return results.ToArray();
    }

    private async Task<string[]> TranslateChunkAsync(string[] texts, string targetLanguage, string? providerOverride)
    {
        var config = _settingsService.LoadConfig();
        var protectedData = texts
            .Select(text => MinecraftTokenProtector.Protect(text, config.CustomRegexes))
            .ToList();

        var stringsToTranslate = protectedData.Select(p => p.ProtectedValue).ToArray();
        var jsonPayload = JsonSerializer.Serialize(stringsToTranslate);
        var systemPrompt = await BuildSystemPromptAsync(texts, targetLanguage, texts.Length, config.TranslationStyle);

        var (providerName, apiUrl, targetModel, apiKey) = ResolveProvider(config, providerOverride);
        var maxRetries = Math.Max(1, config.MaxRetries);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (attempt > 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt - 1));
            }

            try
            {
                var contentString = await SendAiRequestAsync(providerName, apiUrl, apiKey, targetModel, systemPrompt, jsonPayload);
                var parseResult = AiJsonResponseParser.ParseStringArray(contentString, texts.Length);

                if (!parseResult.IsSuccess)
                {
                    _logService.Warn($"AI response parse failed on attempt {attempt}/{maxRetries}: {parseResult.ErrorMessage}");
                    continue;
                }

                if (parseResult.WasAutoFixed)
                {
                    _logService.Warn($"AI response required safe JSON cleanup on attempt {attempt}/{maxRetries}.");
                }

                var finalResults = new string[texts.Length];
                for (var i = 0; i < texts.Length; i++)
                {
                    finalResults[i] = MinecraftTokenProtector.Restore(parseResult.Values[i], protectedData[i].Tokens);
                }

                return finalResults;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logService.Warn($"AI request failed on attempt {attempt}/{maxRetries}: {ex.Message}");
            }
        }

        _logService.Error($"AI translation failed after {maxRetries} attempts. Falling back to original strings for {texts.Length} item(s).");
        return protectedData
            .Select(p => MinecraftTokenProtector.Restore(p.ProtectedValue, p.Tokens))
            .ToArray();
    }

    public async Task<string> SendRawPromptAsync(string systemPrompt, string userPrompt)
    {
        var config = _settingsService.LoadConfig();
        var (providerName, apiUrl, targetModel, apiKey) = ResolveProvider(config, null);
        return await SendAiRequestAsync(providerName, apiUrl, apiKey, targetModel, systemPrompt, userPrompt);
    }

    private async Task<string> BuildSystemPromptAsync(string[] originalTexts, string targetLanguage, int itemCount, string style)
    {
        var systemPrompt = $@"You are a professional Minecraft mod translator. Target language: {targetLanguage}.
Preserve game slang. Do NOT translate technical terms, item IDs, or internal names.
I will send you a JSON array of strings to translate.
Strings may contain special tokens like __AIMT_TOKEN_0__, __AIMT_TOKEN_1__ - these are protected formatting tags. Keep them in the same relative position.
Return ONLY a valid JSON array of translated strings in the SAME order and SAME count ({itemCount} items). No extra text, no markdown, no explanations.";

        var styleInstructions = GetStyleInstructions(style);
        if (!string.IsNullOrEmpty(styleInstructions))
        {
            systemPrompt += $"\n\nTranslation style:\n{styleInstructions}";
        }

        var glossaryTerms = await _glossaryService.GetAllTermsAsync();
        
        // Filter glossary to only include terms present in the current batch to avoid context limits
        var relevantTerms = glossaryTerms
            .Where(t => originalTexts.Any(text => text.Contains(t.OriginalTerm, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (relevantTerms.Any())
        {
            systemPrompt += "\n\nStrict Glossary Rules (You MUST follow these translations EXACTLY for the specified terms):\n" +
                            string.Join("\n", relevantTerms.Select(t => $"- {t.OriginalTerm} -> {t.TranslatedTerm}"));
        }

        return systemPrompt;
    }

    private (string Provider, string ApiUrl, string TargetModel, string ApiKey) ResolveProvider(AppConfig config, string? providerOverride)
    {
        string providerToUse = providerOverride ?? config.Provider;

        if (providerToUse == "OpenAI")
        {
            var apiKey = _settingsService.GetDecryptedApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("API ключ OpenAI не настроен.");
            return ("OpenAI", "https://api.openai.com/v1/chat/completions", "gpt-4o-mini", apiKey);
        }
        else if (providerToUse == "Gemini")
        {
            var apiKey = _settingsService.GetDecryptedGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("API ключ Gemini не настроен.");
            var model = string.IsNullOrWhiteSpace(config.CustomModel) ? "gemini-1.5-flash" : config.CustomModel;
            return ("Gemini", $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}", model, apiKey);
        }
        else if (providerToUse == "Claude")
        {
            var apiKey = _settingsService.GetDecryptedClaudeApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("API ключ Claude не настроен.");
            var model = string.IsNullOrWhiteSpace(config.CustomModel) ? "claude-3-5-sonnet-20240620" : config.CustomModel;
            return ("Claude", "https://api.anthropic.com/v1/messages", model, apiKey);
        }

        return (providerToUse, config.CustomBaseUrl, config.CustomModel, "local-dummy-key");
    }

    private async Task<string> SendAiRequestAsync(string provider, string apiUrl, string apiKey, string targetModel, string systemPrompt, string userPrompt)
    {
        if (provider == "Gemini")
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{userPrompt}" } } }
                },
                generationConfig = new { temperature = 0.3 }
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Content = JsonContent.Create(requestBody);
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
        else if (provider == "Claude")
        {
            var requestBody = new
            {
                model = targetModel,
                max_tokens = 4096,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userPrompt } },
                temperature = 0.3
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(requestBody);
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        }
        else
        {
            var requestBody = new
            {
                model = targetModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
    }

    private static string GetStyleInstructions(string style)
    {
        return style switch
        {
            "Vanilla" => "Используй стиль классического Minecraft - фэнтези, средневековье. Названия должны звучать как в сказке или легенде. Используй устоявшиеся термины из официального перевода Minecraft.",
            "Sci-Fi" => "Используй строгий научно-технический стиль. Термины должны звучать инженерно и точно. Например, 'Flux Infused Pickaxe' -> 'Кирка с инфузией флюкса', а не 'Волшебная кирка'. Избегай фэнтезийных формулировок.",
            "Детский / Смешной" => "Переводи весело и забавно. Используй шутливые формулировки, уменьшительные суффиксы и юмор. Например, 'Diamond Sword' -> 'Алмазный тыкалкин'.",
            "Литературный" => "Используй красивый, художественный стиль. Описания должны звучать атмосферно и поэтично, как в хорошей книге.",
            "Формальный" => "Сухой, точный, документальный стиль. Никаких украшений. Только факты и термины.",
            _ => ""
        };
    }
}
