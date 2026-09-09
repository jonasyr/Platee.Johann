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
/// „Kopieren" has to agree with the HTML and PDF exports about what an entry contains.
/// <para>
/// The clipboard text was hand-written per built-in field, so custom categories were absent
/// from it long after the renderers had learned about them — copying an entry silently
/// dropped exactly the sections the user had configured themselves.
/// </para>
/// </summary>
public sealed class CopyIncludesCustomSectionsTests
{
    [Fact]
    public void BuildCopyText_IncludesCustomSectionsUnderTheirName()
    {
        var vm = CreateVm(new Dictionary<string, string> { ["custom.x"] = "EIGENER TEXT" });

        var text = vm.BuildCopyText();

        text.Should().Contain("EIGENER TEXT");
        text.Should().Contain("EIGENE KATEGORIE", "the heading must be the name, not the raw id");
        text.Should().NotContain("custom.x");
    }

    [Fact]
    public void BuildCopyText_RespectsTheSectionCheckboxes()
    {
        var sections = new SectionVisibilityViewModel();
        sections.CustomSectionVisibility["custom.x"] = false;
        var vm = CreateVm(new Dictionary<string, string> { ["custom.x"] = "EIGENER TEXT" }, sections);

        var text = vm.BuildCopyText();

        text.Should().NotContain("EIGENER TEXT", "an unticked section is absent from PDF and HTML too");
    }

    [Fact]
    public void BuildCopyText_SkipsOrphanedEmptySections()
    {
        var vm = CreateVm(new Dictionary<string, string> { ["custom.x"] = "   " });

        vm.BuildCopyText().Should().NotContain("EIGENE KATEGORIE");
    }

    [Fact]
    public void BuildCopyText_StillIncludesTheBuiltInSections()
    {
        var vm = CreateVm([]);

        var text = vm.BuildCopyText();

        text.Should().Contain("ZUSAMMENFASSUNG").And.Contain("KURZFASSUNG-TEXT");
    }

    private static EntryDetailViewModel CreateVm(
        Dictionary<string, string> customSections,
        SectionVisibilityViewModel? sections = null)
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
                JobId = "260909_001_abc",
                SequenceNumber = 1,
                CreatedAt = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.FromHours(2)),
                Type = EntryType.Projekt,
                ProjectName = "Johann",
                Title = "Test",
                SourceType = "audio",
                Status = new ProcessingStatus(true, true, false, false, false),
                Transcript = "Ein Transkript.",
                LongSummary = "KURZFASSUNG-TEXT",
                CustomSections = customSections,
            },
        };
    }
}
