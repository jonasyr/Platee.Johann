using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

namespace Platee.Johann.Domain.Entities;

/// <summary>
/// Core domain entity representing a single journal/knowledge entry.
/// Immutable record — all mutations produce a new instance.
/// </summary>
public sealed record Entry
{
    // --- Identity ---
    public required string JobId { get; init; }
    public required int SequenceNumber { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    // --- Classification ---
    public required EntryType Type { get; init; }
    public required string ProjectName { get; init; }
    public required string Title { get; init; }
    public required string SourceType { get; init; } // "audio" | "text"

    // --- Status ---
    public required ProcessingStatus Status { get; init; }
    public bool IsDone { get; init; } = false;

    // --- Content (null = not yet generated) ---
    public string? Transcript { get; init; }
    public string? Abstract { get; init; }
    public string? LongSummary { get; init; }
    public string? ProseSummary { get; init; }

    // --- Type-specific fields ---
    public string? EmailText { get; init; }           // EMail
    public string? ConversationNote { get; init; }    // Gesprächsnotiz
    public string? TaskList { get; init; }            // Aufgabe
    public string? StundenzettelText { get; init; }   // Stundenzettel
    public string? AnalogText { get; init; }          // Analog

    // --- Metadata ---
    public double DurationSeconds { get; init; }
    public int WordCount { get; init; }
    public int SchemaVersion { get; init; } = 2;
}
