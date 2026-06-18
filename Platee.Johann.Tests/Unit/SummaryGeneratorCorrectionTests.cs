using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.ValueObjects;
using Xunit;

namespace Platee.Johann.Tests.Unit;

public sealed class SummaryGeneratorCorrectionTests
{
    [Fact]
    public async Task GenerateAbstractAsync_WithCorrections_InjectsIntoSystemPrompt()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("result");

        var corrections = new List<CorrectionEntry>
        {
            new() { Wrong = "Piano", Correct = "Peano" },
            new() { Wrong = "Nele", Correct = "Neele" },
        };
        var settings = AppSettings.Default with { Korrekturliste = corrections };
        var holder = new SettingsHolder(settings);

        var sut = new SummaryGenerator(llm, holder);
        await sut.GenerateAbstractAsync("test transcript");

        await llm.Received(1).GenerateAsync(
            Arg.Is<string>(s => s.Contains("Piano") && s.Contains("Peano")
                             && s.Contains("Nele") && s.Contains("Neele")),
            Arg.Any<string>(),
            Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateAbstractAsync_WithEmptyCorrections_UsesUnmodifiedSystemPrompt()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("result");

        var settings = AppSettings.Default with { Korrekturliste = [] };
        var holder = new SettingsHolder(settings);

        var sut = new SummaryGenerator(llm, holder);
        await sut.GenerateAbstractAsync("test transcript");

        await llm.Received(1).GenerateAsync(
            Arg.Is<string>(s => s == PromptSettings.Default.SystemMessage),
            Arg.Any<string>(),
            Arg.Any<LlmOptions>());
    }

    [Fact]
    public async Task GenerateProseSummaryAsync_WithCorrections_InjectsIntoSystemPrompt()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>())
           .Returns("result");

        var corrections = new List<CorrectionEntry>
        {
            new() { Wrong = "Piano", Correct = "Peano" },
        };
        var settings = AppSettings.Default with { Korrekturliste = corrections };
        var holder = new SettingsHolder(settings);

        var sut = new SummaryGenerator(llm, holder);
        await sut.GenerateProseSummaryAsync("test transcript");

        await llm.Received(1).GenerateAsync(
            Arg.Is<string>(s => s.Contains("Piano") && s.Contains("Peano")),
            Arg.Any<string>(),
            Arg.Any<LlmOptions>());
    }
}
