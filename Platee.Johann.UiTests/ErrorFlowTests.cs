namespace Platee.Johann.UiTests;

using System.IO;
using FluentAssertions;
using Platee.Johann.UiDriver.Audio;
using Xunit;

/// <summary>
/// Task 13: error flows and the release notes against the real Johann exe — a server error
/// during transcription ends in an error toast with a details link, a recording over 25 MB is
/// refused before anything is uploaded (#77), and „Neuigkeiten“ opens the release notes (#78).
/// <para>
/// Deviations from the brief (details in the Task 13 report): the transcription endpoint fails
/// five times, not once — the OpenAI SDK retries a 5xx three times (four requests observed
/// live), so a single failure never reaches Johann; the oversized MP3 is written next to the watch folder and then moved in,
/// so the watcher never sees a half-written file; the release notes are closed again before
/// the test ends.
/// </para>
/// </summary>
[Collection("Desktop")]
public sealed class ErrorFlowTests
{
    private const string TranscriptionPath = "/v1/audio/transcriptions";

    [Fact]
    public async Task ServerError_ShowsErrorToast()
    {
        using var ctx = UiTestContext.Start(stub: s => s.FailNext(TranscriptionPath, 500, times: 5));
        ctx.DropFile("D1");

        var toast = await ctx.WaitForToastAsync(t => t.StartsWith("Fehler", StringComparison.Ordinal));

        toast.Should().Contain("Fehler");
        ctx.App.Find("Toast.Details").Should().NotBeNull("an error toast links to the status log");
    }

    [Fact]
    public async Task TooLargeFile_IsRefused_WithoutUpload()
    {
        using var ctx = UiTestContext.Start();
        var staging = Path.Combine(ctx.Sandbox.Root, "gross.mp3");
        SilenceMp3.Write(staging, TimeSpan.FromMinutes(30));
        new FileInfo(staging).Length.Should().BeGreaterThan(25L * 1024 * 1024, "the fixture must exceed AudioUploadLimit.MaxBytes");
        File.Move(staging, Path.Combine(ctx.Sandbox.Eingang, "gross.mp3"));

        (await ctx.WaitForToastAsync(t => t.Contains("gross.mp3", StringComparison.Ordinal))).Should().Contain("25 MB");
        ctx.Stub.Requests.Should().NotContain(r => r.Path == TranscriptionPath);
    }

    [Fact]
    public void ReleaseNotesButton_OpensWindow()
    {
        using var ctx = UiTestContext.Start();
        ctx.App.Click("Main.ReleaseNotes", mouse: true);

        ctx.App.Find("ReleaseNotes.Close").Should().NotBeNull();

        ctx.App.Click("ReleaseNotes.Close");
        ctx.WaitUntil(
            () => ctx.App.TryFind("ReleaseNotes.Close", TimeSpan.FromMilliseconds(200)) is null,
            TimeSpan.FromSeconds(10),
            "das Neuigkeiten-Fenster schließt sich");
    }
}
