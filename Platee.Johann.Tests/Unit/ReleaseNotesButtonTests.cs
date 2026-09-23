namespace Platee.Johann.Tests.Unit;

using System.Xml.Linq;
using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Audio;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// Release Notes jederzeit über einen Knopf neben „?“ öffnen (#78). Bisher erschienen sie nur
/// beim ersten Start nach einem Update und waren danach nicht mehr erreichbar.
/// </summary>
public sealed class ReleaseNotesButtonTests
{
    private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Fact]
    public void The_command_asks_the_window_to_show_the_release_notes()
    {
        var vm = CreateVm();
        var shown = 0;
        vm.ShowReleaseNotes = () => shown++;

        vm.OpenReleaseNotesCommand.Execute(null);

        shown.Should().Be(1);
    }

    [Fact]
    public void Without_a_window_the_command_does_nothing()
    {
        var vm = CreateVm();

        var act = () => vm.OpenReleaseNotesCommand.Execute(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void The_button_sits_next_to_the_handbook_in_the_quiet_role()
    {
        var header = LoadMainWindow().Descendants(Wpf + "Button").ToList();
        var notes = header.Single(b => (string?)b.Attribute("Command") == "{Binding OpenReleaseNotesCommand}");
        var handbook = header.Single(b => (string?)b.Attribute("Command") == "{Binding OpenHandbookCommand}");

        notes.ElementsAfterSelf().FirstOrDefault().Should().BeSameAs(handbook, "it sits directly before „?“");
        ((string?)notes.Attribute("Style")).Should().Be("{StaticResource QuietButtonStyle}");
        ((string?)notes.Attribute("ToolTip")).Should().NotBeNullOrWhiteSpace();
        ((string?)notes.Attribute("AutomationProperties.Name")).Should().NotBeNullOrWhiteSpace();
        notes.Attributes().Select(a => a.Name.LocalName).Should()
            .NotContain(["Background", "Foreground", "BorderBrush"], "a local colour kills the hover state (#97)");
    }

    private static MainViewModel CreateVm()
    {
        var holder = new SettingsHolder(new AppSettings(), PromptSettings.Default);
        return new MainViewModel(
            Substitute.For<IEntryRepository>(),
            [],
            string.Empty,
            Substitute.For<IEntryProcessor>(),
            Substitute.For<ISettingsRepository>(),
            Substitute.For<IPromptSettingsRepository>(),
            holder,
            holder,
            new NoOpMicrophoneRecorder());
    }

    private static XDocument LoadMainWindow()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Platee.Johann.slnx")))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("the tests run below the repository, next to Platee.Johann.slnx");
        return XDocument.Load(Path.Combine(dir!.FullName, "Platee.Johann.UI", "MainWindow.xaml"));
    }
}
