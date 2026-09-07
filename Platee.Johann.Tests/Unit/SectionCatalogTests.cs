namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Services;
using Platee.Johann.Application.Settings;

/// <summary>
/// Covers the unified read model: how global and personal category lists merge, and how
/// built-ins and custom categories are projected into one ordered section list.
/// </summary>
public sealed class SectionCatalogTests
{
    [Fact]
    public void MergeCategories_PersonalWinsOverGlobalOnSameId()
    {
        var global = new PromptSettings { CustomCategories = [Cat("custom.x", "Global-Name")] };
        var local = new PromptSettings { CustomCategories = [Cat("custom.x", "Persönlich-Name")] };

        var merged = PromptSettingsLoader.MergeCategories(global, local);

        merged.Should().HaveCount(1);
        merged[0].Name.Should().Be("Persönlich-Name");
        merged[0].Scope.Should().Be(CategoryScope.Personal);
    }

    [Fact]
    public void MergeCategories_TagsScopeFromTheSourceFile()
    {
        var global = new PromptSettings { CustomCategories = [Cat("custom.g", "G")] };
        var local = new PromptSettings { CustomCategories = [Cat("custom.p", "P")] };

        var merged = PromptSettingsLoader.MergeCategories(global, local);

        merged.Single(c => c.Id == "custom.g").Scope.Should().Be(CategoryScope.Global);
        merged.Single(c => c.Id == "custom.p").Scope.Should().Be(CategoryScope.Personal);
    }

    [Fact]
    public void MergeCategories_IgnoresScopeStoredInTheFile()
    {
        // Scope is derived from the source file, never trusted from JSON — otherwise a
        // hand-edited global file could claim to be personal and escape the "affects
        // everyone" warning.
        var global = new PromptSettings
        {
            CustomCategories = [Cat("custom.g", "G") with { Scope = CategoryScope.Personal }],
        };

        var merged = PromptSettingsLoader.MergeCategories(global, new PromptSettings());

        merged[0].Scope.Should().Be(CategoryScope.Global);
    }

    [Fact]
    public void MergeCategories_OrdersByOrderThenName()
    {
        var global = new PromptSettings
        {
            CustomCategories =
            [
                Cat("custom.b", "Beta", order: 2),
                Cat("custom.a", "Alpha", order: 1),
            ],
        };

        var merged = PromptSettingsLoader.MergeCategories(global, new PromptSettings());

        merged.Select(c => c.Id).Should().ContainInOrder("custom.a", "custom.b");
    }

    [Fact]
    public void MergeCategories_BothEmpty_ReturnsEmpty()
    {
        PromptSettingsLoader.MergeCategories(new PromptSettings(), new PromptSettings())
            .Should().BeEmpty();
    }

    [Fact]
    public void Build_ReturnsSevenBuiltInsPlusCustomInOrder()
    {
        var prompts = new PromptSettings { CustomCategories = [Cat("custom.z", "Zuletzt", order: 99)] };

        var catalog = SectionCatalog.Build(prompts, new Dictionary<string, GenerationMode>());

        catalog.Should().HaveCount(8);
        catalog.Last().Id.Should().Be("custom.z");
        catalog.Select(s => s.Id).Should().Contain(BuiltInSections.TaskList);
    }

    [Fact]
    public void Build_AppliesModeFromMapAndDefaultsBuiltInsToAuto()
    {
        var modes = new Dictionary<string, GenerationMode>
        {
            [BuiltInSections.Stundenzettel] = GenerationMode.OnDemand,
        };

        var catalog = SectionCatalog.Build(PromptSettings.Default, modes);

        catalog.Single(s => s.Id == BuiltInSections.Stundenzettel).Mode
            .Should().Be(GenerationMode.OnDemand);
        catalog.Single(s => s.Id == BuiltInSections.LongSummary).Mode
            .Should().Be(GenerationMode.Auto);
    }

    [Fact]
    public void Build_UnknownCustomCategoryDefaultsToOnDemand()
    {
        // Adding a category must never silently slow down processing.
        var prompts = new PromptSettings { CustomCategories = [Cat("custom.neu", "Neu")] };

        var catalog = SectionCatalog.Build(prompts, new Dictionary<string, GenerationMode>());

        catalog.Single(s => s.Id == "custom.neu").Mode.Should().Be(GenerationMode.OnDemand);
    }

    [Fact]
    public void Build_CarriesTheCategoryOnCustomDescriptorsAndNullOnBuiltIns()
    {
        var prompts = new PromptSettings { CustomCategories = [Cat("custom.k", "K")] };

        var catalog = SectionCatalog.Build(prompts, new Dictionary<string, GenerationMode>());

        catalog.Single(s => s.Id == "custom.k").Category.Should().NotBeNull();
        catalog.Single(s => s.Id == "custom.k").IsBuiltIn.Should().BeFalse();
        catalog.Single(s => s.Id == BuiltInSections.Analog).Category.Should().BeNull();
        catalog.Single(s => s.Id == BuiltInSections.Analog).IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    public void Build_UsesGermanDisplayNamesForBuiltIns()
    {
        var catalog = SectionCatalog.Build(PromptSettings.Default, new Dictionary<string, GenerationMode>());

        catalog.Single(s => s.Id == BuiltInSections.TaskList).Name.Should().Be("Aufgaben");
    }

    private static CategoryDefinition Cat(string id, string name, int order = 0) =>
        new() { Id = id, Name = name, Prompt = "P {transcript}", Order = order };
}
