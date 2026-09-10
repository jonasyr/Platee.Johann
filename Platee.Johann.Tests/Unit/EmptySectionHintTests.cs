using System.Collections.Generic;
using FluentAssertions;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Überbrückt die Lücke bis #65: die Liste links steuert Sichtbarkeit, nicht Erzeugung.
/// Hakt jemand eine nie erzeugte Vorlage an, passiert scheinbar nichts.
/// </summary>
public sealed class EmptySectionHintTests
{
    [Fact]
    public void Shows_when_the_section_has_no_content()
    {
        EmptySectionHint.ShouldShow(content: null, suppressed: false).Should().BeTrue();
        EmptySectionHint.ShouldShow(content: "", suppressed: false).Should().BeTrue();
        EmptySectionHint.ShouldShow(content: "   ", suppressed: false).Should().BeTrue();
    }

    [Fact]
    public void Stays_quiet_when_the_section_actually_has_content()
    {
        // Hier tut die Checkbox genau, was der Nutzer erwartet — ein Hinweis waere Laerm.
        EmptySectionHint.ShouldShow(content: "Es gibt Aufgaben.", suppressed: false)
            .Should().BeFalse();
    }

    [Fact]
    public void Stays_quiet_once_the_user_asked_not_to_see_it_again()
    {
        EmptySectionHint.ShouldShow(content: null, suppressed: true).Should().BeFalse();
    }

    [Fact]
    public void Finds_the_text_of_a_built_in_section()
    {
        var entry = MakeEntry() with { EmailText = "Sehr geehrte Damen und Herren" };

        EmptySectionHint.ContentFor(entry, nameof(Entry.EmailText))
            .Should().Be("Sehr geehrte Damen und Herren");
        EmptySectionHint.ContentFor(entry, nameof(Entry.AnalogText)).Should().BeNull();
    }

    [Fact]
    public void Finds_the_text_of_a_custom_section_by_id()
    {
        var entry = MakeEntry() with
        {
            CustomSections = new Dictionary<string, string> { ["custom.test-ab12"] = "Inhalt" },
        };

        EmptySectionHint.ContentFor(entry, "custom.test-ab12").Should().Be("Inhalt");
        EmptySectionHint.ContentFor(entry, "custom.gibtesnicht-99zz").Should().BeNull();
    }

    [Fact]
    public void Treats_a_missing_entry_as_having_no_content()
    {
        EmptySectionHint.ContentFor(null, nameof(Entry.TaskList)).Should().BeNull();
    }

    [Fact]
    public void The_message_names_the_workaround()
    {
        // Ein Hinweis, der nur sagt "geht nicht", ist schlimmer als keiner.
        EmptySectionHint.Message.Should().Contain("Neu generieren");
    }

    private static Entry MakeEntry() => new()
    {
        JobId = "260910_001_abcdef12",
        SequenceNumber = 1,
        CreatedAt = new System.DateTimeOffset(2026, 9, 10, 12, 0, 0, System.TimeSpan.Zero),
        Type = EntryType.Projekt,
        ProjectName = "Test",
        Title = "Hinweis-Test",
        SourceType = "audio",
        Status = ProcessingStatus.Empty,
    };
}
