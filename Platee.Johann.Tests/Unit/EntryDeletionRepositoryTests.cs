namespace Platee.Johann.Tests.Unit;

using System.Text.Json;
using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.Services;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Json;

/// <summary>
/// Einträge löschen (#55) auf der Platte: alle Dateien eines Eintrags wandern in den
/// Johann-Papierkorb, alles oder nichts, und nichts anderes wird angefasst — vor allem nicht
/// der Sequenzzähler, sonst würde eine Nummer samt Dateinamen wiederverwendet.
/// </summary>
public sealed class EntryDeletionRepositoryTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 9, 22);
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly string root;
    private readonly JsonRepository sut;

    public EntryDeletionRepositoryTests()
    {
        this.root = Path.Combine(Path.GetTempPath(), $"JohannDeleteTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.root);
        this.sut = new JsonRepository(this.root, () => Now);
    }

    private string DayDir => Path.Combine(this.root, "2026-09-22");

    private string RawDir => Path.Combine(this.DayDir, "_raw");

    private string TrashRoot => Path.Combine(this.root, "_Papierkorb");

    public void Dispose()
    {
        if (Directory.Exists(this.root))
        {
            Directory.Delete(this.root, recursive: true);
        }
    }

    // ── Löschen ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task Delete_moves_every_file_of_the_entry_into_the_trash()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1, title: "Angebot Müller prüfen");
        var stem = FilenameBuilder.Build(entry);

        var result = await this.sut.DeleteAsync(entry.JobId);

        result.Found.Should().BeTrue();
        result.MovedFiles.Should().HaveCount(6);
        this.FilesUnder(this.DayDir).Should().NotContain(f => f.Contains(stem, StringComparison.Ordinal));

        var trash = result.TrashDirectory!;
        File.Exists(Path.Combine(trash, stem + ".pdf")).Should().BeTrue();
        File.Exists(Path.Combine(trash, stem + ".html")).Should().BeTrue();
        File.Exists(Path.Combine(trash, "_raw", stem + "_status.json")).Should().BeTrue();
        File.Exists(Path.Combine(trash, "_raw", stem + ".html")).Should().BeTrue();
        File.Exists(Path.Combine(trash, "_raw", stem + ".txt")).Should().BeTrue();
        File.Exists(Path.Combine(trash, "_raw", stem + ".mp3")).Should().BeTrue();
    }

    [Fact]
    public async Task Deleted_entry_is_gone_for_every_lookup()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1);

        await this.sut.DeleteAsync(entry.JobId);

        (await this.sut.GetByJobIdAsync(entry.JobId)).Should().BeNull();
        (await this.sut.GetEntriesForDateAsync(Day)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_leaves_other_entries_and_similarly_named_files_alone()
    {
        var doomed = await this.SaveWithArtifactsAsync(seq: 1, title: "Termin");
        var neighbour = await this.SaveWithArtifactsAsync(seq: 2, title: "Termin");
        var stem = FilenameBuilder.Build(doomed);
        var lookalikes = new[]
        {
            Path.Combine(this.DayDir, stem + "_Anmerkung.pdf"),
            Path.Combine(this.DayDir, stem + ".pdf.bak"),
            Path.Combine(this.RawDir, stem + "x.txt"),
        };
        foreach (var file in lookalikes)
        {
            await File.WriteAllTextAsync(file, "fremd");
        }

        await this.sut.DeleteAsync(doomed.JobId);

        (await this.sut.GetEntriesForDateAsync(Day)).Should().ContainSingle()
            .Which.JobId.Should().Be(neighbour.JobId);
        var neighbourStem = FilenameBuilder.Build(neighbour);
        File.Exists(Path.Combine(this.DayDir, neighbourStem + ".pdf")).Should().BeTrue();
        File.Exists(Path.Combine(this.RawDir, neighbourStem + ".mp3")).Should().BeTrue();
        lookalikes.Should().OnlyContain(f => File.Exists(f), "only exact names belong to the entry");
    }

    [Fact]
    public async Task Delete_keeps_the_counter_so_the_number_is_never_reused()
    {
        var seq = await this.sut.GetNextSequenceNumberAsync(Day);
        var entry = await this.SaveWithArtifactsAsync(seq: seq);

        await this.sut.DeleteAsync(entry.JobId);

        Directory.Exists(this.RawDir).Should().BeTrue("the day folder and _raw stay");
        File.Exists(Path.Combine(this.RawDir, "_counter.json")).Should().BeTrue();
        (await this.sut.GetNextSequenceNumberAsync(Day)).Should().Be(seq + 1);
    }

    [Fact]
    public async Task Delete_of_an_unknown_job_id_changes_nothing()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1);
        var before = this.FilesUnder(this.root);

        var result = await this.sut.DeleteAsync("260922_099_deadbeef");

        result.Should().Be(EntryDeletionResult.NotFound);
        this.FilesUnder(this.root).Should().BeEquivalentTo(before);
        (await this.sut.GetByJobIdAsync(entry.JobId)).Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_with_a_locked_file_rolls_everything_back_and_names_the_file()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1);
        var stem = FilenameBuilder.Build(entry);
        var before = this.FilesUnder(this.root);
        var pdf = Path.Combine(this.DayDir, stem + ".pdf");

        EntryDeletionException? error;
        using (new FileStream(pdf, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var act = () => this.sut.DeleteAsync(entry.JobId);
            error = (await act.Should().ThrowAsync<EntryDeletionException>()).Which;
        }

        error.BlockingFile.Should().Be(pdf);
        error.Message.Should().Contain(stem + ".pdf");
        this.FilesUnder(this.root).Should().BeEquivalentTo(before, "nothing may be half deleted");
        Directory.Exists(this.TrashRoot).Should().BeFalse("an aborted deletion leaves no trash folder behind");
        (await this.sut.GetByJobIdAsync(entry.JobId)).Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_finds_a_legacy_entry_whose_file_name_differs_from_the_builder()
    {
        var entry = MakeEntry(seq: 7, title: "Alter Eintrag");
        await this.sut.SaveAsync(entry);
        var built = Path.Combine(this.RawDir, FilenameBuilder.Build(entry) + "_status.json");
        File.Move(built, Path.Combine(this.RawDir, "altname_status.json"));
        await File.WriteAllTextAsync(Path.Combine(this.RawDir, "altname.txt"), "Transkript");
        await File.WriteAllTextAsync(Path.Combine(this.DayDir, "altname.pdf"), "pdf");

        var result = await this.sut.DeleteAsync(entry.JobId);

        result.MovedFiles.Should().HaveCount(3);
        this.FilesUnder(this.DayDir).Should().NotContain(f => f.Contains("altname", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Delete_is_not_stopped_by_a_corrupt_neighbour_file()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 2);
        await File.WriteAllTextAsync(Path.Combine(this.RawDir, "kaputt_status.json"), "{ nicht json");

        var result = await this.sut.DeleteAsync(entry.JobId);

        result.Found.Should().BeTrue();
        File.Exists(Path.Combine(this.RawDir, "kaputt_status.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Trash_folder_records_what_was_deleted_and_from_where()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1, title: "Angebot");

        var result = await this.sut.DeleteAsync(entry.JobId);

        var record = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(result.TrashDirectory!, "geloescht.json")));
        record.RootElement.GetProperty("jobId").GetString().Should().Be(entry.JobId);
        record.RootElement.GetProperty("deletedAt").GetDateTimeOffset().Should().Be(Now);
        record.RootElement.GetProperty("date").GetString().Should().Be("2026-09-22");
        record.RootElement.GetProperty("files").GetArrayLength().Should().Be(6);
        Path.GetDirectoryName(result.TrashDirectory).Should().Be(this.TrashRoot);
    }

    [Fact]
    public async Task Deleting_the_same_job_id_again_after_a_restore_keeps_both_trash_folders()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1);
        var first = await this.sut.DeleteAsync(entry.JobId);
        await this.SaveWithArtifactsAsync(entry);

        var second = await this.sut.DeleteAsync(entry.JobId);

        second.Found.Should().BeTrue();

        second.TrashDirectory.Should().NotBe(first.TrashDirectory);
        Directory.Exists(first.TrashDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task Trash_is_invisible_to_dates_lookup_and_migration()
    {
        var legacy = MakeEntry(seq: 1) with { JobId = "legacy-id" };
        await this.sut.SaveAsync(legacy);
        await this.sut.DeleteAsync(legacy.JobId);

        (await this.sut.GetAvailableDatesAsync()).Should().BeEmpty();
        (await this.sut.GetByJobIdAsync("legacy-id")).Should().BeNull();
        (await this.sut.MigrateJobIdsAsync()).Migrated.Should().Be(0, "the migration must not reach into the trash");
    }

    // ── Tage ohne Einträge ────────────────────────────────────────────────────
    [Fact]
    public async Task Days_without_entries_are_not_listed()
    {
        var entry = await this.SaveWithArtifactsAsync(seq: 1);
        Directory.CreateDirectory(Path.Combine(this.root, "2026-09-03"));
        await this.sut.SaveAsync(MakeEntry(seq: 1, date: new DateOnly(2026, 9, 21)));

        await this.sut.DeleteAsync(entry.JobId);

        (await this.sut.GetAvailableDatesAsync()).Should().Equal(new DateOnly(2026, 9, 21));
    }

    // ── Papierkorb leeren ─────────────────────────────────────────────────────
    [Fact]
    public async Task Purge_removes_only_trash_folders_deleted_before_the_cutoff()
    {
        var old = await this.SaveWithArtifactsAsync(seq: 1);
        var oldTrash = (await new JsonRepository(this.root, () => Now.AddDays(-31)).DeleteAsync(old.JobId)).TrashDirectory!;
        var recent = await this.SaveWithArtifactsAsync(seq: 2);
        var recentTrash = (await this.sut.DeleteAsync(recent.JobId)).TrashDirectory!;
        var foreign = Path.Combine(this.TrashRoot, "vom-nutzer-abgelegt");
        Directory.CreateDirectory(foreign);
        await File.WriteAllTextAsync(Path.Combine(foreign, "notiz.txt"), "bleibt");

        var result = await this.sut.PurgeTrashAsync(Now.AddDays(-30));

        result.Purged.Should().Be(1);
        Directory.Exists(oldTrash).Should().BeFalse();
        Directory.Exists(recentTrash).Should().BeTrue();
        Directory.Exists(foreign).Should().BeTrue("folders without a deletion record are not Johann's to remove");
    }

    [Fact]
    public async Task Purge_skips_a_folder_with_an_unreadable_record()
    {
        var broken = Path.Combine(this.TrashRoot, "260922_001_abcdef01");
        Directory.CreateDirectory(broken);
        await File.WriteAllTextAsync(Path.Combine(broken, "geloescht.json"), "{ kaputt");

        var result = await this.sut.PurgeTrashAsync(Now);

        result.Purged.Should().Be(0);
        result.Skipped.Should().ContainSingle().Which.Should().Contain("260922_001_abcdef01");
        Directory.Exists(broken).Should().BeTrue();
    }

    [Fact]
    public async Task Purge_without_a_trash_folder_does_nothing()
    {
        (await this.sut.PurgeTrashAsync(Now)).Should().Be(TrashPurgeResult.Empty);
    }

    // ── Hilfen ────────────────────────────────────────────────────────────────
    private Task<Entry> SaveWithArtifactsAsync(int seq, string title = "Test Eintrag") =>
        this.SaveWithArtifactsAsync(MakeEntry(seq: seq, title: title));

    private async Task<Entry> SaveWithArtifactsAsync(Entry entry)
    {
        await this.sut.SaveAsync(entry);
        var stem = FilenameBuilder.Build(entry);
        await File.WriteAllTextAsync(Path.Combine(this.DayDir, stem + ".pdf"), "pdf");
        await File.WriteAllTextAsync(Path.Combine(this.DayDir, stem + ".html"), "html export");
        await File.WriteAllTextAsync(Path.Combine(this.RawDir, stem + ".html"), "html");
        await File.WriteAllTextAsync(Path.Combine(this.RawDir, stem + ".txt"), "transkript");
        await File.WriteAllTextAsync(Path.Combine(this.RawDir, stem + ".mp3"), "audio");
        return entry;
    }

    private List<string> FilesUnder(string dir) =>
        Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Order().ToList()
            : [];

    private static Entry MakeEntry(int seq, string title = "Test Eintrag", DateOnly? date = null)
    {
        var day = date ?? Day;
        return new Entry
        {
            JobId = $"{day:yyMMdd}_{seq:D3}_{Guid.NewGuid().ToString("N")[..8]}",
            SequenceNumber = seq,
            CreatedAt = new DateTimeOffset(day.Year, day.Month, day.Day, 9, 30, 0, TimeSpan.FromHours(1)),
            Type = EntryType.Projekt,
            ProjectName = "Johann",
            Title = title,
            SourceType = "audio",
            Status = ProcessingStatus.Empty,
        };
    }
}
