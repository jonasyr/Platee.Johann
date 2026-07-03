using NAudio.CoreAudioApi;
using NAudio.Wave;
using Platee.Johann.Application.Interfaces;

namespace Platee.Johann.Infrastructure.Audio;

public sealed class WindowsMicrophoneRecorder : IMicrophoneRecorder, IDisposable
{
    private WasapiCapture? capture;
    private WaveFileWriter? writer;

    public bool IsMicrophoneAvailable
    {
        get
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                return device is not null;
            }
            catch
            {
                return false;
            }
        }
    }

    public Task StartAsync(string outputFilePath, CancellationToken ct = default)
    {
        this.capture = new WasapiCapture();
        this.writer = new WaveFileWriter(outputFilePath, this.capture.WaveFormat);

        this.capture.DataAvailable += (_, e) =>
        {
            this.writer?.Write(e.Buffer, 0, e.BytesRecorded);
        };

        this.capture.StartRecording();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        this.capture?.StopRecording();
        this.writer?.Flush();
        this.writer?.Dispose();
        this.writer = null;
        this.capture?.Dispose();
        this.capture = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        this.writer?.Dispose();
        this.capture?.Dispose();
    }
}
