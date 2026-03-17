using System.Text;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;

namespace Platee.Johann.Infrastructure.Renderers;

/// <summary>
/// Generates the daily _ItemÜbersicht.html overview page in the date directory.
/// Mirrors the Python html_generator.py daily index functionality.
/// </summary>
public sealed class HtmlOverviewService : IHtmlOverviewService
{
    private readonly IEntryRepository _repository;
    private readonly string _outputRoot;

    public HtmlOverviewService(IEntryRepository repository, string outputRoot)
    {
        _repository = repository;
        _outputRoot = outputRoot;
    }

    public async Task RegenerateAsync(DateOnly date, CancellationToken ct = default)
    {
        var entries = await _repository.GetEntriesForDateAsync(date, ct);
        var html = BuildOverviewHtml(date, entries);

        var dateDir = Path.Combine(_outputRoot, date.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dateDir);

        var path = Path.Combine(dateDir, "_ItemÜbersicht.html");
        await File.WriteAllTextAsync(path, html, Encoding.UTF8, ct);
    }

    private static string BuildOverviewHtml(DateOnly date, IReadOnlyList<Entry> entries)
    {
        var sb = new StringBuilder();
        var dateFormatted = date.ToString("dd.MM.yyyy");
        var dayName = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Montag",
            DayOfWeek.Tuesday => "Dienstag",
            DayOfWeek.Wednesday => "Mittwoch",
            DayOfWeek.Thursday => "Donnerstag",
            DayOfWeek.Friday => "Freitag",
            DayOfWeek.Saturday => "Samstag",
            DayOfWeek.Sunday => "Sonntag",
            _ => string.Empty
        };

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"de\"><head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>Übersicht – {dateFormatted}</title>");
        sb.AppendLine(OverviewCss());
        sb.AppendLine("</head><body>");

        // Page header
        sb.AppendLine("<div class=\"page-header\">");
        sb.AppendLine($"  <div class=\"page-title\">");
        sb.AppendLine($"    <span class=\"day-name\">{dayName}</span>");
        sb.AppendLine($"    <span class=\"date-value\">{dateFormatted}</span>");
        sb.AppendLine($"  </div>");
        sb.AppendLine($"  <div class=\"entry-count\">{entries.Count} {(entries.Count == 1 ? "Eintrag" : "Einträge")}</div>");
        sb.AppendLine("</div>");

        if (entries.Count == 0)
        {
            sb.AppendLine("<p class=\"empty\">Keine Einträge für diesen Tag.</p>");
        }
        else
        {
            sb.AppendLine("<div class=\"entries\">");
            foreach (var entry in entries)
                AppendEntryCard(sb, entry);
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<footer><p>Generiert von Johann · " + dateFormatted + "</p></footer>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendEntryCard(StringBuilder sb, Entry entry)
    {
        var typeColor = entry.Type switch
        {
            EntryType.Aufgabe => "#E63123",
            EntryType.Gesprächsnotiz => "#2980B9",
            EntryType.EMail => "#27AE60",
            EntryType.Stundenzettel => "#8E44AD",
            _ => "#555555",
        };

        var duration = entry.DurationSeconds > 0
            ? $" · {FormatDuration(entry.DurationSeconds)}"
            : string.Empty;

        // Build the first-5-words title for the link
        var titleWords = entry.Title
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(5);
        var fileBase = $"{entry.CreatedAt:yyMMdd}_{entry.SequenceNumber:D3}";
        if (entry.Type == EntryType.Gesprächsnotiz)
            fileBase += "_Gesprächsnotiz";
        fileBase += $"_{SanitizeForFilename(entry.ProjectName)}_{SanitizeForFilename(string.Join("_", titleWords))}";

        sb.AppendLine($"  <div class=\"card\" style=\"border-left: 4px solid {typeColor}\">");
        sb.AppendLine($"    <div class=\"card-header\">");
        sb.AppendLine($"      <span class=\"badge\" style=\"background:{typeColor}\">{HtmlEncode(entry.Type.ToString())}</span>");
        sb.AppendLine($"      <span class=\"project\">{HtmlEncode(entry.ProjectName)}</span>");
        sb.AppendLine($"      <span class=\"seq\">#{entry.SequenceNumber:D3}</span>");
        sb.AppendLine($"      <span class=\"duration\">{entry.CreatedAt:HH:mm}{duration}</span>");
        sb.AppendLine($"    </div>");
        sb.AppendLine($"    <h2><a href=\"{HtmlEncode(fileBase)}.html\">{HtmlEncode(entry.Title)}</a></h2>");

        if (!string.IsNullOrWhiteSpace(entry.Abstract))
        {
            sb.AppendLine($"    <div class=\"abstract\">{MarkdownHelper.ToHtml(entry.Abstract)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.TaskList))
        {
            sb.AppendLine($"    <div class=\"section-label\">Aufgaben</div>");
            sb.AppendLine($"    <div class=\"task-list\">{MarkdownHelper.ToHtml(entry.TaskList)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.ConversationNote))
        {
            sb.AppendLine($"    <div class=\"section-label\">Gesprächsnotiz</div>");
            sb.AppendLine($"    <div class=\"conv-note\">{MarkdownHelper.ToHtml(entry.ConversationNote)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.StundenzettelText))
        {
            sb.AppendLine($"    <div class=\"section-label\">Stundenzettel</div>");
            sb.AppendLine($"    <div class=\"stundenzettel\">{MarkdownHelper.ToHtml(entry.StundenzettelText)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.AnalogText))
        {
            sb.AppendLine($"    <div class=\"section-label\">Analog</div>");
            sb.AppendLine($"    <div class=\"analog\">{MarkdownHelper.ToHtml(entry.AnalogText)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.EmailText))
        {
            sb.AppendLine($"    <div class=\"section-label\">E-Mail</div>");
            sb.AppendLine($"    <div class=\"email-text\">{MarkdownHelper.ToHtml(entry.EmailText)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.LongSummary))
        {
            sb.AppendLine($"    <div class=\"section-label\">Zusammenfassung</div>");
            sb.AppendLine($"    <div class=\"long-summary\">{MarkdownHelper.ToHtml(entry.LongSummary)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(entry.ProseSummary))
        {
            sb.AppendLine($"    <div class=\"section-label\">Ausführliche Zusammenfassung</div>");
            sb.AppendLine($"    <div class=\"prose-summary\">{MarkdownHelper.ToHtml(entry.ProseSummary)}</div>");
        }

        sb.AppendLine($"  </div>");
    }

    private static string HtmlEncode(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string SanitizeForFilename(string s)
        => System.Text.RegularExpressions.Regex.Replace(s, @"[<>:""/\\|?*\s]+", "_").Trim('_');

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private static string OverviewCss() => @"
<style>
  * { box-sizing: border-box; }
  body { font-family: 'Segoe UI', Arial, sans-serif; max-width: 900px; margin: 0 auto;
         padding: 24px; color: #222; line-height: 1.5; background: #f9f9f9; }
  .page-header { display: flex; justify-content: space-between; align-items: baseline;
                 border-bottom: 2px solid #E63123; padding-bottom: 12px; margin-bottom: 28px; }
  .page-title { display: flex; align-items: baseline; gap: 12px; }
  .day-name  { font-size: 22px; font-weight: 700; color: #222; }
  .date-value { font-size: 16px; color: #555; }
  .entry-count { font-size: 13px; color: #888; }
  .entries { display: flex; flex-direction: column; gap: 16px; }
  .card { background: white; border-radius: 6px; padding: 16px;
          box-shadow: 0 1px 4px rgba(0,0,0,.08); }
  .card-header { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; flex-wrap: wrap; }
  .badge { color: white; padding: 2px 7px; border-radius: 4px; font-size: 10px; font-weight: bold; }
  .project { color: #555; font-size: 12px; }
  .seq { color: #aaa; font-size: 11px; margin-left: auto; }
  .duration { color: #aaa; font-size: 11px; }
  h2 { margin: 0 0 8px; font-size: 16px; font-weight: 600; }
  h2 a { color: #222; text-decoration: none; }
  h2 a:hover { color: #E63123; text-decoration: underline; }
  .abstract { font-size: 13px; color: #444; background: #FFF5F4;
              border: 1px solid #FFDBD8; border-radius: 4px; padding: 8px; margin-top: 6px; }
  .section-label { font-size: 10px; font-weight: 600; color: #888; text-transform: uppercase;
                   letter-spacing: 0.5px; margin-top: 10px; margin-bottom: 4px; }
  .task-list { font-size: 12px; color: #333; background: #FFF8F8;
               border: 1px solid #E63123; border-radius: 4px; padding: 8px; }
  .conv-note { font-size: 12px; color: #333; background: #F0F8FF;
               border: 1px solid #B8D4F0; border-radius: 4px; padding: 8px; }
  .stundenzettel { font-size: 12px; color: #333; background: #FAF0FF;
                   border: 1px solid #8E44AD; border-radius: 4px; padding: 8px; }
  .analog { font-size: 12px; color: #333; background: #F8F8F8;
            border: 1px solid #888; border-radius: 4px; padding: 8px; }
  .email-text { font-size: 12px; color: #333; background: #F0FFF4;
                border: 1px solid #27AE60; border-radius: 4px; padding: 8px; }
  .long-summary, .prose-summary { font-size: 12px; color: #333; background: #F5F5F5;
                                   border: 1px solid #E0E0E0; border-radius: 4px; padding: 8px; }
  .abstract p, .task-list p, .conv-note p, .stundenzettel p, .analog p,
  .email-text p, .long-summary p, .prose-summary p { margin: 3px 0; }
  .abstract ul, .task-list ul, .conv-note ul, .stundenzettel ul, .analog ul,
  .email-text ul, .long-summary ul, .prose-summary ul { margin: 3px 0 3px 18px; padding: 0; }
  .abstract li, .task-list li, .conv-note li, .stundenzettel li, .analog li,
  .email-text li, .long-summary li, .prose-summary li { margin: 1px 0; }
  .empty { color: #aaa; font-style: italic; text-align: center; padding: 40px; }
  footer { text-align: center; color: #bbb; font-size: 11px; margin-top: 32px; }
</style>";
}
