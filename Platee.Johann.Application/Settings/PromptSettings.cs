namespace Platee.Johann.Application.Settings;

using Platee.Johann.Application.Processing;

public sealed record PromptSettings
{
    /// <summary>
    /// Gets a legacy marker that is read and written but no longer acted upon.
    /// <para>
    /// It once drove <c>PromptDefaultsMigration</c>, which was never wired up and has been
    /// removed: the team's <c>prompts.json</c> is the single source of truth for prompt
    /// text, and a client that silently rewrote it would overwrite curated wording for
    /// everyone — the same failure mode as a v1.3.2 client stripping
    /// <c>customCategories</c>.
    /// </para>
    /// <para>
    /// The field is kept so the key survives a round-trip. Dropping it from the DTO would
    /// strip it from the shared file on the next save.
    /// </para>
    /// </summary>
    public int PromptDefaultsRevision { get; init; } = LegacyDefaultsRevision;

    /// <summary>The last revision the removed migration would have applied.</summary>
    public const int LegacyDefaultsRevision = 20260513;

    public string SystemMessage { get; init; } = SummaryPrompts.SystemMessage;

    public string AbstractPrompt { get; init; } = SummaryPrompts.Abstract;

    public string StructuredPrompt { get; init; } = SummaryPrompts.Structured;

    public string ProsePrompt { get; init; } = SummaryPrompts.Prose;

    public string EmailPrompt { get; init; } = SummaryPrompts.Email;

    public string AufgabePrompt { get; init; } = SummaryPrompts.Aufgabe;

    public string GespraechsnotizPrompt { get; init; } = SummaryPrompts.Gespraechsnotiz;

    public string StundenzettelPrompt { get; init; } = SummaryPrompts.Stundenzettel;

    public string AnalogPrompt { get; init; } = SummaryPrompts.Analog;

    /// <summary>
    /// Gets the user-defined categories. Present in both the global and the local prompts
    /// file; <c>PromptSettingsLoader.MergeCategories</c> combines them, with a personal
    /// category winning over a global one sharing its Id.
    /// </summary>
    public IReadOnlyList<CategoryDefinition> CustomCategories { get; init; } = [];

    public static PromptSettings Default => new();
}
