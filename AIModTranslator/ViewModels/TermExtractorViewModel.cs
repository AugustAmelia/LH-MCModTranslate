using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIModTranslator.Data;
using AIModTranslator.Services;
using AIModTranslator.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AIModTranslator.ViewModels;

public partial class TermExtractorViewModel : ObservableObject
{
    private readonly OpenAITranslationService _translationService;
    private readonly IGlossaryService _glossaryService;
    private readonly string[] _sourceTexts;

    [ObservableProperty]
    private ObservableCollection<ExtractedTerm> _extractedTerms = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Нажмите \"Анализ\" для запуска.";

    public TermExtractorViewModel(OpenAITranslationService translationService, IGlossaryService glossaryService, string[] sourceTexts)
    {
        _translationService = translationService;
        _glossaryService = glossaryService;
        _sourceTexts = sourceTexts;
    }

    [RelayCommand]
    private async Task ExtractAsync()
    {
        if (_sourceTexts.Length == 0) return;

        IsBusy = true;
        StatusMessage = "ИИ анализирует термины...";
        ExtractedTerms.Clear();

        try
        {
            // Take a representative sample (max 50 strings to avoid token overflow)
            var sample = _sourceTexts.Take(50).ToArray();
            var jsonPayload = JsonSerializer.Serialize(sample);

            var systemPrompt = @"You are a Minecraft mod terminology expert. Analyze the following JSON array of English mod localization strings.
Extract ALL unique game-specific terms (item names, block names, mob names, mechanic names, material names).
For each term, provide a suggested Russian translation.
Return ONLY a valid JSON array of objects with keys ""original"" and ""translation"". Example:
[{""original"": ""Flux Capacitor"", ""translation"": ""Флуксовый конденсатор""}, ...]
No extra text, only the JSON array.";

            var response = await _translationService.SendRawPromptAsync(systemPrompt, jsonPayload);

            // Clean response
            response = response.Trim();
            if (response.StartsWith("```json")) response = response.Substring(7);
            if (response.StartsWith("```")) response = response.Substring(3);
            if (response.EndsWith("```")) response = response.Substring(0, response.Length - 3);

            var terms = JsonSerializer.Deserialize<List<TermDto>>(response.Trim(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (terms != null)
            {
                foreach (var t in terms.Where(t => !string.IsNullOrWhiteSpace(t.Original) && !string.IsNullOrWhiteSpace(t.Translation)))
                {
                    ExtractedTerms.Add(new ExtractedTerm
                    {
                        Original = t.Original,
                        Translation = t.Translation,
                        IsSelected = true
                    });
                }
            }

            StatusMessage = $"Найдено {ExtractedTerms.Count} терминов.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddToGlossaryAsync()
    {
        var selectedTerms = ExtractedTerms.Where(t => t.IsSelected).ToList();
        var dialogService = (AIModTranslator.Services.Interfaces.IDialogService?)((App)Avalonia.Application.Current!).Services?.GetService(typeof(AIModTranslator.Services.Interfaces.IDialogService));

        if (selectedTerms.Count == 0)
        {
            if (dialogService != null) await dialogService.ShowMessageAsync("Внимание", "Выберите хотя бы один термин.");
            return;
        }

        var existingTerms = await _glossaryService.GetAllTermsAsync();
        var newTerms = selectedTerms
            .Where(st => !existingTerms.Any(e => e.OriginalTerm.Equals(st.Original, StringComparison.OrdinalIgnoreCase)))
            .Select(st => new GlossaryEntry { OriginalTerm = st.Original, TranslatedTerm = st.Translation })
            .ToList();

        var allTerms = existingTerms.Concat(newTerms).ToList();
        await _glossaryService.SaveTermsAsync(allTerms);
        if (dialogService != null) await dialogService.ShowMessageAsync("Успех", $"Добавлено {newTerms.Count} новых терминов в Глоссарий!");
    }

    private class TermDto
    {
        public string Original { get; set; } = "";
        public string Translation { get; set; } = "";
    }
}

public partial class ExtractedTerm : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _original = string.Empty;

    [ObservableProperty]
    private string _translation = string.Empty;
}
