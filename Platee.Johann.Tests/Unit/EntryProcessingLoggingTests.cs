namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Parsing;

public sealed class EntryProcessingLoggingTests : IDisposable
{
    private readonly string tempDir;

    public EntryProcessingLoggingTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannLoggingTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    private static IAudioTranscriber CreateTranscriber()
    {
        var transcriber = Substitute.For<IAudioTranscriber>();
        transcriber.IsAvailable.Returns(true);
        transcriber.TranscribeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptionResult("Hallo Welt, dies ist ein Test.", 3.0, 6));
        return transcriber;
    }

    private static (EntryProcessingService Service, IEntryRepository Repo, IEntryProcessingLogger Logger) CreateService(
        string outputRoot,
        string archivverzeichnis,
        IEnumerable<IEntryRenderer> renderers)
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(false);

        var summaryGen = new SummaryGenerator(llm);
        var repo = Substitute.For<IEntryRepository>();
        repo.GetNextSequenceNumberAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(1);

        var settings = new SettingsHolder(AppSettings.Default with { Archivverzeichnis = archivverzeichnis });
        var logger = Substitute.For<IEntryProcessingLogger>();

        var service = new EntryProcessingService(
            CreateTranscriber(),
            summaryGen,
            new HeaderParser(),
            repo,
            outputRoot: outputRoot,
            overviewService: null,
            settings: settings,
            renderers: renderers,
            logger: logger);

        return (service, repo, logger);
    }

    [Fact]
    public async Task ProcessAudioAsync_WhenRendererThrows_LogsWarningWithJobIdAndException()
    {
        var audioPath = Path.Combine(this.tempDir, "input.mp3");
        File.WriteAllText(audioPath, "fake audio bytes");

        var failure = new InvalidOperationException("PDF engine unavailable");
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(failure);

        var (service, _, logger) = CreateService(
            outputRoot: this.tempDir,
            archivverzeichnis: string.Empty,
            renderers: [renderer]);

        var result = await service.ProcessAudioAsync(audioPath, DateOnly.FromDateTime(DateTime.Today));

        logger.Received(1).LogWarning("PDF render", result.JobId, failure);
        result.Status.PdfCreated.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAudioAsync_WhenRawArchivalFails_LogsWarningWithJobIdAndException()
    {
        // Source audio file does not exist, so File.Copy inside ArchiveRawFilesAsync throws.
        var missingAudioPath = Path.Combine(this.tempDir, "does-not-exist.mp3");

        var (service, _, logger) = CreateService(
            outputRoot: this.tempDir,
            archivverzeichnis: string.Empty,
            renderers: []);

        var result = await service.ProcessAudioAsync(missingAudioPath, DateOnly.FromDateTime(DateTime.Today));

        logger.Received(1).LogWarning(
            "Raw file archival",
            result.JobId,
            Arg.Is<Exception>(ex => ex is FileNotFoundException));
    }

    [Fact]
    public async Task ProcessAudioAsync_WhenMp3MoveFails_LogsWarningWithJobIdAndException()
    {
        var audioPath = Path.Combine(this.tempDir, "input.mp3");
        File.WriteAllText(audioPath, "fake audio bytes");

        // A regular file at the archive path makes Directory.CreateDirectory throw.
        var archivePathBlockedByFile = Path.Combine(this.tempDir, "archive-blocker");
        File.WriteAllText(archivePathBlockedByFile, "not a directory");

        var (service, _, logger) = CreateService(
            outputRoot: this.tempDir,
            archivverzeichnis: archivePathBlockedByFile,
            renderers: []);

        var result = await service.ProcessAudioAsync(audioPath, DateOnly.FromDateTime(DateTime.Today));

        logger.Received(1).LogWarning("MP3 archive move", result.JobId, Arg.Any<Exception>());
        result.Status.Archived.Should().BeFalse();
    }
}
