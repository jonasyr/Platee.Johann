namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// The „Im Eintrag anzeigen" checkboxes for user-defined categories.
/// <para>
/// The visibility map was honoured by both renderers and the clipboard from the start, but
/// nothing ever wrote to it: the panel had a fixed checkbox per built-in section and no row
/// for custom categories, so a custom section could never be hidden from an export.
/// </para>
/// </summary>
public sealed class CustomSectionVisibilityUiTests
{
    [Fact]
    public void SyncCustomSections_CreatesOneVisibleTogglePerCategory()
    {
        var sut = new SectionVisibilityViewModel();

        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha"), ("custom.b-2222", "Beta")));

        sut.CustomSections.Select(t => t.Name).Should().Equal("Alpha", "Beta");
        sut.CustomSectionVisibility.Values.Should().AllBeEquivalentTo(true);
    }

    [Fact]
    public void Unticking_WritesThroughToTheMapTheRenderersRead()
    {
        var sut = new SectionVisibilityViewModel();
        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha")));

        sut.CustomSections[0].IsVisible = false;

        sut.CustomSectionVisibility["custom.a-1111"].Should().BeFalse();
    }

    [Fact]
    public void SyncCustomSections_KeepsWhatTheUserAlreadyUnticked()
    {
        var sut = new SectionVisibilityViewModel();
        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha")));
        sut.CustomSections[0].IsVisible = false;

        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha"), ("custom.b-2222", "Beta")));

        sut.CustomSectionVisibility["custom.a-1111"].Should().BeFalse();
        sut.CustomSectionVisibility["custom.b-2222"].Should().BeTrue();
    }

    [Fact]
    public void SyncCustomSections_DropsADeletedCategoryFromTheMap()
    {
        var sut = new SectionVisibilityViewModel();
        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha")));
        sut.CustomSections[0].IsVisible = false;

        sut.SyncCustomSections(Catalog());

        sut.CustomSections.Should().BeEmpty();
        sut.CustomSectionVisibility.Should().BeEmpty();
    }

    [Fact]
    public void SyncCustomSections_PicksUpARename()
    {
        var sut = new SectionVisibilityViewModel();
        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha")));

        sut.SyncCustomSections(Catalog(("custom.a-1111", "Alpha Neu")));

        sut.CustomSections.Single().Name.Should().Be("Alpha Neu");
    }

    [Fact]
    public void SyncCustomSections_GivesDeletedCategoryTextItsOwnToggle()
    {
        // Without this the text of a deleted category renders in the detail view, the PDF,
        // the HTML and the clipboard with no way whatsoever to switch it off.
        var sut = new SectionVisibilityViewModel();

        sut.SyncCustomSections(
            Catalog(),
            new Dictionary<string, string> { ["custom.weg-36d3"] = "ES FUNKTIONIERT" },
            new Dictionary<string, string> { ["custom.weg-36d3"] = "TestOnDemand" });

        var toggle = sut.CustomSections.Single();
        toggle.Name.Should().Be("TestOnDemand");
        toggle.Group.Should().Be(SectionVisibilityViewModel.OrphanGroup);

        toggle.IsVisible = false;
        sut.CustomSectionVisibility["custom.weg-36d3"].Should().BeFalse();
    }

    [Fact]
    public void SyncCustomSections_SeparatesPersonalFromTeamCategories()
    {
        var sut = new SectionVisibilityViewModel();

        sut.SyncCustomSections(ScopedCatalog(
            ("custom.a-1111", "Alpha", CategoryScope.Personal),
            ("custom.b-2222", "Beta", CategoryScope.Global)));

        sut.CustomSections.Single(t => t.Id == "custom.a-1111").Group
            .Should().Be(SectionVisibilityViewModel.PersonalGroup);
        sut.CustomSections.Single(t => t.Id == "custom.b-2222").Group
            .Should().Be(SectionVisibilityViewModel.GlobalGroup);
    }

    [Fact]
    public void SyncCustomSections_ASectionStillConfiguredIsNotListedAsOrphaned()
    {
        var sut = new SectionVisibilityViewModel();

        sut.SyncCustomSections(
            Catalog(("custom.a-1111", "Alpha")),
            new Dictionary<string, string> { ["custom.a-1111"] = "TEXT" });

        sut.CustomSections.Should().ContainSingle()
            .Which.Group.Should().Be(SectionVisibilityViewModel.PersonalGroup);
    }

    [Fact]
    public void SyncCustomSections_IgnoresEmptyOrphanedText()
    {
        var sut = new SectionVisibilityViewModel();

        sut.SyncCustomSections(Catalog(), new Dictionary<string, string> { ["custom.leer"] = "  " });

        sut.CustomSections.Should().BeEmpty();
    }

    private static IReadOnlyList<SectionDescriptor> Catalog(params (string Id, string Name)[] categories) =>
        ScopedCatalog([.. categories.Select(c => (c.Id, c.Name, CategoryScope.Personal))]);

    private static IReadOnlyList<SectionDescriptor> ScopedCatalog(
        params (string Id, string Name, CategoryScope Scope)[] categories) =>
        SectionCatalog.Build(
            PromptSettings.Default with
            {
                CustomCategories = [.. categories.Select(c => new CategoryDefinition
                {
                    Id = c.Id, Name = c.Name, Prompt = "{transcript}", Scope = c.Scope,
                })],
            },
            new Dictionary<string, GenerationMode>());
}
