using Johann.Application.Interfaces;
using Johann.Domain.Entities;
using System.Text;

namespace Johann.Infrastructure.Renderers;

/// <summary>
/// Generates a standalone HTML file for a single entry,
/// and also regenerates the daily _ItemÜbersicht.html overview.
/// </summary>
public sealed class HtmlRenderer : IEntryRenderer
{
    public string RendererName => "HTML";

    public async Task<RenderResult> RenderAsync(Entry entry, RenderOptions options,
                                                 CancellationToken ct = default)
    {
        var filename = $"{entry.JobId}.html";
        var outputDir = options.OutputDirectory
            ?? Path.Combine(Path.GetTempPath(), "JohannHtml");

        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, filename);

        var html = BuildEntryHtml(entry);
        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, ct);

        var bytes = Encoding.UTF8.GetBytes(html);
        return new RenderResult(bytes, "text/html", filename);
    }

    private static string BuildEntryHtml(Entry entry)
    {
        var typeColor = entry.Type switch
        {
            Domain.Enums.EntryType.Aufgabe        => "#E63123",
            Domain.Enums.EntryType.Gesprächsnotiz => "#2980B9",
            Domain.Enums.EntryType.EMail          => "#27AE60",
            Domain.Enums.EntryType.Stundenzettel  => "#8E44AD",
            _                                     => "#555555",
        };

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"de\"><head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{HtmlEncode(entry.Title)}</title>");
        sb.AppendLine(Css(typeColor));
        sb.AppendLine("</head><body>");

        // Header
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine($"  <span class=\"badge\">{HtmlEncode(entry.Type.ToString())}</span>");
        sb.AppendLine($"  <span class=\"project\">{HtmlEncode(entry.ProjectName)}</span>");
        sb.AppendLine($"  <h1>{HtmlEncode(entry.Title)}</h1>");
        sb.AppendLine($"  <p class=\"meta\">{entry.CreatedAt:dd.MM.yyyy} · #{entry.SequenceNumber:D3}");
        if (entry.DurationSeconds > 0)
            sb.Append($" · {FormatDuration(entry.DurationSeconds)}");
        sb.AppendLine("  </p>");
        sb.AppendLine("</div>");

        // Content sections
        if (!string.IsNullOrWhiteSpace(entry.Abstract))
            AppendSection(sb, "Kurzfassung", entry.Abstract!, "section-abstract");

        if (!string.IsNullOrWhiteSpace(entry.TaskList))
            AppendSection(sb, "Aufgaben", entry.TaskList!, "section-task");

        if (!string.IsNullOrWhiteSpace(entry.ConversationNote))
            AppendSection(sb, "Gesprächsnotiz", entry.ConversationNote!, "section-note");

        if (!string.IsNullOrWhiteSpace(entry.LongSummary))
            AppendSection(sb, "Zusammenfassung", entry.LongSummary!, "section-summary");

        if (!string.IsNullOrWhiteSpace(entry.ProseSummary))
            AppendSection(sb, "Ausführliche Zusammenfassung", entry.ProseSummary!, "section-prose");

        if (!string.IsNullOrWhiteSpace(entry.Transcript))
        {
            sb.AppendLine("<details><summary class=\"transcript-toggle\">Originaltranskript</summary>");
            AppendSection(sb, null, entry.Transcript!, "section-transcript");
            sb.AppendLine("</details>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string? title, string body, string cssClass)
    {
        sb.AppendLine($"<div class=\"section {cssClass}\">");
        if (title != null)
            sb.AppendLine($"  <h2>{HtmlEncode(title)}</h2>");
        sb.AppendLine($"  <p>{HtmlEncode(body).Replace("\n", "<br>")}</p>");
        sb.AppendLine("</div>");
    }

    private static string HtmlEncode(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private static string Css(string typeColor) => $@"
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; max-width: 800px; margin: 0 auto;
          padding: 24px; color: #222; line-height: 1.6; }}
  .header {{ border-bottom: 2px solid {typeColor}; padding-bottom: 16px; margin-bottom: 24px; }}
  .badge {{ background: {typeColor}; color: white; padding: 2px 8px; border-radius: 4px;
             font-size: 11px; font-weight: bold; }}
  .project {{ color: #666; margin-left: 8px; font-size: 13px; }}
  h1 {{ margin: 8px 0 4px; font-size: 22px; }}
  .meta {{ color: #888; font-size: 12px; margin: 0; }}
  .section {{ margin-bottom: 20px; }}
  .section h2 {{ font-size: 11px; font-weight: 600; color: #666; text-transform: uppercase;
                  letter-spacing: 0.5px; margin-bottom: 6px; }}
  .section-abstract p {{ background: #FFF5F4; border: 1px solid #FFDBD8;
                          border-radius: 4px; padding: 12px; }}
  .section-task p {{ background: #FFF8F8; border: 1px solid {typeColor};
                      border-radius: 4px; padding: 12px; white-space: pre-line; }}
  .section-note p {{ background: #F0F8FF; border: 1px solid #B8D4F0;
                      border-radius: 4px; padding: 12px; }}
  .section-summary p, .section-prose p {{ background: #F5F5F5; border: 1px solid #E0E0E0;
                                           border-radius: 4px; padding: 12px; }}
  .section-transcript p {{ background: #FAFAFA; border: 1px solid #E8E8E8;
                            border-radius: 4px; padding: 12px; font-size: 12px; }}
  .transcript-toggle {{ cursor: pointer; font-size: 13px; color: #666; margin-bottom: 8px; }}
  details summary {{ list-style: none; }}
  details summary::-webkit-details-marker {{ display: none; }}
</style>";
}
