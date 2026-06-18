namespace Platee.Johann.Infrastructure.Json;

using System.Text.Json;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.ValueObjects;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON to Documents\Johann\settings.json.
/// Falls back to <see cref="AppSettings.Default"/> on missing or corrupt files.
/// </summary>
public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly string filePath;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public JsonSettingsRepository(string settingsDirectory)
    {
        Directory.CreateDirectory(settingsDirectory);
        this.filePath = Path.Combine(settingsDirectory, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(this.filePath))
        {
            return AppSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(this.filePath);
            var dto = await JsonSerializer.DeserializeAsync<SettingsDto>(stream, Options, ct).ConfigureAwait(false);
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
        await using var stream = File.Open(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, dto, Options, ct).ConfigureAwait(false);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────
    private static AppSettings MapToSettings(SettingsDto dto)
    {
        var defaultSettings = AppSettings.Default;
        return new()
        {
            Name = dto.Name ?? defaultSettings.Name,
            Firma = dto.Firma ?? defaultSettings.Firma,
            Quellverzeichnis = dto.Quellverzeichnis ?? defaultSettings.Quellverzeichnis,
            Archivverzeichnis = dto.Archivverzeichnis ?? defaultSettings.Archivverzeichnis,
            Ausgabeverzeichnis = dto.Ausgabeverzeichnis ?? defaultSettings.Ausgabeverzeichnis,
            GlobalPromptFilePath = dto.GlobalPromptFilePath ?? defaultSettings.GlobalPromptFilePath,
            LastSeenReleaseNotesVersion = dto.LastSeenReleaseNotesVersion,
            Korrekturliste = dto.Korrekturliste is { Count: > 0 }
                ? dto.Korrekturliste
                    .Where(c => !string.IsNullOrWhiteSpace(c.Wrong))
                    .Select(c => new CorrectionEntry
                    {
                        Wrong = c.Wrong!.Trim(),
                        Correct = c.Correct?.Trim() ?? string.Empty,
                    })
                    .ToList()
                : defaultSettings.Korrekturliste,
        };
    }

    private static SettingsDto MapToDto(AppSettings s) => new()
    {
        Name = s.Name,
        Firma = s.Firma,
        Quellverzeichnis = s.Quellverzeichnis,
        Archivverzeichnis = s.Archivverzeichnis,
        Ausgabeverzeichnis = s.Ausgabeverzeichnis,
        GlobalPromptFilePath = s.GlobalPromptFilePath,
        LastSeenReleaseNotesVersion = s.LastSeenReleaseNotesVersion,
        Korrekturliste = s.Korrekturliste
            .Select(c => new CorrectionEntryDto { Wrong = c.Wrong, Correct = c.Correct })
            .ToList(),
    };

    // Separate DTO to decouple JSON shape from the domain record
    private sealed class SettingsDto
    {
        public string? Name { get; set; }

        public string? Firma { get; set; }

        public string? Quellverzeichnis { get; set; }

        public string? Archivverzeichnis { get; set; }

        public string? Ausgabeverzeichnis { get; set; }

        public string? GlobalPromptFilePath { get; set; }

        public string? LastSeenReleaseNotesVersion { get; set; }

        public List<CorrectionEntryDto>? Korrekturliste { get; set; }
    }

    private sealed class CorrectionEntryDto
    {
        public string? Wrong { get; set; }

        public string? Correct { get; set; }
    }
}
