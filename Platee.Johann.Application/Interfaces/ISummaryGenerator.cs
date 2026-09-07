namespace Platee.Johann.Application.Interfaces;

/// <summary>
/// Generates GPT-based summaries and type-specific content from transcripts.
/// </summary>
public interface ISummaryGenerator
{
    bool IsAvailable { get; }

    Task<string> GenerateAbstractAsync(string transcript, CancellationToken ct = default);

    Task<string> GenerateLongSummaryAsync(string transcript, CancellationToken ct = default);

    Task<string> GenerateProseSummaryAsync(string transcript, CancellationToken ct = default);

    Task<string> GenerateEmailTextAsync(string proseSummary, CancellationToken ct = default);

    Task<string> GenerateTitleAsync(string transcript, CancellationToken ct = default);

    Task<string?> GenerateAufgabeAsync(string transcript, CancellationToken ct = default);

    Task<string?> GenerateGespraechsnotizAsync(string transcript, CancellationToken ct = default);

    Task<string?> GenerateStundenzettelAsync(string transcript, CancellationToken ct = default);

    Task<string?> GenerateAnalogAsync(string transcript, CancellationToken ct = default);

    /// <summary>
    /// Generates the text for one user-defined category. Built-in sections keep their own
    /// dedicated methods above; this is the single generic path for everything the user adds.
    /// </summary>
    Task<string?> GenerateCustomSectionAsync(
        Platee.Johann.Application.Settings.CategoryDefinition category,
        string transcript,
        CancellationToken ct = default);
}
