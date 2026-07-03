using System.Collections.ObjectModel;
using System.Text;
using AIModTranslator.Models;
using AIModTranslator.Services.Interfaces;

namespace AIModTranslator.Services;

public class SnbtFileService : IFileService
{
    public string[] SupportedExtensions => new[] { ".snbt" };

    public async Task<ObservableCollection<TranslationEntry>> LoadFileAsync(string filePath)
    {
        var entries = new ObservableCollection<TranslationEntry>();

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл не найден.", filePath);

        string snbtText = await File.ReadAllTextAsync(filePath);
        
        var tokens = SnbtParser.Tokenize(snbtText);
        var parser = new SnbtParser(tokens);
        var root = parser.Parse();

        SnbtParser.ExtractTranslatableStrings(root, string.Empty, entries);

        return entries;
    }

    public async Task SaveFileAsync(string filePath, IEnumerable<TranslationEntry> entries)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0) return;

        var originalFile = entryList.First().FilePath;
        string snbtText = File.Exists(originalFile) ? await File.ReadAllTextAsync(originalFile) : "{}";
        
        var tokens = SnbtParser.Tokenize(snbtText);
        var parser = new SnbtParser(tokens);
        var root = parser.Parse();

        var dict = entryList.ToDictionary(e => e.Key, e => !string.IsNullOrWhiteSpace(e.TranslatedText) ? e.TranslatedText : e.OriginalText);

        SnbtParser.ApplyTranslations(root, string.Empty, dict);

        var outputSnbt = root.ToSnbt(0);

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(filePath, outputSnbt);
    }
}

public enum TokenType
{
    OpenBrace,      // {
    CloseBrace,     // }
    OpenBracket,    // [
    CloseBracket,   // ]
    Colon,          // :
    Comma,          // ,
    Semicolon,      // ;
    QuotedString,   // "hello" or 'hello'
    UnquotedString, // hello, 1b, minecraft:stone, 0.5f, true
    EndOfFile
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public char QuoteChar { get; }

    public Token(TokenType type, string value, char quoteChar = '\0')
    {
        Type = type;
        Value = value;
        QuoteChar = quoteChar;
    }
}

public abstract class NbtNode
{
    public abstract string ToSnbt(int indentLevel = 0);
}

public class NbtString : NbtNode
{
    public string Value { get; set; }
    public char QuoteChar { get; set; }

    public NbtString(string value, char quoteChar = '"')
    {
        Value = value;
        QuoteChar = quoteChar;
    }

    public override string ToSnbt(int indentLevel = 0)
    {
        if (QuoteChar == '\0')
            return Value;
        
        var escaped = Value
            .Replace("\\", "\\\\")
            .Replace(QuoteChar.ToString(), "\\" + QuoteChar)
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
        return $"{QuoteChar}{escaped}{QuoteChar}";
    }
}

public class NbtValue : NbtNode
{
    public string RawValue { get; set; }

    public NbtValue(string rawValue)
    {
        RawValue = rawValue;
    }

    public override string ToSnbt(int indentLevel = 0)
    {
        return RawValue;
    }
}

public class NbtArray : NbtNode
{
    public char TypeChar { get; set; }
    public List<string> Values { get; set; } = new();

    public NbtArray(char typeChar)
    {
        TypeChar = typeChar;
    }

    public override string ToSnbt(int indentLevel = 0)
    {
        if (Values.Count == 0) return $"[{TypeChar};]";
        return $"[{TypeChar}; {string.Join(", ", Values)}]";
    }
}

public class NbtList : NbtNode
{
    public List<NbtNode> Elements { get; set; } = new();

