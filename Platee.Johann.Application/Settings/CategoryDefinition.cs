namespace Platee.Johann.Application.Settings;

/// <summary>
/// A user-definable summary category.
/// <para>
/// Built-in sections are deliberately NOT represented as <see cref="CategoryDefinition"/>
/// instances — they keep their fixed properties on <see cref="PromptSettings"/> so the
/// eight legacy <see cref="Domain.Entities.Entry"/> fields and every renderer that reads
/// them stay untouched. See <c>SectionCatalog</c> for the unified read model that presents
/// both kinds as one list.
/// </para>
/// </summary>
public sealed record CategoryDefinition
{
    /// <summary>
    /// Gets the stable identifier, minted once by <see cref="CategoryIdFactory"/> and never
    /// re-derived from <see cref="Name"/>, so renaming cannot orphan generated text.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Gets the display name shown in the detail view and settings.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the LLM prompt template; must contain the <c>{transcript}</c> placeholder.</summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets the scope. Derived from the file the category was read from
    /// (global <c>prompts.json</c> on the share versus the local one), never persisted
    /// inside the JSON itself.
    /// </summary>
    public CategoryScope Scope { get; init; } = CategoryScope.Personal;

    /// <summary>Gets the display order within the custom-section list.</summary>
    public int Order { get; init; }

    /// <summary>Gets the per-category token budget passed to the LLM.</summary>
    public int MaxTokens { get; init; } = 20000;
}

/// <summary>Where a category is stored, and therefore who it affects.</summary>
public enum CategoryScope
{
    /// <summary>Shared with the whole team via the global prompts file.</summary>
    Global,

    /// <summary>Private to this user, stored in the local prompts file.</summary>
    Personal,
}

/// <summary>When a section's text is produced.</summary>
public enum GenerationMode
{
    /// <summary>Generated as part of the normal processing run.</summary>
    Auto,

    /// <summary>Generated only when the user presses "Generieren".</summary>
    OnDemand,
}
