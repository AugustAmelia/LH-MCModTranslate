using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.Services;

public class JsonFileService : IFileService
{
    public string[] SupportedExtensions => new[] { ".json" };

    public async Task<ObservableCollection<TranslationEntry>> LoadFileAsync(string filePath)
    {
        var entries = new ObservableCollection<TranslationEntry>();
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл не найден.", filePath);

        string jsonText = await File.ReadAllTextAsync(filePath);
        
        var jsonOptions = new JsonNodeOptions {};
        var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var jsonNode = JsonNode.Parse(jsonText, jsonOptions, docOptions);

        if (jsonNode is JsonObject jsonObject)
        {
            foreach (var property in jsonObject)
            {
                if (property.Value?.GetValueKind() == JsonValueKind.String)
                {
                    entries.Add(new TranslationEntry
                    {
                        Key = property.Key,
                        OriginalText = property.Value.GetValue<string>(),
                        TranslatedText = string.Empty,
                        Status = "Untranslated"
                    });
                }
            }
        }

        return entries;
    }

    public async Task SaveFileAsync(string filePath, IEnumerable<TranslationEntry> entries)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0) return;

        // Try to load original file to preserve its structure (numbers, arrays, nested objects)
        var originalFile = entryList.First().FilePath;
        string jsonText = File.Exists(originalFile) ? await File.ReadAllTextAsync(originalFile) : "{}";
        
        var jsonOptions = new JsonNodeOptions { };
        var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var jsonNode = JsonNode.Parse(jsonText, jsonOptions, docOptions) ?? new JsonObject();

        if (jsonNode is JsonObject jsonObject)
        {
            foreach (var entry in entryList)
            {
                string value = !string.IsNullOrWhiteSpace(entry.TranslatedText) ? entry.TranslatedText : entry.OriginalText;
                jsonObject[entry.Key] = value;
            }
        }

        var writeOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        };
        
        string updatedJson = jsonNode.ToJsonString(writeOptions);
        
        // Ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
            
        await File.WriteAllTextAsync(filePath, updatedJson);
    }
}
