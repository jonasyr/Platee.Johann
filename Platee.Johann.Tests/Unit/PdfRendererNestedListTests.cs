namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Renderers;

/// <summary>
/// Das PDF rendert verschachtelte Listen, Aufgaben-Kästchen und tiefe Ebenen ohne Fehler (#83).
/// Die Zuordnung Einrückung → Ebene prüft <see cref="BulletOutlineTests"/>; hier geht es darum,
/// dass der QuestPDF-Pfad damit ein Dokument erzeugt, auch mit den Zeichen ◦ und ▪.
/// </summary>
public sealed class PdfRendererNestedListTests
{
    private const string NestedSummary =
        "### Kernaussagen\n" +
        "- Oberpunkt mit zwei Leerzeichen\n" +
        "  - Unterpunkt\n" +
        "    - Unter-Unterpunkt\n" +
        "      - noch tiefer\n" +
        "- Oberpunkt mit vier Leerzeichen\n" +
        "    - Unterpunkt\n" +
        "\n" +
        "### Offene Punkte / ToDos\n" +
        "- [ ] Angebot schicken\n" +
        "- [x] Termin bestätigt\n" +
        "  - eingerückt ohne Oberpunkt davor ist hier keiner\n";

    [Fact]
    public async Task A_summary_with_nested_lists_renders_to_a_pdf()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "JohannPdfTest-83");
        var renderer = new PdfRenderer(new SettingsHolder(AppSettings.Default));

        var result = await renderer.RenderAsync(NestedEntry(), new RenderOptions(OutputDirectory: outputDir));

        result.Data.Should().NotBeEmpty();
        result.MimeType.Should().Be("application/pdf");
    }

    private static Entry NestedEntry() => new()
    {
        JobId = "260921_083_abc",
        SequenceNumber = 83,
        CreatedAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.FromHours(2)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Verschachtelte Listen",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "Ein Transkript.",
        LongSummary = NestedSummary,
    };
}
