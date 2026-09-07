namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;

public sealed class SettingsViewModelSaveTargetTests
{
    private static SettingsViewModel CreateSut()
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo.SaveAsync(Arg.Any<PromptSettings>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var holder = new SettingsHolder(AppSettings.Default, PromptSettings.Default);
        return new SettingsViewModel(repo, promptRepo, holder);
    }

    [Fact]
    public void SaveTarget_DefaultsToPersonal()
    {
        var vm = CreateSut();

        vm.SaveTarget.Should().Be(CategoryScope.Personal);
        vm.IsGlobalTarget.Should().BeFalse();
    }

    [Fact]
    public void IsPromptReadOnly_NoPasswordGateRemains()
    {
        var vm = CreateSut();

        vm.GetType().GetProperty("IsPromptReadOnly").Should().BeNull(
            "the read-only prompt gate is removed together with the admin password");
        vm.GetType().GetProperty("IsAdminMode").Should().BeNull(
            "admin mode is replaced by the explicit save target");
    }

    [Fact]
    public void PromptWarningText_MentionsAllUsers_WhenTargetIsGlobal()
    {
        var vm = CreateSut();

        vm.SaveTarget = CategoryScope.Global;

        vm.IsGlobalTarget.Should().BeTrue();
        vm.PromptWarningText.Should().Contain("alle");
    }

    [Fact]
    public void PromptWarningText_DoesNotMentionAllUsers_WhenTargetIsPersonal()
    {
        var vm = CreateSut();

        vm.PromptWarningText.Should().NotContain("alle Nutzer");
    }
}
