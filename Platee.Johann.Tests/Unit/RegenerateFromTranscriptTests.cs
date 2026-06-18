namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.Domain.ValueObjects;

public sealed class RegenerateFromTranscriptTests
{
    private static Entry CreateEntry(string? transcript = "Original text here.", string? editedTranscript = null) =>
        new()
        {
            JobId = "260617_001_Test_abc",
            SequenceNumber = 1,
            CreatedAt = new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.FromHours(1)),
            Type = EntryType.Projekt,
            ProjectName = "Test",
            Title = "Test Titel",
            SourceType = "audio",
            Status = new ProcessingStatus(true, true, false, false, false),
            Transcript = transcript,
            EditedTranscript = editedTranscript,
            Abstract = "Old abstract",
            LongSummary = "Old summary",
            ProseSummary = "Old prose",
        };

    private static (EntryProcessingService Service, ILlmProvider Llm, IEntryRepository Repo) CreateService()
    {
        var transcriber = Substitute.For<IAudioTranscriber>();
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"Generated: {ci.ArgAt<string>(1)[..Math.Min(20, ci.ArgAt<string>(1).Length)]}");

        var summaryGen = new SummaryGenerator(llm);
        var repo = Substitute.For<IEntryRepository>();
        var parser = new HeaderParser();
        var settings = new SettingsHolder(AppSettings.Default with { Korrekturliste = [] });

        var service = new EntryProcessingService(
            transcriber, summaryGen, parser, repo,
            outputRoot: "",
            overviewService: null,
            settings: settings,
            renderers: []);

        return (service, llm, repo);
    }

    [Fact]
    public async Task RegenerateFromTranscriptAsync_StoresEditedTranscript()
    {
        var (service, _, _) = CreateService();
        var entry = CreateEntry(transcript: "Whisper output");

        var result = await service.RegenerateFromTranscriptAsync(entry, "Corrected text");

        result.EditedTranscript.Should().Be("Corrected text");
        result.Transcript.Should().Be("Whisper output");
    }

    [Fact]
    public async Task RegenerateFromTranscriptAsync_UsesEditedTextForSummaries()
    {
        var (service, llm, _) = CreateService();
        var entry = CreateEntry(transcript: "Whisper output");

        await service.RegenerateFromTranscriptAsync(entry, "Corrected transcript text here");

        // Verify the LLM was called with the edited transcript, not the original
        await llm.Received().GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Corrected transcript text here")),
            Arg.Any<LlmOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateFromTranscriptAsync_PersistsEntry()
    {
        var (service, _, repo) = CreateService();
        var entry = CreateEntry();

        await service.RegenerateFromTranscriptAsync(entry, "New text");

        await repo.Received(1).SaveAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateFromTranscriptAsync_ThrowsOnEmptyText()
    {
        var (service, _, _) = CreateService();
        var entry = CreateEntry();

        var act = () => service.RegenerateFromTranscriptAsync(entry, "   ");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReprocessAsync_UsesEffectiveTranscript_WhenEdited()
    {
        var (service, llm, _) = CreateService();
        var entry = CreateEntry(transcript: "Original", editedTranscript: "Edited version");

        await service.ReprocessAsync(entry);

        // Verify the LLM was called with the edited transcript
        await llm.Received().GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Edited version")),
            Arg.Any<LlmOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReprocessAsync_UsesOriginalTranscript_WhenNoEdit()
    {
        var (service, llm, _) = CreateService();
        var entry = CreateEntry(transcript: "Original text only");

        await service.ReprocessAsync(entry);

        // Verify the LLM was called with the original transcript
        await llm.Received().GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Original text only")),
            Arg.Any<LlmOptions>(),
            Arg.Any<CancellationToken>());
    }
}
