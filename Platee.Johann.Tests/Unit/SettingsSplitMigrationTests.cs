using System.Text.Json;
using FluentAssertions;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Tests.Unit;

public class SettingsSplitMigrationTests : IDisposable
{
    private readonly string tempDir;

    public SettingsSplitMigrationTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "johann-migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public void MigrateIfNeeded_WhenPromptsFileAlreadyExists_DoesNothing()
    {
        var settingsPath = Path.Combine(this.tempDir, "settings.json");
        var promptsPath = Path.Combine(this.tempDir, "prompts.json");
        File.WriteAllText(promptsPath, "{}");

        var result = SettingsSplitMigration.MigrateIfNeeded(settingsPath, promptsPath);

        result.DidMigrate.Should().BeFalse();
    }

    [Fact]
    public void MigrateIfNeeded_WhenNoSettingsFile_DoesNothing()
    {
        var settingsPath = Path.Combine(this.tempDir, "settings.json");
        var promptsPath = Path.Combine(this.tempDir, "prompts.json");

        var result = SettingsSplitMigration.MigrateIfNeeded(settingsPath, promptsPath);

        result.DidMigrate.Should().BeFalse();
    }

    [Fact]
    public void MigrateIfNeeded_WhenLegacySettingsHasPrompts_ExtractsToPromptsFile()
    {
        var settingsPath = Path.Combine(this.tempDir, "settings.json");
        var promptsPath = Path.Combine(this.tempDir, "prompts.json");

        var legacy = new
        {
            name = "Test User",
            firma = "Test GmbH",
            quellverzeichnis = @"C:\input",
            systemMessage = "custom-system",
            abstractPrompt = "custom-abstract",
            promptDefaultsRevision = 20260513,
        };
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(legacy, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        }));

        var result = SettingsSplitMigration.MigrateIfNeeded(settingsPath, promptsPath);

        result.DidMigrate.Should().BeTrue();
        File.Exists(promptsPath).Should().BeTrue();

        var promptsContent = File.ReadAllText(promptsPath);
        promptsContent.Should().Contain("custom-system");
        promptsContent.Should().Contain("custom-abstract");

        var settingsContent = File.ReadAllText(settingsPath);
        settingsContent.Should().NotContain("systemMessage");
        settingsContent.Should().Contain("Test User");
    }
}
