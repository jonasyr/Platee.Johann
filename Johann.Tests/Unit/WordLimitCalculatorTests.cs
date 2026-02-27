using FluentAssertions;
using Johann.Application.Processing;

namespace Johann.Tests.Unit;

public sealed class WordLimitCalculatorTests
{
    [Fact]
    public void Calculate_LessThan300Words_Returns20And50()
    {
        var transcript = BuildTranscript(200);
        var (abstract_, structured) = WordLimitCalculator.Calculate(transcript);

        abstract_.Should().Be(20);
        structured.Should().Be(50);
    }

    [Fact]
    public void Calculate_ExactlyAt300Words_Returns50And150()
    {
        var transcript = BuildTranscript(300);
        var (abstract_, structured) = WordLimitCalculator.Calculate(transcript);

        abstract_.Should().Be(50);
        structured.Should().Be(150);
    }

    [Fact]
    public void Calculate_Between300And999Words_Returns50And150()
    {
        var transcript = BuildTranscript(500);
        var (abstract_, structured) = WordLimitCalculator.Calculate(transcript);

        abstract_.Should().Be(50);
        structured.Should().Be(150);
    }

    [Fact]
    public void Calculate_ExactlyAt1000Words_Returns150And300()
    {
        var transcript = BuildTranscript(1000);
        var (abstract_, structured) = WordLimitCalculator.Calculate(transcript);

        abstract_.Should().Be(150);
        structured.Should().Be(300);
    }

    [Fact]
    public void Calculate_MoreThan1000Words_Returns150And300()
    {
        var transcript = BuildTranscript(2000);
        var (abstract_, structured) = WordLimitCalculator.Calculate(transcript);

        abstract_.Should().Be(150);
        structured.Should().Be(300);
    }

    [Fact]
    public void Calculate_EmptyTranscript_Returns20And50()
    {
        var (abstract_, structured) = WordLimitCalculator.Calculate(string.Empty);

        abstract_.Should().Be(20);
        structured.Should().Be(50);
    }

    [Fact]
    public void Calculate_SingleWord_Returns20And50()
    {
        var (abstract_, structured) = WordLimitCalculator.Calculate("Hallo");

        abstract_.Should().Be(20);
        structured.Should().Be(50);
    }

    // -----------------------------------------------------------------------

    private static string BuildTranscript(int wordCount)
        => string.Join(" ", Enumerable.Repeat("Wort", wordCount));
}
