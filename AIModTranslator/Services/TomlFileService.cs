using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.Services;

public partial class TomlFileService : IFileService
{
    public string[] SupportedExtensions => new[] { ".toml" };

    // Matches TOML sections, e.g., [general.ui]
    [GeneratedRegex(@"^\s*\[(.*?)\]\s*$")]
    private static partial Regex TomlSectionRegex();

    // Matches TOML key-value pairs with quotes, e.g., key = "value" or key = 'value'
    [GeneratedRegex(@"^\s*([\w\.-]+)\s*=\s*([""'])(.*?)\2(.*)$")]
    private static partial Regex TomlKeyValueRegex();

    public async Task<ObservableCollection<TranslationEntry>> LoadFileAsync(string filePath)
    {
        var entries = new ObservableCollection<TranslationEntry>();

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл не найден.", filePath);

        string[] lines = await File.ReadAllLinesAsync(filePath);
        string currentSection = string.Empty;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            var sectionMatch = TomlSectionRegex().Match(line);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups[1].Value.Trim();
                continue;
            }

            var kvMatch = TomlKeyValueRegex().Match(line);
            if (kvMatch.Success)
            {
                string key = kvMatch.Groups[1].Value.Trim();
                string value = kvMatch.Groups[3].Value; // The string inside quotes

                // Build composite key if in section
                string fullKey = string.IsNullOrEmpty(currentSection) ? key : $"{currentSection}.{key}";

                entries.Add(new TranslationEntry
                {
                    Key = fullKey,
                    OriginalText = value,
                    TranslatedText = string.Empty,
                    Status = "Untranslated"
                });
            }
        }

        return entries;
    }

    public async Task SaveFileAsync(string filePath, IEnumerable<TranslationEntry> entries)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0) return;

        var originalFile = entryList.First().FilePath;
        if (!File.Exists(originalFile)) return; // We need the original to preserve structure

        string[] lines = await File.ReadAllLinesAsync(originalFile);
        var outputLines = new List<string>();
        string currentSection = string.Empty;

        var dict = entryList.ToDictionary(e => e.Key, e => !string.IsNullOrWhiteSpace(e.TranslatedText) ? e.TranslatedText : e.OriginalText);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                outputLines.Add(line);
                continue;
            }

            var sectionMatch = TomlSectionRegex().Match(line);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups[1].Value.Trim();
                outputLines.Add(line);
                continue;
            }

            var kvMatch = TomlKeyValueRegex().Match(line);
            if (kvMatch.Success)
            {
                string key = kvMatch.Groups[1].Value.Trim();
                string fullKey = string.IsNullOrEmpty(currentSection) ? key : $"{currentSection}.{key}";

                if (dict.TryGetValue(fullKey, out string? translatedValue))
                {
                    // Reconstruct line preserving exact spacing, quotes, and trailing comments
                    string before = line.Substring(0, kvMatch.Groups[3].Index);
                    string after = line.Substring(kvMatch.Groups[3].Index + kvMatch.Groups[3].Length);
                    
                    // TOML strings usually don't need intense escaping unless it's multiline or has quotes inside quotes,
                    // but we will do a basic quote escape to be safe.
                    string escapedValue = translatedValue.Replace("\"", "\\\"");
                    outputLines.Add($"{before}{escapedValue}{after}");
                    continue;
                }
            }

            // Not translated or doesn't match our regex, keep as is
            outputLines.Add(line);
        }

        // Ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllLinesAsync(filePath, outputLines);
    }
}
