using Platee.Johann.Domain.Services;

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
                page.Content().Element(content => ComposeContent(content, entry, sections, options));
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
                        meta.Item().Text(DurationFormatter.Format(entry.DurationSeconds))
                            .FontColor("#888").FontSize(9);
                    }

                    meta.Item().Text($"#{entry.SequenceNumber:D3}")
                        .FontColor("#888").FontSize(9);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DDDDDD");
        });
    }

    private static void ComposeContent(IContainer content, Entry entry, SectionVisibility sections,
                                       RenderOptions options)
    {
        content.Column(col =>
        {
            col.Spacing(10);

            if (!string.IsNullOrWhiteSpace(entry.Abstract))
            {
                col.Item().Element(c => Section(c, "Kurzfassung", entry.Abstract!, "#FFF5F4", "#FFDBD8"));
            }

            // These sections are markdown too (lists, and bold since the central markdown rule in
            // #73) and go through the same renderer as the rest; before, they were printed as raw
            // text with literal dashes and asterisks. Colours and borders stay as they were.
            if (sections.TaskList && !string.IsNullOrWhiteSpace(entry.TaskList))
            {
                col.Item().Element(c => Section(c, "Aufgaben", entry.TaskList!, "#FFF8F8", "#E63123"));
            }

            if (sections.ConversationNote && !string.IsNullOrWhiteSpace(entry.ConversationNote))
            {
                col.Item().Element(c => Section(c, "Gesprächsnotiz", entry.ConversationNote!, "#F0F8FF", "#2980B9", "#2980B9"));
            }

            if (sections.StundenzettelText && !string.IsNullOrWhiteSpace(entry.StundenzettelText))
            {
                col.Item().Element(c => Section(c, "Stundenzettel", entry.StundenzettelText!, "#FAF0FF", "#8E44AD", "#8E44AD"));
            }

            if (sections.AnalogText && !string.IsNullOrWhiteSpace(entry.AnalogText))
            {
                col.Item().Element(c => Section(c, "Analog", entry.AnalogText!, "#F8F8F8", "#888888"));
            }

            if (sections.EmailText && !string.IsNullOrWhiteSpace(entry.EmailText))
            {
                col.Item().Element(c => Section(c, "E-Mail", entry.EmailText!, "#F0FFF4", "#27AE60", "#27AE60"));
            }

            if (sections.LongSummary && !string.IsNullOrWhiteSpace(entry.LongSummary))
            {
                col.Item().Element(c => Section(c, "Zusammenfassung", entry.LongSummary!, "#F5F5F5", "#E0E0E0"));
            }

            if (sections.ProseSummary && !string.IsNullOrWhiteSpace(entry.ProseSummary))
            {
                col.Item().Element(c => Section(c, "Ausführliche Zusammenfassung", entry.ProseSummary!, "#F0F8FF", "#B8D4F0"));
            }

            // User-defined categories, rendered through the same Section helper as the
            // built-ins so they share its markdown handling.
            foreach (var (_, heading, text) in options.SelectCustomSections(entry))
            {
                var capturedHeading = heading;
                var capturedText = text;
                col.Item().Element(c => Section(c, capturedHeading, capturedText, "#F7F7FB", "#D8D8E4"));
            }

            if (sections.Transcript && !string.IsNullOrWhiteSpace(entry.EffectiveTranscript))
            {
                // One sentence per line, display only — the stored transcript is unchanged (#112).
                var transcript = SentenceLines.Split(entry.EffectiveTranscript!);
                col.Item().Element(c => Section(c, "Transkript", transcript, "#FAFAFA", "#E8E8E8"));
            }
        });
    }

    private static void Section(IContainer container, string title,
                                 string body, string bg, string border, string titleColor = "#555")
    {
        container.Column(col =>
        {
            col.Item().Text(title).Bold().FontSize(11).FontColor(titleColor);
            col.Item().PaddingTop(4)
               .Border(1).BorderColor(border)
               .Background(bg)
               .Padding(10)
               .Column(inner => RenderMarkdown(inner, body));
        });
    }

    /// <summary>Bullet marker per nesting level; deeper levels repeat the last one.</summary>
    private static readonly string[] BulletMarkers = ["•", "◦", "▪"];

    /// <summary>Horizontal offset per nesting level, in points.</summary>
    private const float BulletIndentPerLevel = 12;

    /// <summary>
    /// Renders a markdown string into QuestPDF column items.
    /// Handles ### h3, ## h2, # h1, nested bullet lists (see <see cref="BulletOutline"/>),
    /// blank lines, and plain text.
    /// </summary>
    private static void RenderMarkdown(ColumnDescriptor col, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var pendingBullets = new List<BulletLine>();

        void FlushBullets()
        {
            if (pendingBullets.Count == 0)
            {
                return;
            }

            var levels = BulletOutline.AssignLevels(pendingBullets.Select(b => b.Indent).ToList());
            for (var i = 0; i < pendingBullets.Count; i++)
            {
                var level = levels[i];
                var captured = pendingBullets[i].Text;
                var marker = BulletMarkers[Math.Min(level, BulletMarkers.Length - 1)];
                col.Item().PaddingLeft(level * BulletIndentPerLevel).Row(row =>
                {
                    row.ConstantItem(12).AlignTop()
                       .Text(marker).FontSize(10).FontColor("#555");
                    row.RelativeItem()
                       .Text(t => Inline(t, captured, s => s.FontSize(10)));
                });
            }

            pendingBullets.Clear();
        }

        foreach (var rawLine in text.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushBullets();
                col.Item().PaddingTop(6)
                   .Text(t => Inline(t, line[4..], s => s.SemiBold().FontSize(11).FontColor("#333")));
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushBullets();
                col.Item().PaddingTop(6)
                   .Text(t => Inline(t, line[3..], s => s.SemiBold().FontSize(12).FontColor("#222")));
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                FlushBullets();
                col.Item().PaddingTop(8)
                   .Text(t => Inline(t, line[2..], s => s.Bold().FontSize(13).FontColor("#111")));
            }
            else if (BulletOutline.TryParse(line, out var bullet))
            {
                pendingBullets.Add(bullet);
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                FlushBullets();
                col.Item().Height(3);
            }
            else
            {
                FlushBullets();
                col.Item().Text(t => Inline(t, line, s => s.FontSize(10)));
            }
        }

        FlushBullets();
    }

    /// <summary>
    /// Writes one line with bold and italic as real type instead of literal asterisks
    /// (<see cref="InlineMarkdown"/>).
    /// </summary>
    private static void Inline(TextDescriptor text, string line, Func<TextStyle, TextStyle> style)
    {
        text.DefaultTextStyle(style);
        foreach (var run in InlineMarkdown.Split(line))
        {
            var span = text.Span(run.Text);
            if (run.Bold)
            {
                span.SemiBold();
            }

            if (run.Italic)
            {
                span.Italic();
            }
        }
    }
}
