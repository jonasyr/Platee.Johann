using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// "Standard wiederherstellen" muss unter JOHANN_HOME auf die vom Aufrufer uebergebenen Pfade
/// zurueckfallen statt auf AppSettings.Default (Documents\Johann, Z:\...\prompts.json) — sonst
/// zoege Reset + Speichern eine Sandbox heimlich auf die echten Ordner (#111 Fix-Runde 2).
/// </summary>
public sealed class SettingsViewModelDefaultsTests
{
    private static IPromptSettingsRepository CreatePromptRepo()
    {
        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo.SaveAsync(Arg.Any<PromptSettings>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return promptRepo;
    }

    private static SettingsViewModel CreateSut(AppSettings? defaults = null)
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);
        var holder = new SettingsHolder(AppSettings.Default);
        return new SettingsViewModel(repo, CreatePromptRepo(), holder, defaults: defaults);
    }

    [Fact]
    public void Reset_WithCustomDefaults_UsesGivenPathsAndClearsGlobalPromptFilePath()
    {
        var customDefaults = new AppSettings
        {
            Quellverzeichnis = @"C:\Sandbox\Eingang",
            Archivverzeichnis = @"C:\Sandbox\Eingang\Archiv",
            Ausgabeverzeichnis = @"C:\Sandbox\output",
            GlobalPromptFilePath = null,
        };
        var sut = CreateSut(customDefaults);

        sut.ResetCommand.Execute(null);

        sut.Quellverzeichnis.Should().Be(@"C:\Sandbox\Eingang");
        sut.Archivverzeichnis.Should().Be(@"C:\Sandbox\Eingang\Archiv");
        sut.Ausgabeverzeichnis.Should().Be(@"C:\Sandbox\output");
        sut.GlobalPromptFilePath.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Reset_WithoutDefaults_StillUsesAppSettingsDefault()
    {
        var sut = CreateSut();

        sut.ResetCommand.Execute(null);

        sut.Quellverzeichnis.Should().Be(AppSettings.Default.Quellverzeichnis);
        sut.Archivverzeichnis.Should().Be(AppSettings.Default.Archivverzeichnis);
        sut.Ausgabeverzeichnis.Should().Be(AppSettings.Default.Ausgabeverzeichnis);
        sut.GlobalPromptFilePath.Should().Be(AppSettings.Default.GlobalPromptFilePath);
    }
}
