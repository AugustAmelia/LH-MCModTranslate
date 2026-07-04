using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
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

        bool isPatchouli = filePath.Replace('\\', '/').Contains("/patchouli_books/");
        string jsonText = await File.ReadAllTextAsync(filePath);
        
        var jsonOptions = new JsonNodeOptions {};
        var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var jsonNode = JsonNode.Parse(jsonText, jsonOptions, docOptions);

        if (jsonNode != null)
        {
            ExtractStringsRecursive(jsonNode, string.Empty, entries, isPatchouli);
        }

        return entries;
    }

    private void ExtractStringsRecursive(JsonNode node, string currentPath, ObservableCollection<TranslationEntry> entries, bool isPatchouli)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                string newPath = string.IsNullOrEmpty(currentPath) ? prop.Key : $"{currentPath}.{prop.Key}";
                
                if (prop.Value is JsonValue val && val.TryGetValue(out string? strVal))
                {
                    bool shouldExtract = !isPatchouli || IsPatchouliTranslatableKey(prop.Key);
                    
                    if (shouldExtract && !string.IsNullOrWhiteSpace(strVal))
                    {
                        entries.Add(new TranslationEntry
                        {
                            Key = newPath,
                            OriginalText = strVal,
                            TranslatedText = string.Empty,
                            Status = "Untranslated"
                        });
                    }
                }
                else if (prop.Value != null)
                {
                    ExtractStringsRecursive(prop.Value, newPath, entries, isPatchouli);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                string newPath = $"{currentPath}[{i}]";
                if (arr[i] is JsonValue val && val.TryGetValue(out string? strVal))
                {
                    // Patchouli arrays rarely contain plain strings we want to translate directly, 
                    // but if they do, we extract them if it's not restricted by Patchouli rules.
                    if (!isPatchouli && !string.IsNullOrWhiteSpace(strVal))
                    {
                        entries.Add(new TranslationEntry
                        {
                            Key = newPath,
                            OriginalText = strVal,
                            TranslatedText = string.Empty,
                            Status = "Untranslated"
                        });
                    }
                }
                else if (arr[i] != null)
                {
                    ExtractStringsRecursive(arr[i]!, newPath, entries, isPatchouli);
                }
            }
        }
    }

    private bool IsPatchouliTranslatableKey(string key)
    {
        string k = key.ToLowerInvariant();
        return k == "name" || k == "title" || k == "text" || k == "description" || k.EndsWith(".name") || k.EndsWith(".text");
    }

    public async Task SaveFileAsync(string filePath, IEnumerable<TranslationEntry> entries)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0) return;

        var originalFile = entryList.First().FilePath;
        string jsonText = !string.IsNullOrEmpty(originalFile) && File.Exists(originalFile) ? await File.ReadAllTextAsync(originalFile) : "{}";
        
        var jsonOptions = new JsonNodeOptions { };
        var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var jsonNode = JsonNode.Parse(jsonText, jsonOptions, docOptions) ?? new JsonObject();

        var translationDict = entryList.ToDictionary(e => e.Key, e => !string.IsNullOrWhiteSpace(e.TranslatedText) ? e.TranslatedText : e.OriginalText);

        var appliedKeys = new HashSet<string>();
        ApplyTranslationsRecursive(jsonNode, string.Empty, translationDict, appliedKeys);

        // Fallback for missing keys (e.g. if the original file didn't exist or it's a flat json)
        if (jsonNode is JsonObject rootObj)
        {
            foreach (var kvp in translationDict)
            {
                if (!appliedKeys.Contains(kvp.Key))
                {
                    rootObj[kvp.Key] = kvp.Value;
                }
            }
        }

        var writeOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
        };
        
        string updatedJson = jsonNode.ToJsonString(writeOptions);
        
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
            
        await File.WriteAllTextAsync(filePath, updatedJson);
    }

    private void ApplyTranslationsRecursive(JsonNode node, string currentPath, Dictionary<string, string> dict, HashSet<string> appliedKeys)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj.ToList())
            {
                string newPath = string.IsNullOrEmpty(currentPath) ? prop.Key : $"{currentPath}.{prop.Key}";
                
                if (prop.Value is JsonValue val && val.TryGetValue(out string? _))
                {
                    if (dict.TryGetValue(newPath, out string? translatedText))
                    {
                        obj[prop.Key] = translatedText;
                        appliedKeys.Add(newPath);
                    }
                }
                else if (prop.Value != null)
                {
                    ApplyTranslationsRecursive(prop.Value, newPath, dict, appliedKeys);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                string newPath = $"{currentPath}[{i}]";
                if (arr[i] is JsonValue val && val.TryGetValue(out string? _))
                {
                    if (dict.TryGetValue(newPath, out string? translatedText))
                    {
                        arr[i] = translatedText;
                        appliedKeys.Add(newPath);
                    }
                }
                else if (arr[i] != null)
                {
                    ApplyTranslationsRecursive(arr[i]!, newPath, dict, appliedKeys);
                }
            }
        }
    }
}
