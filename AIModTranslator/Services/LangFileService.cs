using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;
using UtfUnknown;

namespace AIModTranslator.Services;

public class LangFileService : IFileService
{
    public string[] SupportedExtensions => new[] { ".lang", ".properties" };

    static LangFileService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<ObservableCollection<TranslationEntry>> LoadFileAsync(string filePath)
    {
        var entries = new ObservableCollection<TranslationEntry>();

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл не найден.", filePath);

        var bytes = await File.ReadAllBytesAsync(filePath);
        var detected = CharsetDetector.DetectFromBytes(bytes);
        var encoding = detected.Detected?.Encoding ?? Encoding.UTF8;
        var text = encoding.GetString(bytes);
        string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            // Skip comments and empty lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                continue;

            int separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex > 0)
            {
                string key = trimmed.Substring(0, separatorIndex).Trim();
                string value = trimmed.Substring(separatorIndex + 1).Trim();

                entries.Add(new TranslationEntry
                {
                    Key = key,
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
        var outputLines = new List<string>();

        foreach (var entry in entries)
        {
            string value = !string.IsNullOrWhiteSpace(entry.TranslatedText) ? entry.TranslatedText : entry.OriginalText;
            outputLines.Add($"{entry.Key}={value}");
        }

        // Ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllLinesAsync(filePath, outputLines, Encoding.UTF8);
    }
}
