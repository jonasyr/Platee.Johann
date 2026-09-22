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
/// Kopiersymbol je Abschnitt (#56) und der zentrale „Kopieren“-Knopf, der sich seitdem aus
/// denselben Abschnitten zusammensetzt — gleiche Auswahl, Sichtbarkeit und Reihenfolge wie
/// die Detailansicht.
/// </summary>
public sealed class CopySectionTests
{
    [Fact]
    public void Single_builtin_section_is_copied_with_its_heading()
    {
        var vm = CreateVm();

        var text = vm.BuildSectionCopyText(BuiltInSections.LongSummary);

        text.Should().StartWith("ZUSAMMENFASSUNG").And.Contain("KURZFASSUNG-TEXT");
        text.Should().NotContain("AUFGABEN-TEXT", "only the one section is copied");
    }

    [Fact]
    public void Single_custom_section_is_copied_under_its_name()
    {
        var vm = CreateVm();

        var text = vm.BuildSectionCopyText("custom.x");

        text.Should().StartWith("EIGENE KATEGORIE").And.Contain("EIGENER TEXT");
    }

    [Fact]
    public void Single_abstract_is_copied()
    {
        var vm = CreateVm();

        vm.BuildSectionCopyText(EntryDetailViewModel.AbstractSectionId)
            .Should().StartWith("ABSTRACT").And.Contain("ABSTRACT-TEXT");
    }

    [Fact]
    public void Single_transcript_is_copied_verbatim()
    {
        var vm = CreateVm();

        vm.BuildSectionCopyText(EntryDetailViewModel.TranscriptSectionId)
            .Should().StartWith("ORIGINALTRANSKRIPT").And.Contain("Sag mal 3 * 4 und **das** hier.");
    }

    [Fact]
    public void Single_section_drops_markdown_markers()
    {
        var vm = CreateVm();
        vm.Entry = vm.Entry! with { TaskList = "- **Angebot** bis *Montag*" };

        vm.BuildSectionCopyText(BuiltInSections.TaskList)
            .Should().Contain("- Angebot bis Montag").And.NotContain("*");
    }

    [Theory]
    [InlineData("builtin.gibt-es-nicht")]
    [InlineData(BuiltInSections.Analog)]
    public void Unknown_or_empty_section_yields_nothing(string sectionId)
    {
        var vm = CreateVm();
        vm.Entry = vm.Entry! with { AnalogText = "   " };

        vm.BuildSectionCopyText(sectionId).Should().BeNull();
    }

    [Fact]
    public void Copy_icon_is_only_available_for_a_section_with_text()
    {
        // Ein Abschnitt „auf Knopfdruck“, der noch nicht erzeugt wurde, hat nichts zu kopieren.
        var vm = CreateVm();
        vm.Entry = vm.Entry! with { AnalogText = null };

        vm.CopySectionCommand.CanExecute(BuiltInSections.LongSummary).Should().BeTrue();
        vm.CopySectionCommand.CanExecute(BuiltInSections.Analog).Should().BeFalse();
        vm.CopySectionCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Copy_all_includes_stundenzettel_analog_and_email()
    {
        var vm = CreateVm(AllShown());

        var text = vm.BuildCopyText();

        text.Should().Contain("STUNDENZETTEL-TEXT")
            .And.Contain("ANALOG-TEXT")
            .And.Contain("EMAIL-TEXT");
    }

    [Fact]
    public void Copy_all_respects_the_builtin_checkboxes()
    {
        var sections = new SectionVisibilityViewModel { ShowLongSummary = false, ShowEmailText = false };
        var vm = CreateVm(sections);

        var text = vm.BuildCopyText();

        text.Should().NotContain("KURZFASSUNG-TEXT").And.NotContain("EMAIL-TEXT");
        text.Should().Contain("AUFGABEN-TEXT");
    }

    [Fact]
    public void Copy_all_follows_the_detail_view_order()
    {
        var vm = CreateVm(AllShown());

        var text = vm.BuildCopyText()!;

        string[] expectedOrder =
        [
            "ABSTRACT-TEXT", "KURZFASSUNG-TEXT", "AUSFUEHRLICH-TEXT", "AUFGABEN-TEXT",
            "NOTIZ-TEXT", "STUNDENZETTEL-TEXT", "ANALOG-TEXT", "EMAIL-TEXT", "EIGENER TEXT",
            "Sag mal 3 * 4",
        ];
        expectedOrder.Select(marker => text.IndexOf(marker, StringComparison.Ordinal))
            .Should().OnlyContain(i => i >= 0).And.BeInAscendingOrder();
    }

    [Fact]
    public void Copy_all_contains_every_single_section_text()
    {
        // Eine Wahrheit: der zentrale Knopf setzt sich aus den Einzelkopien zusammen.
        var vm = CreateVm();

        var all = vm.BuildCopyText()!;

        all.Should().Contain(vm.BuildSectionCopyText(BuiltInSections.ConversationNote)!);
        all.Should().Contain(vm.BuildSectionCopyText("custom.x")!);
    }

    // E-Mail, Stundenzettel, Analog und Transkript sind ab Werk ausgeblendet.
    private static SectionVisibilityViewModel AllShown() => new()
    {
        ShowEmailText = true,
        ShowStundenzettelText = true,
        ShowAnalogText = true,
        ShowTranscript = true,
    };

    private static EntryDetailViewModel CreateVm(SectionVisibilityViewModel? sections = null)
    {
        var catalog = SectionCatalog.Build(
            PromptSettings.Default with
            {
                CustomCategories =
                [
                    new CategoryDefinition
                    {
                        Id = "custom.x", Name = "Eigene Kategorie", Prompt = "{transcript}",
                    },
                ],
            },
            new Dictionary<string, GenerationMode>());

        return new EntryDetailViewModel(
            [],
            string.Empty,
            Substitute.For<IEntryProcessor>(),
            Substitute.For<IEntryRepository>(),
            sections: sections,
            addLog: (message, running) => new ProcessLogItem(message, DateTime.Now, running),
            sectionCatalog: () => catalog)
        {
            Entry = new Entry
            {
                JobId = "260922_001_abc",
                SequenceNumber = 1,
                CreatedAt = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.FromHours(2)),
                Type = EntryType.Projekt,
                ProjectName = "Johann",
                Title = "Test",
                SourceType = "audio",
                Status = new ProcessingStatus(true, true, false, false, false),
                Transcript = "Sag mal 3 * 4 und **das** hier.",
                Abstract = "ABSTRACT-TEXT",
                LongSummary = "KURZFASSUNG-TEXT",
                ProseSummary = "AUSFUEHRLICH-TEXT",
                TaskList = "AUFGABEN-TEXT",
                ConversationNote = "NOTIZ-TEXT",
                StundenzettelText = "STUNDENZETTEL-TEXT",
                AnalogText = "ANALOG-TEXT",
                EmailText = "EMAIL-TEXT",
                CustomSections = new Dictionary<string, string> { ["custom.x"] = "EIGENER TEXT" },
            },
        };
    }
}
