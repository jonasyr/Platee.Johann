namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Infrastructure.Llm;

/// <summary>
/// Die Transkriptions-API nimmt höchstens 25 MB je Datei (#77). Eine größere Aufnahme lief
/// bisher in einen rohen SDK-Fehler; jetzt scheitert sie vor dem Hochladen mit einer Meldung,
/// die Größe, Grenze und den Ausweg nennt.
/// </summary>
public sealed class AudioUploadLimitTests : IDisposable
{
    private readonly string dir = Directory.CreateTempSubdirectory("johann-upload-limit-").FullName;

    [Fact]
    public void The_limit_is_the_documented_25_megabytes()
    {
        AudioUploadLimit.MaxBytes.Should().Be(25 * 1024 * 1024);
    }

    [Fact]
    public void A_file_at_the_limit_is_accepted()
    {
        var path = this.FileOf(AudioUploadLimit.MaxBytes);

        var act = () => AudioUploadLimit.Ensure(path);

        act.Should().NotThrow();
    }

    [Fact]
    public void One_byte_over_the_limit_is_refused_with_size_limit_and_what_to_do()
    {
        var path = this.FileOf(AudioUploadLimit.MaxBytes + 1, "Baustelle Nord.mp3");

        var act = () => AudioUploadLimit.Ensure(path);

        act.Should().Throw<AudioTooLargeException>()
            .WithMessage("*Baustelle Nord.mp3*")
            .WithMessage("*25 MB*")
            .WithMessage("*kürzere*");
    }

    [Fact]
    public async Task The_transcriber_refuses_before_uploading_anything()
    {
        // A dummy key: had the file been uploaded, this would fail with an authentication or
        // network error instead of the size message.
        var transcriber = new WhisperTranscriber("sk-test-not-a-real-key");
        var path = this.FileOf(AudioUploadLimit.MaxBytes + 1);

        var act = () => transcriber.TranscribeAsync(path);

        await act.Should().ThrowAsync<AudioTooLargeException>();
    }

    public void Dispose() => Directory.Delete(this.dir, recursive: true);

    /// <summary>A sparse file of the given length — no need to write 25 MB of data.</summary>
    private string FileOf(long length, string name = "aufnahme.mp3")
    {
        var path = Path.Combine(this.dir, name);
        using var stream = File.Create(path);
        stream.SetLength(length);
        return path;
    }
}
