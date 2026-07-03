namespace AIModTranslator.Models;

public class FuzzyMatchItem
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public string FormattedSimilarity => $"{(int)(Similarity * 100)}%";
}
