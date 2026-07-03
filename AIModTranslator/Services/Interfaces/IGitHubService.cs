namespace AIModTranslator.Services.Interfaces;

public interface IGitHubService
{
    /// <summary>
    /// Downloads language files from a GitHub repository to a temp folder.
    /// Returns the path to the temp folder containing downloaded files.
    /// </summary>
    Task<string> DownloadLangFilesAsync(string repoUrl, IProgress<string>? progress = null);
}
