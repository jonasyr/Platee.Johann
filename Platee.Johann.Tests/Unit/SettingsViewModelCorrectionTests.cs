using System.Collections.ObjectModel;
using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;
using Xunit;

namespace Platee.Johann.Tests.Unit;

public sealed class SettingsViewModelCorrectionTests
{
    private static SettingsViewModel CreateSut(AppSettings? settings = null)
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);
        var holder = new SettingsHolder(settings ?? AppSettings.Default);
        return new SettingsViewModel(repo, holder);
    }

    [Fact]
    public void LoadFromHolder_PopulatesKorrekturen()
    {
        var corrections = new List<CorrectionEntry>
        {
            new() { Wrong = "Piano", Correct = "Peano" },
            new() { Wrong = "Nele", Correct = "Neele" },
        };
        var settings = AppSettings.Default with { Korrekturliste = corrections };
        var sut = CreateSut(settings);

        sut.Korrekturen.Should().HaveCount(2);
        sut.Korrekturen[0].Wrong.Should().Be("Piano");
        sut.Korrekturen[0].Correct.Should().Be("Peano");
        sut.Korrekturen[1].Wrong.Should().Be("Nele");
        sut.Korrekturen[1].Correct.Should().Be("Neele");
    }

    [Fact]
    public void AddCorrection_AddsEmptyRow()
    {
        var sut = CreateSut(AppSettings.Default with { Korrekturliste = [] });
        sut.Korrekturen.Should().BeEmpty();

        sut.AddCorrectionCommand.Execute(null);

        sut.Korrekturen.Should().HaveCount(1);
        sut.Korrekturen[0].Wrong.Should().BeEmpty();
        sut.Korrekturen[0].Correct.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCorrection_RemovesGivenEntry()
    {
        var corrections = new List<CorrectionEntry>
        {
            new() { Wrong = "Piano", Correct = "Peano" },
            new() { Wrong = "Nele", Correct = "Neele" },
        };
        var sut = CreateSut(AppSettings.Default with { Korrekturliste = corrections });

        var toRemove = sut.Korrekturen[0];
        sut.RemoveCorrectionCommand.Execute(toRemove);

        sut.Korrekturen.Should().HaveCount(1);
        sut.Korrekturen[0].Wrong.Should().Be("Nele");
    }

    [Fact]
    public async Task SaveAsync_PersistsKorrekturlisteToSettings()
    {
        var repo = Substitute.For<ISettingsRepository>();
        AppSettings? saved = null;
        repo.SaveAsync(Arg.Do<AppSettings>(s => saved = s)).Returns(Task.CompletedTask);
        var settings = AppSettings.Default with { Korrekturliste = [] };
        var holder = new SettingsHolder(settings);
        var sut = new SettingsViewModel(repo, holder);

        sut.AddCorrectionCommand.Execute(null);
        sut.Korrekturen[0].Wrong = "Piano";
        sut.Korrekturen[0].Correct = "Peano";

        sut.SaveCommand.Execute(null);
        // Allow async command to complete
        await Task.Delay(200);

        saved.Should().NotBeNull();
        saved!.Korrekturliste.Should().HaveCount(1);
        saved.Korrekturliste[0].Wrong.Should().Be("Piano");
        saved.Korrekturliste[0].Correct.Should().Be("Peano");
    }

    [Fact]
    public async Task SaveAsync_FiltersOutEmptyWrongEntries()
    {
        var repo = Substitute.For<ISettingsRepository>();
        AppSettings? saved = null;
        repo.SaveAsync(Arg.Do<AppSettings>(s => saved = s)).Returns(Task.CompletedTask);
        var settings = AppSettings.Default with { Korrekturliste = [] };
        var holder = new SettingsHolder(settings);
        var sut = new SettingsViewModel(repo, holder);

        sut.AddCorrectionCommand.Execute(null);
        sut.Korrekturen[0].Wrong = "";
        sut.Korrekturen[0].Correct = "Peano";

        sut.AddCorrectionCommand.Execute(null);
        sut.Korrekturen[1].Wrong = "Nele";
        sut.Korrekturen[1].Correct = "Neele";

        sut.SaveCommand.Execute(null);
        await Task.Delay(200);

        saved.Should().NotBeNull();
        saved!.Korrekturliste.Should().HaveCount(1);
        saved.Korrekturliste[0].Wrong.Should().Be("Nele");
    }

    [Fact]
    public void Reset_RestoresDefaultKorrekturen()
    {
        var corrections = new List<CorrectionEntry>
        {
            new() { Wrong = "Piano", Correct = "Peano" },
            new() { Wrong = "Extra", Correct = "Etwas" },
        };
        var sut = CreateSut(AppSettings.Default with { Korrekturliste = corrections });
        sut.Korrekturen.Should().HaveCount(2);

        sut.ResetCommand.Execute(null);

        sut.Korrekturen.Should().HaveCount(4);
        sut.Korrekturen[0].Wrong.Should().Be("Piano");
        sut.Korrekturen[1].Wrong.Should().Be("Nele");
        sut.Korrekturen[2].Wrong.Should().Be("JATJPT");
        sut.Korrekturen[3].Wrong.Should().Be("JGPT");
    }

    [Fact]
    public void Sections_ContainsKorrekturliste()
    {
        var sut = CreateSut();

        sut.Sections.Should().Contain(s => s.Key == "korrekturliste");
    }

    [Fact]
    public void IsKorrekturlisteSelected_WhenSectionSelected_ReturnsTrue()
    {
        var sut = CreateSut();
        var section = sut.Sections.First(s => s.Key == "korrekturliste");

        sut.SelectedSection = section;

        sut.IsKorrekturlisteSelected.Should().BeTrue();
    }
}
