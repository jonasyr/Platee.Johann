namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// Detail-side edits have to reach the entry list.
/// <para>
/// The list row owns the <see cref="Entry"/> instance the detail view is re-seeded from on
/// every re-selection (<c>MainViewModel.OnSelectedEntryChanged</c> assigns
/// <c>Detail.Entry = value?.Entry</c>). <see cref="Entry"/> is an immutable record, so a
/// detail command that only assigns its own <c>Entry</c> leaves the row pointing at the
/// pre-update instance. Clicking away and back then re-seeds the detail view from that stale
/// row, and the user's generated text appears to have been silently thrown away — even though
/// it is safely on disk.
/// </para>
/// </summary>
public sealed class EntryUpdatePropagationTests
{
    [Fact]
    public async Task GenerateSection_PublishesTheUpdatedEntry()
    {
        var (vm, published) = CreateVm(
            p => p.GenerateSectionAsync(
                    Arg.Any<Entry>(), Arg.Any<string>(),
                    Arg.Any<IProgress<ProcessingProgress>>(), Arg.Any<CancellationToken>())
                .Returns(WithCustomSection("custom.x", "NEUER TEXT")));

        await vm.GenerateSectionCommand.ExecuteAsync("custom.x");

        published.Should().ContainSingle(
            "an on-demand section that is not published to the list row reappears as an "
            + "ungenerated 'Generieren' button the moment the user re-selects the entry");
        published[0].CustomSections["custom.x"].Should().Be("NEUER TEXT");
    }

    [Fact]
    public async Task RegenerateFromTranscript_PublishesTheUpdatedEntry()
    {
        var (vm, published) = CreateVm(
            p => p.RegenerateFromTranscriptAsync(
                    Arg.Any<Entry>(), Arg.Any<string>(),
                    Arg.Any<IProgress<ProcessingProgress>>(), Arg.Any<CancellationToken>())
                .Returns(BaseEntry() with { EditedTranscript = "KORRIGIERT", LongSummary = "NEU" }));

        vm.EditTranscriptCommand.Execute(null);
        vm.EditableTranscriptText = "KORRIGIERT";
        await vm.RegenerateFromTranscriptCommand.ExecuteAsync(null);

        published.Should().NotBeEmpty("a corrected transcript must survive re-selection");
        published[^1].EditedTranscript.Should().Be("KORRIGIERT");
    }

    [Fact]
    public async Task Reprocess_PublishesTheUpdatedEntry()
    {
        var (vm, published) = CreateVm(
            p => p.ReprocessAsync(
                    Arg.Any<Entry>(),
                    Arg.Any<IProgress<ProcessingProgress>>(), Arg.Any<CancellationToken>())
                .Returns(BaseEntry() with { LongSummary = "NEU GENERIERT" }));

        await vm.ReprocessCommand.ExecuteAsync(null);

        published.Should().ContainSingle();
        published[0].LongSummary.Should().Be("NEU GENERIERT");
    }

    [Fact]
    public void EntryRow_UpdateEntry_SwapsTheInstanceAndNotifies()
    {
        var row = new EntryRowViewModel(BaseEntry());
        var changed = new List<string?>();
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        row.UpdateEntry(BaseEntry() with { IsDone = true, Title = "Neuer Titel" });

        row.Entry.Title.Should().Be("Neuer Titel");
        row.IsDone.Should().BeTrue();
        changed.Should().Contain([nameof(EntryRowViewModel.Entry), nameof(EntryRowViewModel.DisplayName)]);
    }

    private static Entry BaseEntry() => new()
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

    private static Entry WithCustomSection(string id, string text) =>
        BaseEntry() with { CustomSections = new Dictionary<string, string> { [id] = text } };

    private static (EntryDetailViewModel Vm, List<Entry> Published) CreateVm(
        Action<IEntryProcessor> configure)
    {
        var processor = Substitute.For<IEntryProcessor>();
        processor.CanProcess.Returns(true);
        configure(processor);

        var catalog = SectionCatalog.Build(
            PromptSettings.Default with
            {
                CustomCategories =
                [
                    new CategoryDefinition { Id = "custom.x", Name = "Eigene", Prompt = "{transcript}" },
                ],
            },
            new Dictionary<string, GenerationMode>());

        var vm = new EntryDetailViewModel(
            [],
            string.Empty,
            processor,
            Substitute.For<IEntryRepository>(),
            sections: null,
            addLog: (message, running) => new ProcessLogItem(message, DateTime.Now, running),
            completeLog: (_, _) => { },
            updateStatus: _ => { },
            sectionCatalog: () => catalog)
        {
            Entry = BaseEntry(),
        };

        var published = new List<Entry>();
        vm.EntryUpdated += published.Add;
        return (vm, published);
    }
}
