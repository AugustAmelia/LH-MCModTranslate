using System.Collections.Generic;
using System.Threading.Tasks;
using AIModTranslator.Data;

namespace AIModTranslator.Services.Interfaces;

public interface ITranslationMemoryService
{
    Task EnsureCreatedAsync();
    Task<string?> GetTranslationAsync(string originalText);
    Task SaveTranslationsAsync(IEnumerable<(string Original, string Translated)> translations);
    Task ExportToJsonAsync(string filePath);
    Task ImportFromJsonAsync(string filePath);
    Task<int> GetTotalCountAsync();
    Task<List<TmEntry>> GetEntriesAsync(string? searchQuery, int limit = 200);
    Task DeleteEntryAsync(string originalText);
    Task ClearAllAsync();
    Task<System.Collections.Generic.List<(Data.TmEntry Entry, double Similarity)>> GetFuzzyMatchesAsync(string originalText, double minSimilarity = 0.75, int maxResults = 3);
}
