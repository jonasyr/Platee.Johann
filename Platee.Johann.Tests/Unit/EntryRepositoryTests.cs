namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Json;

public sealed class EntryRepositoryTests : IDisposable
{
    private readonly string tempDir;
    private readonly JsonRepository sut;

    public EntryRepositoryTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
        this.sut = new JsonRepository(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    // ── SaveAsync + GetEntriesForDateAsync ────────────────────────────────────
    [Fact]
    public async Task SaveAsync_then_GetEntriesForDateAsync_returns_saved_entry()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "repo_001", seq: 1, date: date);

        await this.sut.SaveAsync(entry);
        var result = await this.sut.GetEntriesForDateAsync(date);

        result.Should().HaveCount(1);
        result[0].JobId.Should().Be("repo_001");
        result[0].SequenceNumber.Should().Be(1);
        result[0].ProjectName.Should().Be("Test");
        result[0].Title.Should().Be("Test Entry");
    }

    [Fact]
    public async Task SaveAsync_updates_existing_entry_with_same_jobId()
    {
        var date = new DateOnly(2026, 3, 17);
        var original = MakeEntry(jobId: "repo_002", seq: 2, date: date);

        // Use IsDone (not Title) so the filename stays the same — FilenameBuilder includes the title
        var updated = original with { IsDone = true };

        await this.sut.SaveAsync(original);
        await this.sut.SaveAsync(updated);

        var result = await this.sut.GetEntriesForDateAsync(date);

        // Both saves use the same filename, so only one file exists
        result.Should().HaveCount(1);
        result[0].IsDone.Should().BeTrue();
    }

    // ── GetAvailableDatesAsync ────────────────────────────────────────────────
    [Fact]
    public async Task GetAvailableDatesAsync_returns_all_dates_with_entries()
    {
        var date1 = new DateOnly(2026, 3, 15);
        var date2 = new DateOnly(2026, 3, 16);
        var date3 = new DateOnly(2026, 3, 17);

        await this.sut.SaveAsync(MakeEntry(jobId: "dates_001", seq: 1, date: date1));
        await this.sut.SaveAsync(MakeEntry(jobId: "dates_002", seq: 1, date: date2));
        await this.sut.SaveAsync(MakeEntry(jobId: "dates_003", seq: 1, date: date3));

        var result = await this.sut.GetAvailableDatesAsync();

        result.Should().HaveCount(3);
        result.Should().Contain(date1);
        result.Should().Contain(date2);
        result.Should().Contain(date3);

        // newest first
        result[0].Should().Be(date3);
        result[2].Should().Be(date1);
    }

    // ── GetByJobIdAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task GetByJobIdAsync_returns_null_for_unknown_id()
    {
        var result = await this.sut.GetByJobIdAsync("does_not_exist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByJobIdAsync_returns_entry_when_found()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "find_me", seq: 1, date: date);

        await this.sut.SaveAsync(entry);
        var result = await this.sut.GetByJobIdAsync("find_me");

        result.Should().NotBeNull();
        result!.JobId.Should().Be("find_me");
    }

    [Fact]
    public async Task GetByJobIdAsync_uses_date_prefix_for_fast_lookup()
    {
        var date = new DateOnly(2026, 3, 17);
        var jobId = "260317_001_abcd1234";
        var entry = MakeEntry(jobId: jobId, seq: 1, date: date);

        await this.sut.SaveAsync(entry);
        var result = await this.sut.GetByJobIdAsync(jobId);

        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
    }

    [Fact]
    public async Task GetByJobIdAsync_falls_back_to_full_scan_for_nonstandard_jobid()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "custom_id", seq: 1, date: date);

        await this.sut.SaveAsync(entry);
        var result = await this.sut.GetByJobIdAsync("custom_id");

        result.Should().NotBeNull();
        result!.JobId.Should().Be("custom_id");
    }

    // ── MigrateJobIdsAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task MigrateJobIdsAsync_rewrites_nonstandard_jobids()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "old_legacy_id", seq: 5, date: date);

        await this.sut.SaveAsync(entry);
        await this.sut.MigrateJobIdsAsync();

        var entries = await this.sut.GetEntriesForDateAsync(date);
        var migrated = entries.Should().ContainSingle().Subject;
        migrated.JobId.Should().MatchRegex(@"^\d{6}_\d{3}_[a-f0-9]{8}$");
        migrated.SequenceNumber.Should().Be(5);
        migrated.Title.Should().Be("Test Entry");
    }

    [Fact]
    public async Task MigrateJobIdsAsync_leaves_standard_jobids_unchanged()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "260317_005_abcd1234", seq: 5, date: date);

        await this.sut.SaveAsync(entry);
        await this.sut.MigrateJobIdsAsync();

        var result = await this.sut.GetByJobIdAsync("260317_005_abcd1234");
        result.Should().NotBeNull();
        result!.JobId.Should().Be("260317_005_abcd1234");
    }

    [Fact]
    public async Task MigrateJobIdsAsync_does_not_create_duplicate_files()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "legacy_no_date", seq: 3, date: date);

        await this.sut.SaveAsync(entry);
        await this.sut.MigrateJobIdsAsync();
        await this.sut.MigrateJobIdsAsync(); // run twice — must be idempotent

        var entries = await this.sut.GetEntriesForDateAsync(date);
        entries.Should().ContainSingle("migration must not produce duplicate files");
    }

    [Fact]
    public async Task MigrateJobIdsAsync_preserves_all_entry_data()
    {
        var date = new DateOnly(2026, 4, 15);
        var entry = MakeEntry(jobId: "old_format", seq: 2, date: date) with
        {
            Title = "Wichtiges Meeting",
            ProjectName = "Iris",
            Transcript = "Hallo, das ist ein Test-Transkript.",
            Abstract = "Zusammenfassung des Meetings",
        };

        await this.sut.SaveAsync(entry);
        await this.sut.MigrateJobIdsAsync();

        var entries = await this.sut.GetEntriesForDateAsync(date);
        var migrated = entries.Should().ContainSingle().Subject;
        migrated.JobId.Should().MatchRegex(@"^\d{6}_\d{3}_[a-f0-9]{8}$");
        migrated.Title.Should().Be("Wichtiges Meeting");
        migrated.ProjectName.Should().Be("Iris");
        migrated.Transcript.Should().Be("Hallo, das ist ein Test-Transkript.");
        migrated.Abstract.Should().Be("Zusammenfassung des Meetings");
        migrated.SequenceNumber.Should().Be(2);
        migrated.CreatedAt.Should().Be(entry.CreatedAt);
    }

    [Fact]
    public async Task MigrateJobIdsAsync_generates_correct_date_prefix()
    {
        var date = new DateOnly(2026, 4, 15);
        var entry = MakeEntry(jobId: "no_date_prefix", seq: 7, date: date);

        await this.sut.SaveAsync(entry);
        await this.sut.MigrateJobIdsAsync();

        var entries = await this.sut.GetEntriesForDateAsync(date);
        var migrated = entries.Should().ContainSingle().Subject;
        migrated.JobId.Should().StartWith("260415_007_",
            "date prefix should match entry's CreatedAt and sequence number");
    }

    [Fact]
    public async Task MigrateJobIdsAsync_migrated_entry_is_found_by_fast_path()
    {
        var date = new DateOnly(2026, 5, 20);
        var entry = MakeEntry(jobId: "unmigrated_legacy", seq: 1, date: date);

        await this.sut.SaveAsync(entry);
        await this.sut.MigrateJobIdsAsync();

        // Get the migrated JobId
        var entries = await this.sut.GetEntriesForDateAsync(date);
        var migrated = entries.Should().ContainSingle().Subject;

        // Look it up by the new JobId — exercises the fast path
        var found = await this.sut.GetByJobIdAsync(migrated.JobId);
        found.Should().NotBeNull();
        found!.JobId.Should().Be(migrated.JobId);
    }

    [Fact]
    public async Task MigrateJobIdsAsync_handles_multiple_legacy_entries_on_same_date()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry1 = MakeEntry(jobId: "old_one", seq: 1, date: date) with { Title = "First" };
        var entry2 = MakeEntry(jobId: "old_two", seq: 2, date: date) with { Title = "Second" };
        var entry3 = MakeEntry(jobId: "260317_003_abcd1234", seq: 3, date: date) with { Title = "Already Standard" };

        await this.sut.SaveAsync(entry1);
        await this.sut.SaveAsync(entry2);
        await this.sut.SaveAsync(entry3);
        await this.sut.MigrateJobIdsAsync();

        var entries = await this.sut.GetEntriesForDateAsync(date);
        entries.Should().HaveCount(3);
        entries.Should().OnlyContain(e => System.Text.RegularExpressions.Regex.IsMatch(
            e.JobId, @"^\d{6}_\d{3}_[a-f0-9]{8}$") || e.JobId == "260317_003_abcd1234");
        entries.Select(e => e.Title).Should().BeEquivalentTo(["First", "Second", "Already Standard"]);
    }

    [Fact]
    public async Task MigrateJobIdsAsync_removes_old_file_when_path_differs()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "will_be_migrated", seq: 4, date: date);

        await this.sut.SaveAsync(entry);

        // Count files before migration
        var rawDir = Path.Combine(this.tempDir, date.ToString("yyyy-MM-dd"), "_raw");
        var filesBefore = Directory.GetFiles(rawDir, "*_status.json").Length;

        await this.sut.MigrateJobIdsAsync();

        var filesAfter = Directory.GetFiles(rawDir, "*_status.json").Length;
        filesAfter.Should().Be(filesBefore, "migration should not leave orphaned files");

        // Old JobId should no longer be findable
        var oldResult = await this.sut.GetByJobIdAsync("will_be_migrated");
        oldResult.Should().BeNull("old non-standard JobId should not exist after migration");
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private static Entry MakeEntry(string jobId = "test_001", int seq = 1, DateOnly? date = null) => new()
    {
        JobId = jobId,
        SequenceNumber = seq,
        CreatedAt = new DateTimeOffset((date ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)),
        Type = EntryType.Projekt,
        ProjectName = "Test",
        Title = "Test Entry",
        SourceType = "text",
        Status = ProcessingStatus.Empty,
    };
}
