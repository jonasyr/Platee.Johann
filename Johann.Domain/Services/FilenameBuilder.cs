using System.Text.RegularExpressions;
using Johann.Domain.Entities;
using Johann.Domain.Enums;

namespace Johann.Domain.Services;

/// <summary>
/// Builds filenames according to the rule:
///   YYMMDD_NNN_[Gesprächsnotiz]_Projektname_ErsteFünfWorteDesTitels
/// </summary>
public static class FilenameBuilder
{
    private static readonly Regex UnsafeChars = new(@"[<>:""/\\|?*\s]+", RegexOptions.Compiled);

    public static string Build(Entry entry)
    {
        var date = entry.CreatedAt.ToString("yyMMdd");
        var seq = $"{entry.SequenceNumber:D3}";
        var typePart = entry.Type == EntryType.Gesprächsnotiz ? "_Gesprächsnotiz" : string.Empty;
        var project = Sanitize(entry.ProjectName);
        var titleWords = entry.Title
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(5);
        var titlePart = Sanitize(string.Join("_", titleWords));

        return $"{date}_{seq}{typePart}_{project}_{titlePart}";
    }

    private static string Sanitize(string s) =>
        UnsafeChars.Replace(s, "_").Trim('_');
}
