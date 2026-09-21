using FluentAssertions;
using Platee.Johann.Domain.Services;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Fett und kursiv innerhalb einer Zeile – für PDF und Klartext (#57, zentrale Markdown-Regel aus
/// #73). Ohne das stünden im PDF und in kopierten Mails wörtlich Sternchen.
/// </summary>
public sealed class InlineMarkdownTests
{
    [Fact]
    public void Plain_text_is_one_normal_run()
        => InlineMarkdown.Split("Nur Text.").Should().Equal(new InlineRun("Nur Text.", Bold: false, Italic: false));

    [Fact]
    public void Bold_and_italic_become_their_own_runs()
        => InlineMarkdown.Split("Frist **Freitag** und *ungefähr* drei Tage").Should().Equal(
            new InlineRun("Frist ", false, false),
            new InlineRun("Freitag", true, false),
            new InlineRun(" und ", false, false),
            new InlineRun("ungefähr", false, true),
            new InlineRun(" drei Tage", false, false));

    [Theory]
    [InlineData("3 * 4 = 12")]
    [InlineData("Faktor 2*3")]
    [InlineData("ein einzelnes ** ohne Ende")]
    public void Lone_asterisks_stay_text(string line)
        => InlineMarkdown.Split(line).Should().Equal(new InlineRun(line, false, false));

    [Fact]
    public void Plain_text_drops_headings_bold_and_italic_but_keeps_list_dashes()
        => InlineMarkdown.ToPlainText("### Kopf\n**fett** und *kursiv*\n- Punkt\r\n  - Unterpunkt")
            .Should().Be("Kopf\nfett und kursiv\n- Punkt\n  - Unterpunkt");


    [Fact]
    public void The_plain_text_mail_file_carries_no_asterisks()
    {
        var entry = new Platee.Johann.Domain.Entities.Entry
        {
            JobId = "260921_001_abc",
            SequenceNumber = 1,
            CreatedAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.FromHours(2)),
            Type = Platee.Johann.Domain.Enums.EntryType.Projekt,
            ProjectName = "Johann",
            Title = "Test",
            SourceType = "audio",
            Status = Platee.Johann.Domain.ValueObjects.ProcessingStatus.Empty,
            EmailText = "Betreff: Angebot\n\nDas Angebot kommt **Freitag**.",
        };

        var text = Platee.Johann.Infrastructure.Renderers.EmailRenderer.BuildEmailText(entry);

        text.Should().Contain("Das Angebot kommt Freitag.").And.NotContain("**");
    }
}
