namespace Platee.Johann.Infrastructure.Json;

using System.Text.Json;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

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
            PromptDefaultsRevision = dto.PromptDefaultsRevision ?? 0,
            Name = dto.Name ?? defaultSettings.Name,
            Firma = dto.Firma ?? defaultSettings.Firma,
            Quellverzeichnis = dto.Quellverzeichnis ?? defaultSettings.Quellverzeichnis,
            Archivverzeichnis = dto.Archivverzeichnis ?? defaultSettings.Archivverzeichnis,
            Ausgabeverzeichnis = dto.Ausgabeverzeichnis ?? defaultSettings.Ausgabeverzeichnis,

            SystemMessage = dto.SystemMessage ?? defaultSettings.SystemMessage,
            AbstractPrompt = dto.AbstractPrompt ?? defaultSettings.AbstractPrompt,
            StructuredPrompt = dto.StructuredPrompt ?? defaultSettings.StructuredPrompt,
            ProsePrompt = dto.ProsePrompt ?? defaultSettings.ProsePrompt,

            EmailPrompt = dto.EmailPrompt ?? defaultSettings.EmailPrompt,
            AufgabePrompt = dto.AufgabePrompt ?? defaultSettings.AufgabePrompt,
            GespraechsnotizPrompt = dto.GespraechsnotizPrompt ?? defaultSettings.GespraechsnotizPrompt,
            StundenzettelPrompt = dto.StundenzettelPrompt ?? defaultSettings.StundenzettelPrompt,
            AnalogPrompt = dto.AnalogPrompt ?? defaultSettings.AnalogPrompt,
        };
    }

    private static SettingsDto MapToDto(AppSettings s) => new()
    {
        PromptDefaultsRevision = s.PromptDefaultsRevision,
        Name = s.Name,
        Firma = s.Firma,
        Quellverzeichnis = s.Quellverzeichnis,
        Archivverzeichnis = s.Archivverzeichnis,
        Ausgabeverzeichnis = s.Ausgabeverzeichnis,

        SystemMessage = s.SystemMessage,
        AbstractPrompt = s.AbstractPrompt,
        StructuredPrompt = s.StructuredPrompt,
        ProsePrompt = s.ProsePrompt,

        EmailPrompt = s.EmailPrompt,
        AufgabePrompt = s.AufgabePrompt,
        GespraechsnotizPrompt = s.GespraechsnotizPrompt,
        StundenzettelPrompt = s.StundenzettelPrompt,
        AnalogPrompt = s.AnalogPrompt,
    };

    // Separate DTO to decouple JSON shape from the domain record
    private sealed class SettingsDto
    {
        public int? PromptDefaultsRevision { get; set; }

        public string? Name { get; set; }

        public string? Firma { get; set; }

        public string? Quellverzeichnis { get; set; }

        public string? Archivverzeichnis { get; set; }

        public string? Ausgabeverzeichnis { get; set; }

        public string? SystemMessage { get; set; }

        public string? AbstractPrompt { get; set; }

        public string? StructuredPrompt { get; set; }

        public string? ProsePrompt { get; set; }

        public string? EmailPrompt { get; set; }

        public string? AufgabePrompt { get; set; }

        public string? GespraechsnotizPrompt { get; set; }

        public string? StundenzettelPrompt { get; set; }

        public string? AnalogPrompt { get; set; }
    }
}
