namespace Platee.Johann.Application.Processing;

using Platee.Johann.Application.Settings;

/// <summary>
/// One entry in the unified section list: either a built-in section or a user-defined
/// category. <see cref="Category"/> is null for built-ins.
/// </summary>
public sealed record SectionDescriptor(
    string Id,
    string Name,
    GenerationMode Mode,
    bool IsBuiltIn,
    CategoryDefinition? Category);

/// <summary>
/// Projects the eight fixed built-in sections and the user's custom categories into one
/// ordered list.
/// <para>
/// This is the single place anything asks "what sections exist?", so the split storage
/// (built-ins as fixed properties, custom ones in a list) never leaks into the pipeline or
/// the UI.
/// </para>
/// </summary>
public static class SectionCatalog
{
    /// <summary>
    /// Builds the catalog.
    /// <para>
    /// Built-ins default to <see cref="GenerationMode.Auto"/>, preserving today's behaviour
    /// for anyone who has not been through the first-run mode prompt. New custom categories
    /// default to <see cref="GenerationMode.OnDemand"/> so adding one never silently slows
    /// processing down.
    /// </para>
    /// </summary>
    public static IReadOnlyList<SectionDescriptor> Build(
        PromptSettings prompts,
        IReadOnlyDictionary<string, GenerationMode> modes)
    {
        var result = new List<SectionDescriptor>(BuiltInSections.All.Count + prompts.CustomCategories.Count);

        foreach (var id in BuiltInSections.All)
        {
            result.Add(new SectionDescriptor(
                id,
                BuiltInSections.DisplayNameOf(id),
                modes.TryGetValue(id, out var builtInMode) ? builtInMode : GenerationMode.Auto,
                IsBuiltIn: true,
                Category: null));
        }

        foreach (var category in prompts.CustomCategories.OrderBy(c => c.Order))
        {
            result.Add(new SectionDescriptor(
                category.Id,
                category.Name,
                modes.TryGetValue(category.Id, out var customMode) ? customMode : GenerationMode.OnDemand,
                IsBuiltIn: false,
                Category: category));
        }

        return result;
    }
}
