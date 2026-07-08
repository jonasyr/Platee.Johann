using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Platee.Johann.Application.Interfaces;

namespace Platee.Johann.Infrastructure.Audio;

public sealed class WindowsMicrophoneRecorder : IMicrophoneRecorder, IDisposable
{
    private WasapiCapture? capture;
    private WaveFileWriter? wavWriter;
    private string? tempWavPath;
    private string? outputPath;

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
        if (this.capture is not null)
        {
            throw new InvalidOperationException("Recording is already in progress.");
        }

        var internalWavPath = Path.ChangeExtension(outputFilePath, ".tmp.wav");
        var capture = new WasapiCapture();
        var writer = new WaveFileWriter(internalWavPath, capture.WaveFormat);

        try
        {
            capture.DataAvailable += (_, e) =>
            {
                this.wavWriter?.Write(e.Buffer, 0, e.BytesRecorded);
            };
            capture.StartRecording();
        }
        catch
        {
            writer.Dispose();
            capture.Dispose();
            throw;
        }

        this.capture = capture;
        this.wavWriter = writer;
        this.tempWavPath = internalWavPath;
        this.outputPath = outputFilePath;
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (this.capture is null)
        {
            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.capture.RecordingStopped += (_, _) => tcs.TrySetResult(true);
        this.capture.StopRecording();
        await tcs.Task;

        this.wavWriter?.Flush();
        this.wavWriter?.Dispose();
        this.wavWriter = null;
        this.capture.Dispose();
        this.capture = null;

        var tempWav = this.tempWavPath;
        var output = this.outputPath;
        this.tempWavPath = null;
        this.outputPath = null;

        if (tempWav is not null && output is not null && File.Exists(tempWav))
        {
            await Task.Run(() =>
            {
                try
                {
                    using var reader = new AudioFileReader(tempWav);
                    MediaFoundationEncoder.EncodeToMp3(reader, output);
                }
                finally
                {
                    try { File.Delete(tempWav); }
                    catch { }
                }
            });
        }
    }

    public void Dispose()
    {
        this.wavWriter?.Dispose();
        this.capture?.Dispose();
        if (this.tempWavPath is not null)
        {
            try { File.Delete(this.tempWavPath); }
            catch { }
        }
    }
}
