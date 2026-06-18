namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

public sealed class EditableTranscriptTests
{
    private static Entry CreateEntry(string? transcript = "Original text", string? editedTranscript = null) =>
        new()
        {
            JobId = "test_001",
            SequenceNumber = 1,
            CreatedAt = DateTimeOffset.Now,
            Type = EntryType.Projekt,
            ProjectName = "Test",
            Title = "Test",
            SourceType = "audio",
            Status = ProcessingStatus.Empty,
            Transcript = transcript,
            EditedTranscript = editedTranscript,
        };

    [Fact]
    public void EffectiveTranscript_WhenNoEdit_ReturnsOriginal()
    {
        var entry = CreateEntry(transcript: "Original text");
        entry.EffectiveTranscript.Should().Be("Original text");
    }

    [Fact]
    public void EffectiveTranscript_WhenEdited_ReturnsEditedText()
    {
        var entry = CreateEntry(transcript: "Original", editedTranscript: "Corrected");
        entry.EffectiveTranscript.Should().Be("Corrected");
    }

    [Fact]
    public void EffectiveTranscript_WhenBothNull_ReturnsNull()
    {
        var entry = CreateEntry(transcript: null, editedTranscript: null);
        entry.EffectiveTranscript.Should().BeNull();
    }

    [Fact]
    public void SchemaVersion_DefaultsTo3()
    {
        var entry = CreateEntry();
        entry.SchemaVersion.Should().Be(3);
    }

    [Fact]
    public void EditedTranscript_DefaultsToNull()
    {
        var entry = CreateEntry();
        entry.EditedTranscript.Should().BeNull();
    }

    [Fact]
    public void WithExpression_SetsEditedTranscript_PreservesOriginal()
    {
        var original = CreateEntry(transcript: "Whisper output");
        var edited = original with { EditedTranscript = "User correction" };

        edited.Transcript.Should().Be("Whisper output");
        edited.EditedTranscript.Should().Be("User correction");
        edited.EffectiveTranscript.Should().Be("User correction");
    }
}
