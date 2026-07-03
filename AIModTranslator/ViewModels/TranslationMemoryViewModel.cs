using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIModTranslator.Data;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.ViewModels;

public partial class TranslationMemoryViewModel : ObservableObject
{
    private readonly ITranslationMemoryService _tmService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<TmEntry> _entries = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isBusy;

    public TranslationMemoryViewModel(ITranslationMemoryService tmService, IDialogService dialogService)
    {
        _tmService = tmService;
        _dialogService = dialogService;
        
        _ = LoadEntriesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadEntriesAsync();
    }

    [RelayCommand]
    private async Task LoadEntriesAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _tmService.GetTotalCountAsync();
            var results = await _tmService.GetEntriesAsync(SearchText);
            
            Entries.Clear();
            foreach (var entry in results)
            {
                Entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось загрузить базу: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(TmEntry entry)
    {
        if (entry == null) return;
        
        try
        {
            await _tmService.DeleteEntryAsync(entry.OriginalText);
            Entries.Remove(entry);
            TotalCount = await _tmService.GetTotalCountAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось удалить запись: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        var confirm = await _dialogService.ShowConfirmAsync("Очистка базы", "Вы уверены, что хотите полностью очистить базу Translation Memory? Это действие необратимо.");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            await _tmService.ClearAllAsync();
            Entries.Clear();
            TotalCount = 0;
            await _dialogService.ShowMessageAsync("Успех", "База Translation Memory успешно очищена.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось очистить базу: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveChangesAsync(Avalonia.Controls.Window window)
    {
        IsBusy = true;
        try
        {
            // Update modified entries
            var translations = Entries.Select(e => (e.OriginalText, e.TranslatedText));
            await _tmService.SaveTranslationsAsync(translations);
            window?.Close();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось сохранить изменения: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
