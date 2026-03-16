using FluentAssertions;
using Johann.Domain.Enums;
using Johann.Domain.Parsing;

namespace Johann.Tests.Unit;

public sealed class HeaderParserTests
{
    private readonly HeaderParser _sut = new();

    // --- Type detection ---

    [Theory]
    [InlineData("Aufgabe Johann rest", EntryType.Aufgabe, "Johann")]
    [InlineData("aufgabe Johann rest", EntryType.Aufgabe, "Johann")]
    [InlineData("AUFGABE Johann rest", EntryType.Aufgabe, "Johann")]
    [InlineData("Gesprächsnotiz Iris rest", EntryType.Gesprächsnotiz, "Iris")]
    [InlineData("E-Mail Johann rest", EntryType.EMail, "Johann")]
    [InlineData("email Johann rest", EntryType.EMail, "Johann")]
    [InlineData("Stundenzettel Peano rest", EntryType.Stundenzettel, "Peano")]
    [InlineData("Projekt Johann rest", EntryType.Projekt, "Johann")]
    public void Parse_KnownTypeKeyword_SetsTypeAndProject(
        string transcript, EntryType expectedType, string expectedProject)
    {
        var result = _sut.Parse(transcript);
        result.Type.Should().Be(expectedType);
        result.ProjectName.Should().Be(expectedProject);
    }

    [Fact]
    public void Parse_NoTypeKeyword_UsesLegacyResolver()
    {
        var result = _sut.Parse("Hallo, wir arbeiten für Projekt Johann gerade");
        result.Type.Should().Be(EntryType.Projekt);
        result.ProjectName.Should().Be("Johann");
    }

    [Fact]
    public void Parse_UnknownWordNoProjectRegex_FallsBackToAllgemein()
    {
        var result = _sut.Parse("Hallo wie geht es dir heute");
        result.Type.Should().Be(EntryType.Projekt);
        result.ProjectName.Should().Be("Allgemein");
    }

    [Fact]
    public void Parse_EmptyString_ReturnsDefaults()
    {
        var result = _sut.Parse(string.Empty);
        result.Type.Should().Be(EntryType.Projekt);
        result.ProjectName.Should().Be("Allgemein");
        result.ExplicitTitle.Should().BeNull();
    }

    // --- Title extraction ---

    [Fact]
    public void Parse_TitelKeyword_ExtractsTitleUntilEnde()
    {
        // "Aufgabe Johann Titel A B C D E F G H I J K L M Ende N"
        // → Title = "A B C D E F G H I J K L M" (13 words, stops at Ende)
        var transcript = "Aufgabe Johann Titel A B C D E F G H I J K L M Ende N";
        var result = _sut.Parse(transcript);

        result.ExplicitTitle.Should().Be("A B C D E F G H I J K L M");
        result.RemainderText.Should().Be("N");
    }

    [Fact]
    public void Parse_TitelKeyword_StopsAt15WordsWithoutEnde()
    {
        var words = string.Join(" ", Enumerable.Range(1, 16).Select(i => $"W{i}"));
        var transcript = $"Aufgabe Johann Titel {words}";
        var result = _sut.Parse(transcript);

        var titleWords = result.ExplicitTitle!.Split(' ');
        titleWords.Should().HaveCount(15);
        titleWords[0].Should().Be("W1");
        titleWords[14].Should().Be("W15");
    }

    [Fact]
    public void Parse_BetreffKeyword_WorksLikeTitel()
    {
        var result = _sut.Parse("Aufgabe Johann Betreff Monatsbericht Ende");
        result.ExplicitTitle.Should().Be("Monatsbericht");
    }

    [Fact]
    public void Parse_NoTitelKeyword_ExplicitTitleIsNull()
    {
        var result = _sut.Parse("Aufgabe Johann Das ist der Rest");
        result.ExplicitTitle.Should().BeNull();
        result.RemainderText.Should().Be("Das ist der Rest");
    }

    [Fact]
    public void Parse_EndeAt16To29Words_Caps15WordsAndEndeNotInRemainder()
    {
        // "Ende" appears at word 20 (16–29 range): title capped at 15, "Ende" consumed.
        var titlePart = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"W{i}"));
        var transcript = $"Aufgabe Johann Titel {titlePart} Ende Nachtext";
        var result = _sut.Parse(transcript);

        var titleWords = result.ExplicitTitle!.Split(' ');
        titleWords.Should().HaveCount(15);
        titleWords[14].Should().Be("W15");
        result.RemainderText.Should().Be("Nachtext");
        result.RemainderText.Should().NotContain("Ende");
    }

    [Fact]
    public void Parse_EndeAt30OrMoreWords_ReturnsNullTitleAndEndeNotInRemainder()
    {
        // "Ende" appears 30 words after "Titel" → too late → GPT generates (null title).
        var titlePart = string.Join(" ", Enumerable.Range(1, 30).Select(i => $"W{i}"));
        var transcript = $"Aufgabe Johann Titel {titlePart} Ende Nachtext";
        var result = _sut.Parse(transcript);

        result.ExplicitTitle.Should().BeNull();
        result.RemainderText.Should().Be("Nachtext");
        result.RemainderText.Should().NotContain("Ende");
    }

    // --- Remainder ---

    [Fact]
    public void Parse_AfterHeaderTokens_RemainderIsCorrect()
    {
        var result = _sut.Parse("Aufgabe Johann Titel Foo Bar Ende Das ist der Inhalt");
        result.RemainderText.Should().Be("Das ist der Inhalt");
    }
}
