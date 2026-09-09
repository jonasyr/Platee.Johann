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

    private static IReadOnlyList<SectionDescriptor> Catalog(params (string Id, string Name)[] categories) =>
        SectionCatalog.Build(
            PromptSettings.Default with
            {
                CustomCategories = [.. categories.Select(c => new CategoryDefinition
                {
                    Id = c.Id, Name = c.Name, Prompt = "{transcript}",
                })],
            },
            new Dictionary<string, GenerationMode>());
}
