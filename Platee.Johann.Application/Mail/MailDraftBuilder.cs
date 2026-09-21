namespace Platee.Johann.Application.Mail;

using Platee.Johann.Domain.Entities;

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
            var trimmed = line.Trim();
            if (trimmed.StartsWith(SubjectPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[SubjectPrefix.Length..].Trim();
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
        var index = lines.FindIndex(l => l.Trim().StartsWith(SubjectPrefix, StringComparison.OrdinalIgnoreCase));
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

    private static string ProjectOrTitle(Entry entry)
        => string.IsNullOrWhiteSpace(entry.ProjectName) ? entry.Title : entry.ProjectName;

    private static string Normalize(string? text)
        => (text ?? string.Empty).ReplaceLineEndings("\n").Trim();
}
