using Johann.Application.Interfaces;

namespace Johann.Application.Processing;

/// <summary>
/// Generates GPT-based summaries for a transcript.
/// Mirrors generate_abstract / generate_long_summary / generate_prose_summary / generate_email_text
/// from Python summarizer.py.
/// </summary>
public sealed class SummaryGenerator
{
    private readonly ILlmProvider _llm;

    public bool IsAvailable => _llm.IsAvailable;

    public SummaryGenerator(ILlmProvider llm) => _llm = llm;

    public async Task<string> GenerateAbstractAsync(string transcript, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var (abstractLimit, _) = WordLimitCalculator.Calculate(transcript);
        var userContent = SummaryPrompts.Abstract
            .Replace("{word_limit}", abstractLimit.ToString())
            .Replace("{transcript}", transcript);

        return await _llm.GenerateAsync(
            SummaryPrompts.SystemMessage, userContent, new LlmOptions(20000), ct);
    }

    public async Task<string> GenerateLongSummaryAsync(string transcript, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var (_, structuredLimit) = WordLimitCalculator.Calculate(transcript);
        var userContent = SummaryPrompts.Structured
            .Replace("{word_limit}", structuredLimit.ToString())
            .Replace("{transcript}", transcript);

        return await _llm.GenerateAsync(
            SummaryPrompts.SystemMessage, userContent, new LlmOptions(20000), ct);
    }

    public async Task<string> GenerateProseSummaryAsync(string transcript, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var userContent = SummaryPrompts.Prose
            .Replace("{transcript}", transcript);

        return await _llm.GenerateAsync(
            SummaryPrompts.SystemMessage, userContent, new LlmOptions(20000), ct);
    }

    public async Task<string> GenerateEmailTextAsync(string proseSummary, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(proseSummary))
            return string.Empty;

        var userContent = SummaryPrompts.Email
            .Replace("{prose_summary}", proseSummary);

        return await _llm.GenerateAsync(
            SummaryPrompts.SystemMessage, userContent, new LlmOptions(4000), ct);
    }
}
