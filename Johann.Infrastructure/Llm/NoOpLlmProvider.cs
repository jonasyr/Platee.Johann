using Johann.Application.Interfaces;

namespace Johann.Infrastructure.Llm;

/// <summary>
/// Phase 1 stub — LLM not available yet.
/// Replace with OpenAiLlmProvider in Phase 3.
/// </summary>
public sealed class NoOpLlmProvider : ILlmProvider
{
    public bool IsAvailable => false;

    public Task<string> GenerateAsync(string prompt, LlmOptions options, CancellationToken ct = default)
        => Task.FromResult(string.Empty);
}
