using System.Text.RegularExpressions;

namespace AIModTranslator.Helpers;

public static partial class RegexHelpers
{
    // Matches Minecraft formatting codes, e.g., §c, §l, §r
    [GeneratedRegex("§[0-9a-fk-or]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    public static partial Regex MinecraftColorCodes();

    // Matches placeholders like %s, %d, %f, %1$s, %2$d, %%, {0}, {count}
    [GeneratedRegex(@"%(?:%|(?:\d+\$)?[sdf])|\{[^{}]+\}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    public static partial Regex Placeholders();
    
    // Example Tokenization logic can go here later
}
