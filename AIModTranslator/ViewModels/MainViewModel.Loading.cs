using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIModTranslator.Models;
using Avalonia.Threading;

namespace AIModTranslator.ViewModels;

public partial class MainViewModel
{
    public async Task LoadDroppedFileAsync(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            _logService.Warn($"Dropped file does not exist: {path}");
            return;
        }

        await LoadFilePathAsync(path);
    }

    public async Task LoadDroppedFolderAsync(string path)
    {
        if (!System.IO.Directory.Exists(path))
        {
            _logService.Warn($"Dropped folder does not exist: {path}");
            return;
        }

        await LoadFolderPathAsync(path);
    }

    public async Task LoadDroppedJarAsync(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            _logService.Warn($"Dropped JAR does not exist: {path}");
            return;
        }

        await LoadJarPathAsync(path);
    }

    private async Task LoadFilePathAsync(string filePath)
    {
        _currentFilePath = filePath;
        var extension = System.IO.Path.GetExtension(_currentFilePath).ToLowerInvariant();
        _currentFileService = _fileServices.FirstOrDefault(s => s.SupportedExtensions.Contains(extension));

        if (_currentFileService == null)
        {
            var message = $"Unsupported file format: {extension}";
            _logService.Warn(message);
            await _dialogService.ShowMessageAsync("Unsupported file", message);
            return;
        }

        try
        {
            IsBusy = true;
            LoadingMessage = "Loading file...";
            CurrentFileName = System.IO.Path.GetFileName(_currentFilePath);
            _allTranslations.Clear();
            Translations.Clear();

            var loadedEntries = await _currentFileService.LoadFileAsync(_currentFilePath);
            await AddEntriesAsync(loadedEntries, _currentFilePath);
            _logService.Info($"Loaded {loadedEntries.Count} strings from {CurrentFileName}.");
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to load file {filePath}: {ex.Message}");
            await _dialogService.ShowMessageAsync("Load error", $"Failed to load file: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            CurrentFileName = string.Empty;
            ApplyFilter();
            if (_allTranslations.Count > 0) ShowDashboard = false;
        }
    }

    private async Task LoadFolderPathAsync(string folderPath)
    {
        _currentFilePath = folderPath;
        _isJarLoaded = false;
        _logService.Info($"Loading folder: {folderPath}");
        await LoadFromDirectoryAsync(folderPath);
    }

    private async Task LoadJarPathAsync(string jarPath)
    {
        _currentFilePath = jarPath;
        _loadedJarOriginalName = System.IO.Path.GetFileNameWithoutExtension(_currentFilePath);
        _isJarLoaded = true;
        _loadedJarTempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AIModTranslator", Guid.NewGuid().ToString());

        try
        {
            IsBusy = true;
            LoadingMessage = "Extracting mod...";
            CurrentFileName = System.IO.Path.GetFileName(jarPath);
            _logService.Info($"Extracting JAR: {jarPath}");
            await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(jarPath, _loadedJarTempPath, overwriteFiles: true));

            await LoadFromDirectoryAsync(_loadedJarTempPath);
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to extract JAR {jarPath}: {ex.Message}");
            await _dialogService.ShowMessageAsync("JAR error", $"Failed to extract JAR: {ex.Message}");
            IsBusy = false;
        }
        finally
        {
            CurrentFileName = string.Empty;
        }
    }

    private async Task LoadModpackPathAsync(string path)
    {
        _currentFilePath = path;
        _loadedJarOriginalName = System.IO.Path.GetFileNameWithoutExtension(path) ?? "Modpack";
        _isJarLoaded = true; 
        _loadedJarTempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AIModTranslator", Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(_loadedJarTempPath);

        try
        {
            IsBusy = true;
            LoadingMessage = "Processing modpack...";
            _allTranslations.Clear();
            Translations.Clear();
            
            string sourceFolder = path;
            
            if (System.IO.File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                LoadingMessage = "Extracting ZIP archive...";
                _logService.Info($"Extracting Modpack ZIP: {path}");
                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(path, _loadedJarTempPath, overwriteFiles: true));
                sourceFolder = _loadedJarTempPath;
            }

            LoadingMessage = "Scanning for JAR files...";
            var jarFiles = System.IO.Directory.GetFiles(sourceFolder, "*.jar", System.IO.SearchOption.AllDirectories);
            _logService.Info($"Found {jarFiles.Length} JAR file(s) in modpack.");

            foreach (var jar in jarFiles)
            {
                string jarName = System.IO.Path.GetFileNameWithoutExtension(jar);
                string extractPath = System.IO.Path.Combine(_loadedJarTempPath, "jars", jarName);
                
                try
                {
                    await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(jar, extractPath, overwriteFiles: true));
                }
                catch (Exception ex)
                {
                    _logService.Warn($"Skipped JAR {jar}: {ex.Message}");
                }
            }

            await LoadFromDirectoryAsync(_loadedJarTempPath);
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to process modpack {path}: {ex.Message}");
            await _dialogService.ShowMessageAsync("Error", $"Failed to process modpack: {ex.Message}");
            IsBusy = false;
        }
        finally
        {
            CurrentFileName = string.Empty;
        }
    }

    private async Task AddEntriesAsync(IEnumerable<TranslationEntry> entries, string filePath)
    {
        foreach (var entry in entries)
        {
            entry.FilePath = filePath;
            entry.FileName = System.IO.Path.GetFileName(filePath);

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
        await RunOnUiThreadAsync(() => ApplyFilter());
    }

    private static async Task RunOnUiThreadAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(action);
    }

    private void UpdateProgressStats(int translatedSoFar, int total, TimeSpan elapsed)
    {
        if (translatedSoFar <= 0 || elapsed.TotalSeconds <= 0)
        {
            SpeedText = string.Empty;
            EtaText = string.Empty;
            return;
        }

        var speedPerMinute = translatedSoFar / Math.Max(elapsed.TotalMinutes, 0.001);
        SpeedText = $"{speedPerMinute:F1} strings/min";

        var remaining = Math.Max(0, total - translatedSoFar);
        if (remaining == 0)
        {
            EtaText = "ETA: done";
            return;
        }

        var eta = TimeSpan.FromMinutes(remaining / Math.Max(speedPerMinute, 0.001));
        EtaText = eta.TotalHours >= 1
            ? $"ETA: {eta:h\\:mm\\:ss}"
            : $"ETA: {eta:mm\\:ss}";
    }

    private sealed record TranslationWorkItem(TranslationEntry Entry, int OriginalIndex);
    private sealed record TranslationBatch(int Number, List<TranslationWorkItem> Items, string? ProviderOverride = null);
}
