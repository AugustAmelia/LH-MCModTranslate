using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIModTranslator.Services;

internal static partial class AiJsonResponseParser
{
    public static AiJsonParseResult ParseStringArray(string? content, int expectedCount)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return AiJsonParseResult.Failure("AI response is empty.");
        }

        var cleaned = Clean(content);
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(cleaned);
            if (values == null)
            {
                return AiJsonParseResult.Failure("AI response was not a JSON string array.");
            }

            if (values.Length != expectedCount)
            {
                return AiJsonParseResult.Failure($"AI returned {values.Length} items, expected {expectedCount}.");
            }

            return AiJsonParseResult.Success(values, cleaned != content.Trim());
        }
        catch (JsonException ex)
        {
            return AiJsonParseResult.Failure($"AI response JSON parse failed: {ex.Message}");
        }
    }

    private static string Clean(string content)
    {
        var cleaned = content.Trim();

        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = cleaned.IndexOf('\n');
            cleaned = firstNewline >= 0 ? cleaned[(firstNewline + 1)..] : cleaned[3..];
            cleaned = cleaned.Trim();
            if (cleaned.EndsWith("```", StringComparison.Ordinal))
            {
                cleaned = cleaned[..^3].Trim();
            }
        }

        var start = cleaned.IndexOf('[');
        var end = cleaned.LastIndexOf(']');
        if (start >= 0 && end > start)
        {
            cleaned = cleaned.Substring(start, end - start + 1);
        }

        return TrailingCommaBeforeArrayEndRegex().Replace(cleaned, "]");
    }

    [GeneratedRegex(@",\s*\]")]
    private static partial Regex TrailingCommaBeforeArrayEndRegex();
}

internal sealed record AiJsonParseResult(bool IsSuccess, string[] Values, string? ErrorMessage, bool WasAutoFixed)
{
    public static AiJsonParseResult Success(string[] values, bool wasAutoFixed) =>
        new(true, values, null, wasAutoFixed);

    public static AiJsonParseResult Failure(string errorMessage) =>
        new(false, Array.Empty<string>(), errorMessage, false);
}
