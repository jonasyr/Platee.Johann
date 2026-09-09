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

/// <summary>
/// Covers the on-demand single-section entry point and its double-click guard.
/// <para>
/// A CanExecute flag in the UI is not sufficient for this: two clicks can be dispatched
/// before the first command completes, and each wasted call costs real money. The guard
/// has to live in the processor.
/// </para>
/// </summary>
public sealed class SectionGenerationCoalescingTests : IDisposable
{
    private readonly string tempDir;

    public SectionGenerationCoalescingTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannCoalescing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateSectionAsync_CalledTwiceConcurrently_IssuesOneLlmCall()
    {
        var gate = new TaskCompletionSource<string>();
        var callCount = 0;
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref callCount);
                return gate.Task;
            });

        var (service, _) = this.CreateService(llm);
        var entry = MakeEntry();

        var first = service.GenerateSectionAsync(entry, BuiltInSections.TaskList);
        var second = service.GenerateSectionAsync(entry, BuiltInSections.TaskList);

        gate.SetResult("AUFGABEN-TEXT");
        var results = await Task.WhenAll(first, second);

        callCount.Should().Be(1, "the second call must await the first, not spend a second GPT call");
        results[0].TaskList.Should().Be("AUFGABEN-TEXT");
        results[1].TaskList.Should().Be("AUFGABEN-TEXT");
    }

    [Fact]
    public async Task GenerateSectionAsync_AfterCompletion_AllowsARegeneration()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("ERST", "ZWEIT");

        var (service, _) = this.CreateService(llm);

        var one = await service.GenerateSectionAsync(MakeEntry(), BuiltInSections.TaskList);
        var two = await service.GenerateSectionAsync(one, BuiltInSections.TaskList);

        one.TaskList.Should().Be("ERST");
        two.TaskList.Should().Be("ZWEIT", "the in-flight entry must be released when the call completes");
    }

    [Fact]
    public async Task GenerateSectionAsync_DifferentSections_RunIndependently()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("TEXT");

        var (service, _) = this.CreateService(llm);
        var entry = MakeEntry();

        var a = await service.GenerateSectionAsync(entry, BuiltInSections.TaskList);
        var b = await service.GenerateSectionAsync(a, BuiltInSections.Analog);

        b.TaskList.Should().Be("TEXT");
        b.AnalogText.Should().Be("TEXT");
    }

    [Fact]
    public async Task GenerateSectionAsync_CustomCategory_IsMergedIntoCustomSections()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("PROGRAMMIER-TEXT");

        var category = new CategoryDefinition
        {
            Id = "custom.prog", Name = "Programmierung", Prompt = "{transcript}",
        };
        var (service, _) = this.CreateService(
            llm, PromptSettings.Default with { CustomCategories = [category] });

        var result = await service.GenerateSectionAsync(MakeEntry(), "custom.prog");

        result.CustomSections["custom.prog"].Should().Be("PROGRAMMIER-TEXT");
    }

    [Fact]
    public async Task GenerateSectionAsync_PersistsTheResult()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("TEXT");

        var (service, repo) = this.CreateService(llm);

        await service.GenerateSectionAsync(MakeEntry(), BuiltInSections.TaskList);

        await repo.Received(1).SaveAsync(
            Arg.Is<Entry>(e => e.TaskList == "TEXT"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSectionAsync_UnknownSectionId_Throws()
    {
        var (service, _) = this.CreateService(Substitute.For<ILlmProvider>());

        Func<Task> act = () => service.GenerateSectionAsync(MakeEntry(), "custom.gibtsnicht");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateSectionAsync_WithoutTranscript_Throws()
    {
        var (service, _) = this.CreateService(Substitute.For<ILlmProvider>());
        var entry = MakeEntry() with { Transcript = null, EditedTranscript = null };

        Func<Task> act = () => service.GenerateSectionAsync(entry, BuiltInSections.TaskList);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateSectionAsync_UsesTheEditedTranscriptWhenPresent()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("TEXT");

        var (service, _) = this.CreateService(llm);
        var entry = MakeEntry() with { EditedTranscript = "KORRIGIERTES TRANSKRIPT" };

        await service.GenerateSectionAsync(entry, BuiltInSections.TaskList);

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(u => u.Contains("KORRIGIERTES TRANSKRIPT")),
            Arg.Any<LlmOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static Entry MakeEntry() => new()
    {
        JobId = "260907_001_abc",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.FromHours(1)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Test",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "Ein Transkript.",
    };

    private (EntryProcessingService Service, IEntryRepository Repo) CreateService(
        ILlmProvider llm, PromptSettings? prompts = null)
    {
        var transcriber = Substitute.For<IAudioTranscriber>();
        var repo = Substitute.For<IEntryRepository>();
        var settings = new SettingsHolder(AppSettings.Default, prompts ?? PromptSettings.Default);

        var service = new EntryProcessingService(
            transcriber,
            new SummaryGenerator(llm, settings),
            new HeaderParser(),
            repo,
            outputRoot: Path.Combine(this.tempDir, "out"),
            overviewService: null,
            settings: settings,
            renderers: [],
            logger: Substitute.For<IEntryProcessingLogger>());

        return (service, repo);
    }
}
