using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using NSubstitute;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Tests for the four new entry-type-specific generation methods added to SummaryGenerator
/// in Phase 3f-g: GenerateAufgabeAsync, GenerateGespraechsnotizAsync,
/// GenerateStundenzettelAsync, GenerateAnalogAsync.
///
/// NOTE: These tests will not compile until Phase 3f-g adds the methods to SummaryGenerator.
/// This is intentional (TDD red phase).
/// </summary>
public sealed class SummaryGeneratorTypeTests
{
    // ── GenerateAufgabeAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAufgabeAsync_WhenLlmUnavailable_ReturnsNull()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateAufgabeAsync("some transcript");

        result.Should().BeNull();
        await llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateAufgabeAsync_calls_AufgabePrompt_with_transcript()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("Aufgabe Ergebnis");
        var sut = new SummaryGenerator(llm);

        await sut.GenerateAufgabeAsync("mein Transkript");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("mein Transkript") && !s.Contains("{transcript}")),
            Arg.Any<LlmOptions>());
    }

    // ── GenerateGespraechsnotizAsync ──────────────────────────────────────────

    [Fact]
    public async Task GenerateGespraechsnotizAsync_WhenLlmUnavailable_ReturnsNull()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateGespraechsnotizAsync("some transcript");

        result.Should().BeNull();
        await llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateGespraechsnotizAsync_calls_prompt_with_transcript()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("Gesprächsnotiz Ergebnis");
        var sut = new SummaryGenerator(llm);

        await sut.GenerateGespraechsnotizAsync("Gesprächs-Transkript");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Gesprächs-Transkript") && !s.Contains("{transcript}")),
            Arg.Any<LlmOptions>());
    }

    // ── GenerateStundenzettelAsync ────────────────────────────────────────────

    [Fact]
    public async Task GenerateStundenzettelAsync_WhenLlmUnavailable_ReturnsNull()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateStundenzettelAsync("some transcript");

        result.Should().BeNull();
        await llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateStundenzettelAsync_calls_prompt_with_transcript()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("Stundenzettel Ergebnis");
        var sut = new SummaryGenerator(llm);

        await sut.GenerateStundenzettelAsync("Stundenzettel-Transkript");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Stundenzettel-Transkript") && !s.Contains("{transcript}")),
            Arg.Any<LlmOptions>());
    }

    // ── GenerateAnalogAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAnalogAsync_WhenLlmUnavailable_ReturnsNull()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);
        var sut = new SummaryGenerator(llm);

        var result = await sut.GenerateAnalogAsync("some transcript");

        result.Should().BeNull();
        await llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateAnalogAsync_calls_prompt_with_transcript()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("Analog Ergebnis");
        var sut = new SummaryGenerator(llm);

        await sut.GenerateAnalogAsync("Analog-Transkript");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Analog-Transkript") && !s.Contains("{transcript}")),
            Arg.Any<LlmOptions>());
    }

    // ── GenerateEmailTextAsync (existing method, verifies prose_summary injection) ──

    [Fact]
    public async Task GenerateEmailTextAsync_calls_EmailPrompt_with_proseSummary()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("E-Mail Text");
        var sut = new SummaryGenerator(llm);

        await sut.GenerateEmailTextAsync("meine Fließtext-Zusammenfassung");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s =>
                s.Contains("meine Fließtext-Zusammenfassung") &&
                !s.Contains("{prose_summary}")),
            Arg.Is<LlmOptions>(o => o.MaxTokens == 4000));
    }
}
