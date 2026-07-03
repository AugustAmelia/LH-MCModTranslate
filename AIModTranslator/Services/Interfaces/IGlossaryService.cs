using AIModTranslator.Data;

namespace AIModTranslator.Services.Interfaces;

public interface IGlossaryService
{
    Task<List<GlossaryEntry>> GetAllTermsAsync();
    Task SaveTermsAsync(IEnumerable<GlossaryEntry> terms);
}
