using AIModTranslator.Data;
using AIModTranslator.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIModTranslator.Services;

public class GlossaryService : IGlossaryService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public GlossaryService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<GlossaryEntry>> GetAllTermsAsync()
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await _dbContext.Glossary.AsNoTracking().ToListAsync();
    }

    public async Task SaveTermsAsync(IEnumerable<GlossaryEntry> terms)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync();
        var existing = await _dbContext.Glossary.ToListAsync();
        _dbContext.Glossary.RemoveRange(existing);
        
        var validTerms = terms.Where(t => !string.IsNullOrWhiteSpace(t.OriginalTerm) && !string.IsNullOrWhiteSpace(t.TranslatedTerm));
        
        // Remove duplicates by Key before adding to avoid PK constraints
        var uniqueTerms = validTerms.GroupBy(t => t.OriginalTerm).Select(g => g.First()).ToList();
        
        await _dbContext.Glossary.AddRangeAsync(uniqueTerms);
        await _dbContext.SaveChangesAsync();
    }
}
