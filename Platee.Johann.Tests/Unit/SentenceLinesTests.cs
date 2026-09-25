namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Domain.Services;

/// <summary>
/// #112: the transcript is shown one sentence per line. Only the display changes — the stored
/// transcript stays the literal record — so a wrong break costs readability, never data. The
/// cases below are the ones the issue names: abbreviations, numbers, dates, times, ellipses,
/// URLs/mail addresses and punctuation inside closing quotes or brackets.
/// </summary>
public sealed class SentenceLinesTests
{
    [Theory]
    [InlineData("Hallo Thomas. Wie geht es dir? Gut!", "Hallo Thomas.\nWie geht es dir?\nGut!")]
    [InlineData("Erster Satz.  Zweiter Satz.", "Erster Satz.\nZweiter Satz.")]
    [InlineData("Ende.", "Ende.")]
    [InlineData("Kein Satzzeichen", "Kein Satzzeichen")]
    [InlineData("", "")]
    public void BreaksAfterEachSentenceEnd(string input, string expected) =>
        SentenceLines.Split(input).Should().Be(expected);

    [Theory]
    [InlineData("Wir brauchen z. B. Holz. Dann geht es los.", "Wir brauchen z. B. Holz.\nDann geht es los.")]
    [InlineData("Das kostet ca. 500 Euro. Passt.", "Das kostet ca. 500 Euro.\nPasst.")]
    [InlineData("Termin mit Dr. Berger. Danach Büro.", "Termin mit Dr. Berger.\nDanach Büro.")]
    [InlineData("Siehe Nr. 4 im Plan. Fertig.", "Siehe Nr. 4 im Plan.\nFertig.")]
    [InlineData("Holz, Stahl usw. Alles da.", "Holz, Stahl usw.\nAlles da.")]
    [InlineData("Herr M. Müller kommt. Gut.", "Herr M. Müller kommt.\nGut.")]
    public void DoesNotBreakAfterAbbreviationsOrInitials(string input, string expected) =>
        SentenceLines.Split(input).Should().Be(expected);

    [Theory]
    [InlineData("Der Wert ist 2.5 Meter. Gut.", "Der Wert ist 2.5 Meter.\nGut.")]
    [InlineData("Das sind 10.000 Euro. Gut.", "Das sind 10.000 Euro.\nGut.")]
    [InlineData("Termin am 24.09. Bitte bestätigen.", "Termin am 24.09. Bitte bestätigen.")]
    [InlineData("Im 3. Stock. Rechts.", "Im 3. Stock.\nRechts.")]
    [InlineData("Um 10.30 Uhr. Dann Pause.", "Um 10.30 Uhr.\nDann Pause.")]
    public void DoesNotBreakInsideNumbersDatesOrTimes(string input, string expected) =>
        SentenceLines.Split(input).Should().Be(expected);

    [Theory]
    [InlineData("Also... Das ist so. Gut.", "Also... Das ist so.\nGut.")]
    [InlineData("Mail an info@peano.de. Danke.", "Mail an info@peano.de.\nDanke.")]
    [InlineData("Siehe www.peano.de/johann. Danke.", "Siehe www.peano.de/johann.\nDanke.")]
    public void DoesNotBreakInEllipsesUrlsOrAddresses(string input, string expected) =>
        SentenceLines.Split(input).Should().Be(expected);

    [Theory]
    [InlineData("Er sagte „Komm morgen.“ Dann ging er.", "Er sagte „Komm morgen.“\nDann ging er.")]
    [InlineData("Das ist wichtig (sehr wichtig!). Weiter.", "Das ist wichtig (sehr wichtig!).\nWeiter.")]
    [InlineData("He said \"Come tomorrow.\" Then he left.", "He said \"Come tomorrow.\"\nThen he left.")]
    public void KeepsClosingQuotesAndBracketsOnTheSentence(string input, string expected) =>
        SentenceLines.Split(input).Should().Be(expected);

    [Fact]
    public void KeepsExistingLineBreaks_AndDoesNotAddBlankLines() =>
        SentenceLines.Split("Erster Satz.\nZweiter Satz. Dritter.")
            .Should().Be("Erster Satz.\nZweiter Satz.\nDritter.");

    [Fact]
    public void TreatsForeignLanguageTranscriptsTheSame() =>
        SentenceLines.Split("Quick note after the call. They want the plans by Tuesday! OK?")
            .Should().Be("Quick note after the call.\nThey want the plans by Tuesday!\nOK?");

    [Fact]
    public void DoesNotBreakBeforeALowercaseContinuation() =>
        SentenceLines.Split("Das war gut. oder? Ja.")
            .Should().Be("Das war gut. oder?\nJa.");
}
