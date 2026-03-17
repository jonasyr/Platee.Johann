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
}
