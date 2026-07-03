using System.Net.Http;
using System.Text.Json;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.Services;

public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("AIModTranslator/1.0");
    }

    public async Task<string> DownloadLangFilesAsync(string repoUrl, IProgress<string>? progress = null)
    {
        // Parse owner/repo from URL like https://github.com/owner/repo
        var uri = new Uri(repoUrl.TrimEnd('/'));
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
            throw new Exception("Неверный формат URL. Ожидается: https://github.com/owner/repo");

        string owner = segments[0];
        string repo = segments[1];

        progress?.Report($"Поиск файлов в {owner}/{repo}...");

        // Use GitHub API to search for lang files recursively
        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/git/trees/main?recursive=1";
        
        string treeJson;
        try
        {
            treeJson = await _httpClient.GetStringAsync(apiUrl);
        }
        catch
        {
            // Try 'master' branch if 'main' fails
            apiUrl = $"https://api.github.com/repos/{owner}/{repo}/git/trees/master?recursive=1";
            treeJson = await _httpClient.GetStringAsync(apiUrl);
        }

        using var doc = JsonDocument.Parse(treeJson);
        var tree = doc.RootElement.GetProperty("tree");

        var langFiles = new List<string>();
        foreach (var item in tree.EnumerateArray())
        {
            var path = item.GetProperty("path").GetString() ?? "";
            var type = item.GetProperty("type").GetString() ?? "";
            
            if (type == "blob" && IsLangFile(path))
            {
                langFiles.Add(path);
            }
        }

        if (langFiles.Count == 0)
            throw new Exception("Не найдено файлов локализации (.json, .lang) в репозитории.");

        progress?.Report($"Найдено {langFiles.Count} файлов. Скачивание...");

        // Download files to temp folder
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AIModTranslator", $"github_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        int downloaded = 0;
        foreach (var filePath in langFiles)
        {
            var rawUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/main/{filePath}";
            
            string content;
            try
            {
                content = await _httpClient.GetStringAsync(rawUrl);
            }
            catch
            {
                rawUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/master/{filePath}";
                content = await _httpClient.GetStringAsync(rawUrl);
            }

            // Preserve directory structure
            var localPath = System.IO.Path.Combine(tempDir, filePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            var localDir = System.IO.Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(localDir))
                System.IO.Directory.CreateDirectory(localDir);

            await System.IO.File.WriteAllTextAsync(localPath, content);
            downloaded++;
            progress?.Report($"Скачано {downloaded}/{langFiles.Count}: {System.IO.Path.GetFileName(filePath)}");
        }

        progress?.Report($"Готово! Скачано {downloaded} файлов.");
        return tempDir;
    }

    private static bool IsLangFile(string path)
    {
        // Match common Minecraft mod lang file patterns
        var fileName = System.IO.Path.GetFileName(path).ToLowerInvariant();
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

        if (ext == ".lang") return true;

        // Match en_us.json or similar locale JSON files in lang directories
        if (ext == ".json" && path.Contains("lang", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
