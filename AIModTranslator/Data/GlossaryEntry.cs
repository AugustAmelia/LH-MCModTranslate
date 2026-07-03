using System.ComponentModel.DataAnnotations;

namespace AIModTranslator.Data;

public class GlossaryEntry
{
    [Key]
    public string OriginalTerm { get; set; } = string.Empty;
    public string TranslatedTerm { get; set; } = string.Empty;
}
