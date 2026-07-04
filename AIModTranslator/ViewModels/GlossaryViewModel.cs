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
    private void ImportVanillaGlossary()
    {
        var vanillaTerms = new Dictionary<string, string>
        {
            {"Cobblestone", "Булыжник"},
            {"Iron Ingot", "Железный слиток"},
            {"Gold Ingot", "Золотой слиток"},
            {"Diamond", "Алмаз"},
            {"Nether", "Незер"},
            {"End", "Энд"},
            {"Overworld", "Верхний мир"},
            {"Crafting Table", "Верстак"},
            {"Furnace", "Печь"},
            {"Chest", "Сундук"},
            {"Dirt", "Земля"},
            {"Oak Log", "Дубовое бревно"},
            {"Oak Planks", "Дубовые доски"},
            {"Stick", "Палка"},
            {"Coal", "Уголь"},
            {"Redstone", "Редстоун"},
            {"Lapis Lazuli", "Лазурит"},
            {"Obsidian", "Обсидиан"},
            {"Flint and Steel", "Огниво"},
            {"Apple", "Яблоко"},
            {"Bread", "Хлеб"},
            {"Sword", "Меч"},
            {"Pickaxe", "Кирка"},
            {"Axe", "Топор"},
            {"Shovel", "Лопата"},
            {"Hoe", "Мотыга"},
            {"Bow", "Лук"},
            {"Arrow", "Стрела"},
            {"Helmet", "Шлем"},
            {"Chestplate", "Нагрудник"},
            {"Leggings", "Поножи"},
            {"Boots", "Ботинки"},
            {"Zombie", "Зомби"},
            {"Skeleton", "Скелет"},
            {"Creeper", "Крипер"},
            {"Spider", "Паук"},
            {"Enderman", "Эндермен"},
            {"Villager", "Крестьянин"},
            {"Emerald", "Изумруд"},
            {"Bed", "Кровать"},
            {"Water Bucket", "Ведро воды"},
            {"Lava Bucket", "Ведро лавы"},
            {"Milk Bucket", "Ведро молока"},
            {"Minecart", "Вагонетка"},
            {"Boat", "Лодка"}
        };

        foreach (var kvp in vanillaTerms)
        {
            if (!Terms.Any(t => t.OriginalTerm?.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) == true))
            {
                Terms.Add(new GlossaryEntry { OriginalTerm = kvp.Key, TranslatedTerm = kvp.Value });
            }
        }
    }

    [RelayCommand]
    private async Task SaveTermsAsync(Avalonia.Controls.Window window)
    {
        await _glossaryService.SaveTermsAsync(Terms);
        window?.Close();
    }
}
