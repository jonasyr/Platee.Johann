namespace Platee.Johann.UI.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Application.Settings;

/// <summary>
/// Binding wrapper around one <see cref="CategoryDefinition"/> in the settings view.
/// <para>
/// <see cref="Id"/> is deliberately read-only and never re-derived from
/// <see cref="Name"/>: generated text is persisted under the id in
/// <c>Entry.CustomSections</c>, so a rename that changed the id would orphan every
/// section already produced for that category.
/// </para>
/// </summary>
public sealed partial class CategoryEditorViewModel : ObservableObject
{
    /// <summary>The placeholder every category prompt must contain to receive the text.</summary>
    public const string TranscriptPlaceholder = "{transcript}";

    /// <summary>The prompt a freshly added category starts with — already valid.</summary>
    public const string DefaultPrompt =
        "Erstelle den gewünschten Text aus dem folgenden Transkript:\n\n{transcript}";

    public CategoryEditorViewModel(CategoryDefinition definition, GenerationMode mode)
    {
        this.Id = definition.Id;
        this.name = definition.Name;
        this.prompt = definition.Prompt;
        this.scope = definition.Scope;
        this.order = definition.Order;
        this.mode = mode;
        this.MaxTokens = definition.MaxTokens;
    }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPromptValid))]
    [NotifyPropertyChangedFor(nameof(IsPromptInvalid))]
    private string prompt;

    [ObservableProperty]
    private CategoryScope scope;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuto))]
    private GenerationMode mode;

    [ObservableProperty]
    private int order;

    /// <summary>Gets the stable id. Fixed once the category has been saved; never changes after that.</summary>
    public string Id { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this category has never been saved, so its id is
    /// still a placeholder derived from the default name rather than the one the user typed.
    /// </summary>
    internal bool HasProvisionalId { get; private set; }

    /// <summary>Marks a newly added category whose id must be re-minted on first save.</summary>
    internal void MarkProvisional() => this.HasProvisionalId = true;

    /// <summary>
    /// Replaces a provisional id with the final one, derived from the name the user actually
    /// typed. Does nothing once the category has been saved, because generated text is stored
    /// under the id and re-deriving it later would orphan every existing section.
    /// </summary>
    internal void FinalizeId(string id)
    {
        if (!this.HasProvisionalId)
        {
            return;
        }

        this.Id = id;
        this.HasProvisionalId = false;
    }

    /// <summary>Gets the per-category token budget, carried through unchanged.</summary>
    public int MaxTokens { get; }

    /// <summary>
    /// Gets a value indicating whether the prompt carries the <c>{transcript}</c>
    /// placeholder. Without it the category silently produces garbage (spec risk 8).
    /// </summary>
    public bool IsPromptValid =>
        this.Prompt.Contains(TranscriptPlaceholder, StringComparison.Ordinal);

    /// <summary>Gets the inverse of <see cref="IsPromptValid"/>, for the warning's visibility.</summary>
    public bool IsPromptInvalid => !this.IsPromptValid;

    /// <summary>Gets or sets a value indicating whether the section is generated automatically.</summary>
    public bool IsAuto
    {
        get => this.Mode == GenerationMode.Auto;
        set => this.Mode = value ? GenerationMode.Auto : GenerationMode.OnDemand;
    }

    /// <summary>Gets or sets a value indicating whether the category is shared with the team.</summary>
    public bool IsGlobal
    {
        get => this.Scope == CategoryScope.Global;
        set => this.Scope = value ? CategoryScope.Global : CategoryScope.Personal;
    }

    /// <summary>Projects the edited state back into an immutable definition.</summary>
    public CategoryDefinition ToDefinition() => new()
    {
        Id = this.Id,
        Name = this.Name.Trim(),
        Prompt = this.Prompt.Trim(),
        Scope = this.Scope,
        Order = this.Order,
        MaxTokens = this.MaxTokens,
    };

    partial void OnScopeChanged(CategoryScope value) => this.OnPropertyChanged(nameof(this.IsGlobal));
}
