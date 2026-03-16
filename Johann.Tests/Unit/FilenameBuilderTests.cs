using FluentAssertions;
using Johann.Domain.Entities;
using Johann.Domain.Enums;
using Johann.Domain.Services;
using Johann.Domain.ValueObjects;

namespace Johann.Tests.Unit;

public sealed class FilenameBuilderTests
{
    private static Entry MakeEntry(EntryType type, string project, string title, int seq = 1) => new()
    {
        JobId = "test",
        SequenceNumber = seq,
        CreatedAt = new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.FromHours(1)),
        Type = type,
        ProjectName = project,
        Title = title,
        SourceType = "text",
        Status = ProcessingStatus.Empty,
    };

    [Fact]
    public void Build_StandardEntry_ReturnsCorrectFormat()
    {
        var entry = MakeEntry(EntryType.Aufgabe, "Johann", "wir müssen Änderungen vornehmen jetzt");
        var name = FilenameBuilder.Build(entry);

        name.Should().StartWith("260227_001_Johann_");
        name.Should().Contain("wir_müssen_Änderungen_vornehmen_jetzt");
    }

    [Fact]
    public void Build_GesprächsnotizType_ContainsTypSegment()
    {
        var entry = MakeEntry(EntryType.Gesprächsnotiz, "Iris", "kurzes Gespräch über Projekt");
        var name = FilenameBuilder.Build(entry);

        name.Should().Contain("_Gesprächsnotiz_");
    }

    [Fact]
    public void Build_OtherTypes_DoNotContainTypeSegment()
    {
        foreach (var type in new[] { EntryType.Projekt, EntryType.EMail, EntryType.Aufgabe, EntryType.Stundenzettel })
        {
            var entry = MakeEntry(type, "Test", "Titel");
            var name = FilenameBuilder.Build(entry);
            name.Should().NotContain("_Gesprächsnotiz_",
                because: $"type {type} should not include Gesprächsnotiz segment");
        }
    }

    [Fact]
    public void Build_SequenceNumber_IsAlwaysThreeDigits()
    {
        var entry = MakeEntry(EntryType.Projekt, "Test", "Titel", seq: 5);
        var name = FilenameBuilder.Build(entry);
        name.Should().Contain("_005_");
    }

    [Fact]
    public void Build_TitleWithSpecialChars_AreSanitized()
    {
        var entry = MakeEntry(EntryType.Projekt, "Test", "Titel mit: Doppelpunkt");
        var name = FilenameBuilder.Build(entry);
        name.Should().NotContain(":");
    }

    [Fact]
    public void Build_TitleWith6Words_OnlyFirst5Used()
    {
        var entry = MakeEntry(EntryType.Projekt, "Test", "W1 W2 W3 W4 W5 W6");
        var name = FilenameBuilder.Build(entry);
        name.Should().EndWith("W1_W2_W3_W4_W5");
    }
}
