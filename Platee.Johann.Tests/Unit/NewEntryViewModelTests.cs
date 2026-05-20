using FluentAssertions;
using Platee.Johann.Domain.Enums;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.Tests.Unit;

public sealed class NewEntryViewModelTests
{
    [Fact]
    public void SaveCommand_CannotExecute_WhenRequiredFieldsAreMissing()
    {
        var sut = new NewEntryViewModel(1, new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero));

        sut.SaveCommand.CanExecute(null).Should().BeFalse();

        sut.ProjectName = "Projekt";
        sut.SaveCommand.CanExecute(null).Should().BeFalse();

        sut.ProjectName = string.Empty;
        sut.TitleText = "Titel";
        sut.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_CreatesEntryAndSetsDialogResult_WhenRequiredFieldsArePresent()
    {
        var createdAt = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var sut = new NewEntryViewModel(7, createdAt)
        {
            SelectedType = EntryType.Aufgabe,
            ProjectName = "  Alpha_1  ",
            TitleText = "  Quarterly Review  ",
            Content = "  some transcript  ",
        };

        sut.SaveCommand.CanExecute(null).Should().BeTrue();

        sut.SaveCommand.Execute(null);

        sut.DialogResult.Should().BeTrue();
        sut.CreatedEntry.Should().NotBeNull();
        sut.CreatedEntry!.SequenceNumber.Should().Be(7);
        sut.CreatedEntry.Type.Should().Be(EntryType.Aufgabe);
        sut.CreatedEntry.ProjectName.Should().Be("Alpha_1");
        sut.CreatedEntry.Title.Should().Be("Quarterly Review");
        sut.CreatedEntry.Transcript.Should().Be("some transcript");
        sut.CreatedEntry.CreatedAt.Should().Be(createdAt);
        sut.CreatedEntry.JobId.Should().StartWith("260515_007_Alpha_1_");
    }
}
