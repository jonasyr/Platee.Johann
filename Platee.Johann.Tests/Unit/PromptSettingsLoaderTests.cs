using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Services;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Tests.Unit;

public class PromptSettingsLoaderTests
{
    private readonly IPromptSettingsRepository localRepo = Substitute.For<IPromptSettingsRepository>();
    private readonly IPromptSettingsRepository globalRepo = Substitute.For<IPromptSettingsRepository>();

    [Fact]
    public async Task LoadWithFallbackAsync_WhenNoGlobalRepo_ReturnsLocalWithLocalSource()
    {
        var local = PromptSettings.Default with { SystemMessage = "local-msg" };
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(local);

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, globalRepo: null);

        result.Settings.SystemMessage.Should().Be("local-msg");
        result.Source.Should().Be(PromptSource.Local);
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenGlobalIsReachable_ReturnsGlobalWithGlobalSource()
    {
        var local = PromptSettings.Default with { SystemMessage = "local-msg" };
        var global = PromptSettings.Default with { SystemMessage = "global-msg" };
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(local);
        this.globalRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(global);
        this.globalRepo.IsReachable.Returns(true);

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.Settings.SystemMessage.Should().Be("global-msg");
        result.Source.Should().Be(PromptSource.Global);
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenGlobalNotReachable_FallsBackToLocal()
    {
        var local = PromptSettings.Default with { SystemMessage = "local-msg" };
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(local);
        this.globalRepo.IsReachable.Returns(false);

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.Settings.SystemMessage.Should().Be("local-msg");
        result.Source.Should().Be(PromptSource.GlobalFallbackToLocal);
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenGlobalThrows_FallsBackToLocal()
    {
        var local = PromptSettings.Default with { SystemMessage = "local-msg" };
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(local);
        this.globalRepo.IsReachable.Returns(true);
        this.globalRepo.LoadAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("network error"));

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.Settings.SystemMessage.Should().Be("local-msg");
        result.Source.Should().Be(PromptSource.GlobalFallbackToLocal);
    }
}
