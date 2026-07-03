namespace Platee.Johann.Application.Interfaces;

public interface IMicrophoneRecorder
{
    bool IsMicrophoneAvailable { get; }

    Task StartAsync(string outputFilePath, CancellationToken ct = default);

    Task StopAsync();
}
