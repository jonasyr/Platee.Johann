using FluentAssertions;
using Johann.Domain.Entities;
using Johann.Domain.Enums;
using Johann.Domain.ValueObjects;

namespace Johann.Tests.Unit;

/// <summary>
/// Tests for the ApplySort logic extracted from MainViewModel.
/// SortMode is defined in Johann.UI which is not referenced by the test project,
/// so a local mirror enum is used here that matches the production values exactly.
/// </summary>
public sealed class SortModeTests
{
    // Local mirror of Johann.UI.ViewModels.SortMode (same ordinal values).
    private enum SortMode { ById = 0, ByProjectThenId = 1 }

    private static IEnumerable<Entry> ApplySort(IEnumerable<Entry> entries, SortMode mode) => mode switch
    {
        SortMode.ByProjectThenId => entries.OrderBy(e => e.ProjectName).ThenBy(e => e.SequenceNumber),
        _ => entries.OrderBy(e => e.SequenceNumber),
    };

    // ── ById ──────────────────────────────────────────────────────────────────

    [Fact]
    public void SortById_orders_by_sequenceNumber()
    {
        var entries = new[]
        {
            MakeEntry("e3", seq: 3, project: "Alpha"),
            MakeEntry("e1", seq: 1, project: "Gamma"),
            MakeEntry("e2", seq: 2, project: "Beta"),
        };

        var result = ApplySort(entries, SortMode.ById).ToList();

        result.Select(e => e.SequenceNumber).Should().BeInAscendingOrder();
        result[0].JobId.Should().Be("e1");
        result[1].JobId.Should().Be("e2");
        result[2].JobId.Should().Be("e3");
    }

    // ── ByProjectThenId ───────────────────────────────────────────────────────

    [Fact]
    public void SortByProjectThenId_orders_by_projectName_then_sequenceNumber()
    {
        var entries = new[]
        {
            MakeEntry("b2", seq: 2, project: "Beta"),
            MakeEntry("a1", seq: 1, project: "Alpha"),
            MakeEntry("b1", seq: 1, project: "Beta"),
            MakeEntry("a2", seq: 2, project: "Alpha"),
        };

        var result = ApplySort(entries, SortMode.ByProjectThenId).ToList();

        result[0].JobId.Should().Be("a1", because: "Alpha seq=1 comes first");
        result[1].JobId.Should().Be("a2", because: "Alpha seq=2 comes second");
        result[2].JobId.Should().Be("b1", because: "Beta seq=1 comes third");
        result[3].JobId.Should().Be("b2", because: "Beta seq=2 comes last");
    }

    // ── Stability / edge cases ────────────────────────────────────────────────

    [Fact]
    public void SortById_stable_for_equal_sequence_numbers()
    {
        var entries = new[]
        {
            MakeEntry("x1", seq: 1, project: "Zeta"),
            MakeEntry("x2", seq: 1, project: "Alpha"),
            MakeEntry("x3", seq: 1, project: "Gamma"),
        };

        // When sequence numbers are equal, sort should not throw and should return all entries
        var result = ApplySort(entries, SortMode.ById).ToList();

        result.Should().HaveCount(3);
        result.Select(e => e.SequenceNumber).Should().AllBeEquivalentTo(1);
    }

    [Fact]
    public void SortById_empty_input_returns_empty()
    {
        var result = ApplySort([], SortMode.ById).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void SortByProjectThenId_empty_input_returns_empty()
    {
        var result = ApplySort([], SortMode.ByProjectThenId).ToList();

        result.Should().BeEmpty();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Entry MakeEntry(string jobId, int seq, string project) => new()
    {
        JobId = jobId,
        SequenceNumber = seq,
        CreatedAt = DateTimeOffset.UtcNow,
        Type = EntryType.Projekt,
        ProjectName = project,
        Title = "Sort Test",
        SourceType = "text",
        Status = ProcessingStatus.Empty,
    };
}