    public override string ToSnbt(int indentLevel = 0)
    {
        if (Elements.Count == 0)
            return "[]";

        var sb = new StringBuilder();
        sb.AppendLine("[");
        var indent = new string('\t', indentLevel + 1);
        for (int i = 0; i < Elements.Count; i++)
        {
            sb.Append(indent);
            sb.Append(Elements[i].ToSnbt(indentLevel + 1));
            sb.AppendLine();
        }
        sb.Append(new string('\t', indentLevel));
        sb.Append("]");
        return sb.ToString();
    }
}

public class NbtCompound : NbtNode
{
    public List<NbtProperty> Properties { get; set; } = new();

    public override string ToSnbt(int indentLevel = 0)
    {
        if (Properties.Count == 0)
            return "{}";

        var sb = new StringBuilder();
        sb.AppendLine("{");
        var indent = new string('\t', indentLevel + 1);
        foreach (var prop in Properties)
        {
            sb.Append(indent);
            sb.Append(prop.KeyToken.ToSnbt(0));
            sb.Append(": ");
            sb.Append(prop.Value.ToSnbt(indentLevel + 1));
            sb.AppendLine();
        }
        sb.Append(new string('\t', indentLevel));
        sb.Append("}");
        return sb.ToString();
    }
}

public class NbtProperty
{
    public string Key { get; }
    public NbtNode KeyToken { get; }
    public NbtNode Value { get; set; }

    public NbtProperty(string key, NbtNode keyToken, NbtNode value)
    {
        Key = key;
        KeyToken = keyToken;
        Value = value;
    }
}

public class SnbtParser
{
    private readonly List<Token> _tokens;
    private int _index = 0;

    public SnbtParser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        int length = text.Length;

        while (i < length)
        {
            char c = text[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '#')
            {
                i++;
                while (i < length && text[i] != '\n' && text[i] != '\r') i++;
                continue;
            }
            if (c == '/' && i + 1 < length && text[i + 1] == '/')
            {
                i += 2;
                while (i < length && text[i] != '\n' && text[i] != '\r') i++;
                continue;
            }

            if (c == '{') { tokens.Add(new Token(TokenType.OpenBrace, "{")); i++; continue; }
            if (c == '}') { tokens.Add(new Token(TokenType.CloseBrace, "}")); i++; continue; }
            if (c == '[') { tokens.Add(new Token(TokenType.OpenBracket, "[")); i++; continue; }
            if (c == ']') { tokens.Add(new Token(TokenType.CloseBracket, "]")); i++; continue; }
            if (c == ':') { tokens.Add(new Token(TokenType.Colon, ":")); i++; continue; }
            if (c == ',') { tokens.Add(new Token(TokenType.Comma, ",")); i++; continue; }
            if (c == ';') { tokens.Add(new Token(TokenType.Semicolon, ";")); i++; continue; }

            if (c == '"' || c == '\'')
            {
                char quoteChar = c;
                i++; 
                var sb = new StringBuilder();
                bool escaped = false;
                while (i < length)
                {
                    char nextChar = text[i];
                    if (escaped)
                    {
                        if (nextChar == 'n') sb.Append('\n');
                        else if (nextChar == 'r') sb.Append('\r');
                        else if (nextChar == 't') sb.Append('\t');
                        else sb.Append(nextChar); 
                        escaped = false;
                    }
                    else if (nextChar == '\\')
                    {
                        escaped = true;
                    }
                    else if (nextChar == quoteChar)
                    {
                        i++; 
                        break;
                    }
                    else
                    {
                        sb.Append(nextChar);
                    }
                    i++;
                }
                tokens.Add(new Token(TokenType.QuotedString, sb.ToString(), quoteChar));
                continue;
            }

            {
                int start = i;
                while (i < length)
                {
                    char nextChar = text[i];
                    if (char.IsWhiteSpace(nextChar) || 
                        nextChar == '{' || nextChar == '}' || 
                        nextChar == '[' || nextChar == ']' || 
                        nextChar == ':' || nextChar == ',' || 
                        nextChar == ';' || 
                        nextChar == '"' || nextChar == '\'')
                    {
                        break;
                    }
                    i++;
                }
                string val = text.Substring(start, i - start);
                tokens.Add(new Token(TokenType.UnquotedString, val));
            }
        }

