using AIModTranslator.Data;
using AIModTranslator.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIModTranslator.Services;

public class TranslationMemoryService : ITranslationMemoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TranslationMemoryService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task EnsureCreatedAsync()
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        // Ensure Glossary table exists (Hack since EnsureCreated doesn't migrate)
        await _dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS Glossary (
                OriginalTerm TEXT PRIMARY KEY,
                TranslatedTerm TEXT NOT NULL
            );
        ");
    }

    public async Task<string?> GetTranslationAsync(string originalText)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entry = await _dbContext.TranslationMemory.AsNoTracking().FirstOrDefaultAsync(e => e.OriginalText == originalText);
        return entry?.TranslatedText;
    }

    public async Task SaveTranslationsAsync(IEnumerable<(string Original, string Translated)> translations)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var uniqueTranslations = translations
            .Where(t => !string.IsNullOrWhiteSpace(t.Original) && !string.IsNullOrWhiteSpace(t.Translated))
            .GroupBy(t => t.Original)
            .ToDictionary(g => g.Key, g => g.Last().Translated);

        foreach (var kvp in uniqueTranslations)
        {
            var existing = await _dbContext.TranslationMemory.FirstOrDefaultAsync(e => e.OriginalText == kvp.Key);
            if (existing != null)
            {
                existing.TranslatedText = kvp.Value;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.TranslationMemory.Add(new TmEntry 
                { 
                    OriginalText = kvp.Key, 
                    TranslatedText = kvp.Value 
                });
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task ExportToJsonAsync(string filePath)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        var allEntries = await _dbContext.TranslationMemory.AsNoTracking().ToListAsync();
        var json = System.Text.Json.JsonSerializer.Serialize(allEntries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(filePath, json);
    }

    public async Task ImportFromJsonAsync(string filePath)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        var json = await System.IO.File.ReadAllTextAsync(filePath);
        var entries = System.Text.Json.JsonSerializer.Deserialize<List<TmEntry>>(json);
        if (entries != null && entries.Count > 0)
        {
            var existingKeys = await _dbContext.TranslationMemory.Select(e => e.OriginalText).ToListAsync();
            var newEntries = entries.Where(e => !existingKeys.Contains(e.OriginalText)).ToList();
            if (newEntries.Count > 0)
            {
                await _dbContext.TranslationMemory.AddRangeAsync(newEntries);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await _dbContext.TranslationMemory.CountAsync();
    }

    public async Task<System.Collections.Generic.List<TmEntry>> GetEntriesAsync(string? searchQuery, int limit = 200)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        System.Linq.IQueryable<TmEntry> query = _dbContext.TranslationMemory.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(e => e.OriginalText.Contains(searchQuery) || e.TranslatedText.Contains(searchQuery));
        }
        
        return await query.OrderBy(e => e.OriginalText).Take(limit).ToListAsync();
    }

    public async Task DeleteEntryAsync(string originalText)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        var existing = await _dbContext.TranslationMemory.FirstOrDefaultAsync(e => e.OriginalText == originalText);
        if (existing != null)
        {
            _dbContext.TranslationMemory.Remove(existing);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task ClearAllAsync()
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        _dbContext.TranslationMemory.RemoveRange(_dbContext.TranslationMemory);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<System.Collections.Generic.List<(TmEntry Entry, double Similarity)>> GetFuzzyMatchesAsync(string originalText, double minSimilarity = 0.75, int maxResults = 3)
    {
        if (string.IsNullOrWhiteSpace(originalText)) 
            return new System.Collections.Generic.List<(TmEntry Entry, double Similarity)>();
        
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        var minLength = (int)(originalText.Length * minSimilarity);
        var maxLength = (int)(originalText.Length / minSimilarity) + 1;

        var candidates = await _dbContext.TranslationMemory.AsNoTracking()
            .Where(e => e.OriginalText.Length >= minLength && e.OriginalText.Length <= maxLength)
            .ToListAsync();

        var matches = new System.Collections.Generic.List<(TmEntry Entry, double Similarity)>();

        foreach (var candidate in candidates)
        {
            if (candidate.OriginalText == originalText) continue;

            double similarity = CalculateSimilarity(originalText, candidate.OriginalText);
            if (similarity >= minSimilarity)
            {
                matches.Add((candidate, similarity));
            }
        }

        return matches.OrderByDescending(m => m.Similarity).Take(maxResults).ToList();
    }

    private static double CalculateSimilarity(string source, string target)
    {
        if (source == target) return 1.0;
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;

        int sourceLength = source.Length;
        int targetLength = target.Length;

        int[,] distance = new int[sourceLength + 1, targetLength + 1];

        for (int i = 0; i <= sourceLength; distance[i, 0] = i++) { }
        for (int j = 0; j <= targetLength; distance[0, j] = j++) { }

        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        int maxLen = Math.Max(sourceLength, targetLength);
        return 1.0 - (double)distance[sourceLength, targetLength] / maxLen;
    }
}
