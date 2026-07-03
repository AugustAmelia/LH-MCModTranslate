using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIModTranslator.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AIModTranslator.ViewModels;

public partial class GitHubViewModel : ObservableObject
{
    private readonly IGitHubService _gitHubService;

    [ObservableProperty]
    private string _repoUrl = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Вставьте ссылку на GitHub репозиторий мода.";

    /// <summary>
    /// The path to downloaded files, set after successful download.
    /// </summary>
    public string? DownloadedPath { get; private set; }

    public GitHubViewModel(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(RepoUrl))
        {
            StatusMessage = "Пожалуйста, введите URL репозитория.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Подключение к GitHub...";

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            DownloadedPath = await _gitHubService.DownloadLangFilesAsync(RepoUrl, progress);
            var dialogService = (IDialogService?)((App)Avalonia.Application.Current!).Services?.GetService(typeof(IDialogService));
            if (dialogService != null) await dialogService.ShowMessageAsync("Успех", "Файлы успешно скачаны!\nТеперь они будут загружены в программу.");
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
}
