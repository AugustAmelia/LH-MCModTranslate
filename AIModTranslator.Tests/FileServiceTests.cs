using System.Text.Json.Nodes;
using System.Text;
using AIModTranslator.Services;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class FileServiceTests
{
    [Fact]
    public async Task JsonFileService_LoadsOnlyStringPropertiesAndSavesTranslatedValues()
    {
        var tempDir = CreateTempDirectory();
        var inputPath = Path.Combine(tempDir, "en_us.json");
        var outputPath = Path.Combine(tempDir, "ru_ru.json");
        await File.WriteAllTextAsync(inputPath, """
            {
              "item.example": "Example Item",
              "numeric": 42,
              "nested": { "value": "Skipped" }
            }
            """);

        var service = new JsonFileService();
        var entries = await service.LoadFileAsync(inputPath);
        entries[0].TranslatedText = "Тестовый предмет";

        await service.SaveFileAsync(outputPath, entries);

        entries.Should().ContainSingle();
        entries[0].Key.Should().Be("item.example");
        entries[0].OriginalText.Should().Be("Example Item");

        var savedJson = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
        savedJson["item.example"]!.GetValue<string>().Should().Be("Тестовый предмет");
        savedJson.ContainsKey("numeric").Should().BeFalse();
        savedJson.ContainsKey("nested").Should().BeFalse();
    }

    [Fact]
    public async Task LangFileService_LoadsKeyValuesIgnoresCommentsAndSavesOriginalFallback()
    {
        var tempDir = CreateTempDirectory();
        var inputPath = Path.Combine(tempDir, "en_us.lang");
        var outputPath = Path.Combine(tempDir, "ru_ru.lang");
        await File.WriteAllTextAsync(inputPath, """
            # comment
            // another comment
            item.example=Example Item
            item.fallback=Fallback Item

            """);

        var service = new LangFileService();
        var entries = await service.LoadFileAsync(inputPath);
        entries.Single(e => e.Key == "item.example").TranslatedText = "Тестовый предмет";

        await service.SaveFileAsync(outputPath, entries);

        entries.Should().HaveCount(2);
        var savedLines = await File.ReadAllLinesAsync(outputPath);
        savedLines.Should().Contain("item.example=Тестовый предмет");
        savedLines.Should().Contain("item.fallback=Fallback Item");
        savedLines.Should().NotContain(line => line.StartsWith('#') || line.StartsWith("//"));
    }

    [Fact]
    public async Task LangFileService_LoadsLatin1EncodedFile()
    {
        var tempDir = CreateTempDirectory();
        var inputPath = Path.Combine(tempDir, "latin1.lang");
        await File.WriteAllBytesAsync(inputPath, Encoding.Latin1.GetBytes("item.cafe=Café"));

        var service = new LangFileService();
        var entries = await service.LoadFileAsync(inputPath);

        entries.Should().ContainSingle();
        entries[0].OriginalText.Should().Be("Café");
    }

    [Fact]
    public async Task LangFileService_LoadsWindows1251EncodedFile()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var tempDir = CreateTempDirectory();
        var inputPath = Path.Combine(tempDir, "cp1251.lang");
        var windows1251 = Encoding.GetEncoding(1251);
        await File.WriteAllBytesAsync(inputPath, windows1251.GetBytes("item.test=Привет"));

        var service = new LangFileService();
        var entries = await service.LoadFileAsync(inputPath);

        entries.Should().ContainSingle();
        entries[0].OriginalText.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LangFileService_SavesUtf8()
    {
        var tempDir = CreateTempDirectory();
        var outputPath = Path.Combine(tempDir, "ru_ru.lang");
        var service = new LangFileService();
        var entries = new[]
        {
            new AIModTranslator.Models.TranslationEntry
            {
                Key = "item.test",
                OriginalText = "Original",
                TranslatedText = "Привет"
            }
        };

        await service.SaveFileAsync(outputPath, entries);

        var bytes = await File.ReadAllBytesAsync(outputPath);
        Encoding.UTF8.GetString(bytes).Should().Contain("Привет");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AIModTranslatorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
