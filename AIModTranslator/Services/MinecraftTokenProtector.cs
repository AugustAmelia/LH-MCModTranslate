using System.Text;
using System.Text.RegularExpressions;
using AIModTranslator.Helpers;

namespace AIModTranslator.Services;

internal static class MinecraftTokenProtector
{
    private const string TokenPrefix = "__AIMT_TOKEN_";
    private const string TokenSuffix = "__";

    public static ProtectedString Protect(string input, IEnumerable<string>? customRegexes = null)
    {
        input ??= string.Empty;

        var matches = new List<Match>();
        AddMatches(matches, RegexHelpers.MinecraftColorCodes(), input);
        AddMatches(matches, RegexHelpers.Placeholders(), input);

        if (customRegexes != null)
        {
            foreach (var pattern in customRegexes.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    AddMatches(matches, new Regex(pattern, RegexOptions.Compiled), input);
                }
                catch
                {
                    // Invalid user regexes should not block translation.
                }
            }
        }

        var orderedMatches = matches
            .Where(m => m.Success && m.Length > 0)
            .OrderBy(m => m.Index)
            .ThenByDescending(m => m.Length)
            .ToList();

        var tokens = new Dictionary<string, string>();
        var protectedText = new StringBuilder(input.Length);
        var cursor = 0;
        var tokenIndex = 0;

        foreach (var match in orderedMatches)
        {
            if (match.Index < cursor)
            {
                continue;
            }

            protectedText.Append(input, cursor, match.Index - cursor);
            var token = $"{TokenPrefix}{tokenIndex++}{TokenSuffix}";
            tokens[token] = match.Value;
            protectedText.Append(token);
            cursor = match.Index + match.Length;
        }

        protectedText.Append(input, cursor, input.Length - cursor);
        return new ProtectedString(protectedText.ToString(), tokens);
    }

    public static string Restore(string protectedText, IReadOnlyDictionary<string, string> tokens)
    {
        var restored = protectedText ?? string.Empty;
        foreach (var (token, value) in tokens)
        {
            restored = restored.Replace(token, value);
        }

        return restored;
    }

    private static void AddMatches(ICollection<Match> matches, Regex regex, string input)
    {
        foreach (Match match in regex.Matches(input))
        {
            matches.Add(match);
        }
    }
}

internal sealed record ProtectedString(string ProtectedValue, IReadOnlyDictionary<string, string> Tokens);
