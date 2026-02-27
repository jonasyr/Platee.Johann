using System.Text.Json;

namespace Johann.Infrastructure.Json;

/// <summary>
/// Migrates JSON documents from v1 (Python-generated) to v2 (C# schema).
///
/// v1 differences:
///   - Missing "schemaVersion" field (treat as v1)
///   - Missing "type" field → default "Projekt"
///   - Missing "title" field → derive from longSummary first line or jobId
///   - Missing "conversationNote", "taskList" fields → null
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
        // type: missing in v1 → Projekt
        if (!element.TryGetProperty("type", out _))
            dto.Type = "Projekt";

        // title: missing in v1 → derive from longSummary first line
        if (!element.TryGetProperty("title", out _) || string.IsNullOrWhiteSpace(dto.Title))
        {
            dto.Title = DeriveTitle(dto);
        }

        // source_type: v1 used "mp3" instead of "audio"
        if (dto.SourceType == "mp3")
            dto.SourceType = "audio";

        // project: v1 field was "project" (snake_case) in some versions
        if (string.IsNullOrWhiteSpace(dto.ProjectName))
        {
            if (element.TryGetProperty("project", out var projProp))
                dto.ProjectName = projProp.GetString() ?? "Allgemein";
        }
    }

    private static string DeriveTitle(EntryDto dto)
    {
        // Try first line of longSummary
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
