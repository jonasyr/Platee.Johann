using Johann.Application.Interfaces;
using Johann.Domain.Entities;
using Johann.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Johann.Infrastructure.Renderers;

public sealed class PdfRenderer : IEntryRenderer
{
    public string RendererName => "PDF";

    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<RenderResult> RenderAsync(Entry entry, RenderOptions options,
                                          CancellationToken ct = default)
    {
        var filename = $"{entry.JobId}.pdf";
        var outputDir = options.OutputDirectory
            ?? Path.Combine(Path.GetTempPath(), "JohannPdf");

        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, filename);

        var doc = Document.Create(container =>
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI", "Arial").FontSize(10));

                page.Header().Element(header => ComposeHeader(header, entry));
                page.Content().Element(content => ComposeContent(content, entry));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Johann · ").FontColor("#999");
                    x.Span(entry.CreatedAt.ToString("dd.MM.yyyy HH:mm")).FontColor("#999");
                });
            }));

        doc.GeneratePdf(filePath);

        var bytes = File.ReadAllBytes(filePath);
        return Task.FromResult(new RenderResult(bytes, "application/pdf", filename));
    }

    private static void ComposeHeader(IContainer header, Entry entry)
    {
        header.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    var badgeColor = entry.Type switch
                    {
                        EntryType.Aufgabe        => "#E63123",
                        EntryType.Gesprächsnotiz => "#2980B9",
                        EntryType.EMail          => "#27AE60",
                        EntryType.Stundenzettel  => "#8E44AD",
                        _                        => "#555555",
                    };

                    inner.Item().Row(r =>
                    {
                        r.AutoItem()
                         .Background(badgeColor)
                         .PaddingHorizontal(6).PaddingVertical(2)
                         .Text(entry.Type.ToString())
                         .FontColor("#FFFFFF").FontSize(8).Bold();

                        r.AutoItem().Width(8);

                        r.RelativeItem()
                         .Text(entry.ProjectName)
                         .FontColor("#666").FontSize(10);
                    });

                    inner.Item().PaddingTop(4)
                         .Text(entry.Title)
                         .FontSize(16).Bold();
                });

                row.AutoItem().AlignRight().Column(meta =>
                {
                    meta.Item().Text(entry.CreatedAt.ToString("dd.MM.yyyy"))
                        .FontColor("#888").FontSize(9);
                    if (entry.DurationSeconds > 0)
                        meta.Item().Text(FormatDuration(entry.DurationSeconds))
                            .FontColor("#888").FontSize(9);
                    meta.Item().Text($"#{entry.SequenceNumber:D3}")
                        .FontColor("#888").FontSize(9);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DDDDDD");
        });
    }

    private static void ComposeContent(IContainer content, Entry entry)
    {
        content.Column(col =>
        {
            col.Spacing(10);

            switch (entry.Type)
            {
                case EntryType.Stundenzettel:
                    ComposeStundenzettel(col, entry);
                    break;
                case EntryType.Aufgabe:
                    ComposeAufgabe(col, entry);
                    break;
                case EntryType.Gesprächsnotiz:
                    ComposeGesprächsnotiz(col, entry);
                    break;
                default:
                    ComposeStandard(col, entry);
                    break;
            }
        });
    }

    private static void ComposeStandard(ColumnDescriptor col, Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Abstract))
        {
            col.Item().Element(c => Section(c, "Kurzfassung",
                entry.Abstract!, "#FFF5F4", "#FFDBD8"));
        }

        if (!string.IsNullOrWhiteSpace(entry.LongSummary))
        {
            col.Item().Element(c => Section(c, "Zusammenfassung",
                entry.LongSummary!, "#F5F5F5", "#E0E0E0"));
        }

        if (!string.IsNullOrWhiteSpace(entry.ProseSummary))
        {
            col.Item().Element(c => Section(c, "Ausführlich",
                entry.ProseSummary!, "#F0F8FF", "#B8D4F0"));
        }

        if (!string.IsNullOrWhiteSpace(entry.Transcript))
        {
            col.Item().Element(c => Section(c, "Transkript",
                entry.Transcript!, "#FAFAFA", "#E8E8E8"));
        }
    }

    private static void ComposeAufgabe(ColumnDescriptor col, Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Abstract))
            col.Item().Element(c => Section(c, "Kurzfassung", entry.Abstract!, "#FFF5F4", "#FFDBD8"));

        if (!string.IsNullOrWhiteSpace(entry.TaskList))
        {
            col.Item().Column(inner =>
            {
                inner.Item().Text("Aufgaben").Bold().FontSize(11).FontColor("#555");
                inner.Item().PaddingTop(4)
                     .Border(1).BorderColor("#E63123")
                     .Background("#FFF8F8")
                     .Padding(10)
                     .Text(entry.TaskList!).FontSize(10);
            });
        }

        if (!string.IsNullOrWhiteSpace(entry.LongSummary))
            col.Item().Element(c => Section(c, "Zusammenfassung", entry.LongSummary!, "#F5F5F5", "#E0E0E0"));
    }

    private static void ComposeGesprächsnotiz(ColumnDescriptor col, Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.ConversationNote))
        {
            col.Item().Column(inner =>
            {
                inner.Item().Text("Gesprächsnotiz").Bold().FontSize(11).FontColor("#2980B9");
                inner.Item().PaddingTop(4)
                     .Border(1).BorderColor("#2980B9")
                     .Background("#F0F8FF")
                     .Padding(10)
                     .Text(entry.ConversationNote!).FontSize(10);
            });
        }

        if (!string.IsNullOrWhiteSpace(entry.Abstract))
            col.Item().Element(c => Section(c, "Kurzfassung", entry.Abstract!, "#FFF5F4", "#FFDBD8"));

        if (!string.IsNullOrWhiteSpace(entry.LongSummary))
            col.Item().Element(c => Section(c, "Zusammenfassung", entry.LongSummary!, "#F5F5F5", "#E0E0E0"));
    }

    private static void ComposeStundenzettel(ColumnDescriptor col, Entry entry)
    {
        // Compact layout: just the essentials
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1);
                c.RelativeColumn(3);
            });

            void Row(string label, string value)
            {
                table.Cell().Background("#F5F5F5").Padding(6)
                     .Text(label).Bold().FontSize(10);
                table.Cell().Padding(6)
                     .Text(value).FontSize(10);
            }

            Row("Datum", entry.CreatedAt.ToString("dd.MM.yyyy"));
            Row("Projekt", entry.ProjectName);
            Row("Titel", entry.Title);

            if (entry.DurationSeconds > 0)
                Row("Dauer", FormatDuration(entry.DurationSeconds));

            if (!string.IsNullOrWhiteSpace(entry.Abstract))
                Row("Beschreibung", entry.Abstract!);
        });

        if (!string.IsNullOrWhiteSpace(entry.LongSummary))
            col.Item().Element(c => Section(c, "Details", entry.LongSummary!, "#F5F5F5", "#E0E0E0"));
    }

    private static void Section(IContainer container, string title,
                                 string body, string bg, string border)
    {
        container.Column(col =>
        {
            col.Item().Text(title).Bold().FontSize(11).FontColor("#555");
            col.Item().PaddingTop(4)
               .Border(1).BorderColor(border)
               .Background(bg)
               .Padding(10)
               .Text(body).FontSize(10);
        });
    }

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}
