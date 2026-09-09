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

    /// <summary>
    /// Set when the last <see cref="LoadAsync"/> could not read an existing file.
    /// This file is usually the team-shared prompts.json, so a silent fallback to
    /// built-in defaults degrades every colleague at once (#45 H3).
    /// </summary>
    public SettingsFileFault? LastLoadFault { get; private set; }

    /// <inheritdoc/>
    public bool LastLoadReadFile { get; private set; }

    public async Task<PromptSettings> LoadAsync(CancellationToken ct = default)
    {
        this.LastLoadFault = null;
        this.LastLoadReadFile = false;

        if (!File.Exists(this.filePath))
        {
            return PromptSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(this.filePath);
            var dto = await JsonSerializer.DeserializeAsync<PromptDto>(stream, Options, ct).ConfigureAwait(false);
            this.LastLoadReadFile = true;
            return dto is null ? PromptSettings.Default : MapToSettings(dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.LastLoadFault = CorruptSettingsBackup.Preserve(this.filePath, ex);
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

            // Absent in files written before v1.4.0 and in any file a v1.3.x client has
            // rewritten, so a missing key means "none", not "fall back to defaults".
            CustomCategories = dto.CustomCategories is { Count: > 0 }
                ? [.. dto.CustomCategories
                    .Where(c => !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name))
                    .Select(c => new CategoryDefinition
                    {
                        Id = c.Id!,
                        Name = c.Name!,
                        Prompt = c.Prompt ?? string.Empty,
                        Order = c.Order,
                        MaxTokens = c.MaxTokens > 0 ? c.MaxTokens : 20000,
                    })]
                : [],
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

        // Scope is deliberately NOT written: it is derived from which file the category
        // was read from, so persisting it would let a hand-edited global file claim to be
        // personal and escape the "wirkt für alle Nutzer" warning.
        CustomCategories = [.. s.CustomCategories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Prompt = c.Prompt,
            Order = c.Order,
            MaxTokens = c.MaxTokens,
        })],
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

        public List<CategoryDto>? CustomCategories { get; set; }
    }

    private sealed class CategoryDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Prompt { get; set; }

        public int Order { get; set; }

        public int MaxTokens { get; set; }
    }
}
