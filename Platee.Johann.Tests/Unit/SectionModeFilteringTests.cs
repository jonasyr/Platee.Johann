namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Settings;

/// <summary>
/// Covers the v1.4.0 generation-mode presets and their storage on <see cref="AppSettings"/>.
/// Modes are personal per user by design — a colleague must never be able to change
/// someone else's waiting time through the shared prompts file.
/// </summary>
public sealed class SectionModeFilteringTests
{
    [Fact]
    public void Recommended_MarksExactlyFourSectionsAuto()
    {
        var auto = SectionModeDefaults.Recommended
            .Where(kv => kv.Value == GenerationMode.Auto)
            .Select(kv => kv.Key)
            .ToList();

        auto.Should().HaveCount(4);
        auto.Should().BeEquivalentTo(new[]
        {
            BuiltInSections.LongSummary,
            BuiltInSections.ProseSummary,
            BuiltInSections.TaskList,
            BuiltInSections.ConversationNote,
        });
    }

    [Fact]
    public void Recommended_MarksStundenzettelAnalogAndEmailOnDemand()
    {
        SectionModeDefaults.Recommended[BuiltInSections.Stundenzettel]
            .Should().Be(GenerationMode.OnDemand);
        SectionModeDefaults.Recommended[BuiltInSections.Analog]
            .Should().Be(GenerationMode.OnDemand);
        SectionModeDefaults.Recommended[BuiltInSections.EmailText]
            .Should().Be(GenerationMode.OnDemand);
    }

    [Fact]
    public void Recommended_CoversEveryBuiltInSection()
    {
        // A missing key would silently fall back to Auto in SectionCatalog, quietly
        // undoing the speed win this preset exists to deliver.
        SectionModeDefaults.Recommended.Keys.Should().BeEquivalentTo(BuiltInSections.All);
    }

    [Fact]
    public void AllAuto_MarksEveryBuiltInAuto()
    {
        SectionModeDefaults.AllAuto.Should().HaveCount(BuiltInSections.All.Count);
        SectionModeDefaults.AllAuto.Values.Should().AllBeEquivalentTo(GenerationMode.Auto);
    }

    [Fact]
    public void AppSettings_Default_HasNotYetRunTheModeMigration()
    {
        AppSettings.Default.SectionModesMigrationDone.Should().BeFalse();
        AppSettings.Default.SectionModes.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ShouldShow_IsTrueOnlyBeforeTheMigrationHasRun()
    {
        SectionModeMigration.ShouldShow(AppSettings.Default).Should().BeTrue();
        SectionModeMigration.ShouldShow(
            AppSettings.Default with { SectionModesMigrationDone = true }).Should().BeFalse();
    }

    [Fact]
    public void Apply_Recommended_SetsFourAutoSectionsAndMarksDone()
    {
        var result = SectionModeMigration.Apply(AppSettings.Default, useRecommended: true);

        result.SectionModesMigrationDone.Should().BeTrue();
        result.SectionModes.Count(kv => kv.Value == GenerationMode.Auto).Should().Be(4);
    }

    [Fact]
    public void Apply_KeepPrevious_MarksEveryBuiltInAutoAndMarksDone()
    {
        var result = SectionModeMigration.Apply(AppSettings.Default, useRecommended: false);

        result.SectionModesMigrationDone.Should().BeTrue();
        result.SectionModes.Values.Should().AllBeEquivalentTo(GenerationMode.Auto);
    }

    [Fact]
    public void Apply_DoesNotMutateTheSourceSettings()
    {
        var original = AppSettings.Default;

        SectionModeMigration.Apply(original, useRecommended: true);

        original.SectionModesMigrationDone.Should().BeFalse();
        original.SectionModes.Should().BeEmpty();
    }
}
