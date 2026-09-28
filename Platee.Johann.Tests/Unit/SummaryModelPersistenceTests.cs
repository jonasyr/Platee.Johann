using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #71 — die Modellwahl muss einen Neustart ueberleben.
/// <para>
/// Die Mapper in <see cref="JsonSettingsRepository"/> sind handgeschrieben und haben in
/// diesem Projekt bereits dreimal ein Feld still gefressen (CustomCategories, SectionModes,
/// CustomSections). Der Rundlauf-Test ist deshalb Pflicht.
/// </para>
/// </summary>
public sealed class SummaryModelPersistenceTests : IDisposable
{
    private readonly string directory =
        Path.Combine(Path.GetTempPath(), "johann-tests-" + Guid.NewGuid().ToString("N"));

    public SummaryModelPersistenceTests() => Directory.CreateDirectory(this.directory);

    private string SettingsFile => Path.Combine(this.directory, "settings.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(this.directory, recursive: true);
        }
        catch
        {
            // Aufraeumen darf den Test nicht zum Kippen bringen.
        }
    }

    [Fact]
    public void Default_settings_use_the_catalog_default()
    {
        AppSettings.Default.SummaryModel.Should().Be(ModelNames.Summaries);
    }

    [Fact]
    public async Task SummaryModel_survives_a_save_and_load_round_trip()
    {
        var repo = new JsonSettingsRepository(this.directory);
        var saved = AppSettings.Default with { SummaryModel = "gpt-5.6-sol" };

        await repo.SaveAsync(saved);
        var loaded = await new JsonSettingsRepository(this.directory).LoadAsync();

        loaded.SummaryModel.Should().Be("gpt-5.6-sol");
    }

    [Fact]
    public async Task A_missing_key_falls_back_to_the_default()
    {
        // settings.json aus einer Version vor #71 kennt den Schluessel nicht.
        await File.WriteAllTextAsync(this.SettingsFile, """{"Name":"Test"}""");

        var loaded = await new JsonSettingsRepository(this.directory).LoadAsync();

        loaded.SummaryModel.Should().Be(ModelNames.Summaries);
    }

    [Fact]
    public async Task An_unknown_id_is_written_through_unrepaired()
    {
        // Der Mapper repariert nicht still — das entscheidet SummaryModelResolver beim Start.
        await File.WriteAllTextAsync(this.SettingsFile, """{"SummaryModel":"gpt-4o"}""");

        var loaded = await new JsonSettingsRepository(this.directory).LoadAsync();

        loaded.SummaryModel.Should().Be("gpt-4o");
    }
}
