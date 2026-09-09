namespace Platee.Johann.Domain.Entities;

using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

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

    public string? EditedTranscript { get; init; }

    /// <summary>Returns the user-corrected transcript if available, otherwise the original Whisper transcript.</summary>
    public string? EffectiveTranscript => EditedTranscript ?? Transcript;

    public string? Abstract { get; init; }

    public string? LongSummary { get; init; }

    public string? ProseSummary { get; init; }

    // --- Type-specific fields ---
    public string? EmailText { get; init; } // EMail

    public string? ConversationNote { get; init; } // Gesprächsnotiz

    public string? TaskList { get; init; } // Aufgabe

    public string? StundenzettelText { get; init; } // Stundenzettel

    public string? AnalogText { get; init; } // Analog

    // --- Metadata ---
    public double DurationSeconds { get; init; }

    public int WordCount { get; init; }

    /// <summary>
    /// Gets generated text for user-defined categories, keyed by CategoryDefinition.Id.
    /// The eight built-in sections keep their own fixed properties above; this map holds
    /// only what the user added, which is why a v1.3.x client reading a v4 entry loses
    /// nothing it ever knew about.
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomSections { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Gets the display name each custom section was generated under, keyed by the same id
    /// as <see cref="CustomSections"/>.
    /// <para>
    /// Recorded here rather than looked up in the settings because it is needed exactly when
    /// the category no longer exists. It also keeps the heading historically honest: the text
    /// shows the name it was actually generated under.
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomSectionNames { get; init; }
        = new Dictionary<string, string>();

    public int SchemaVersion { get; init; } = 4;
}
