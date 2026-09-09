namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Settings;

/// <summary>
/// Gating for the one-time v1.4.0 first-run prompt. The WPF dialog itself cannot be
/// exercised here; what is testable — and what actually matters — is that the question is
/// asked exactly once and that each answer produces the promised set of modes.
/// </summary>
public sealed class SectionModeMigrationTests
{
    [Fact]
    public void ShouldShow_TrueOnlyBeforeTheMigrationHasRun()
    {
        SectionModeMigration.ShouldShow(AppSettings.Default).Should().BeTrue();
        SectionModeMigration.ShouldShow(
            AppSettings.Default with { SectionModesMigrationDone = true }).Should().BeFalse();
    }

    [Fact]
    public void ShouldShow_IsFalseOnceTheDialogsAnswerHasBeenApplied()
    {
        var answered = SectionModeMigration.Apply(AppSettings.Default, useRecommended: true);

        SectionModeMigration.ShouldShow(answered).Should().BeFalse(
            "the prompt must never reappear once the user has answered it");
    }

    [Fact]
    public void Apply_Recommended_SetsFourAutoSectionsAndMarksDone()
    {
        var result = SectionModeMigration.Apply(AppSettings.Default, useRecommended: true);

        result.SectionModesMigrationDone.Should().BeTrue();
        result.SectionModes.Count(kv => kv.Value == GenerationMode.Auto).Should().Be(4);
        result.SectionModes[BuiltInSections.Stundenzettel].Should().Be(GenerationMode.OnDemand);
        result.SectionModes[BuiltInSections.Analog].Should().Be(GenerationMode.OnDemand);
        result.SectionModes[BuiltInSections.EmailText].Should().Be(GenerationMode.OnDemand);
    }

    [Fact]
    public void Apply_KeepPrevious_MarksEveryBuiltInAutoAndMarksDone()
    {
        var result = SectionModeMigration.Apply(AppSettings.Default, useRecommended: false);

        result.SectionModesMigrationDone.Should().BeTrue();
        result.SectionModes.Values.Should().AllBeEquivalentTo(GenerationMode.Auto);
        result.SectionModes.Should().HaveCount(BuiltInSections.All.Count);
    }

    [Fact]
    public void Apply_LeavesEveryOtherSettingAlone()
    {
        var settings = AppSettings.Default with { Name = "Jonas", Firma = "Peano" };

        var result = SectionModeMigration.Apply(settings, useRecommended: true);

        result.Name.Should().Be("Jonas");
        result.Firma.Should().Be("Peano");
    }
}
