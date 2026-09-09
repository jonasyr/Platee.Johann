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
using Platee.Johann.Infrastructure.Json;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// Text whose category was later deleted must still show a human name.
/// <para>
/// The name is recorded on the entry at generation time rather than looked up in the
/// settings, because by the time it is needed the category is gone — that is precisely
/// the case it exists for. Storing it per entry also keeps it historically honest: the
/// section shows the name it was actually generated under.
/// </para>
/// </summary>
public sealed class CustomSectionNameTombstoneTests : IDisposable
{
    private readonly string tempDir;

    public CustomSectionNameTombstoneTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannTombstone_{Guid.NewGuid():N}");
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
    public async Task GenerateSectionAsync_RecordsTheCategoryNameAlongsideTheText()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("TEXT");

        var service = this.CreateService(llm, Category("custom.prog-1a2b", "Programmierung"));

        var result = await service.GenerateSectionAsync(MakeEntry(), "custom.prog-1a2b");

        result.CustomSectionNames["custom.prog-1a2b"].Should().Be("Programmierung");
    }

    [Fact]
    public async Task EntryRepository_RoundTripsCustomSectionNames()
    {
        // The hand-written EntryDto mapper has silently dropped a new field three times now
        // (CustomCategories, SectionModes, and this map's own sibling CustomSections).
        var repo = new JsonRepository(this.tempDir);
        var entry = MakeEntry() with
        {
            CustomSections = new Dictionary<string, string> { ["custom.prog-1a2b"] = "TEXT" },
            CustomSectionNames = new Dictionary<string, string> { ["custom.prog-1a2b"] = "Programmierung" },
        };

        await repo.SaveAsync(entry);
        var loaded = await repo.GetByJobIdAsync(entry.JobId);

        loaded.Should().NotBeNull();
        loaded!.CustomSectionNames.Should().ContainKey("custom.prog-1a2b")
            .WhoseValue.Should().Be("Programmierung");
    }

    [Fact]
    public void OrphanedRow_UsesTheRecordedNameInsteadOfTheRawId()
    {
        var vm = CreateDetailVm(MakeEntry() with
        {
            CustomSections = new Dictionary<string, string> { ["custom.prog-1a2b"] = "TEXT" },
            CustomSectionNames = new Dictionary<string, string> { ["custom.prog-1a2b"] = "Programmierung" },
        });

        var row = vm.SectionRows.Single(r => r.Id == "custom.prog-1a2b");

        row.IsOrphaned.Should().BeTrue("the category is not in the catalog any more");
        row.Name.Should().Be("Programmierung");
    }

    [Fact]
    public void OrphanedRow_WithoutARecordedName_StillFallsBackToTheId()
    {
        // Sections generated before this field existed have no recorded name.
        var vm = CreateDetailVm(MakeEntry() with
        {
            CustomSections = new Dictionary<string, string> { ["custom.alt"] = "TEXT" },
        });

        vm.SectionRows.Single(r => r.Id == "custom.alt").Name.Should().Be("custom.alt");
    }

    private static CategoryDefinition Category(string id, string name) =>
        new() { Id = id, Name = name, Prompt = "{transcript}" };

    private static Entry MakeEntry() => new()
    {
        JobId = "260909_001_abc",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.FromHours(2)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Test",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "Ein Transkript.",
    };

    private static EntryDetailViewModel CreateDetailVm(Entry entry) =>
        new(
            [],
            string.Empty,
            Substitute.For<IEntryProcessor>(),
            Substitute.For<IEntryRepository>(),
            sectionCatalog: () => SectionCatalog.Build(
                PromptSettings.Default, new Dictionary<string, GenerationMode>()))
        {
            Entry = entry,
        };

    private EntryProcessingService CreateService(ILlmProvider llm, params CategoryDefinition[] categories)
    {
        var settings = new SettingsHolder(
            AppSettings.Default, PromptSettings.Default with { CustomCategories = categories });

        return new EntryProcessingService(
            Substitute.For<IAudioTranscriber>(),
            new SummaryGenerator(llm, settings),
            new HeaderParser(),
            Substitute.For<IEntryRepository>(),
            outputRoot: Path.Combine(this.tempDir, "out"),
            overviewService: null,
            settings: settings,
            renderers: [],
            logger: Substitute.For<IEntryProcessingLogger>());
    }
}
