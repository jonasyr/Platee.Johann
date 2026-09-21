namespace Platee.Johann.Application.Mail;

using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Services;

/// <summary>
/// Builds the two mails behind the detail view's mail buttons (#57).
/// <list type="bullet">
/// <item><b>Aufgaben</b> — internal and informal: the user's intro text, then the task section
/// (which already opens with a short summary paragraph, #66), with the entry's PDF attached.</item>
/// <item><b>E-Mail</b> — external and formal: the generated mail text, no attachment. An external
/// recipient gets a finished message, not an internal working document.</item>
/// </list>
/// </summary>
public static class MailDraftBuilder
{
    /// <summary>Placeholder in the intro text, replaced with the entry's project.</summary>
    public const string ProjectPlaceholder = "{Projekt}";

    /// <summary>Default intro for the internal task mail; users change it in the settings.</summary>
    public const string DefaultTaskMailIntro =
        "Hallo zusammen,\n\nanbei die Aufgaben zu {Projekt}. Das vollständige Protokoll hängt als PDF an.";

    private const string SubjectPrefix = "Betreff:";

    /// <summary>Builds the internal task mail.</summary>
    /// <param name="entry">The entry; its <see cref="Entry.TaskList"/> should be generated.</param>
    /// <param name="introTemplate">Intro text; <see cref="ProjectPlaceholder"/> is replaced.</param>
    /// <param name="pdfPath">The rendered PDF to attach, or <c>null</c> for none.</param>
    /// <returns>The draft.</returns>
    public static MailDraft ForTasks(Entry entry, string? introTemplate, string? pdfPath)
    {
        var project = ProjectOrTitle(entry);
        var subject = string.IsNullOrWhiteSpace(entry.ProjectName)
            ? $"Aufgaben – {entry.Title}"
            : $"Aufgaben – {entry.ProjectName} – {entry.Title}";

        var intro = Normalize(introTemplate).Replace(ProjectPlaceholder, project, StringComparison.Ordinal);
        var tasks = Normalize(entry.TaskList);
        var body = string.IsNullOrEmpty(intro) ? tasks : $"{intro}\n\n{tasks}";

        var attachments = pdfPath is null ? Array.Empty<string>() : [pdfPath];
        return new MailDraft(subject, body.Trim(), attachments);
    }

    /// <summary>Builds the external mail from the generated mail text, without attachment.</summary>
    /// <param name="entry">The entry; its <see cref="Entry.EmailText"/> should be generated.</param>
    /// <returns>The draft.</returns>
    public static MailDraft ForExternal(Entry entry)
    {
        var text = Normalize(entry.EmailText);
        var subject = ExtractSubject(text) ?? $"{entry.ProjectName}: {entry.Title}";
        return new MailDraft(subject, StripSubjectLine(text), []);
    }

    /// <summary>Returns the text after the first "Betreff:" line, or <c>null</c> without one.</summary>
    /// <param name="text">Mail text.</param>
    /// <returns>The subject or <c>null</c>.</returns>
    public static string? ExtractSubject(string? text)
    {
        foreach (var line in Normalize(text).Split('\n'))
        {
            if (TrySubject(line, out var subject))
            {
                return subject;
            }
        }

        return null;
    }

    /// <summary>Removes the first "Betreff:" line and the blank line after it.</summary>
    /// <param name="text">Mail text.</param>
    /// <returns>The body.</returns>
    public static string StripSubjectLine(string? text)
    {
        var lines = Normalize(text).Split('\n').ToList();
        var index = lines.FindIndex(l => TrySubject(l, out _));
        if (index >= 0)
        {
            lines.RemoveAt(index);
            if (index < lines.Count && string.IsNullOrWhiteSpace(lines[index]))
            {
                lines.RemoveAt(index);
            }
        }

        return string.Join('\n', lines).Trim();
    }

    /// <summary>
    /// Recognises the subject line also when wrapped in markdown ("**Betreff: …**", "## Betreff:");
    /// with the central markdown rule the model set it in bold, which would otherwise have lost
    /// the subject and put a bold line on top of the mail.
    /// </summary>
    private static bool TrySubject(string line, out string subject)
    {
        var plain = InlineMarkdown.ToPlainText(line).Trim();
        if (plain.StartsWith(SubjectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            subject = plain[SubjectPrefix.Length..].Trim();
            return true;
        }

        subject = string.Empty;
        return false;
    }

    private static string ProjectOrTitle(Entry entry)
        => string.IsNullOrWhiteSpace(entry.ProjectName) ? entry.Title : entry.ProjectName;

    private static string Normalize(string? text)
        => (text ?? string.Empty).ReplaceLineEndings("\n").Trim();
}
