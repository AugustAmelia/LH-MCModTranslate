using AIModTranslator.Services;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class MinecraftTokenProtectorTests
{
    [Fact]
    public void ProtectAndRestore_PreservesMinecraftColorCodeAndIndexedPlaceholder()
    {
        const string original = "§cHello %1$s";

        var protectedString = MinecraftTokenProtector.Protect(original);
        var restored = MinecraftTokenProtector.Restore(protectedString.ProtectedValue, protectedString.Tokens);

        protectedString.ProtectedValue.Should().NotContain("§c");
        protectedString.ProtectedValue.Should().NotContain("%1$s");
        restored.Should().Be(original);
    }

    [Fact]
    public void ProtectAndRestore_PreservesSupportedPlaceholders()
    {
        const string original = "Progress %%: %d / %f for {count}";

        var protectedString = MinecraftTokenProtector.Protect(original);
        var restored = MinecraftTokenProtector.Restore(protectedString.ProtectedValue, protectedString.Tokens);

        protectedString.Tokens.Values.Should().Contain(new[] { "%%", "%d", "%f", "{count}" });
        restored.Should().Be(original);
    }

    [Fact]
    public void Protect_IgnoresInvalidCustomRegexAndAppliesValidCustomRegex()
    {
        const string original = "Use <item:minecraft:stone> now";
        var customRegexes = new[] { "<[^>]+>", "[" };

        var protectedString = MinecraftTokenProtector.Protect(original, customRegexes);
        var restored = MinecraftTokenProtector.Restore(protectedString.ProtectedValue, protectedString.Tokens);

        protectedString.ProtectedValue.Should().NotContain("<item:minecraft:stone>");
        protectedString.Tokens.Values.Should().Contain("<item:minecraft:stone>");
        restored.Should().Be(original);
    }
}
