using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;
using AIModTranslator.Services;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AIModTranslator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IEnumerable<IFileService> _fileServices;
    private readonly ITranslationService _translationService;
    private readonly ITranslationMemoryService _tmService;
    private readonly QAService _qaService;
    private readonly ISettingsService _settingsService;
    private readonly LogService _logService;
    private readonly IDialogService _dialogService;
    private IFileService? _currentFileService;
    private string _currentFilePath = string.Empty;
    private bool _isJarLoaded = false;
    private string _loadedJarTempPath = string.Empty;
    private string _loadedJarOriginalName = string.Empty;
    
    [ObservableProperty]
    private bool _isBusy;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    private string _loadingMessage = string.Empty;

    [ObservableProperty]
    private string _currentFileName = string.Empty;

    [ObservableProperty]
    private string _etaText = string.Empty;

    [ObservableProperty]
    private string _speedText = string.Empty;

    [ObservableProperty]
    private int _translationProgress;

    [ObservableProperty]
    private int _translationTotal;

    [ObservableProperty]
    private bool _showDashboard = true;

    [ObservableProperty]
    private int _tmCount = 0;

    partial void OnTranslationTotalChanged(int value)
    {
        OnPropertyChanged(nameof(LoadingProgressVisibility));
        OnTranslationProgressChanged(TranslationProgress);
    }

    public bool LoadingProgressVisibility => TranslationTotal == 0;
    public bool TranslationProgressVisibility => TranslationTotal > 0;

    public readonly List<TranslationEntry> _allTranslations = new();

    [ObservableProperty]
    private ObservableCollection<TranslationEntry> _translations = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _filterMode = "Все";

    public string[] FilterOptions { get; } = { "Все", "Непереведенные", "С ошибками", "Переведенные" };

    [ObservableProperty]
    private string _statsText = string.Empty;

    [ObservableProperty]
    private bool _isDragOver;

    public bool IsProjectLoaded => _allTranslations.Count > 0;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFilterModeChanged(string value) => ApplyFilter();

    public void ApplyFilter()
    {
        var filtered = _allTranslations.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lowerQuery = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t => 
                (t.Key?.ToLowerInvariant().Contains(lowerQuery) == true) ||
                (t.OriginalText?.ToLowerInvariant().Contains(lowerQuery) == true) ||
                (t.TranslatedText?.ToLowerInvariant().Contains(lowerQuery) == true));
        }

        switch (FilterMode)
        {
            case "Непереведенные":
                filtered = filtered.Where(t => string.IsNullOrWhiteSpace(t.TranslatedText));
                break;
            case "С ошибками":
                filtered = filtered.Where(t => t.HasErrors || t.HasWarnings);
                break;
            case "Переведенные":
                filtered = filtered.Where(t => !string.IsNullOrWhiteSpace(t.TranslatedText));
                break;
        }

        var results = filtered.ToList();
        Translations.Clear();
        foreach (var r in results) Translations.Add(r);

        StatsText = $"Показано: {results.Count} / Всего: {_allTranslations.Count}";
        OnPropertyChanged(nameof(IsProjectLoaded));
    }

    [ObservableProperty]
    private TranslationEntry? _selectedTranslation;

    [ObservableProperty]
    private ObservableCollection<AIModTranslator.Models.FuzzyMatchItem> _suggestedTranslations = new();

    [ObservableProperty]
    private ObservableCollection<AIModTranslator.Data.GlossaryEntry> _currentGlossaryTerms = new();

    partial void OnSelectedTranslationChanged(TranslationEntry? value)
    {
        SuggestedTranslations.Clear();
        CurrentGlossaryTerms.Clear();
        if (value == null || string.IsNullOrWhiteSpace(value.OriginalText)) return;

        _ = LoadFuzzySuggestionsAsync(value.OriginalText);
        _ = FindGlossaryTermsAsync(value.OriginalText);
    }

    private async Task FindGlossaryTermsAsync(string originalText)
    {
        try
        {
            var glossaryService = App.Current?.Services?.GetRequiredService<IGlossaryService>();
            if (glossaryService == null) return;
            
            var allTerms = await glossaryService.GetAllTermsAsync();
            var lowerOriginal = originalText.ToLowerInvariant();
            
            var found = allTerms.Where(t => lowerOriginal.Contains(t.OriginalTerm.ToLowerInvariant())).ToList();
            
            await RunOnUiThreadAsync(() =>
            {
                CurrentGlossaryTerms.Clear();
                foreach (var term in found) CurrentGlossaryTerms.Add(term);
            });
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to find glossary terms: {ex.Message}");
        }
    }

    [RelayCommand]
    private void InsertGlossaryTerm(string translation)
    {
        if (SelectedTranslation != null && !string.IsNullOrWhiteSpace(translation))
        {
            if (string.IsNullOrWhiteSpace(SelectedTranslation.TranslatedText))
                SelectedTranslation.TranslatedText = translation;
            else
                SelectedTranslation.TranslatedText += " " + translation;
                
            _qaService.Validate(SelectedTranslation);
        }
    }

    private async Task LoadFuzzySuggestionsAsync(string originalText)
    {
        try
        {
            var matches = await _tmService.GetFuzzyMatchesAsync(originalText, 0.75, 5);
            await RunOnUiThreadAsync(() =>
            {
                SuggestedTranslations.Clear();
                foreach (var match in matches)
                {
                    SuggestedTranslations.Add(new FuzzyMatchItem
                    {
                        OriginalText = match.Entry.OriginalText,
                        TranslatedText = match.Entry.TranslatedText,
                        Similarity = match.Similarity
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logService.Error($"Fuzzy search failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ApplySuggestion(string translatedText)
    {
        if (SelectedTranslation != null)
        {
            SelectedTranslation.TranslatedText = translatedText;
            _qaService.Validate(SelectedTranslation);
        }
    }

    public LogService LogService => _logService;

    public MainViewModel(IEnumerable<IFileService> fileServices, ITranslationService translationService, ITranslationMemoryService tmService, QAService qaService, ISettingsService settingsService, LogService logService, IDialogService dialogService)
    {
        _fileServices = fileServices;
        _translationService = translationService;
        _tmService = tmService;
        _qaService = qaService;
        _settingsService = settingsService;
        _logService = logService;
        _dialogService = dialogService;

        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        await _tmService.EnsureCreatedAsync();
        TmCount = await _tmService.GetTotalCountAsync();
    }

    private IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }
        return null;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a mod localization file",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                await LoadFilePathAsync(path);
        }
    }

    [RelayCommand]
    private async Task OpenGlossaryAsync()
    {
        var window = App.Current?.Services?.GetRequiredService<Views.GlossaryWindow>();
        if (window != null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            window.DataContext = App.Current?.Services?.GetRequiredService<ViewModels.GlossaryViewModel>();
            await window.ShowDialog(desktop.MainWindow);
        }
    }

    [RelayCommand]
    private async Task ExtractTermsAsync()
    {
        if (_allTranslations.Count == 0)
        {
            await _dialogService.ShowMessageAsync("Внимание", "Сначала загрузите файлы мода.");
            return;
        }

        var sourceTexts = Translations.Select(t => t.OriginalText).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        var translationService = (OpenAITranslationService)_translationService;
        var glossaryService = App.Current?.Services?.GetRequiredService<IGlossaryService>();
        
        if (glossaryService != null)
        {
            var vm = new TermExtractorViewModel(translationService, glossaryService, sourceTexts);
            var window = new Views.TermExtractorWindow { DataContext = vm };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                await window.ShowDialog(desktop.MainWindow);
            }
        }
    }

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        var window = App.Current?.Services?.GetRequiredService<Views.GitHubWindow>();
        if (window != null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            window.DataContext = App.Current?.Services?.GetRequiredService<ViewModels.GitHubViewModel>();
            await window.ShowDialog(desktop.MainWindow);
            var vm = (GitHubViewModel?)window.DataContext;
            
            if (vm != null && !string.IsNullOrEmpty(vm.DownloadedPath))
            {
                await LoadFromDirectoryAsync(vm.DownloadedPath);
                if (Translations.Count > 0) ShowDashboard = false;
            }
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var window = App.Current?.Services?.GetRequiredService<Views.SettingsWindow>();
        if (window != null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            window.DataContext = App.Current?.Services?.GetRequiredService<ViewModels.SettingsViewModel>();
            await window.ShowDialog(desktop.MainWindow);
        }
    }

    [RelayCommand]
    private async Task OpenTMAsync()
    {
        var window = App.Current?.Services?.GetRequiredService<Views.TranslationMemoryWindow>();
        if (window != null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            window.DataContext = App.Current?.Services?.GetRequiredService<ViewModels.TranslationMemoryViewModel>();
            await window.ShowDialog(desktop.MainWindow);
        }
    }

    [RelayCommand]
    private async Task ExportTMAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Translation Memory",
            SuggestedFileName = "TranslationMemory.json",
            DefaultExtension = ".json"
        });

        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                IsBusy = true;
                LoadingMessage = "Экспорт базы переводов...";
                await _tmService.ExportToJsonAsync(path);
                _dialogService.ShowToast("Успех", "База переводов успешно экспортирована!", Avalonia.Controls.Notifications.NotificationType.Success);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", $"Ошибка экспорта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task ImportTMAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Translation Memory",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                IsBusy = true;
                LoadingMessage = "Импорт базы переводов...";
                await _tmService.ImportFromJsonAsync(path);
                _dialogService.ShowToast("Успех", "База переводов успешно импортирована!", Avalonia.Controls.Notifications.NotificationType.Success);
                TmCount = await _tmService.GetTotalCountAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", $"Ошибка импорта: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose an unpacked mod folder",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                await LoadFolderPathAsync(path);
        }
    }

    [RelayCommand]
    private async Task OpenSnbtFolderAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose FTB Quests folder (.snbt)",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                await LoadFolderPathAsync(path);
        }
    }

    [RelayCommand]
    private async Task OpenJarAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a Minecraft mod JAR",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                await LoadJarPathAsync(path);
        }
    }

    [RelayCommand]
    private async Task OpenModpackAsync()
    {
        var sp = GetStorageProvider();
        if (sp == null) return;

        var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a Modpack ZIP or Folder",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                await LoadModpackPathAsync(path);
        }
    }

    private async Task LoadFromDirectoryAsync(string directoryPath)
    {
        IsBusy = true;
        LoadingMessage = "Сканирование файлов локализации...";
        _allTranslations.Clear();
        Translations.Clear();

        try
        {
            var allFiles = System.IO.Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories);
            var langFiles = allFiles.Where(IsMinecraftLangFile).ToList();
            
            LoadingMessage = $"Found {langFiles.Count} localization file(s)...";
            _logService.Info($"Found {langFiles.Count} localization file(s) in {directoryPath}.");

            foreach (var file in langFiles)
            {
                string extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
                var service = _fileServices.FirstOrDefault(s => s.SupportedExtensions.Contains(extension));
                if (service != null)
                {
                    try
                    {
                        var loadedEntries = await service.LoadFileAsync(file);
                        _logService.Info($"Loaded {loadedEntries.Count} strings from {System.IO.Path.GetFileName(file)}.");
                        foreach (var entry in loadedEntries)
                        {
                            entry.FilePath = file;
                            entry.FileName = System.IO.Path.GetFileName(file);
                            
                            var relativePath = System.IO.Path.GetRelativePath(directoryPath, file);
                            entry.RelativeResourcePath = relativePath.Replace('\\', '/');

                            var memMatch = await _tmService.GetTranslationAsync(entry.OriginalText);
                            if (!string.IsNullOrEmpty(memMatch))
                            {
                                entry.TranslatedText = memMatch;
                                entry.Status = "MemoryMatch";
                            }
                            else
                            {
                                var fuzzyMatches = await _tmService.GetFuzzyMatchesAsync(entry.OriginalText, 0.95, 1);
                                if (fuzzyMatches.Count > 0)
                                {
                                    entry.TranslatedText = fuzzyMatches[0].Entry.TranslatedText;
                                    entry.Status = "FuzzyMatch";
                                }
                            }
                            _allTranslations.Add(entry);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Skipped {file}: {ex.Message}");
                        _logService.Warn($"Skipped {file}: {ex.Message}");
                        await _dialogService.ShowMessageAsync("Ошибка парсинга", $"Не удалось прочитать файл:\n{System.IO.Path.GetFileName(file)}\n\n{ex.Message}");
                    }
                }
            }

            ApplyFilter();

            if (_allTranslations.Count == 0)
            {
                _logService.Warn($"No localization files found in {directoryPath}.");
                await _dialogService.ShowMessageAsync("Пусто", "В выбранной папке не найдено файлов локализации.\n\nПрограмма ищет файлы в папках 'lang/' (*.json, *.lang) и файлы квестов (*.snbt).");
            }
            else
            {
                ShowDashboard = false;
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Directory scan failed for {directoryPath}: {ex.Message}");
            await _dialogService.ShowMessageAsync("Ошибка", $"Ошибка сканирования: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsMinecraftLangFile(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath).ToLowerInvariant();
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var fullPath = filePath.Replace('\\', '/').ToLowerInvariant();

        string[] skipFiles = { "fabric.mod.json", "pack.mcmeta", "mods.toml", "pack.toml", "sounds.json" };
        if (skipFiles.Contains(fileName)) return false;
        if (fileName.EndsWith(".mixins.json") || fileName.EndsWith(".accesswidener")) return false;

        if (ext == ".lang") return true;
        if (ext == ".snbt") return true;

        if (ext == ".json")
        {
            return fullPath.Contains("/lang/") || fullPath.Contains("/patchouli_books/");
        }

        return false;
    }

    [RelayCommand]
    private async Task TranslateAllAsync()
    {
        if (Translations.Count == 0)
        {
            const string message = "Load mod files first (File, Folder, JAR, or GitHub).";
            _logService.Warn(message);
            await _dialogService.ShowMessageAsync("No data", message);
            return;
        }

        var untranslated = Translations.Where(t => string.IsNullOrWhiteSpace(t.TranslatedText)).ToList();
        if (untranslated.Count == 0)
        {
            const string message = "All strings are already translated.";
            _logService.Info(message);
            _dialogService.ShowToast("Успех", message, Avalonia.Controls.Notifications.NotificationType.Success);
            return;
        }

        IsBusy = true;
        LoadingMessage = "AI translation in progress...";
        TranslationTotal = untranslated.Count;
        TranslationProgress = 0;
        EtaText = string.Empty;
        SpeedText = string.Empty;
        CurrentFileName = string.Empty;

        var stopwatch = Stopwatch.StartNew();
        var translatedSoFar = 0;

        try
        {
            await _qaService.LoadGlossaryCacheAsync();
            var config = _settingsService.LoadConfig();
            int batchSize = Math.Max(1, config.BatchSize);
            int maxParallelRequests = Math.Clamp(config.MaxParallelRequests, 1, 5);
            var batches = new List<TranslationBatch>();
            
            if (config.UseHybridMode)
            {
                var shortStrings = untranslated.Where(t => t.OriginalText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length <= config.HybridWordThreshold)
                                               .Select((entry, index) => new TranslationWorkItem(entry, index)).ToList();
                var longStrings = untranslated.Where(t => t.OriginalText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > config.HybridWordThreshold)
                                              .Select((entry, index) => new TranslationWorkItem(entry, index)).ToList();
                
                int batchNumber = 1;
                int localBatchSize = (config.Provider == "Ollama" || config.Provider == "LM Studio") ? Math.Min(batchSize, 5) : batchSize;
                foreach (var chunk in shortStrings.Chunk(localBatchSize))
                {
                    batches.Add(new TranslationBatch(batchNumber++, chunk.ToList(), null));
                }
                foreach (var chunk in longStrings.Chunk(batchSize))
                {
                    batches.Add(new TranslationBatch(batchNumber++, chunk.ToList(), config.HybridCloudProvider));
                }
            }
            else
            {
                batches = untranslated
                    .Select((entry, index) => new TranslationWorkItem(entry, index))
                    .Chunk(batchSize)
                    .Select((items, index) => new TranslationBatch(index + 1, items.ToList(), null))
                    .ToList();
            }

            _logService.Info($"Starting translation: {untranslated.Count} strings, {batches.Count} batches, parallel={maxParallelRequests}.");
            if (config.UseHybridMode)
            {
                var shortStringsCount = untranslated.Count(t => t.OriginalText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length <= config.HybridWordThreshold);
                var longStringsCount = untranslated.Count - shortStringsCount;
                _logService.Info($"Hybrid Savings: {shortStringsCount} items routed to {config.Provider} (Local/Free), {longStringsCount} items to {config.HybridCloudProvider} (Cloud/Paid).");
            }

            using var semaphore = new SemaphoreSlim(maxParallelRequests);
            var tasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync();
                var batchStopwatch = Stopwatch.StartNew();
                try
                {
                    await RunOnUiThreadAsync(() =>
                    {
                        CurrentFileName = batch.Items.FirstOrDefault()?.Entry.FileName ?? string.Empty;
                        LoadingMessage = $"Translating batch {batch.Number}/{batches.Count}...";
                    });

                    _logService.Info($"Batch {batch.Number}/{batches.Count} started ({batch.Items.Count} strings).");
                    var textsToTranslate = batch.Items.Select(i => i.Entry.OriginalText).ToArray();
                    var translatedTexts = await _translationService.TranslateBatchAsync(textsToTranslate, config.TargetLanguage, batch.ProviderOverride);

                    await RunOnUiThreadAsync(() =>
                    {
                        string usedProvider = batch.ProviderOverride ?? config.Provider;
                        for (int j = 0; j < batch.Items.Count; j++)
                        {
                            batch.Items[j].Entry.TranslatedText = translatedTexts[j];
                            batch.Items[j].Entry.Status = $"AutoTranslated ({usedProvider})";
                            _qaService.Validate(batch.Items[j].Entry);
                        }

                        translatedSoFar += batch.Items.Count;
                        TranslationProgress = translatedSoFar;
                        UpdateProgressStats(translatedSoFar, untranslated.Count, stopwatch.Elapsed);
                    });

                    _logService.Info($"Batch {batch.Number}/{batches.Count} completed in {batchStopwatch.Elapsed.TotalSeconds:F1}s.");
                }
                catch (Exception ex)
                {
                    _logService.Error($"Batch {batch.Number}/{batches.Count} failed: {ex.Message}");
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            _logService.Info($"Translation finished: {untranslated.Count} strings in {stopwatch.Elapsed.TotalSeconds:F1}s.");
        }
        catch (Exception ex)
        {
            _logService.Error($"Translation failed: {ex.Message}");
            await _dialogService.ShowMessageAsync("AI error", $"Translation error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            TranslationProgress = 0;
            TranslationTotal = 0;
            CurrentFileName = string.Empty;
            EtaText = string.Empty;
            SpeedText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RunQAAsync()
    {
        await _qaService.LoadGlossaryCacheAsync();
        foreach (var entry in _allTranslations)
        {
            _qaService.Validate(entry);
        }

        var errorCount = _allTranslations.Count(t => t.HasErrors);
        _logService.Info($"QA completed. Errors: {errorCount}.");
    }

    [RelayCommand]
    private async Task AutoFixGlossaryAsync(TranslationEntry entry)
    {
        if (entry == null) return;
        
        await _qaService.LoadGlossaryCacheAsync();
        _qaService.TryAutoFixGlossary(entry);
        // Validate is already called inside TryAutoFixGlossary if it fixed anything,
        // but let's call it anyway to ensure UI updates.
        _qaService.Validate(entry);
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (_allTranslations.Count == 0)
        {
            _logService.Warn("No translations to save.");
            return;
        }

        await RunQAAsync();
        if (_allTranslations.Any(t => t.HasErrors))
        {
            var result = await _dialogService.ShowConfirmAsync("Предупреждение", "Найдены ошибки QA (отсутствуют технические теги в переводах). Строки с ошибками подсвечены красным.\n\nВы уверены, что хотите сохранить файл с ошибками?");
            if (!result)
            {
                return;
            }
        }

        IsBusy = true;
        LoadingMessage = "Сохранение файлов...";

        try
        {
            var config = _settingsService.LoadConfig();
            string targetLang = config.TargetLanguage;
            string localeCode = GetMinecraftLocaleCode(targetLang);

            bool createResourcePack = false;
            if (_isJarLoaded)
            {
                createResourcePack = await _dialogService.ShowConfirmAsync("Выбор формата", "Создать Resource Pack (.zip) с переводами?\n\n'Да' - Сгенерировать Ресурспак (Безопасно, не ломает исходные файлы).\n'Нет' - Пересобрать исходный архив целиком.");
            }

            var groupedByFile = _allTranslations.Where(t => !string.IsNullOrEmpty(t.FilePath)).GroupBy(t => t.FilePath);

            if (createResourcePack)
            {
                var sp = GetStorageProvider();
                if (sp != null)
                {
                    var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Сохранить Resource Pack",
                        SuggestedFileName = $"{_loadedJarOriginalName}_{localeCode}_ResourcePack.zip",
                        DefaultExtension = ".zip"
                    });

                    if (file != null)
                    {
                        var path = file.TryGetLocalPath();
                        if (!string.IsNullOrEmpty(path))
                        {
                            LoadingMessage = "Генерация Ресурспака...";
                            
                            var rpTempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AIModTranslator_RP", Guid.NewGuid().ToString());
                            System.IO.Directory.CreateDirectory(rpTempDir);
                            System.IO.File.WriteAllText(System.IO.Path.Combine(rpTempDir, "pack.mcmeta"), "{\n  \"pack\": {\n    \"pack_format\": 15,\n    \"description\": \"AI Translated Resource Pack\"\n  }\n}");

                            foreach (var group in groupedByFile)
                            {
                                string extension = System.IO.Path.GetExtension(group.Key).ToLowerInvariant();
                                var service = _fileServices.FirstOrDefault(s => s.SupportedExtensions.Contains(extension));
                                if (service != null)
                                {
                                    var firstEntry = group.First();
                                    string rpRelativePath = string.Empty;
                                    
                                    if (!string.IsNullOrEmpty(firstEntry.RelativeResourcePath))
                                    {
                                        int assetsIndex = firstEntry.RelativeResourcePath.Replace('\\', '/').IndexOf("assets/");
                                        if (assetsIndex >= 0)
                                        {
                                            rpRelativePath = firstEntry.RelativeResourcePath.Substring(assetsIndex);
                                        }
                                    }
                                    
                                    if (!string.IsNullOrEmpty(rpRelativePath))
                                    {
                                        string dir = System.IO.Path.GetDirectoryName(rpRelativePath) ?? "";
                                        string ext = System.IO.Path.GetExtension(rpRelativePath);
                                        string translatedRpPath = System.IO.Path.Combine(dir, localeCode + ext);
                                        
                                        string fullDestPath = System.IO.Path.Combine(rpTempDir, translatedRpPath);
                                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullDestPath)!);
                                        
                                        await service.SaveFileAsync(fullDestPath, group);
                                    }
                                    else
                                    {
                                        // Fallback in-place for files not in assets
                                        string newFilePath = GetTranslatedFilePath(group.Key, localeCode);
                                        await service.SaveFileAsync(newFilePath, group);
                                    }
                                }
                            }
                            
                            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                            await Task.Run(() => System.IO.Compression.ZipFile.CreateFromDirectory(rpTempDir, path, System.IO.Compression.CompressionLevel.Optimal, false));
                            
                            _dialogService.ShowToast("Успех", $"Ресурспак успешно сохранён:\n{path}", Avalonia.Controls.Notifications.NotificationType.Success);
                        }
                    }
                }
            }
            else
            {
                foreach (var group in groupedByFile)
                {
                    string extension = System.IO.Path.GetExtension(group.Key).ToLowerInvariant();
                    var service = _fileServices.FirstOrDefault(s => s.SupportedExtensions.Contains(extension));
                    if (service != null)
                    {
                        string newFilePath = GetTranslatedFilePath(group.Key, localeCode);
                        await service.SaveFileAsync(newFilePath, group);
                    }
                }

                if (_isJarLoaded)
                {
                    var sp = GetStorageProvider();
                    if (sp != null)
                    {
                        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
                        {
                            Title = "Сохранить переведённый JAR/ZIP",
                            SuggestedFileName = $"{_loadedJarOriginalName}_{localeCode}.zip",
                            DefaultExtension = ".zip"
                        });

                        if (file != null)
                        {
                            var path = file.TryGetLocalPath();
                            if (!string.IsNullOrEmpty(path))
                            {
                                LoadingMessage = "Упаковка архива...";
                                if (System.IO.File.Exists(path))
                                    System.IO.File.Delete(path);
                                
                                await Task.Run(() => System.IO.Compression.ZipFile.CreateFromDirectory(
                                    _loadedJarTempPath, path, System.IO.Compression.CompressionLevel.Optimal, false));
                                
                                _dialogService.ShowToast("Успех", $"Архив успешно сохранён:\n{path}", Avalonia.Controls.Notifications.NotificationType.Success);
                            }
                        }
                    }
                }
                else
                {
                    _dialogService.ShowToast("Успех", $"Файлы перевода ({localeCode}) успешно сохранены рядом с оригиналами!", Avalonia.Controls.Notifications.NotificationType.Success);
                }
            }

            var toSaveToTm = _allTranslations
                .Where(t => !string.IsNullOrWhiteSpace(t.OriginalText) && !string.IsNullOrWhiteSpace(t.TranslatedText))
                .Select(t => (t.OriginalText, t.TranslatedText));
            
            await _tmService.SaveTranslationsAsync(toSaveToTm);
            TmCount = await _tmService.GetTotalCountAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", $"Ошибка при сохранении: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetTranslatedFilePath(string originalPath, string localeCode)
    {
        string dir = System.IO.Path.GetDirectoryName(originalPath) ?? "";
        string ext = System.IO.Path.GetExtension(originalPath);
        string fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath).ToLowerInvariant();

        string[] knownLocales = { "en_us", "en_gb", "en_au", "en_nz", "en_ca" };
        
        if (knownLocales.Contains(fileName))
        {
            return System.IO.Path.Combine(dir, localeCode + ext);
        }

        return System.IO.Path.Combine(dir, localeCode + ext);
    }

    private static string GetMinecraftLocaleCode(string langCode)
    {
        return langCode.ToLowerInvariant() switch
        {
            "ru" => "ru_ru",
            "es" => "es_es",
            "fr" => "fr_fr",
            "de" => "de_de",
            "zh" => "zh_cn",
            "ja" => "ja_jp",
            "ko" => "ko_kr",
            "pt" => "pt_br",
            "it" => "it_it",
            "pl" => "pl_pl",
            "tr" => "tr_tr",
            "nl" => "nl_nl",
            "uk" => "uk_ua",
            "cs" => "cs_cz",
            "ar" => "ar_sa",
            _ => $"{langCode}_{langCode}"
        };
    }
}
