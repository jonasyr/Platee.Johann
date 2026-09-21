namespace Platee.Johann.Tests.Unit;

using System.Text.RegularExpressions;
using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

public sealed class SummaryPromptsTests
{
    // ── Systemnachricht (#73, GPT-5.6) ────────────────────────────────────────
    [Fact]
    public void SystemMessage_DoesNotPrescribeThinkingSteps()
    {
        // Reasoning-Modelle denken selbst; ein vorgeschriebener Denkprozess kostete ~300 Token
        // je Aufruf und enthielt die Widersprüche S-03/S-04 (Befundliste #73).
        SummaryPrompts.SystemMessage.Should().NotContain("CHAIN OF THOUGHTS");
        SummaryPrompts.SystemMessage.Should().NotContain("DENKPROZESS");
    }

    [Fact]
    public void SystemMessage_IsWrittenInNormalCase()
    {
        // Durchgehende Versalien tokenisierten 37 % schlechter, ohne belegten Nutzen.
        Regex.Matches(SummaryPrompts.SystemMessage, @"\b[A-ZÄÖÜ]{5,}\b").Should().BeEmpty();
    }

    [Fact]
    public void SystemMessage_ResolvesUnclearContentWithOneDecisionRule()
    {
        // Früher: „Unklarheiten markieren" gegen „NIEMALS unklare Formulierungen stehen lassen".
        SummaryPrompts.SystemMessage.Should().Contain("benenne es knapp als unklar");
        SummaryPrompts.SystemMessage.Should().NotContain("NIEMALS");
    }

    [Fact]
    public void SystemMessage_LeavesTheDegreeOfCompressionToTheSection()
    {
        // Sonst widerspricht „fasse zusammen" der vollständigen Aufbereitung (P-01).
        SummaryPrompts.SystemMessage.Should().Contain("vollständige Aufbereitung");
    }

    [Fact]
    public void SystemMessage_KeepsTheRegisterExamplePair()
    {
        // Formmuster, kein Aufgabenbeispiel — ausdrücklich behalten (S-08).
        SummaryPrompts.SystemMessage.Should().Contain("Also wir haben irgendwie über das Projekt geredet");
        SummaryPrompts.SystemMessage.Should().Contain("Es wurde der aktuelle Stand des Projekts besprochen");
    }

