using System;
using System.IO;
using FluentAssertions;
using Platee.Johann.Infrastructure.Audio;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #67 — die neuen Transkriptionsmodelle liefern keine Audiodauer mehr.
/// <para>
/// <c>whisper-1</c> gab sie im Verbose-Format zurück; <c>gpt-transcribe</c> antwortet mit
/// <c>json</c> und kennt das Feld nicht. Die Dauer wird deshalb lokal aus der Datei
/// gelesen. Sie hängt an der Eintragsliste, der Detailansicht und dem PDF-Kopf.
/// </para>
/// <para>
/// Entscheidend ist, dass das Lesen <b>nie wirft</b>: eine unlesbare Datei darf höchstens
/// eine fehlende Dauer bedeuten, niemals ein gescheitertes Diktat.
/// </para>
/// </summary>
public sealed class AudioDurationReaderTests : IDisposable
{
    private readonly string tempDir;

    public AudioDurationReaderTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannDuration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public void A_missing_file_yields_no_duration_instead_of_throwing()
    {
        var path = Path.Combine(this.tempDir, "gibtesnicht.mp3");

        AudioDurationReader.ReadSeconds(path).Should().Be(0.0);
    }

    [Fact]
    public void A_file_that_is_not_audio_yields_no_duration_instead_of_throwing()
    {
        var path = Path.Combine(this.tempDir, "kaputt.mp3");
        File.WriteAllText(path, "das hier ist kein MP3, sondern Text");

        AudioDurationReader.ReadSeconds(path).Should().Be(0.0);
    }

    [Fact]
    public void An_empty_file_yields_no_duration_instead_of_throwing()
    {
        var path = Path.Combine(this.tempDir, "leer.mp3");
        File.WriteAllBytes(path, []);

        AudioDurationReader.ReadSeconds(path).Should().Be(0.0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_path_yields_no_duration_instead_of_throwing(string path)
    {
        AudioDurationReader.ReadSeconds(path).Should().Be(0.0);
    }
}
