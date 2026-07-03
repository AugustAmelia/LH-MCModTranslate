using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIModTranslator.Data;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.ViewModels;

public partial class GlossaryViewModel : ObservableObject
{
    private readonly IGlossaryService _glossaryService;

    [ObservableProperty]
    private ObservableCollection<GlossaryEntry> _terms = new();

    public GlossaryViewModel(IGlossaryService glossaryService)
    {
        _glossaryService = glossaryService;
        LoadTermsAsync();
    }

    private async void LoadTermsAsync()
    {
        var terms = await _glossaryService.GetAllTermsAsync();
        Terms.Clear();
        foreach (var t in terms)
        {
            Terms.Add(t);
        }
    }

    [RelayCommand]
    private void AddTerm()
    {
        Terms.Add(new GlossaryEntry { OriginalTerm = "Term", TranslatedTerm = "Термин" });
    }

    [RelayCommand]
    private void RemoveTerm(GlossaryEntry entry)
    {
        if (entry != null && Terms.Contains(entry))
        {
            Terms.Remove(entry);
        }
    }

    [RelayCommand]
    private async Task SaveTermsAsync(Avalonia.Controls.Window window)
    {
        await _glossaryService.SaveTermsAsync(Terms);
        window?.Close();
    }
}
