namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// The Kategorien editor in the settings view.
/// <para>
/// The load-bearing invariant here is that an id is minted once and never re-derived from
/// the mutable name: generated text lives under the id in <c>Entry.CustomSections</c>, so
/// a rename that changed it would orphan every section already produced.
/// </para>
/// </summary>
public sealed class CategoryEditorTests
{
    private static SettingsViewModel CreateSut(
        AppSettings? settings = null,
        PromptSettings? prompts = null)
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo.SaveAsync(Arg.Any<PromptSettings>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var holder = new SettingsHolder(
            settings ?? AppSettings.Default,
            prompts ?? PromptSettings.Default);

        return new SettingsViewModel(repo, promptRepo, holder);
    }

    [Fact]
    public void AddCategory_MintsAUniqueIdAndDefaultsToPersonalOnDemand()
    {
        var sut = CreateSut();

        sut.AddCategoryCommand.Execute(null);

        sut.Categories.Should().HaveCount(1);
        sut.Categories[0].Id.Should().StartWith("custom.");
        sut.Categories[0].Scope.Should().Be(CategoryScope.Personal);
        sut.Categories[0].Mode.Should().Be(GenerationMode.OnDemand);
    }

    [Fact]
    public void AddCategory_Twice_DoesNotCollideOnId()
    {
        var sut = CreateSut();

        sut.AddCategoryCommand.Execute(null);
        sut.AddCategoryCommand.Execute(null);

        sut.Categories.Should().HaveCount(2);
        sut.Categories[0].Id.Should().NotBe(sut.Categories[1].Id);
    }

    [Fact]
    public void RenamingACategory_DoesNotChangeItsId()
    {
        var sut = CreateSut();
        sut.AddCategoryCommand.Execute(null);
        var originalId = sut.Categories[0].Id;

        sut.Categories[0].Name = "Ganz anderer Name";

        sut.Categories[0].Id.Should().Be(originalId, "renaming must never orphan generated text");
        sut.Categories[0].ToDefinition().Id.Should().Be(originalId);
    }

    [Fact]
    public void PromptWithoutTranscriptPlaceholder_IsFlaggedInvalid()
    {
        var sut = CreateSut();
        sut.AddCategoryCommand.Execute(null);

        sut.Categories[0].Prompt = "Kein Platzhalter hier";

        sut.Categories[0].IsPromptValid.Should().BeFalse();
        sut.Categories[0].IsPromptInvalid.Should().BeTrue();
    }

    [Fact]
    public void ANewCategory_StartsWithAValidPrompt()
    {
        var sut = CreateSut();

        sut.AddCategoryCommand.Execute(null);

        sut.Categories[0].IsPromptValid.Should().BeTrue();
    }

    [Fact]
    public void DuplicateCategory_CopiesTheContentButNotTheId()
    {
        var sut = CreateSut();
        sut.AddCategoryCommand.Execute(null);
        var original = sut.Categories[0];
        original.Name = "Programmierung";
        original.Prompt = "Baue einen Prompt aus {transcript}";
        original.Mode = GenerationMode.Auto;

        sut.DuplicateCategoryCommand.Execute(original);

        sut.Categories.Should().HaveCount(2);
        var copy = sut.Categories[1];
        copy.Id.Should().NotBe(original.Id, "two categories writing to one slot would overwrite each other");
        copy.Prompt.Should().Be(original.Prompt);
        copy.Mode.Should().Be(GenerationMode.Auto);
    }

    [Fact]
    public void RemoveCategory_DropsItFromTheList()
    {
        var sut = CreateSut();
        sut.AddCategoryCommand.Execute(null);
        sut.AddCategoryCommand.Execute(null);
        var doomed = sut.Categories[0];

        sut.RemoveCategoryCommand.Execute(doomed);

        sut.Categories.Should().HaveCount(1);
        sut.Categories.Should().NotContain(doomed);
    }

    [Fact]
    public void MoveCategoryDown_ThenUp_RestoresTheOrderAndRenumbers()
    {
        var sut = CreateSut();
        sut.AddCategoryCommand.Execute(null);
        sut.AddCategoryCommand.Execute(null);
        var first = sut.Categories[0];

        sut.MoveCategoryDownCommand.Execute(first);

        sut.Categories[1].Should().BeSameAs(first);
        sut.Categories[0].Order.Should().Be(0);
        sut.Categories[1].Order.Should().Be(1);

        sut.MoveCategoryUpCommand.Execute(first);

        sut.Categories[0].Should().BeSameAs(first);
    }

    [Fact]
    public void MoveCategoryUp_AtTheTop_IsANoOp()
    {
        var sut = CreateSut();
        sut.AddCategoryCommand.Execute(null);
        var only = sut.Categories[0];

        sut.MoveCategoryUpCommand.Execute(only);

        sut.Categories.Should().ContainSingle().Which.Should().BeSameAs(only);
    }

