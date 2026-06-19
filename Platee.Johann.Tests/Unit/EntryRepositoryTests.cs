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
