namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;

public sealed class EntryDetailTranscriptEditTests
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
            Status = new ProcessingStatus(true, true, false, false, false),
            Transcript = transcript,
            EditedTranscript = editedTranscript,
        };

    private static EntryDetailViewModel CreateVm(IEntryProcessor? processor = null)
    {
        processor ??= Substitute.For<IEntryProcessor>();
        processor.CanProcess.Returns(true);
        var repo = Substitute.For<IEntryRepository>();
        return new EntryDetailViewModel([], "", processor, repo);
    }

    [Fact]
    public void EditTranscriptCommand_SetsEditingMode()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(transcript: "Some text");

        vm.EditTranscriptCommand.Execute(null);

        vm.IsEditingTranscript.Should().BeTrue();
        vm.EditableTranscriptText.Should().Be("Some text");
    }

    [Fact]
    public void EditTranscriptCommand_WhenAlreadyEdited_ShowsEditedText()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(transcript: "Original", editedTranscript: "Previously edited");

        vm.EditTranscriptCommand.Execute(null);

        vm.EditableTranscriptText.Should().Be("Previously edited");
    }

    [Fact]
    public void CancelEditTranscriptCommand_ExitsEditingMode()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(transcript: "Text");
        vm.EditTranscriptCommand.Execute(null);
        vm.EditableTranscriptText = "Changed by user";

        vm.CancelEditTranscriptCommand.Execute(null);

        vm.IsEditingTranscript.Should().BeFalse();
        vm.EditableTranscriptText.Should().BeEmpty();
    }

    [Fact]
    public void OnEntryChanged_ExitsEditingMode()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(transcript: "Text");
        vm.EditTranscriptCommand.Execute(null);

        vm.Entry = CreateEntry(transcript: "Different entry");

        vm.IsEditingTranscript.Should().BeFalse();
    }

    [Fact]
    public void DisplayTranscript_ShowsEffectiveTranscript()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(transcript: "Original", editedTranscript: "Corrected");

        vm.DisplayTranscript.Should().Be("Corrected");
    }

    [Fact]
    public void HasTranscriptBeenEdited_WhenEdited_ReturnsTrue()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(editedTranscript: "Something");

        vm.HasTranscriptBeenEdited.Should().BeTrue();
    }

    [Fact]
    public void HasTranscriptBeenEdited_WhenNotEdited_ReturnsFalse()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry();

        vm.HasTranscriptBeenEdited.Should().BeFalse();
    }

    [Fact]
    public void IsNotEditingTranscript_InverseOfIsEditing()
    {
        var vm = CreateVm();
        vm.Entry = CreateEntry(transcript: "Text");

        vm.IsNotEditingTranscript.Should().BeTrue();

        vm.EditTranscriptCommand.Execute(null);

        vm.IsNotEditingTranscript.Should().BeFalse();
    }
}