    [Fact]
    public void LoadFromHolder_RestoresCategoriesWithTheirPersistedMode()
    {
        var prompts = PromptSettings.Default with
        {
            CustomCategories =
            [
                new CategoryDefinition
                {
                    Id = "custom.prog",
                    Name = "Programmierung",
                    Prompt = "{transcript}",
                    Scope = CategoryScope.Global,
                    Order = 0,
                },
            ],
        };
        var settings = AppSettings.Default with
        {
            SectionModes = new Dictionary<string, GenerationMode>
            {
                ["custom.prog"] = GenerationMode.Auto,
            },
        };

        var sut = CreateSut(settings, prompts);

        sut.Categories.Should().ContainSingle();
        sut.Categories[0].Id.Should().Be("custom.prog");
        sut.Categories[0].Mode.Should().Be(GenerationMode.Auto);
        sut.Categories[0].Scope.Should().Be(CategoryScope.Global);
    }

    [Fact]
    public async Task SaveAsync_PersistsCategoriesAndSectionModes()
    {
        var repo = Substitute.For<ISettingsRepository>();
        AppSettings? savedSettings = null;
        repo.SaveAsync(Arg.Do<AppSettings>(s => savedSettings = s)).Returns(Task.CompletedTask);

        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        PromptSettings? savedPrompts = null;
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo
            .SaveAsync(Arg.Do<PromptSettings>(p => savedPrompts = p), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new SettingsViewModel(
            repo, promptRepo, new SettingsHolder(AppSettings.Default, PromptSettings.Default));

        sut.AddCategoryCommand.Execute(null);
        sut.Categories[0].Name = "Programmierung";
        sut.Categories[0].Mode = GenerationMode.Auto;

        await sut.SaveCommand.ExecuteAsync(null);

        // The id is minted on save, from the name the user typed — not at creation time from
        // the „Neue Kategorie" placeholder, which used to give every first category the same id.
        var id = sut.Categories[0].Id;
        id.Should().StartWith("custom.programmierung-");

        savedSettings.Should().NotBeNull();
        savedSettings!.SectionModes.Should().ContainKey(id)
            .WhoseValue.Should().Be(GenerationMode.Auto);

        // Every built-in also carries a mode, so the pipeline never falls back to a guess.
        savedSettings.SectionModes.Should().ContainKey(BuiltInSections.LongSummary);

        savedPrompts.Should().NotBeNull();
        savedPrompts!.CustomCategories.Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [Fact]
    public async Task SaveAsync_RaisesPropertyChangedForTheFinalisedId()
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.LoadAsync(Arg.Any<CancellationToken>()).Returns(AppSettings.Default);
        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.IsReachable.Returns(true);
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);

        var sut = new SettingsViewModel(
            repo, promptRepo, new SettingsHolder(AppSettings.Default, PromptSettings.Default));

        sut.AddCategoryCommand.Execute(null);
        sut.Categories[0].Name = "TestOnDemand";

        var changed = new List<string?>();
        sut.Categories[0].PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await sut.SaveCommand.ExecuteAsync(null);

        changed.Should().Contain(
            nameof(CategoryEditorViewModel.Id),
            "the id shown under the category name must update without reopening the window");
    }

    [Fact]
    public void Sections_ContainsKategorien()
    {
        var sut = CreateSut();

        sut.Sections.Should().Contain(s => s.Key == "kategorien");
    }

    [Fact]
    public void IsKategorienSelected_WhenSectionSelected_ReturnsTrue()
    {
        var sut = CreateSut();

        sut.SelectedSection = sut.Sections.First(s => s.Key == "kategorien");

        sut.IsKategorienSelected.Should().BeTrue();
    }

    [Fact]
    public void BuiltInSectionModes_CoversEveryBuiltInSection()
    {
        var sut = CreateSut();

        sut.BuiltInSectionModes.Select(r => r.Id)
            .Should().BeEquivalentTo(BuiltInSections.All);
    }

    [Fact]
    public void BuiltInSectionModes_ReflectThePersistedModes()
    {
        var settings = AppSettings.Default with
        {
            SectionModes = new Dictionary<string, GenerationMode>
            {
                [BuiltInSections.Stundenzettel] = GenerationMode.OnDemand,
            },
        };

        var sut = CreateSut(settings);

        var row = sut.BuiltInSectionModes.Single(r => r.Id == BuiltInSections.Stundenzettel);
        row.IsAuto.Should().BeFalse();

        row.IsAuto = true;
        row.Mode.Should().Be(GenerationMode.Auto);
    }
}
