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
        this.globalRepo.LastLoadReadFile.Returns(true);

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

    [Fact]
    public async Task LoadWithFallbackAsync_WhenGlobalLoadFaulted_FallsBackToLocalInsteadOfDefaults()
    {
        // A repository that answers a corrupt file with built-in defaults looks
        // exactly like a successful load; only LastLoadFault tells them apart.
        var local = PromptSettings.Default with { SystemMessage = "local-msg" };
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(local);
        this.globalRepo.IsReachable.Returns(true);
        this.globalRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        this.globalRepo.LastLoadFault.Returns(
            new SettingsFileFault("prompts.json", "prompts.corrupt.json", "unexpected token"));

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.Settings.SystemMessage.Should().Be("local-msg");
        result.Source.Should().Be(PromptSource.GlobalFallbackToLocal);
        result.FallbackReason.Should().Be("unexpected token");
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenGlobalSucceeds_ReportsNoFallbackReason()
    {
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        this.globalRepo.IsReachable.Returns(true);
        this.globalRepo.LastLoadReadFile.Returns(true);
        this.globalRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        this.globalRepo.LastLoadFault.Returns((SettingsFileFault?)null);

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.Source.Should().Be(PromptSource.Global);
        result.FallbackReason.Should().BeNull();
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenUnreachable_ExplainsWhy()
    {
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        this.globalRepo.IsReachable.Returns(false);

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.FallbackReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenGlobalVanishedBetweenProbeAndRead_FallsBackToLocal()
    {
        // The share dropped after IsReachable said yes. The repository answers with
        // built-in defaults and no fault, which is indistinguishable from a genuine
        // load of an empty configuration — and caching it would destroy the last
        // known good team prompts.
        var local = PromptSettings.Default with { SystemMessage = "local-msg" };
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(local);
        this.globalRepo.IsReachable.Returns(true);
        this.globalRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        this.globalRepo.LastLoadFault.Returns((SettingsFileFault?)null);
        this.globalRepo.LastLoadReadFile.Returns(false);

        var result = await PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        result.Settings.SystemMessage.Should().Be("local-msg");
        result.Source.Should().Be(PromptSource.GlobalFallbackToLocal);
        result.FallbackReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoadWithFallbackAsync_WhenCancelled_PropagatesInsteadOfFallingBack()
    {
        this.localRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        this.globalRepo.IsReachable.Returns(true);
        this.globalRepo.LoadAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => PromptSettingsLoader.LoadWithFallbackAsync(this.localRepo, this.globalRepo);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
