namespace Platee.Johann.Tests.Unit;

using System.Text;
using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Renderers;
using UglyToad.PdfPig;

/// <summary>
/// #112: HTML and PDF show the transcript one sentence per line, like the detail view. The
/// stored transcript is untouched — only the rendered output breaks.
/// </summary>
public sealed class TranscriptSentenceLinesRenderTests : IDisposable
{
    private const string Transcript = "Hallo Thomas. Wie geht es dir? Bis morgen.";

    private readonly string tempDir = Path.Combine(Path.GetTempPath(), $"Johann112_{Guid.NewGuid():N}");

    public TranscriptSentenceLinesRenderTests() => Directory.CreateDirectory(this.tempDir);

    public void Dispose() => Directory.Delete(this.tempDir, recursive: true);

    [Fact]
    public async Task Html_BreaksTheTranscriptAfterEachSentence()
    {
        var result = await new HtmlRenderer().RenderAsync(
            MakeEntry(), new RenderOptions(this.tempDir, IncludeTranscript: true), CancellationToken.None);

        Encoding.UTF8.GetString(result.Data)
            .Should().Contain("Hallo Thomas.<br>Wie geht es dir?<br>Bis morgen.");
    }

    [Fact]
    public async Task Pdf_PutsEachSentenceOfTheTranscriptOnItsOwnLine()
    {
        var renderer = new PdfRenderer(new SettingsHolder(AppSettings.Default));
        var result = await renderer.RenderAsync(
            MakeEntry(), new RenderOptions(this.tempDir, IncludeTranscript: true), CancellationToken.None);

        using var pdf = PdfDocument.Open(result.Data);
        var words = pdf.GetPages().SelectMany(p => p.GetWords()).ToList();
        double Baseline(string text) => words.Single(w => w.Text == text).BoundingBox.Bottom;

        Baseline("Thomas.").Should().NotBe(Baseline("Wie"), "„Wie“ beginnt einen neuen Satz, also eine neue Zeile");
        Baseline("dir?").Should().NotBe(Baseline("Bis"));
        Baseline("Hallo").Should().Be(Baseline("Thomas."), "innerhalb eines Satzes wird nicht umbrochen");
    }

    private static Entry MakeEntry() => new()
    {
        JobId = "260925_112_abc",
        SequenceNumber = 112,
        CreatedAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(2)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Satzweise",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = Transcript,
    };
}
