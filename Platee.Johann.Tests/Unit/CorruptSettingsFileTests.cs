using FluentAssertions;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Covers the audit findings H2/H3: a corrupt settings file used to be swallowed,
/// replaced by defaults, and then overwritten by the next save.
/// </summary>
public sealed class CorruptSettingsFileTests : IDisposable
{
    private readonly string tempDir = Path.Combine(
        Path.GetTempPath(), "johann-corrupt-" + Guid.NewGuid().ToString("N"));

    public CorruptSettingsFileTests() => Directory.CreateDirectory(this.tempDir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Temp cleanup only — a locked file must not fail the test run.
        }
    }

    // ── AppSettings ────────────────────────────────────────────────────────────
    [Fact]
    public async Task SettingsRepository_WhenFileIsCorrupt_ReportsFault()
    {
        var path = Path.Combine(this.tempDir, "settings.json");
        File.WriteAllText(path, "{ this is not json");
        var sut = new JsonSettingsRepository(this.tempDir);

        var loaded = await sut.LoadAsync();

        loaded.Should().BeEquivalentTo(AppSettings.Default);
        sut.LastLoadFault.Should().NotBeNull();
        sut.LastLoadFault!.FilePath.Should().Be(path);
        sut.LastLoadFault.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SettingsRepository_WhenFileIsCorrupt_PreservesContentBeforeItCanBeOverwritten()
    {
        var path = Path.Combine(this.tempDir, "settings.json");
        File.WriteAllText(path, "{ \"name\": \"Jonas\", broken");
        var sut = new JsonSettingsRepository(this.tempDir);

        await sut.LoadAsync();
        var backupPath = sut.LastLoadFault!.BackupPath;

        // The save that follows a failed load is what used to destroy the data.
        await sut.SaveAsync(AppSettings.Default);

        backupPath.Should().NotBeNull();
        File.Exists(backupPath!).Should().BeTrue();
        File.ReadAllText(backupPath!).Should().Contain("Jonas");
    }

    [Fact]
    public async Task SettingsRepository_WhenFileIsValid_ReportsNoFault()
    {
        var sut = new JsonSettingsRepository(this.tempDir);
        await sut.SaveAsync(AppSettings.Default with { Name = "Jonas" });

        var loaded = await sut.LoadAsync();

        loaded.Name.Should().Be("Jonas");
        sut.LastLoadFault.Should().BeNull();
    }

    [Fact]
    public async Task SettingsRepository_WhenFileIsMissing_ReportsNoFault()
    {
        var sut = new JsonSettingsRepository(this.tempDir);

        await sut.LoadAsync();

        sut.LastLoadFault.Should().BeNull();
    }

    [Fact]
    public async Task SettingsRepository_AfterRecovery_ClearsTheEarlierFault()
    {
        var path = Path.Combine(this.tempDir, "settings.json");
        File.WriteAllText(path, "{ broken");
        var sut = new JsonSettingsRepository(this.tempDir);
        await sut.LoadAsync();
        sut.LastLoadFault.Should().NotBeNull();

        await sut.SaveAsync(AppSettings.Default with { Name = "Repariert" });
        var loaded = await sut.LoadAsync();

        loaded.Name.Should().Be("Repariert");
        sut.LastLoadFault.Should().BeNull();
    }

    // ── PromptSettings ─────────────────────────────────────────────────────────
    [Fact]
    public async Task PromptRepository_WhenFileIsCorrupt_ReportsFaultAndPreservesContent()
    {
        var path = Path.Combine(this.tempDir, "prompts.json");
        File.WriteAllText(path, "{ \"systemMessage\": \"team prompt\", broken");
        var sut = new JsonPromptSettingsRepository(this.tempDir);

        var loaded = await sut.LoadAsync();

        loaded.SystemMessage.Should().Be(PromptSettings.Default.SystemMessage);
        sut.LastLoadFault.Should().NotBeNull();
        File.ReadAllText(sut.LastLoadFault!.BackupPath!).Should().Contain("team prompt");
    }

    [Fact]
    public async Task PromptRepository_CorruptSharedFile_IsCopiedNotMoved()
    {
        // The shared prompts.json may be open by every colleague at once; renaming
        // it would break them all. The original must stay exactly where it was.
        var path = Path.Combine(this.tempDir, "prompts.json");
        File.WriteAllText(path, "{ broken");
        var sut = JsonPromptSettingsRepository.FromFilePath(path);

        await sut.LoadAsync();

        File.Exists(path).Should().BeTrue();
        sut.LastLoadFault!.BackupPath.Should().NotBe(path);
    }

    [Fact]
    public async Task PromptRepository_WhenFileIsValid_ReportsNoFault()
    {
        var sut = new JsonPromptSettingsRepository(this.tempDir);
        await sut.SaveAsync(PromptSettings.Default with { SystemMessage = "ok" });

        var loaded = await sut.LoadAsync();

        loaded.SystemMessage.Should().Be("ok");
        sut.LastLoadFault.Should().BeNull();
    }

    // ── Backup helper ──────────────────────────────────────────────────────────
    [Fact]
    public void Preserve_WhenBackupItselfFails_StillReportsTheOriginalReason()
    {
        var missing = Path.Combine(this.tempDir, "does-not-exist.json");

        var fault = CorruptSettingsBackup.Preserve(missing, new InvalidDataException("kaputt"));

        fault.BackupPath.Should().BeNull();
        fault.Reason.Should().Contain("kaputt");
    }
}
