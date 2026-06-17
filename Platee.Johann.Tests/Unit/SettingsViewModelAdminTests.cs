namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;

public class SettingsViewModelAdminTests
{
    private readonly ISettingsRepository settingsRepo = Substitute.For<ISettingsRepository>();

    private SettingsViewModel CreateVm(AppSettings? settings = null, PromptSettings? prompts = null)
    {
        var s = settings ?? AppSettings.Default;
        var p = prompts ?? PromptSettings.Default;
        var holder = new SettingsHolder(s, p);
        return new SettingsViewModel(this.settingsRepo, holder);
    }

    [Fact]
    public void IsAdminMode_DefaultsFalse()
    {
        var vm = this.CreateVm();
        vm.IsAdminMode.Should().BeFalse();
    }

    [Fact]
    public void IsPromptReadOnly_WhenNotAdmin_ReturnsTrue()
    {
        var vm = this.CreateVm();
        vm.IsPromptReadOnly.Should().BeTrue();
    }

    [Fact]
    public void AdminButtonLabel_WhenNotAdmin_ShowsAdmin()
    {
        var vm = this.CreateVm();
        vm.AdminButtonLabel.Should().Be("Admin");
    }

    [Fact]
    public void ActivateAdmin_WithCorrectPassword_EnablesAdminMode()
    {
        var vm = this.CreateVm();
        vm.ActivateAdmin("123");

        vm.IsAdminMode.Should().BeTrue();
        vm.IsPromptReadOnly.Should().BeFalse();
        vm.AdminButtonLabel.Should().Be("Admin aktiv");
    }

    [Fact]
    public void ActivateAdmin_WithWrongPassword_StaysInNormalMode()
    {
        var vm = this.CreateVm();
        vm.ActivateAdmin("wrong");

        vm.IsAdminMode.Should().BeFalse();
        vm.IsPromptReadOnly.Should().BeTrue();
    }

    [Fact]
    public void DeactivateAdmin_ReturnsToNormalMode()
    {
        var vm = this.CreateVm();
        vm.ActivateAdmin("123");
        vm.DeactivateAdmin();

        vm.IsAdminMode.Should().BeFalse();
        vm.IsPromptReadOnly.Should().BeTrue();
    }
}
