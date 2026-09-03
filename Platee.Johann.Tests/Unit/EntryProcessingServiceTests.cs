namespace Platee.Johann.Tests.Unit;

using System.Net.Http;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Parsing;

/// <summary>
/// Covers the orchestration gaps left open by <see cref="EntryProcessingLoggingTests"/>
/// (swallow-site logging) and <see cref="RegenerateFromTranscriptTests"/> (regeneration):
/// the end-to-end happy path, transcription failure, LLM failure, and concurrency.
/// This is the regression net for the v1.4.0 category rework, which rewrites
/// <see cref="EntryProcessingService"/>.
/// </summary>
public sealed class EntryProcessingServiceTests : IDisposable
{
    private readonly string tempDir;
    private readonly string audioPath;
    private readonly string secondAudioPath;

    public EntryProcessingServiceTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannProcessingTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);

        this.audioPath = Path.Combine(this.tempDir, "first.mp3");
        this.secondAudioPath = Path.Combine(this.tempDir, "second.mp3");
        File.WriteAllBytes(this.audioPath, [0x00]);
        File.WriteAllBytes(this.secondAudioPath, [0x00]);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessAudioAsync_HappyPath_SavesSummarizedEntryAndInvokesRenderers()
    {
        var renderer = Substitute.For<IEntryRenderer>();
        // Dispatch in EntryProcessingService is an exact match on "PDF" / "HTML".
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .Returns(new RenderResult([0x01], "application/pdf", "entry.pdf"));

        var ctx = this.CreateService(renderers: [renderer]);

        var entry = await ctx.Service.ProcessAudioAsync(this.audioPath, new DateOnly(2026, 9, 3));

        entry.Status.Transcribed.Should().BeTrue();
        entry.Status.Summarized.Should().BeTrue();
        entry.Transcript.Should().NotBeNullOrWhiteSpace();
        entry.JobId.Should().NotBeNullOrWhiteSpace();

        await ctx.Repo.Received().SaveAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
        await renderer.Received().RenderAsync(
            Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAudioAsync_WhenTranscriptionFails_DoesNotCallTheLlm()
    {
        var ctx = this.CreateService();
        ctx.Transcriber
            .TranscribeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("whisper down"));

        var act = () => ctx.Service.ProcessAudioAsync(this.audioPath, new DateOnly(2026, 9, 3));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("whisper down");
        await ctx.Llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>());
        await ctx.Repo.DidNotReceive().SaveAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAudioAsync_WhenLlmFails_SurfacesTheFailure()
    {
        var ctx = this.CreateService();
        ctx.Llm
            .GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("gpt down"));

        var act = () => ctx.Service.ProcessAudioAsync(this.audioPath, new DateOnly(2026, 9, 3));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ProcessAudioAsync_TwoConcurrentCalls_BothCompleteWithDistinctJobIds()
    {
        var ctx = this.CreateService();
        var seq = 0;
        ctx.Repo.GetNextSequenceNumberAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref seq));

        var date = new DateOnly(2026, 9, 3);

        var results = await Task.WhenAll(
            ctx.Service.ProcessAudioAsync(this.audioPath, date),
            ctx.Service.ProcessAudioAsync(this.secondAudioPath, date));

        results.Should().HaveCount(2);
        results.Select(e => e.JobId).Should().OnlyHaveUniqueItems();
        results.Should().AllSatisfy(e => e.Status.Summarized.Should().BeTrue());
    }

    [Fact]
    public async Task ProcessAudioAsync_RendererNameCasingDiffers_StillRenders()
    {
        // Dispatch must agree with EntryDetailViewModel, which matches
        // case-insensitively. See #40.
        var renderer = CreateRenderer("pdf");
        var ctx = this.CreateService(renderers: [renderer]);

        var entry = await ctx.Service.ProcessAudioAsync(this.audioPath, new DateOnly(2026, 9, 3));

        await renderer.Received().RenderAsync(
            Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>());
        entry.Status.PdfCreated.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAudioAsync_UnrecognisedRendererName_LogsExactlyOneWarning()
    {
        // A renderer that matches no branch used to vanish without a trace.
        var renderer = CreateRenderer("Sparkline");
        var ctx = this.CreateService(renderers: [renderer]);

        await ctx.Service.ProcessAudioAsync(this.audioPath, new DateOnly(2026, 9, 3));

        ctx.Logger.Received(1).LogWarning(
            Arg.Is<string>(op => op.Contains("Sparkline", StringComparison.Ordinal)),
            Arg.Any<string>(),
            Arg.Any<Exception>());
        await renderer.DidNotReceive().RenderAsync(
            Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAudioAsync_EmailRenderer_IsSkippedWithoutWarningNoise()
    {
        // EmailRenderer is deliberately on-demand only; skipping it is correct
        // and must not be reported as a problem.
        var renderer = CreateRenderer("Email");
        var ctx = this.CreateService(renderers: [renderer]);

        await ctx.Service.ProcessAudioAsync(this.audioPath, new DateOnly(2026, 9, 3));

        ctx.Logger.DidNotReceive().LogWarning(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception>());
        await renderer.DidNotReceive().RenderAsync(
            Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>());
    }

    private static IEntryRenderer CreateRenderer(string name)
    {
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns(name);
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .Returns(new RenderResult([0x01], "application/octet-stream", $"entry.{name}"));
        return renderer;
    }

    private Context CreateService(IEnumerable<IEntryRenderer>? renderers = null)
    {
        var transcriber = Substitute.For<IAudioTranscriber>();
        transcriber.IsAvailable.Returns(true);
        transcriber.TranscribeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptionResult("Hallo Welt, dies ist ein Test.", 3.0, 6));

        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("Generierter Abschnitt.");

        var repo = Substitute.For<IEntryRepository>();
        repo.GetNextSequenceNumberAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(1);

        var settings = new SettingsHolder(AppSettings.Default with
        {
            Archivverzeichnis = Path.Combine(this.tempDir, "archiv"),
        });

        var logger = Substitute.For<IEntryProcessingLogger>();

        var service = new EntryProcessingService(
            transcriber,
            new SummaryGenerator(llm, settings),
            new HeaderParser(),
            repo,
            outputRoot: Path.Combine(this.tempDir, "out"),
            overviewService: null,
            settings: settings,
            renderers: renderers ?? [],
            logger: logger);

        return new Context(service, repo, llm, transcriber, logger);
    }

    private sealed record Context(
        EntryProcessingService Service,
        IEntryRepository Repo,
        ILlmProvider Llm,
        IAudioTranscriber Transcriber,
        IEntryProcessingLogger Logger);
}
