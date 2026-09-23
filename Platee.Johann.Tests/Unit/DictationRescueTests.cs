namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.Helpers;

/// <summary>
/// Ein gescheitertes Diktat wird gesichert statt gelöscht (#106). Bisher löschte
/// <c>StopDictation</c> die temporäre Aufnahme bei jedem Fehler — das Diktat war verloren.
/// </summary>
public sealed class DictationRescueTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 9, 23, 16, 12, 5);

    private readonly string root = Directory.CreateTempSubdirectory("johann-rescue-").FullName;

    [Fact]
    public void A_failed_dictation_is_moved_into_the_rescue_folder_of_the_output()
    {
        var temp = this.Recording();

        var kept = DictationRescue.Save(temp, this.Output(), Now);

        kept.Should().Be(Path.Combine(this.Output(), DictationRescue.FolderName, "Diktat_2026-09-23_161205.mp3"));
        File.ReadAllText(kept!).Should().Be("audio");
        File.Exists(temp).Should().BeFalse("it was moved, not copied");
    }

    [Fact]
    public void A_second_failure_in_the_same_second_does_not_overwrite_the_first()
    {
        var first = DictationRescue.Save(this.Recording("first"), this.Output(), Now);
        var second = DictationRescue.Save(this.Recording("second"), this.Output(), Now);

        second.Should().NotBe(first);
        File.ReadAllText(first!).Should().Be("first");
        File.ReadAllText(second!).Should().Be("second");
    }

    [Fact]
    public void If_the_rescue_folder_cannot_be_created_the_recording_stays_where_it_is()
    {
        var temp = this.Recording();
        var blocked = Path.Combine(this.root, "blocked");
        File.WriteAllText(blocked, "a file where the output folder should be");

        var kept = DictationRescue.Save(temp, blocked, Now);

        kept.Should().Be(temp, "nothing may be lost — the message then names the temp path");
        File.Exists(temp).Should().BeTrue();
    }

    [Fact]
    public void A_recording_that_is_already_gone_yields_nothing()
    {
        // Processing can fail after the MP3 was archived (e.g. the overview); then there is
        // nothing left to rescue and the message must not name a path.
        DictationRescue.Save(Path.Combine(this.root, "gone.mp3"), this.Output(), Now).Should().BeNull();
    }

    public void Dispose() => Directory.Delete(this.root, recursive: true);

    private string Output() => Path.Combine(this.root, "output");

    private string Recording(string content = "audio")
    {
        var path = Path.Combine(this.root, $"johann_dictation_{Guid.NewGuid():N}.mp3");
        File.WriteAllText(path, content);
        return path;
    }
}
