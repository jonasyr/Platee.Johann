namespace Platee.Johann.Infrastructure.Json;

using System.Text.Json;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

/// <summary>
/// Persists <see cref="PromptSettings"/> as JSON to prompts.json.
/// Falls back to <see cref="PromptSettings.Default"/> on missing or corrupt files.
/// </summary>
public sealed class JsonPromptSettingsRepository : IPromptSettingsRepository
{
    private readonly string filePath;
    private readonly string directory;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public JsonPromptSettingsRepository(string settingsDirectory, bool createDirectory = true)
        : this(Path.Combine(settingsDirectory, "prompts.json"), settingsDirectory, createDirectory)
    {
    }

    private JsonPromptSettingsRepository(string filePath, string directory, bool createDirectory)
    {
        this.filePath = filePath;
        this.directory = directory;
        if (createDirectory)
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static JsonPromptSettingsRepository FromFilePath(string fullFilePath)
    {
        var dir = Path.GetDirectoryName(fullFilePath) ?? string.Empty;
        return new JsonPromptSettingsRepository(fullFilePath, dir, createDirectory: false);
    }

    public bool IsReachable
    {
        get
        {
            try
            {
                return File.Exists(this.filePath);
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<PromptSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(this.filePath))
        {
            return PromptSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(this.filePath);
            var dto = await JsonSerializer.DeserializeAsync<PromptDto>(stream, Options, ct).ConfigureAwait(false);
            return dto is null ? PromptSettings.Default : MapToSettings(dto);
        }
        catch
        {
            return PromptSettings.Default;
        }
    }

    public async Task SaveAsync(PromptSettings settings, CancellationToken ct = default)
    {
        var dto = MapToDto(settings);
        await using var stream = File.Open(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, dto, Options, ct).ConfigureAwait(false);
    }

    private static PromptSettings MapToSettings(PromptDto dto)
    {
        var defaults = PromptSettings.Default;
        return new()
        {
            PromptDefaultsRevision = dto.PromptDefaultsRevision ?? defaults.PromptDefaultsRevision,
            SystemMessage = dto.SystemMessage ?? defaults.SystemMessage,
            AbstractPrompt = dto.AbstractPrompt ?? defaults.AbstractPrompt,
            StructuredPrompt = dto.StructuredPrompt ?? defaults.StructuredPrompt,
            ProsePrompt = dto.ProsePrompt ?? defaults.ProsePrompt,
            EmailPrompt = dto.EmailPrompt ?? defaults.EmailPrompt,
            AufgabePrompt = dto.AufgabePrompt ?? defaults.AufgabePrompt,
            GespraechsnotizPrompt = dto.GespraechsnotizPrompt ?? defaults.GespraechsnotizPrompt,
            StundenzettelPrompt = dto.StundenzettelPrompt ?? defaults.StundenzettelPrompt,
            AnalogPrompt = dto.AnalogPrompt ?? defaults.AnalogPrompt,
        };
    }

    private static PromptDto MapToDto(PromptSettings s) => new()
    {
        PromptDefaultsRevision = s.PromptDefaultsRevision,
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

    private sealed class PromptDto
    {
        public int? PromptDefaultsRevision { get; set; }

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
