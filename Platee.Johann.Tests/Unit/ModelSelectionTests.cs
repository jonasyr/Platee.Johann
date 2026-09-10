using FluentAssertions;
using Platee.Johann.Infrastructure.Llm;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #67 — Johann läuft auf den aktuellen OpenAI-Modellen.
/// <para>
/// Die Namen stehen hier als Test, weil ein stillschweigend zurückgedrehtes Modell sonst
/// niemandem auffiele: die App liefe weiter, nur schlechter und teurer.
/// </para>
/// </summary>
public sealed class ModelSelectionTests
{
    [Fact]
    public void Transcription_uses_gpt_transcribe()
    {
        // Billiger als whisper-1 (0,0045 statt 0,006 $/min) und deutlich genauer,
        // besonders ausserhalb des Deutschen — siehe #58.
        WhisperTranscriber.ModelName.Should().Be("gpt-transcribe");
    }

    [Fact]
    public void Summaries_use_gpt_5_6_luna()
    {
        // Von OpenAI ausdruecklich fuer "summarization, drafting, classification"
        // positioniert — genau Johanns Aufgabe.
        OpenAiLlmProvider.ModelName.Should().Be("gpt-5.6-luna");
    }

    [Fact]
    public void Transcription_does_not_force_a_language()
    {
        // Solange "de" fest verdrahtet war, konnte ein arabisches oder ukrainisches
        // Diktat nicht korrekt transkribiert werden (#58). Ohne Vorgabe erkennt das
        // Modell die Sprache selbst.
        WhisperTranscriber.ForcedLanguage.Should().BeNull();
    }
}
