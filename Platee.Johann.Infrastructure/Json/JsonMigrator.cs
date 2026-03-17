using System.Text.Json;

namespace Platee.Johann.Infrastructure.Json;

/// <summary>
/// Migrates JSON documents from v1 (Python-generated) to v2 (C# schema).
///
/// v1 differences:
///   - Missing "schemaVersion" field (treat as v1)
///   - All fields in snake_case (job_id, long_summary, etc.) instead of camelCase
///   - Missing "type" field → default "Projekt"
///   - Missing "title" field → derive from long_summary first line or job_id
///   - "source_type" was "mp3" instead of "audio"
///   - "project" field instead of "projectName"
/// </summary>
public static class JsonMigrator
{
    public static EntryDto Migrate(JsonElement element)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Deserialize whatever fields exist (missing ones get defaults)
        var dto = element.Deserialize<EntryDto>(options) ?? new EntryDto();

        int version = element.TryGetProperty("schemaVersion", out var vProp) && vProp.TryGetInt32(out var v)
            ? v : 1;

        if (version < 2)
            ApplyV1Fixes(dto, element);

        dto.SchemaVersion = 2;
        return dto;
    }

    private static void ApplyV1Fixes(EntryDto dto, JsonElement element)
    {
        // ── Snake_case field mapping (Python v1 used snake_case throughout) ──
        // Applied unconditionally when the snake_case key exists:
        // v1 JSONs only have snake_case keys, never camelCase.

        if (element.TryGetProperty("job_id", out var ji))
            dto.JobId = ji.GetString() ?? string.Empty;

        if (element.TryGetProperty("sequence_number", out var sn) && sn.TryGetInt32(out var snVal))
            dto.SequenceNumber = snVal;

        if (element.TryGetProperty("created_at", out var ca)
            && DateTimeOffset.TryParse(ca.GetString(), out var caVal))
            dto.CreatedAt = caVal;

        if (element.TryGetProperty("duration_seconds", out var ds) && ds.TryGetDouble(out var dsVal))
            dto.DurationSeconds = dsVal;

        if (element.TryGetProperty("word_count", out var wc) && wc.TryGetInt32(out var wcVal))
            dto.WordCount = wcVal;

        if (element.TryGetProperty("source_type", out var st))
            dto.SourceType = st.GetString() ?? "text";

        if (element.TryGetProperty("project", out var proj))
            dto.ProjectName = proj.GetString() ?? "Allgemein";

        if (element.TryGetProperty("long_summary", out var ls))
            dto.LongSummary = ls.GetString();

        if (element.TryGetProperty("prose_summary", out var ps))
            dto.ProseSummary = ps.GetString();

        if (element.TryGetProperty("email_text", out var et))
            dto.EmailText = et.GetString();

        // status: pdf_created / email_created are snake_case in Python
        if (element.TryGetProperty("status", out var statusEl))
        {
            if (statusEl.TryGetProperty("pdf_created", out var pc))
                dto.Status.PdfCreated = pc.GetBoolean();
            if (statusEl.TryGetProperty("email_created", out var ec))
                dto.Status.EmailCreated = ec.GetBoolean();
        }

        // ── Semantic v1 fixes ──

        // type: missing in v1 → Projekt
        if (!element.TryGetProperty("type", out _))
            dto.Type = "Projekt";

        // source_type: v1 used "mp3" instead of "audio"
        if (dto.SourceType == "mp3")
            dto.SourceType = "audio";

        // title: missing in v1 → derive from long_summary first line
        if (!element.TryGetProperty("title", out _) || string.IsNullOrWhiteSpace(dto.Title))
            dto.Title = DeriveTitle(dto);
    }

    private static string DeriveTitle(EntryDto dto)
    {
        // Try first non-heading line of longSummary
        if (!string.IsNullOrWhiteSpace(dto.LongSummary))
        {
            var firstLine = dto.LongSummary
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(l => !l.StartsWith('#'))
                ?.Trim();
            if (!string.IsNullOrWhiteSpace(firstLine))
                return Truncate(firstLine, 60);
        }

        // Try abstract
        if (!string.IsNullOrWhiteSpace(dto.Abstract))
            return Truncate(dto.Abstract, 60);

        // Fallback to jobId
        return dto.JobId;
    }

    private static string Truncate(string s, int maxLength) =>
        s.Length <= maxLength ? s : s[..maxLength].TrimEnd() + "…";
}