        tokens.Add(new Token(TokenType.EndOfFile, string.Empty));
        return tokens;
    }

    private Token Peek() => _tokens[_index];
    private Token Read() => _tokens[_index++];
    private void Consume(TokenType type)
    {
        var t = Read();
        if (t.Type != type)
            throw new Exception($"Expected token {type}, got {t.Type} ('{t.Value}')");
    }

    public NbtNode Parse()
    {
        var node = ParseTag();
        if (Peek().Type != TokenType.EndOfFile)
            throw new Exception($"Unexpected token at end of file: {Peek().Type} ('{Peek().Value}')");
        return node;
    }

    private NbtNode ParseTag()
    {
        var t = Peek();
        if (t.Type == TokenType.OpenBrace)
        {
            return ParseCompound();
        }
        else if (t.Type == TokenType.OpenBracket)
        {
            if (_index + 2 < _tokens.Count && 
                _tokens[_index + 1].Type == TokenType.UnquotedString && 
                (_tokens[_index + 1].Value == "B" || _tokens[_index + 1].Value == "I" || _tokens[_index + 1].Value == "L") &&
                _tokens[_index + 2].Type == TokenType.Semicolon)
            {
                return ParseArray();
            }
            else
            {
                return ParseList();
            }
        }
        else if (t.Type == TokenType.QuotedString)
        {
            var token = Read();
            return new NbtString(token.Value, token.QuoteChar);
        }
        else if (t.Type == TokenType.UnquotedString)
        {
            var token = Read();
            string val = token.Value;
            
            while (Peek().Type == TokenType.Colon)
            {
                Read();
                val += ":";
                if (Peek().Type == TokenType.UnquotedString)
                {
                    val += Read().Value;
                }
            }
            
            return new NbtValue(val);
        }
        else
        {
            throw new Exception($"Unexpected token when parsing tag: {t.Type} ('{t.Value}')");
        }
    }

    private NbtCompound ParseCompound()
    {
        Consume(TokenType.OpenBrace);
        var compound = new NbtCompound();
        
        while (Peek().Type != TokenType.CloseBrace && Peek().Type != TokenType.EndOfFile)
        {
            var keyToken = Read();
            if (keyToken.Type != TokenType.QuotedString && keyToken.Type != TokenType.UnquotedString)
                throw new Exception($"Expected key, got {keyToken.Type} ('{keyToken.Value}')");
            
            string key = keyToken.Value;
            NbtNode keyNode = keyToken.Type == TokenType.QuotedString 
                ? new NbtString(key, keyToken.QuoteChar) 
                : new NbtValue(key);
            
            Consume(TokenType.Colon);
            
            var val = ParseTag();
            
            compound.Properties.Add(new NbtProperty(key, keyNode, val));
            
            if (Peek().Type == TokenType.Comma)
            {
                Read();
            }
        }
        
        Consume(TokenType.CloseBrace);
        return compound;
    }

    private NbtList ParseList()
    {
        Consume(TokenType.OpenBracket);
        var list = new NbtList();
        
        while (Peek().Type != TokenType.CloseBracket && Peek().Type != TokenType.EndOfFile)
        {
            var val = ParseTag();
            list.Elements.Add(val);
            
            if (Peek().Type == TokenType.Comma)
            {
                Read();
            }
        }
        
        Consume(TokenType.CloseBracket);
        return list;
    }

    private NbtArray ParseArray()
    {
        Consume(TokenType.OpenBracket);
        
        var typeToken = Read();
        char typeChar = typeToken.Value[0];
        
        Consume(TokenType.Semicolon);
        
        var array = new NbtArray(typeChar);
        
        while (Peek().Type != TokenType.CloseBracket && Peek().Type != TokenType.EndOfFile)
        {
            var valToken = Read();
            if (valToken.Type != TokenType.UnquotedString && valToken.Type != TokenType.QuotedString)
                throw new Exception($"Expected array element, got {valToken.Type} ('{valToken.Value}')");
            
            array.Values.Add(valToken.Value);
            
            if (Peek().Type == TokenType.Comma)
            {
                Read();
            }
        }
        
        Consume(TokenType.CloseBracket);
        return array;
    }

    public static void ExtractTranslatableStrings(NbtNode node, string path, ObservableCollection<TranslationEntry> entries)
    {
        if (node is NbtCompound compound)
        {
            foreach (var prop in compound.Properties)
            {
                string childPath = string.IsNullOrEmpty(path) ? prop.Key : $"{path}.{prop.Key}";
                
                if (IsTranslatableKey(prop.Key))
                {
                    ExtractStringsFromTranslatableNode(prop.Value, childPath, entries);
                }
                else
                {
                    ExtractTranslatableStrings(prop.Value, childPath, entries);
                }
            }
        }
        else if (node is NbtList list)
        {
            for (int i = 0; i < list.Elements.Count; i++)
            {
                string childPath = $"{path}[{i}]";
                ExtractTranslatableStrings(list.Elements[i], childPath, entries);
            }
        }
    }

    private static bool IsTranslatableKey(string key)
    {
        string k = key.ToLowerInvariant();
        return k == "title" || k == "subtitle" || k == "description" || k == "text" ||
               k.EndsWith(".title") || k.EndsWith(".subtitle") || k.EndsWith(".quest_subtitle") || 
               k.EndsWith(".description") || k.EndsWith(".quest_desc") || k.EndsWith(".text");
    }

    private static void ExtractStringsFromTranslatableNode(NbtNode node, string path, ObservableCollection<TranslationEntry> entries)
    {
        if (node is NbtString strNode)
        {
            string val = strNode.Value;
            if (!string.IsNullOrWhiteSpace(val) && !(val.StartsWith("{") && val.EndsWith("}")))
            {
                entries.Add(new TranslationEntry
                {
                    Key = path,
                    OriginalText = val,
                    TranslatedText = string.Empty,
                    Status = "Untranslated"
                });
            }
        }
        else if (node is NbtList listNode)
        {
            for (int i = 0; i < listNode.Elements.Count; i++)
            {
                string childPath = $"{path}[{i}]";
                ExtractStringsFromTranslatableNode(listNode.Elements[i], childPath, entries);
            }
        }
    }

    public static void ApplyTranslations(NbtNode node, string path, Dictionary<string, string> translations)
    {
        if (node is NbtCompound compound)
        {
            foreach (var prop in compound.Properties)
            {
                string childPath = string.IsNullOrEmpty(path) ? prop.Key : $"{path}.{prop.Key}";
                
                if (IsTranslatableKey(prop.Key))
                {
                    UpdateTranslatableNode(prop.Value, childPath, translations);
                }
                else
                {
                    ApplyTranslations(prop.Value, childPath, translations);
                }
            }
        }
        else if (node is NbtList list)
        {
            for (int i = 0; i < list.Elements.Count; i++)
            {
                string childPath = $"{path}[{i}]";
                ApplyTranslations(list.Elements[i], childPath, translations);
            }
        }
    }

    private static void UpdateTranslatableNode(NbtNode node, string path, Dictionary<string, string> translations)
    {
        if (node is NbtString strNode)
        {
            if (translations.TryGetValue(path, out string? translatedText) && !string.IsNullOrWhiteSpace(translatedText))
            {
                strNode.Value = translatedText;
            }
        }
        else if (node is NbtList listNode)
        {
            for (int i = 0; i < listNode.Elements.Count; i++)
            {
                string childPath = $"{path}[{i}]";
                UpdateTranslatableNode(listNode.Elements[i], childPath, translations);
            }
        }
    }
}
