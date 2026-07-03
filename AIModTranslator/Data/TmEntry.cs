using System.ComponentModel.DataAnnotations;

namespace AIModTranslator.Data;

public class TmEntry
{
    [Key]
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
