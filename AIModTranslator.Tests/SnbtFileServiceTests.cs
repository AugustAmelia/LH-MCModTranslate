using System.Text;
using AIModTranslator.Models;
using AIModTranslator.Services;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class SnbtFileServiceTests
{
    [Fact]
    public void Tokenizer_TokenizesCorrectly()
    {
        string snbt = "{ id: \"123\", x: 1b, title: 'Quest 1', description: [ \"Line 1\", \"Line 2\" ], [I; 1, 2] }";
        var tokens = SnbtParser.Tokenize(snbt);

        tokens.Should().NotBeEmpty();
        tokens.Select(t => t.Type).Should().ContainInOrder(
            TokenType.OpenBrace,
            TokenType.UnquotedString, // id
            TokenType.Colon,
            TokenType.QuotedString, // "123"
            TokenType.Comma,
            TokenType.UnquotedString, // x
            TokenType.Colon,
            TokenType.UnquotedString, // 1b
            TokenType.Comma,
            TokenType.UnquotedString, // title
            TokenType.Colon,
            TokenType.QuotedString, // 'Quest 1'
            TokenType.Comma,
            TokenType.UnquotedString, // description
            TokenType.Colon,
            TokenType.OpenBracket,
            TokenType.QuotedString, // "Line 1"
            TokenType.Comma,
            TokenType.QuotedString, // "Line 2"
            TokenType.CloseBracket,
            TokenType.Comma,
            TokenType.OpenBracket,
            TokenType.UnquotedString, // I
            TokenType.Semicolon,
            TokenType.UnquotedString, // 1
            TokenType.Comma,
            TokenType.UnquotedString, // 2
            TokenType.CloseBracket,
            TokenType.CloseBrace
        );
    }

    [Fact]
    public void Parser_HandlesFTBQuestsEdgeCases()
    {
        string[] snbts = {
            "{ id: 123b }",
            "{ item: minecraft:stone }", // Unquoted string with colon!
            "{ title: \"test\" }",
            "{ tasks: [{id: \"hello\", type: \"ftbquests:kill\"}] }",
            "{ invisible: true }"
        };

        foreach (var s in snbts)
        {
            var tokens = SnbtParser.Tokenize(s);
            var parser = new SnbtParser(tokens);
            var node = parser.Parse();
            node.Should().NotBeNull();
        }
    }

    [Fact]
    public void Parser_HandlesRealFTBQuestsFile()
    {
        string snbt = """
{
	id: "0000000000000001",
	group: "",
	order_index: 0,
	filename: "test",
	title: "Test Chapter",
	icon: "minecraft:stone",
	default_quest_shape: "",
	default_hide_dependency_lines: false,
	quests: [
		{
			title: "First Quest",
			x: 0.0d,
			y: 0.0d,
			description: [
				"This is a line.",
				"This is another line."
			],
			dependencies: [],
			id: "0000000000000002",
			tasks: [{
				id: "0000000000000003",
				type: "item",
				item: "minecraft:stone"
			}]
		}
	]
}
""";

        var tokens = SnbtParser.Tokenize(snbt);
        var parser = new SnbtParser(tokens);
        var node = parser.Parse();
        node.Should().NotBeNull();
        
        var entries = new System.Collections.ObjectModel.ObservableCollection<TranslationEntry>();
        SnbtParser.ExtractTranslatableStrings(node, "", entries);
        
        entries.Should().NotBeEmpty();
        entries.Count.Should().Be(4); // title, title, description[0], description[1]
    }

    [Fact]
    public void Parser_HandlesFTBQuestsLocalizationKeys()
    {
        string snbt = """
{
	quest.779E68DD0A0F8483.quest_subtitle: "test subtitle"
	quest.779E68DD0A0F8483.title: "test title"
	chapter.00BD5759F8E2525F.title: "test chapter"
	quest.00D7A88C4C686C02.quest_desc: [
		"line 1"
		"line 2"
	]
}
""";

        var tokens = SnbtParser.Tokenize(snbt);
        var parser = new SnbtParser(tokens);
        var node = parser.Parse();
        
        var entries = new System.Collections.ObjectModel.ObservableCollection<TranslationEntry>();
        SnbtParser.ExtractTranslatableStrings(node, "", entries);
        
        entries.Count.Should().Be(5);
        entries[0].Key.Should().Be("quest.779E68DD0A0F8483.quest_subtitle");
        entries[1].Key.Should().Be("quest.779E68DD0A0F8483.title");
        entries[2].Key.Should().Be("chapter.00BD5759F8E2525F.title");
        entries[3].Key.Should().Be("quest.00D7A88C4C686C02.quest_desc[0]");
        entries[4].Key.Should().Be("quest.00D7A88C4C686C02.quest_desc[1]");
    }

    [Fact]
    public async Task SnbtFileService_LoadsAndSavesTranslatedValues()
    {
        var tempDir = CreateTempDirectory();
        var inputPath = Path.Combine(tempDir, "quest.snbt");
        var outputPath = Path.Combine(tempDir, "quest_ru.snbt");

        string originalSnbt = """
{
	id: "5B3D3DEFA482FA52"
	title: "My Chapter"
	quests: [
		{
			id: "1234"
			title: "Quest 1"
			description: [
				"Line 1"
				"Line 2"
			]
			tasks: [{
				id: "77DE"
				type: "item"
				item: "minecraft:dirt"
			}]
		}
	]
}
""";
        await File.WriteAllTextAsync(inputPath, originalSnbt);

        var service = new SnbtFileService();
        var entries = await service.LoadFileAsync(inputPath);

        entries.Should().HaveCount(4);
        
        foreach (var entry in entries)
        {
            entry.FilePath = inputPath;
        }

        var e1 = entries.First(e => e.Key == "title");
        e1.OriginalText.Should().Be("My Chapter");
        e1.TranslatedText = "Моя Глава";

        var e2 = entries.First(e => e.Key == "quests[0].title");
        e2.OriginalText.Should().Be("Quest 1");
        e2.TranslatedText = "Квест 1";

        var e3 = entries.First(e => e.Key == "quests[0].description[0]");
        e3.OriginalText.Should().Be("Line 1");
        e3.TranslatedText = "Строка 1";

        var e4 = entries.First(e => e.Key == "quests[0].description[1]");
        e4.OriginalText.Should().Be("Line 2");
        e4.TranslatedText = "Строка 2";

        await service.SaveFileAsync(outputPath, entries);

        var savedSnbt = await File.ReadAllTextAsync(outputPath);
        
        savedSnbt.Should().Contain("\"Моя Глава\"");
        savedSnbt.Should().Contain("\"Квест 1\"");
        savedSnbt.Should().Contain("\"Строка 1\"");
        savedSnbt.Should().Contain("\"Строка 2\"");
        
        // Ensure untranslated fields are preserved perfectly
        savedSnbt.Should().Contain("id: \"5B3D3DEFA482FA52\"");
        savedSnbt.Should().Contain("item: \"minecraft:dirt\"");
        savedSnbt.Should().Contain("type: \"item\"");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AIModTranslatorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
