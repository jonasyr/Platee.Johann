namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// The custom-section rows of the detail view: which rows exist for a given entry, and
/// what happens when „Generieren“ succeeds or fails.
/// </summary>
public sealed class EntryDetailSectionRowsTests
{
    private static CategoryDefinition Category(string id, string name, int order = 0) => new()
    {
        Id = id,
        Name = name,
        Prompt = "{transcript}",
        Order = order,
    };

    private static IReadOnlyList<SectionDescriptor> Catalog(params CategoryDefinition[] categories) =>
        SectionCatalog.Build(
            PromptSettings.Default with { CustomCategories = categories },
            new Dictionary<string, GenerationMode>());

    private static Entry CreateEntry(Dictionary<string, string>? customSections = null) => new()
    {
        JobId = "260907_001_abc",
        SequenceNumber = 1,
        CreatedAt = DateTimeOffset.Now,
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Test",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "transkript",
        CustomSections = customSections ?? [],
    };

    private static EntryDetailViewModel CreateVm(
        IEntryProcessor processor,
        IReadOnlyList<SectionDescriptor> catalog,
        List<string>? log = null) =>
        new(
            [],
            string.Empty,
            processor,
            Substitute.For<IEntryRepository>(),
            sections: null,
            addLog: (message, running) =>
            {
                log?.Add(message);
                return new ProcessLogItem(message, DateTime.Now, running);
            },
            completeLog: (_, result) => log?.Add(result),
            updateStatus: null,
            sectionCatalog: () => catalog);

    private static IEntryProcessor CreateProcessor()
    {
        var processor = Substitute.For<IEntryProcessor>();
        processor.CanProcess.Returns(true);
        return processor;
    }

    [Fact]
    public void SectionRows_ContainOneRowPerConfiguredCategory()
    {
        var vm = CreateVm(
            CreateProcessor(),
            Catalog(Category("custom.prog", "Programmierung"), Category("custom.idee", "Ideen", 1)));

        vm.Entry = CreateEntry();

        vm.SectionRows.Select(r => r.Id)
            .Should().Equal("custom.prog", "custom.idee");
        vm.SectionRows.Should().OnlyContain(r => r.IsConfigured);
        vm.HasSectionRows.Should().BeTrue();
    }

    [Fact]
    public void SectionRows_CarryTheGeneratedTextOfTheEntry()
    {
        var vm = CreateVm(CreateProcessor(), Catalog(Category("custom.prog", "Programmierung")));

        vm.Entry = CreateEntry(new Dictionary<string, string> { ["custom.prog"] = "PROG-TEXT" });

        vm.SectionRows.Should().ContainSingle();
        vm.SectionRows[0].Text.Should().Be("PROG-TEXT");
        vm.SectionRows[0].IsGenerated.Should().BeTrue();
    }

    [Fact]
    public void SectionRows_IncludeAnOrphanRowForADeletedCategory()
    {
        var vm = CreateVm(CreateProcessor(), Catalog(Category("custom.prog", "Programmierung")));

        vm.Entry = CreateEntry(new Dictionary<string, string>
        {
            ["custom.prog"] = "PROG-TEXT",
            ["custom.weg"] = "VERWAISTER TEXT",
        });

        var orphan = vm.SectionRows.Single(r => r.Id == "custom.weg");
        orphan.IsOrphaned.Should().BeTrue();
        orphan.Text.Should().Be("VERWAISTER TEXT", "deleting a category must not delete its output");
    }

    [Fact]
    public void SectionRows_AreEmptyWhenNoEntryIsSelected()
    {
        var vm = CreateVm(CreateProcessor(), Catalog(Category("custom.prog", "Programmierung")));

        vm.Entry = CreateEntry();
        vm.Entry = null;

        vm.SectionRows.Should().BeEmpty();
        vm.HasSectionRows.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateSection_CallsTheProcessorWithTheStableIdAndAdoptsTheResult()
    {
        var processor = CreateProcessor();
        var generated = CreateEntry(new Dictionary<string, string> { ["custom.prog"] = "FRISCH" });
        processor
            .GenerateSectionAsync(
                Arg.Any<Entry>(), "custom.prog", Arg.Any<IProgress<ProcessingProgress>>(), Arg.Any<CancellationToken>())
            .Returns(generated);

        var vm = CreateVm(processor, Catalog(Category("custom.prog", "Programmierung")));
        vm.Entry = CreateEntry();

        await vm.GenerateSectionCommand.ExecuteAsync("custom.prog");

        vm.Entry!.CustomSections["custom.prog"].Should().Be("FRISCH");
        vm.SectionRows.Single().Text.Should().Be("FRISCH");
    }

    [Fact]
    public async Task GenerateSection_Failing_SurfacesTheErrorAndClearsTheBusyFlag()
    {
        var processor = CreateProcessor();
        processor
            .GenerateSectionAsync(
                Arg.Any<Entry>(), Arg.Any<string>(), Arg.Any<IProgress<ProcessingProgress>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("LLM nicht erreichbar"));

        var log = new List<string>();
        var vm = CreateVm(processor, Catalog(Category("custom.prog", "Programmierung")), log);
        vm.Entry = CreateEntry();

        await vm.GenerateSectionCommand.ExecuteAsync("custom.prog");

        // #45: a failure that only cleared the spinner would be indistinguishable from an
        // empty section.
        log.Should().Contain(m => m.Contains("Fehler") && m.Contains("LLM nicht erreichbar"));
        vm.SectionRows.Single().IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RenderOptions_CarryTheCategoryNamesAndVisibility()
    {
        // Without this wiring an export prints raw ids as headings and the export
        // checkboxes cannot hide a custom section at all.
        RenderOptions? captured = null;
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer
            .RenderAsync(Arg.Any<Entry>(), Arg.Do<RenderOptions>(o => captured = o), Arg.Any<CancellationToken>())
            .Returns(new RenderResult([], "application/pdf", "entry.pdf"));

        var visibility = new SectionVisibilityViewModel();
        visibility.CustomSectionVisibility["custom.prog"] = false;

        var vm = new EntryDetailViewModel(
            [renderer],
            Path.GetTempPath(),
            CreateProcessor(),
            Substitute.For<IEntryRepository>(),
            visibility,
            sectionCatalog: () => Catalog(Category("custom.prog", "Programmierung")));

        await vm.RenderPdfForDragAsync(CreateEntry(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CustomSectionNames.Should().ContainKey("custom.prog")
            .WhoseValue.Should().Be("Programmierung");
        captured.CustomSectionVisibility.Should().ContainKey("custom.prog")
            .WhoseValue.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateSection_ForABuiltIn_UsesItsIdNotItsGermanName()
    {
        var processor = CreateProcessor();
        var entry = CreateEntry();
        processor
            .GenerateSectionAsync(
                Arg.Any<Entry>(), Arg.Any<string>(), Arg.Any<IProgress<ProcessingProgress>>(), Arg.Any<CancellationToken>())
            .Returns(entry);

        var vm = CreateVm(processor, Catalog());
        vm.Entry = entry;

        await vm.GenerateSectionCommand.ExecuteAsync(BuiltInSections.TaskList);

        await processor.Received(1).GenerateSectionAsync(
            Arg.Any<Entry>(),
            BuiltInSections.TaskList,
            Arg.Any<IProgress<ProcessingProgress>>(),
            Arg.Any<CancellationToken>());
    }
}
