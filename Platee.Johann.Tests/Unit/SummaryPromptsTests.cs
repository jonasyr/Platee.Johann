namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

public sealed class SummaryPromptsTests
{
    [Fact]
    public void SystemMessage_ContainsSpecializedBusinessCommunicationInstructions()
    {
        SummaryPrompts.SystemMessage.Should().Contain("HOCHSPEZIALISIERTER EXPERTE");
        SummaryPrompts.SystemMessage.Should().Contain("### WHAT NOT TO DO ###");
    }

    // ── Structured ────────────────────────────────────────────────────────────
    [Fact]
    public void Structured_ContainsNewSections()
    {
        SummaryPrompts.Structured.Should().Contain("### Kontext");
        SummaryPrompts.Structured.Should().Contain("### Kernaussagen");
        SummaryPrompts.Structured.Should().Contain("### Entscheidungen");
        SummaryPrompts.Structured.Should().Contain("### Offene Punkte / ToDos");
    }

    [Fact]
    public void Structured_DoesNotContainOldSections()
    {
        SummaryPrompts.Structured.Should().NotContain("### Kontext & Ziel");
        SummaryPrompts.Structured.Should().NotContain("### Hauptpunkte");
        SummaryPrompts.Structured.Should().NotContain("### Entscheidungen / Erkenntnisse");
        SummaryPrompts.Structured.Should().NotContain("### Zusätzliche Details");
    }

    [Fact]
    public void Structured_DoesNotUseWordLimitPlaceholder()
    {
        SummaryPrompts.Structured.Should().NotContain("{word_limit}");
    }

    [Fact]
    public void Structured_ContainsTranscriptPlaceholder()
    {
        SummaryPrompts.Structured.Should().Contain("{transcript}");
    }

    [Fact]
    public void Structured_EnforcesExclusiveSectionAssignment()
    {
        SummaryPrompts.Structured.Should().Contain("Jede Information darf nur einer Überschrift zugeordnet werden");
    }

    // ── Email ─────────────────────────────────────────────────────────────────
    [Fact]
    public void Email_ContainsSiezenRequirement()
    {
        SummaryPrompts.Email.Should().Contain("siezen");
    }

    [Fact]
    public void Email_ContainsIchPerspektive()
    {
        SummaryPrompts.Email.Should().Contain("Ich-Perspektive");
    }

    [Fact]
    public void Email_ContainsGreetingWithNameRecognition()
    {
        SummaryPrompts.Email.Should().Contain("Namen des Empfängers erkennen");
    }

    [Fact]
    public void Email_ContainsFließtextRequirement()
    {
        SummaryPrompts.Email.Should().Contain("Fließtext, keine Stichpunkte");
    }

    [Fact]
    public void Email_ContainsProseSummaryPlaceholder()
    {
        SummaryPrompts.Email.Should().Contain("{prose_summary}");
    }

    // ── Aufgabe ───────────────────────────────────────────────────────────────
    [Fact]
    public void Aufgabe_StartsWithContextSentenceInstruction()
    {
        SummaryPrompts.Aufgabe.Should().Contain("Gebe in einem Satz den Kontext an");
    }

    [Fact]
    public void Aufgabe_ContainsStructureRequirements()
    {
        SummaryPrompts.Aufgabe.Should().Contain("fasse zusammengehörige Handlungen zu einer Aufgabe zusammen");
        SummaryPrompts.Aufgabe.Should().Contain("falls vorhanden: nenne Frist");
        SummaryPrompts.Aufgabe.Should().Contain("falls vorhanden: nenne Person die Aufgabe ausführen soll");
    }

    [Fact]
    public void Aufgabe_ContainsNoDuplicatesRule()
    {
        SummaryPrompts.Aufgabe.Should().Contain("keine Dopplungen");
    }

    [Fact]
    public void Aufgabe_ContainsTranscriptionErrorCorrectionRule()
    {
        SummaryPrompts.Aufgabe.Should().Contain("Transkriptions- und Spracherkennungsfehler");
    }

    [Fact]
    public void Aufgabe_ContainsTranscriptPlaceholder()
    {
        SummaryPrompts.Aufgabe.Should().Contain("{transcript}");
    }

    [Fact]
    public void Prose_UsesWholeSentenceReadabilityInstruction()
    {
        SummaryPrompts.Prose.Should().Contain("In ganzen Sätzen");
        SummaryPrompts.Prose.Should().Contain("{transcript}");
        SummaryPrompts.Prose.Should().NotContain("Fließtext-Zusammenfassung");
    }

    // ── PromptSettings.Default uses SummaryPrompts ───────────────────────────
    [Fact]
    public void PromptSettings_Default_StructuredPrompt_MatchesSummaryPrompts()
    {
        PromptSettings.Default.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
    }

    [Fact]
    public void PromptSettings_Default_EmailPrompt_MatchesSummaryPrompts()
    {
        PromptSettings.Default.EmailPrompt.Should().Be(SummaryPrompts.Email);
    }

    [Fact]
    public void PromptSettings_Default_AufgabePrompt_MatchesSummaryPrompts()
    {
        PromptSettings.Default.AufgabePrompt.Should().Be(SummaryPrompts.Aufgabe);
    }
}
