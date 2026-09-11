namespace Platee.Johann.Application.Interfaces;

/// <summary>
/// Optionen für einen einzelnen LLM-Aufruf.
/// <para>
/// <c>Model</c> ist angehängt und optional, damit alle vorhandenen Aufrufstellen und die
/// Test-Fakes unverändert weiterlaufen; <c>null</c> heißt „nimm den Standard des Providers“.
/// </para>
/// </summary>
/// <param name="MaxTokens">Obergrenze für die Ausgabe.</param>
/// <param name="UseReasoning">
/// Wird derzeit <b>nirgends ausgewertet</b>. Das OpenAI-SDK 2.2.0 kennt zwar
/// <c>reasoning_effort</c>, Johann setzt es aber nicht — die Modelle denken im
/// Standardverhalten. Ob eine niedrigere Stufe sinnvoll ist, entscheidet #73:
/// gemessen spart <c>low</c> rund ein Viertel der Ausgabe-Token bei nahezu gleicher
/// Textlänge, <c>high</c> kostet 50 % mehr ohne mehr Inhalt. Ob die Qualität gleich
/// bleibt, sagt keine Token-Zählung.
/// </param>
/// <param name="Model">Die Modell-Id, oder <c>null</c> für den Standard.</param>
public sealed record LlmOptions(int MaxTokens = 20000, bool UseReasoning = false, string? Model = null);

public interface ILlmProvider
{
    bool IsAvailable { get; }

    Task<string> GenerateAsync(
        string systemPrompt,
        string userContent,
        LlmOptions options,
        CancellationToken ct = default);
}
