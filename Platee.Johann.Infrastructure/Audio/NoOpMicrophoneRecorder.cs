using Platee.Johann.Application.Interfaces;

namespace Platee.Johann.Infrastructure.Audio;

public sealed class NoOpMicrophoneRecorder : IMicrophoneRecorder
{
    public bool IsMicrophoneAvailable => false;

    public Task StartAsync(string outputFilePath, CancellationToken ct = default)
        => throw new InvalidOperationException("Kein Mikrofon verfügbar.");

    public Task StopAsync() => Task.CompletedTask;
}
