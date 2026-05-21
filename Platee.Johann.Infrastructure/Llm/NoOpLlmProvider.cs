namespace Platee.Johann.Infrastructure.Llm;

using Platee.Johann.Application.Interfaces;

/// <summary>
/// Phase 1 stub — LLM not available (no API key configured).
/// Replaced by OpenAiLlmProvider when an API key is present.
/// </summary>
public sealed class NoOpLlmProvider : ILlmProvider
{
    public bool IsAvailable => false;

    public Task<string> GenerateAsync(
        string systemPrompt, string userContent, LlmOptions options,
        CancellationToken ct = default)
        => Task.FromResult(string.Empty);
}
