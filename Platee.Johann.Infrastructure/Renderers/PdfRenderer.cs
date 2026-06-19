namespace Platee.Johann.Infrastructure.Renderers;

using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public sealed class PdfRenderer : IEntryRenderer
{
    public string RendererName => "PDF";

    private readonly SettingsHolder settingsHolder;
    private static readonly byte[] logoBytes = LoadLogoBytes();

    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PdfRenderer(SettingsHolder settingsHolder)
    {
        this.settingsHolder = settingsHolder;
    }

    private static byte[] LoadLogoBytes()
    {
        var asm = typeof(PdfRenderer).Assembly;
        using var stream = asm.GetManifestResourceStream(
            "Platee.Johann.Infrastructure.Assets.Peano_Logo.png");
        if (stream is null)
        {
            return [];
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public Task<RenderResult> RenderAsync(Entry entry, RenderOptions options,
                                          CancellationToken ct = default)
    {
        var s = this.settingsHolder.Current;
        var filename = FilenameBuilder.Build(entry) + ".pdf";
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

                var sections = options.Sections ?? new SectionVisibility();
                page.Header().Element(header => ComposeHeader(header, entry));
                page.Content().Element(content => ComposeContent(content, entry, sections));
                page.Footer().Column(col =>
                {
                    col.Item().AlignCenter().Text(x =>
                    {
                        x.Span($"{s.Name} · {s.Firma}").FontColor("#666666").FontSize(9);
                    });
                    col.Item().AlignCenter().Text(x =>
                    {
                        x.Span("Generiert mit KI-Unterstützung · Johann · ").FontColor("#999999").FontSize(8);
                        x.Span(entry.CreatedAt.ToString("dd.MM.yyyy HH:mm")).FontColor("#999999").FontSize(8);
                    });
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
                        EntryType.Aufgabe => "#E63123",
                        EntryType.Gesprächsnotiz => "#2980B9",
                        EntryType.EMail => "#27AE60",
                        EntryType.Stundenzettel => "#8E44AD",
                        EntryType.Analog => "#795548",
                        _ => "#555555",
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
                    if (logoBytes.Length > 0)
                    {
                        meta.Item().AlignRight().Height(28).Image(logoBytes);
                    }

                    meta.Item().Text(entry.CreatedAt.ToString("dd.MM.yyyy"))
                        .FontColor("#888").FontSize(9);
                    if (entry.DurationSeconds > 0)
                    {
                        meta.Item().Text(FormatDuration(entry.DurationSeconds))
                            .FontColor("#888").FontSize(9);
                    }

                    meta.Item().Text($"#{entry.SequenceNumber:D3}")
                        .FontColor("#888").FontSize(9);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DDDDDD");
        });
    }

    private static void ComposeContent(IContainer content, Entry entry, SectionVisibility sections)
    {
        content.Column(col =>
        {
            col.Spacing(10);

            if (!string.IsNullOrWhiteSpace(entry.Abstract))
            {
                col.Item().Element(c => Section(c, "Kurzfassung", entry.Abstract!, "#FFF5F4", "#FFDBD8"));
            }

            if (sections.TaskList && !string.IsNullOrWhiteSpace(entry.TaskList))
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

            if (sections.ConversationNote && !string.IsNullOrWhiteSpace(entry.ConversationNote))
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

            if (sections.StundenzettelText && !string.IsNullOrWhiteSpace(entry.StundenzettelText))
            {
                col.Item().Column(inner =>
                {
                    inner.Item().Text("Stundenzettel").Bold().FontSize(11).FontColor("#8E44AD");
                    inner.Item().PaddingTop(4)
                         .Border(1).BorderColor("#8E44AD")
                         .Background("#FAF0FF")
                         .Padding(10)
                         .Text(entry.StundenzettelText!).FontSize(10);
                });
            }

            if (sections.AnalogText && !string.IsNullOrWhiteSpace(entry.AnalogText))
            {
                col.Item().Column(inner =>
                {
                    inner.Item().Text("Analog").Bold().FontSize(11).FontColor("#555");
                    inner.Item().PaddingTop(4)
                         .Border(1).BorderColor("#888888")
                         .Background("#F8F8F8")
                         .Padding(10)
                         .Text(entry.AnalogText!).FontSize(10);
                });
            }

            if (sections.EmailText && !string.IsNullOrWhiteSpace(entry.EmailText))
            {
                col.Item().Column(inner =>
                {
                    inner.Item().Text("E-Mail").Bold().FontSize(11).FontColor("#27AE60");
                    inner.Item().PaddingTop(4)
                         .Border(1).BorderColor("#27AE60")
                         .Background("#F0FFF4")
                         .Padding(10)
                         .Text(entry.EmailText!).FontSize(10);
                });
            }

            if (sections.LongSummary && !string.IsNullOrWhiteSpace(entry.LongSummary))
            {
                col.Item().Element(c => Section(c, "Zusammenfassung", entry.LongSummary!, "#F5F5F5", "#E0E0E0"));
            }

            if (sections.ProseSummary && !string.IsNullOrWhiteSpace(entry.ProseSummary))
            {
                col.Item().Element(c => Section(c, "Ausführliche Zusammenfassung", entry.ProseSummary!, "#F0F8FF", "#B8D4F0"));
            }

            if (sections.Transcript && !string.IsNullOrWhiteSpace(entry.EffectiveTranscript))
            {
                col.Item().Element(c => Section(c, "Transkript", entry.EffectiveTranscript!, "#FAFAFA", "#E8E8E8"));
            }
        });
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
               .Column(inner => RenderMarkdown(inner, body));
        });
    }

    /// <summary>
    /// Renders a markdown string into QuestPDF column items.
    /// Handles ### h3, ## h2, # h1, - bullet lists, blank lines, and plain text.
    /// </summary>
    private static void RenderMarkdown(ColumnDescriptor col, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var pendingBullets = new List<string>();

        void FlushBullets()
        {
            if (pendingBullets.Count == 0)
            {
                return;
            }

            foreach (var bullet in pendingBullets)
            {
                var captured = bullet;
                col.Item().Row(row =>
                {
                    row.ConstantItem(12).AlignTop()
                       .Text("•").FontSize(10).FontColor("#555");
                    row.RelativeItem()
                       .Text(captured).FontSize(10);
                });
            }

            pendingBullets.Clear();
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushBullets();
                col.Item().PaddingTop(6)
                   .Text(line[4..]).SemiBold().FontSize(11).FontColor("#333");
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushBullets();
                col.Item().PaddingTop(6)
                   .Text(line[3..]).SemiBold().FontSize(12).FontColor("#222");
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                FlushBullets();
                col.Item().PaddingTop(8)
                   .Text(line[2..]).Bold().FontSize(13).FontColor("#111");
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) ||
                     line.StartsWith("* ", StringComparison.Ordinal))
            {
                pendingBullets.Add(line[2..]);
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                FlushBullets();
                col.Item().Height(3);
            }
            else
            {
                FlushBullets();
                col.Item().Text(line).FontSize(10);
            }
        }

        FlushBullets();
    }

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}
