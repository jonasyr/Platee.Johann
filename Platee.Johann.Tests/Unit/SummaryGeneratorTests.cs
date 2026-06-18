namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

public sealed class SummaryGeneratorTests
{
    // ── IsAvailable ───────────────────────────────────────────────────────────
    [Fact]
    public void IsAvailable_WhenLlmIsAvailable_ReturnsTrue()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        var sut = new SummaryGenerator(llm);

        sut.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenLlmIsNotAvailable_ReturnsFalse()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        sut.IsAvailable.Should().BeFalse();
    }

    // ── GenerateAbstractAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task GenerateAbstractAsync_WhenLlmUnavailable_ReturnsEmpty()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateAbstractAsync("any transcript");

        result.Should().BeEmpty();
        await llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateAbstractAsync_WhenTranscriptEmpty_ReturnsEmpty()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateAbstractAsync(string.Empty);

        result.Should().BeEmpty();
        await llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateAbstractAsync_UsesSystemMessageAndWordLimit()
    {
        const string expectedAbstract = "Das ist ein Abstract.";
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns(expectedAbstract);

        var settings = AppSettings.Default with { Korrekturliste = [] };
        var holder = new SettingsHolder(settings);
        var sut = new SummaryGenerator(llm, holder);
        var shortTranscript = BuildTranscript(100); // <300 → limit=20

        var result = await sut.GenerateAbstractAsync(shortTranscript);

        result.Should().Be(expectedAbstract);

        await llm.Received(1).GenerateAsync(
            Arg.Is<string>(s => s == SummaryPrompts.SystemMessage),
            Arg.Is<string>(s => s.Contains("20") && s.Contains(shortTranscript)),
            Arg.Is<LlmOptions>(o => o.MaxTokens == 20000));
    }

    // ── GenerateLongSummaryAsync ──────────────────────────────────────────────
    [Fact]
    public async Task GenerateLongSummaryAsync_WhenLlmUnavailable_ReturnsEmpty()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateLongSummaryAsync("transcript");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateLongSummaryAsync_PassesTranscriptToLlm()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("Zusammenfassung");

        var sut = new SummaryGenerator(llm);
        var transcript = BuildTranscript(500);

        await sut.GenerateLongSummaryAsync(transcript);

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains(transcript)),
            Arg.Any<LlmOptions>());
    }

    // ── GenerateProseSummaryAsync ─────────────────────────────────────────────
    [Fact]
    public async Task GenerateProseSummaryAsync_WhenLlmUnavailable_ReturnsEmpty()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateProseSummaryAsync("transcript");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateProseSummaryAsync_InjectsTranscriptIntoPrompt()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("Prosa");

        var sut = new SummaryGenerator(llm);
        const string transcript = "Mein Transkript";

        await sut.GenerateProseSummaryAsync(transcript);

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains(transcript)),
            Arg.Any<LlmOptions>());
    }

    // ── GenerateEmailTextAsync ────────────────────────────────────────────────
    [Fact]
    public async Task GenerateEmailTextAsync_WhenLlmUnavailable_ReturnsEmpty()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateEmailTextAsync("prose");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateEmailTextAsync_UsesMaxTokens4000()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("E-Mail Text");

        var sut = new SummaryGenerator(llm);

        await sut.GenerateEmailTextAsync("eine Zusammenfassung");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<LlmOptions>(o => o.MaxTokens == 4000));
    }

    // -----------------------------------------------------------------------
    private static string BuildTranscript(int wordCount)
        => string.Join(" ", Enumerable.Repeat("Wort", wordCount));
}
