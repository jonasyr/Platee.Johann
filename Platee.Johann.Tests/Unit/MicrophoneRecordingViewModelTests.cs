using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.Tests.Unit;

public sealed class MicrophoneRecordingViewModelTests
{
    private readonly IEntryRepository repository;
    private readonly IEntryProcessor processor;
    private readonly IMicrophoneRecorder recorder;

    public MicrophoneRecordingViewModelTests()
    {
        this.repository = Substitute.For<IEntryRepository>();
        this.processor = Substitute.For<IEntryProcessor>();
        this.recorder = Substitute.For<IMicrophoneRecorder>();
    }

    private MainViewModel CreateVm()
    {
        var settingsRepo = Substitute.For<ISettingsRepository>();
        var holder = new SettingsHolder(new AppSettings(), PromptSettings.Default);
        return new MainViewModel(
            this.repository,
            [],
            string.Empty,
            this.processor,
            settingsRepo,
            holder,
            holder,
            this.recorder);
    }

    [Fact]
    public async Task StartDictation_WhenApiKeyMissing_DoesNotStartRecording()
    {
        this.processor.CanProcess.Returns(false);
        var vm = this.CreateVm();

        await vm.StartDictationCommand.ExecuteAsync(null);

        vm.IsRecording.Should().BeFalse();
        await this.recorder.DidNotReceive().StartAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task StartDictation_WhenApiKeyMissing_SetsErrorMessage()
    {
        this.processor.CanProcess.Returns(false);
        var vm = this.CreateVm();

        await vm.StartDictationCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StartDictation_WhenMicrophoneUnavailable_DoesNotStartRecording()
    {
        this.processor.CanProcess.Returns(true);
        this.recorder.IsMicrophoneAvailable.Returns(false);
        var vm = this.CreateVm();

        await vm.StartDictationCommand.ExecuteAsync(null);

        vm.IsRecording.Should().BeFalse();
        await this.recorder.DidNotReceive().StartAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task StartDictation_WhenSuccess_SetsIsRecordingTrue()
    {
        this.processor.CanProcess.Returns(true);
        this.recorder.IsMicrophoneAvailable.Returns(true);
        this.recorder.StartAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        var vm = this.CreateVm();

        await vm.StartDictationCommand.ExecuteAsync(null);

        vm.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task StartDictation_WhenSuccess_DisablesStartCommandAndEnablesStopCommand()
    {
        this.processor.CanProcess.Returns(true);
        this.recorder.IsMicrophoneAvailable.Returns(true);
        this.recorder.StartAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        var vm = this.CreateVm();

        await vm.StartDictationCommand.ExecuteAsync(null);

        vm.StartDictationCommand.CanExecute(null).Should().BeFalse();
        vm.StopDictationCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task StartDictation_WhenRecorderThrows_IsRecordingRemainsFalse()
    {
        this.processor.CanProcess.Returns(true);
        this.recorder.IsMicrophoneAvailable.Returns(true);
        this.recorder.StartAsync(Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("device busy")));
        var vm = this.CreateVm();

        await vm.StartDictationCommand.ExecuteAsync(null);

        vm.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void StopDictationCommand_CannotExecute_WhenNotRecording()
    {
        var vm = this.CreateVm();

        vm.StopDictationCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void StartDictationCommand_CanExecute_WhenNotRecording()
    {
        var vm = this.CreateVm();

        vm.StartDictationCommand.CanExecute(null).Should().BeTrue();
    }
}
