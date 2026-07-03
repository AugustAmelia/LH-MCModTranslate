using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIModTranslator.Models;
using AIModTranslator.Helpers;
using AIModTranslator.Services.Interfaces;
using AIModTranslator.Data;
using System.Text.RegularExpressions;

namespace AIModTranslator.Services;

public class QAService
{
    private readonly ISettingsService _settingsService;
    private readonly IGlossaryService _glossaryService;
    private List<GlossaryEntry> _glossaryCache = new();

    public QAService(ISettingsService settingsService, IGlossaryService glossaryService)
    {
        _settingsService = settingsService;
        _glossaryService = glossaryService;
    }

    public async Task LoadGlossaryCacheAsync()
    {
        _glossaryCache = await _glossaryService.GetAllTermsAsync();
    }

    public void Validate(TranslationEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.TranslatedText))
        {
            entry.HasErrors = false;
            entry.ErrorMessage = string.Empty;
            entry.HasWarnings = false;
            entry.WarningMessage = string.Empty;
            return;
        }

        string original = entry.OriginalText ?? "";
        string translated = entry.TranslatedText ?? "";
        var config = _settingsService.LoadConfig();

        var errors = new List<string>();
        var warnings = new List<string>();

        // 1. Gather all tags from original
        var origTags = new List<string>();
        origTags.AddRange(RegexHelpers.MinecraftColorCodes().Matches(original).Select(m => m.Value));
        origTags.AddRange(RegexHelpers.Placeholders().Matches(original).Select(m => m.Value));

        if (config.CustomRegexes != null)
        {
            foreach (var pattern in config.CustomRegexes)
            {
                try { origTags.AddRange(Regex.Matches(original, pattern).Select(m => m.Value)); } catch { }
            }
        }

        // 2. Gather all tags from translated
        var transTags = new List<string>();
        transTags.AddRange(RegexHelpers.MinecraftColorCodes().Matches(translated).Select(m => m.Value));
        transTags.AddRange(RegexHelpers.Placeholders().Matches(translated).Select(m => m.Value));

        if (config.CustomRegexes != null)
        {
            foreach (var pattern in config.CustomRegexes)
            {
                try { transTags.AddRange(Regex.Matches(translated, pattern).Select(m => m.Value)); } catch { }
            }
        }

        // 3. Compare Tags
        foreach (var t in origTags)
        {
            if (transTags.Contains(t))
            {
                transTags.Remove(t);
            }
            else
            {
                errors.Add($"Потерян тег: {t}");
            }
        }

        // 4. Glossary Check (Case-Insensitive)
        foreach (var term in _glossaryCache)
        {
            if (string.IsNullOrWhiteSpace(term.OriginalTerm) || string.IsNullOrWhiteSpace(term.TranslatedTerm))
                continue;

            if (original.Contains(term.OriginalTerm, StringComparison.OrdinalIgnoreCase))
            {
                if (!translated.Contains(term.TranslatedTerm, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Термин глоссария \"{term.OriginalTerm}\" должен быть переведен как \"{term.TranslatedTerm}\"");
                }
            }
        }

        // 5. Length Check (Minecraft UI Limit Warning)
        if (translated.Length > 30 && translated.Length > original.Length * 1.5)
        {
            warnings.Add($"Превышена длина! Перевод ({translated.Length} симв.) значительно длиннее оригинала ({original.Length} симв.). Возможен выход текста за рамки интерфейса игры.");
        }

        if (errors.Count > 0)
        {
            entry.HasErrors = true;
            entry.ErrorMessage = string.Join("\n", errors);
        }
        else
        {
            entry.HasErrors = false;
            entry.ErrorMessage = string.Empty;
        }

        if (warnings.Count > 0)
        {
            entry.HasWarnings = true;
            entry.WarningMessage = string.Join("\n", warnings);
        }
        else
        {
            entry.HasWarnings = false;
            entry.WarningMessage = string.Empty;
        }
    }
}
