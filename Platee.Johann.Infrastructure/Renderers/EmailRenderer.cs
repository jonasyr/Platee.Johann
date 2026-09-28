namespace Platee.Johann.Infrastructure.Renderers;

using System.Text;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Services;

/// <summary>
/// Renders an entry as a plain-text email file (.txt) with
/// subject line + body, suitable for copy-paste into any mail client.
/// Also exposes <see cref="BuildEmailText"/> as public for direct clipboard use.
/// </summary>
public sealed class EmailRenderer : IEntryRenderer
{
    public string RendererName => "Email";

    public async Task<RenderResult> RenderAsync(Entry entry, RenderOptions options,
                                                 CancellationToken ct = default)
    {
        var filename = FilenameBuilder.Build(entry) + "_email.txt";
        var outputDir = options.OutputDirectory
            ?? Path.Combine(Path.GetTempPath(), "JohannEmail");

        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, filename);

        var text = BuildEmailText(entry);
        await File.WriteAllTextAsync(filePath, text, Encoding.UTF8, ct);

        var bytes = Encoding.UTF8.GetBytes(text);
        return new RenderResult(bytes, "text/plain", filename);
    }

    /// <summary>
    /// Builds a plain-text email from the entry's available content.
    /// Public so callers can use the text directly (e.g. copy to clipboard).
    /// </summary>
    public static string BuildEmailText(Entry entry)
    {
        var sb = new StringBuilder();

        // Subject line
        var subject = BuildSubject(entry);
        sb.AppendLine($"Betreff: {subject}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();

        // If there's an explicit emailText field, use it as body
        if (!string.IsNullOrWhiteSpace(entry.EmailText))
        {
            // Plain-text file: the markdown of the mail text (#73) would show as asterisks.
            sb.AppendLine(InlineMarkdown.ToPlainText(entry.EmailText));
            sb.AppendLine();
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"[Automatisch generiert am {entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
            return sb.ToString();
        }

        // Otherwise compose from available content -- as plain text too, the sections carry markdown.
        sb.AppendLine($"[{entry.Type} · {entry.ProjectName} · {entry.CreatedAt:dd.MM.yyyy}]");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(entry.Abstract))
        {
            sb.AppendLine(InlineMarkdown.ToPlainText(entry.Abstract));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(entry.TaskList))
        {
            sb.AppendLine("Aufgaben:");
            sb.AppendLine(InlineMarkdown.ToPlainText(entry.TaskList));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(entry.ConversationNote))
        {
            sb.AppendLine("Gesprächsnotiz:");
            sb.AppendLine(InlineMarkdown.ToPlainText(entry.ConversationNote));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(entry.ProseSummary))
        {
            sb.AppendLine(InlineMarkdown.ToPlainText(entry.ProseSummary));
            sb.AppendLine();
        }
        else if (!string.IsNullOrWhiteSpace(entry.LongSummary))
        {
            sb.AppendLine(InlineMarkdown.ToPlainText(entry.LongSummary));
            sb.AppendLine();
        }

        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"[Automatisch generiert am {entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
        return sb.ToString();
    }

    private static string BuildSubject(Entry entry)
    {
        return entry.Type switch
        {
            Domain.Enums.EntryType.EMail => $"{entry.ProjectName}: {entry.Title}",
            Domain.Enums.EntryType.Aufgabe => $"AW: Aufgaben – {entry.ProjectName} – {entry.Title}",
            Domain.Enums.EntryType.Gesprächsnotiz => $"Gesprächsnotiz – {entry.ProjectName} – {entry.Title}",
            Domain.Enums.EntryType.Stundenzettel => $"Stundenzettel – {entry.ProjectName} – {entry.CreatedAt:dd.MM.yyyy}",
            _ => $"{entry.ProjectName}: {entry.Title}",
        };
    }
}
