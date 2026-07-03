using AIModTranslator.Services;
using FluentAssertions;

namespace AIModTranslator.Tests;

public class AiJsonResponseParserTests
{
    [Fact]
    public void ParseStringArray_ParsesValidArray()
    {
        var result = AiJsonResponseParser.ParseStringArray("""["one","two"]""", 2);

        result.IsSuccess.Should().BeTrue();
        result.Values.Should().Equal("one", "two");
        result.WasAutoFixed.Should().BeFalse();
    }

    [Fact]
    public void ParseStringArray_ExtractsFencedJson()
    {
        var result = AiJsonResponseParser.ParseStringArray("""
            ```json
            ["one","two"]
            ```
            """, 2);

        result.IsSuccess.Should().BeTrue();
        result.Values.Should().Equal("one", "two");
        result.WasAutoFixed.Should().BeTrue();
    }

    [Fact]
    public void ParseStringArray_RemovesTrailingCommaBeforeArrayEnd()
    {
        var result = AiJsonResponseParser.ParseStringArray("""["one","two",]""", 2);

        result.IsSuccess.Should().BeTrue();
        result.Values.Should().Equal("one", "two");
        result.WasAutoFixed.Should().BeTrue();
    }

    [Fact]
    public void ParseStringArray_DropsTextAroundArray()
    {
        var result = AiJsonResponseParser.ParseStringArray("""Here: ["one","two"] thanks""", 2);

        result.IsSuccess.Should().BeTrue();
        result.Values.Should().Equal("one", "two");
        result.WasAutoFixed.Should().BeTrue();
    }

    [Fact]
    public void ParseStringArray_ReturnsFailureForInvalidJson()
    {
        var result = AiJsonResponseParser.ParseStringArray("[\"one\",\"two\"", 2);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }
}
