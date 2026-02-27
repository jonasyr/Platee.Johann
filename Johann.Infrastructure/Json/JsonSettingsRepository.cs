using System.Text.Json;
using Johann.Application.Interfaces;
using Johann.Application.Processing;
using Johann.Application.Settings;

namespace Johann.Infrastructure.Json;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON to Documents\Johann\settings.json.
/// Falls back to <see cref="AppSettings.Default"/> on missing or corrupt files.
/// </summary>
public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented              = true,
        PropertyNamingPolicy       = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public JsonSettingsRepository(string settingsDirectory)
    {
        Directory.CreateDirectory(settingsDirectory);
        _filePath = Path.Combine(settingsDirectory, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return AppSettings.Default;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var dto = await JsonSerializer.DeserializeAsync<SettingsDto>(stream, Options, ct);
            return dto is null ? AppSettings.Default : MapToSettings(dto);
        }
        catch
        {
            // Corrupt file — return defaults silently
            return AppSettings.Default;
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        var dto = MapToDto(settings);
        await using var stream = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, dto, Options, ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static AppSettings MapToSettings(SettingsDto dto) => new()
    {
        SystemMessage    = dto.SystemMessage    ?? SummaryPrompts.SystemMessage,
        AbstractPrompt   = dto.AbstractPrompt   ?? SummaryPrompts.Abstract,
        StructuredPrompt = dto.StructuredPrompt ?? SummaryPrompts.Structured,
        ProsePrompt      = dto.ProsePrompt      ?? SummaryPrompts.Prose,
        EmailPrompt      = dto.EmailPrompt      ?? SummaryPrompts.Email,
    };

    private static SettingsDto MapToDto(AppSettings s) => new()
    {
        SystemMessage    = s.SystemMessage,
        AbstractPrompt   = s.AbstractPrompt,
        StructuredPrompt = s.StructuredPrompt,
        ProsePrompt      = s.ProsePrompt,
        EmailPrompt      = s.EmailPrompt,
    };

    // Separate DTO to decouple JSON shape from the domain record
    private sealed class SettingsDto
    {
        public string? SystemMessage    { get; set; }
        public string? AbstractPrompt   { get; set; }
        public string? StructuredPrompt { get; set; }
        public string? ProsePrompt      { get; set; }
        public string? EmailPrompt      { get; set; }
    }
}
