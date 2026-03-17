using FluentAssertions;
using Johann.Domain.Entities;
using Johann.Domain.Enums;
using Johann.Domain.ValueObjects;
using Johann.Infrastructure.Json;

namespace Johann.Tests.Unit;

public sealed class EntryRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonRepository _sut;

    public EntryRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"JohannTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _sut = new JsonRepository(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── SaveAsync + GetEntriesForDateAsync ────────────────────────────────────

    [Fact]
    public async Task SaveAsync_then_GetEntriesForDateAsync_returns_saved_entry()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "repo_001", seq: 1, date: date);

        await _sut.SaveAsync(entry);
        var result = await _sut.GetEntriesForDateAsync(date);

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
        var updated = original with { Title = "Updated Title" };

        await _sut.SaveAsync(original);
        await _sut.SaveAsync(updated);

        var result = await _sut.GetEntriesForDateAsync(date);

        // Both saves use the same filename, so only one file exists
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Updated Title");
    }

    // ── GetAvailableDatesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableDatesAsync_returns_all_dates_with_entries()
    {
        var date1 = new DateOnly(2026, 3, 15);
        var date2 = new DateOnly(2026, 3, 16);
        var date3 = new DateOnly(2026, 3, 17);

        await _sut.SaveAsync(MakeEntry(jobId: "dates_001", seq: 1, date: date1));
        await _sut.SaveAsync(MakeEntry(jobId: "dates_002", seq: 1, date: date2));
        await _sut.SaveAsync(MakeEntry(jobId: "dates_003", seq: 1, date: date3));

        var result = await _sut.GetAvailableDatesAsync();

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
        var result = await _sut.GetByJobIdAsync("does_not_exist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByJobIdAsync_returns_entry_when_found()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry(jobId: "find_me", seq: 1, date: date);

        await _sut.SaveAsync(entry);
        var result = await _sut.GetByJobIdAsync("find_me");

        result.Should().NotBeNull();
        result!.JobId.Should().Be("find_me");
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
