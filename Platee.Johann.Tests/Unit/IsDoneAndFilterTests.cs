using FluentAssertions;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Json;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Tests for IsDone default value, immutable toggle, JSON round-trip preservation,
/// and pending-only filter logic.
/// EntryMapper is internal to Platee.Johann.Infrastructure, so round-trip tests go via JsonRepository.
/// </summary>
public sealed class IsDoneAndFilterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonRepository _repo;

    public IsDoneAndFilterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"JohannIsDoneTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _repo = new JsonRepository(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Default value ─────────────────────────────────────────────────────────

    [Fact]
    public void Entry_IsDone_defaults_to_false()
    {
        var entry = MakeEntry("default_001");

        entry.IsDone.Should().BeFalse();
    }

    // ── Immutable toggle ──────────────────────────────────────────────────────

    [Fact]
    public void Entry_with_expression_creates_new_instance_with_toggled_IsDone()
    {
        var entry = MakeEntry("toggle_001");
        entry.IsDone.Should().BeFalse();

        var toggled = entry with { IsDone = !entry.IsDone };

        toggled.IsDone.Should().BeTrue();
        entry.IsDone.Should().BeFalse("original must not be mutated");
        toggled.Should().NotBeSameAs(entry);
    }

    // ── JSON round-trip via JsonRepository ───────────────────────────────────

    [Fact]
    public async Task JsonRepository_round_trip_preserves_IsDone_true()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry("isdone_true_001", date: date) with { IsDone = true };

        await _repo.SaveAsync(entry);
        var loaded = await _repo.GetByJobIdAsync("isdone_true_001");

        loaded.Should().NotBeNull();
        loaded!.IsDone.Should().BeTrue();
    }

    [Fact]
    public async Task JsonRepository_round_trip_preserves_IsDone_false()
    {
        var date = new DateOnly(2026, 3, 17);
        var entry = MakeEntry("isdone_false_001", date: date) with { IsDone = false };

        await _repo.SaveAsync(entry);
        var loaded = await _repo.GetByJobIdAsync("isdone_false_001");

        loaded.Should().NotBeNull();
        loaded!.IsDone.Should().BeFalse();
    }

    // ── Filter logic ──────────────────────────────────────────────────────────

    [Fact]
    public void Filter_ShowOnlyPending_excludes_done_entries()
    {
        var entries = new[]
        {
            MakeEntry("filter_001") with { IsDone = false },
            MakeEntry("filter_002") with { IsDone = true },
            MakeEntry("filter_003") with { IsDone = false },
            MakeEntry("filter_004") with { IsDone = true },
        };

        var pending = entries.Where(e => !e.IsDone).ToList();

        pending.Should().HaveCount(2);
        pending.Should().OnlyContain(e => !e.IsDone);
        pending.Select(e => e.JobId).Should().BeEquivalentTo(["filter_001", "filter_003"]);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Entry MakeEntry(string jobId, DateOnly? date = null) => new()
    {
        JobId = jobId,
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset((date ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)),
        Type = EntryType.Projekt,
        ProjectName = "Test",
        Title = "IsDone Test",
        SourceType = "text",
        Status = ProcessingStatus.Empty,
    };
}