    [Theory]
    [InlineData(nameof(SummaryPrompts.Abstract))]
    [InlineData(nameof(SummaryPrompts.Structured))]
    [InlineData(nameof(SummaryPrompts.Gespraechsnotiz))]
    [InlineData(nameof(SummaryPrompts.Stundenzettel))]
    [InlineData(nameof(SummaryPrompts.Analog))]
    public void Section_prompts_do_not_claim_the_transcript_is_German(string name)
    {
        // Seit #58 bleibt das Transkript in der gesprochenen Sprache (B-01).
        var prompt = name switch
        {
            nameof(SummaryPrompts.Abstract) => SummaryPrompts.Abstract,
            nameof(SummaryPrompts.Structured) => SummaryPrompts.Structured,
            nameof(SummaryPrompts.Gespraechsnotiz) => SummaryPrompts.Gespraechsnotiz,
            nameof(SummaryPrompts.Stundenzettel) => SummaryPrompts.Stundenzettel,
            nameof(SummaryPrompts.Analog) => SummaryPrompts.Analog,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

        prompt.Should().NotContain("auf Deutsch");
    }

    // ── Leerfälle (#73) ───────────────────────────────────────────────────────
    [Fact]
    public void Gespraechsnotiz_ReportsDictationsWithoutAConversation()
    {
        // Ohne diese Regel entstand auf 34 von 46 Diktaten ohne Gespräch eine Notiz.
        SummaryPrompts.Gespraechsnotiz.Should().Contain("Kein Gespräch dokumentiert.");
        SummaryPrompts.Gespraechsnotiz.Should().Contain("Aufgabenbeschreibungen");
    }

    [Fact]
    public void Abstract_Stundenzettel_Analog_HaveAnExplicitEmptyCase()
    {
        SummaryPrompts.Abstract.Should().Contain("Kein zusammenfassbarer Inhalt.");
        SummaryPrompts.Stundenzettel.Should().Contain("Keine Zeiten genannt.");
        SummaryPrompts.Analog.Should().Contain("Kein Eintrag erkennbar.");
    }

    [Fact]
    public void Abstract_KeepsTheWordLimitPlaceholder()
    {
        SummaryPrompts.Abstract.Should().Contain("{word_limit}");
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
    public void Email_SiezenCoversRequestsWithAFormExample()
    {
        // „Lieber Jonas" zog die Mail ins Du; ein Du-Imperativ („bitte prüfe") sieht das
        // Pronomen-Verbot nicht. Das Formmuster behebt beides (#73).
        SummaryPrompts.Email.Should().Contain("„Bitte prüfen Sie …“, nicht „Bitte prüfe …“");
    }

    [Fact]
    public void Email_DropsPassagesMarkedAsUnclear()
    {
        // Transkriptionslücken („[inhaltlich unklar]") gehören in den Eintrag, nicht in eine Kundenmail.
        SummaryPrompts.Email.Should().Contain("als unklar kennzeichnet, lässt du weg");
    }

    [Fact]
    public void SystemMessage_AsksForMarkdownWhereItCarriesMeaning()
    {
        // Zentrale Markdown-Regel (#73, S4): erst seit #57 Markdown in der Mail nach HTML wandelt
        // und PDF wie Klartext-Kopien Fett und kursiv verstehen.
        SummaryPrompts.SystemMessage.Should().Contain("Formatiere die Antwort in Markdown");
        SummaryPrompts.SystemMessage.Should().Contain("Gibt der Abschnitt reinen Text oder eine andere Form vor, gilt seine Vorgabe");
    }

    [Fact]
    public void Email_NoLongerForbidsMarkdown_ButStaysFlowingText()
    {
        // Der Text darf Markdown tragen (#57 wandelt es in HTML) ...
        SummaryPrompts.Email.Should().NotContain("Reiner Text ohne Markdown");
        SummaryPrompts.Email.Should().Contain("Fließtext, keine Stichpunkte");

        // ... nur die Betreffzeile nicht: fett gesetzt fand Johann sie im Messlauf nicht mehr.
        SummaryPrompts.Email.Should().Contain("die Betreffzeile steht als reiner Text ohne Markdown in der ersten Zeile");
    }

    [Fact]
    public void Abstract_StaysPlainText_BecauseItShowsBelowTheTitle()
        => SummaryPrompts.Abstract.Should().Contain("ohne Überschrift, Aufzählung oder Markdown-Zeichen");

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
    public void Aufgabe_AsksForASummaryParagraphBeforeTheTaskList()
    {
        SummaryPrompts.Aufgabe.Should().Contain("zwei bis vier Sätzen");
        SummaryPrompts.Aufgabe.Should().Contain("Aufgabenliste");
    }

    [Fact]
    public void Aufgabe_ForbidsHeadingsInTheOutput()
    {
        // Die App setzt „Aufgaben" bereits als Abschnittsüberschrift. Gibt das Modell
        // nochmal eine aus, steht sie doppelt da — in der Detailansicht, im PDF und in
        // der Aufgaben-Mail.
        SummaryPrompts.Aufgabe.Should().Contain("keine Überschriften");
    }

    [Fact]
    public void Aufgabe_ForbidsItsOwnTaskNumbering()
    {
        // gpt-5-nano hat „Aufgabe 1:", „Aufgabe 2:" vor jeden Stichpunkt gesetzt,
        // obwohl die Aufzählung ohnehin nummeriert.
        SummaryPrompts.Aufgabe.Should().Contain("Aufgabe 1:");
    }

    [Fact]
    public void Aufgabe_ShowsAWorkedExampleInsteadOfDictatingWordOrder()
    {
        // „Beginne jede Zeile mit einem Verb im Infinitiv" erzeugte kaputtes Deutsch:
        // „verschriftlichen Diktate automatisch in OneDrive speichern". Im Deutschen steht
        // der Infinitiv am Satzende. Ein Beispiel zeigt die Form, ohne die Wortstellung
        // vorzuschreiben — genau das empfiehlt der OpenAI-Leitfaden.
        SummaryPrompts.Aufgabe.Should().Contain("Beispiel");
        SummaryPrompts.Aufgabe.Should().NotContain("Verb im Infinitiv");
    }

    [Fact]
    public void Aufgabe_SeparatesTheListMarkerFromTheTaskText()
    {
        // Codex-Review zu #75: „jede Zeile beginnt mit einem Bindestrich" und „beginne
        // jede Zeile mit einem Verb" sind woertlich gelesen unerfuellbar. Ein schwaches
        // Modell loest das womoeglich, indem es den Listenmarker weglaesst.
        SummaryPrompts.Aufgabe.Should().Contain("nach dem Bindestrich");
    }

    [Fact]
    public void Aufgabe_LimitsHowManyTasksAreProduced()
    {
        // gpt-5-nano zerlegte ein Diktat in 15 Einzeiler, darunter „auswerten durch
        // ChatGPT" — das ist keine eigene Aufgabe.
        SummaryPrompts.Aufgabe.Should().Contain("höchstens acht Aufgaben");
    }

    [Fact]
    public void Aufgabe_CapsTheLengthOfASingleTask()
    {
        // Ohne Obergrenze liefert das Modell ganze Absaetze statt abhakbarer Aufgaben.
        SummaryPrompts.Aufgabe.Should().Contain("höchstens 20 Wörter");
    }

    [Fact]
    public void Aufgabe_AsksForMarkdownExplicitly()
    {
        // GPT-5 formatiert von sich aus kein Markdown — das muss im Prompt stehen,
        // sonst kommt Fliesstext an, wo die App eine Aufzaehlung rendern will.
        SummaryPrompts.Aufgabe.Should().Contain("Markdown-Aufzählung");
    }

    [Fact]
    public void Aufgabe_TellsTheModelWhatToWriteWhenThereAreNoTasks()
    {
        // Ohne diese Regel erfindet das Modell Aufgaben, statt die Liste leer zu lassen.
        SummaryPrompts.Aufgabe.Should().Contain("Keine Aufgaben genannt");
    }

    [Fact]
    public void Aufgabe_ContainsStructureRequirements()
    {
        SummaryPrompts.Aufgabe.Should().Contain("zusammengehörige Handlungen");
        SummaryPrompts.Aufgabe.Should().Contain("Frist");
        SummaryPrompts.Aufgabe.Should().Contain("zuständige Person");
    }

    [Fact]
    public void Aufgabe_ContainsNoDuplicatesRule()
    {
        SummaryPrompts.Aufgabe.Should().Contain("keine Dopplungen");
    }

    // ── Keine doppelten Abschnittsüberschriften ───────────────────────────────
    [Theory]
    [InlineData(nameof(SummaryPrompts.Gespraechsnotiz))]
    [InlineData(nameof(SummaryPrompts.Stundenzettel))]
    [InlineData(nameof(SummaryPrompts.Analog))]
    [InlineData(nameof(SummaryPrompts.Prose))]
    public void Section_prompts_do_not_repeat_the_heading_the_app_already_renders(string name)
    {
        // Die App überschreibt jeden Abschnitt selbst („Gesprächsnotiz", „Analog", …).
        // Schreibt das Modell dieselbe Überschrift nochmal, steht sie doppelt da — in der
        // Detailansicht, im PDF und in der Aufgaben-Mail.
        var prompt = name switch
        {
            nameof(SummaryPrompts.Gespraechsnotiz) => SummaryPrompts.Gespraechsnotiz,
            nameof(SummaryPrompts.Stundenzettel) => SummaryPrompts.Stundenzettel,
            nameof(SummaryPrompts.Analog) => SummaryPrompts.Analog,
            nameof(SummaryPrompts.Prose) => SummaryPrompts.Prose,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

        prompt.Should().Contain("keine Überschrift für den Abschnitt");
    }

    // ── Ausgabesprache (#58) ──────────────────────────────────────────────────
    [Fact]
    public void SystemMessage_ForcesGermanOutputRegardlessOfTheDictationLanguage()
    {
        // Ohne diese Anweisung spiegelt das Modell die Eingabesprache: ein arabisches
        // Diktat ergaebe eine arabische Zusammenfassung. Johann ist einsprachig deutsch.
        SummaryPrompts.SystemMessage.Should().Contain("unabhängig von der Sprache des Diktats");
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
