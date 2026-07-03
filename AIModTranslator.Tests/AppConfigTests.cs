using AIModTranslator.Models;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class AppConfigTests
{
    [Fact]
    public void Defaults_AreBackwardCompatible()
    {
        var config = new AppConfig();

        config.MaxRetries.Should().Be(3);
        config.MaxParallelRequests.Should().Be(1);
    }
}
