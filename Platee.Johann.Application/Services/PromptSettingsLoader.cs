using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Application.Services;

public enum PromptSource
{
    Local,
    Global,
    GlobalFallbackToLocal,
}

/// <param name="FallbackReason">
/// Why the global file was not used, or <c>null</c> when no fallback happened.
/// </param>
public sealed record PromptSettingsLoadResult(
    PromptSettings Settings,
    PromptSource Source,
    string? FallbackReason = null);

public static class PromptSettingsLoader
{
    /// <summary>
    /// Combines the global and personal category lists.
    /// <para>
    /// <see cref="CategoryDefinition.Scope"/> is derived from the file a category was read
    /// from and deliberately overwritten here, never trusted from the JSON — otherwise a
    /// hand-edited global file could claim to be personal and escape the "wirkt für alle
    /// Nutzer" warning. A personal category wins over a global one sharing its Id.
    /// </para>
    /// </summary>
    public static IReadOnlyList<CategoryDefinition> MergeCategories(
        PromptSettings global,
        PromptSettings local)
    {
        var merged = new Dictionary<string, CategoryDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in global.CustomCategories)
        {
            merged[category.Id] = category with { Scope = CategoryScope.Global };
        }

        foreach (var category in local.CustomCategories)
        {
            merged[category.Id] = category with { Scope = CategoryScope.Personal };
        }

        return [.. merged.Values.OrderBy(c => c.Order).ThenBy(c => c.Name, StringComparer.Ordinal)];
    }

    public static async Task<PromptSettingsLoadResult> LoadWithFallbackAsync(
        IPromptSettingsRepository localRepo,
        IPromptSettingsRepository? globalRepo,
        CancellationToken ct = default)
    {
        var localSettings = await localRepo.LoadAsync(ct).ConfigureAwait(false);

        if (globalRepo is null)
        {
            return new(localSettings, PromptSource.Local);
        }

        if (!globalRepo.IsReachable)
        {
            return new(localSettings, PromptSource.GlobalFallbackToLocal, "Die Datei ist nicht erreichbar.");
        }

        try
        {
            var globalSettings = await globalRepo.LoadAsync(ct).ConfigureAwait(false);

            // A repository that swallows a parse error and answers with defaults
            // looks identical to a successful load. Only LastLoadFault tells them
            // apart — without this check the caller would cache built-in defaults
            // over the last known good prompts (#45 H1/H3).
            var fault = globalRepo.LastLoadFault;
            if (fault is not null)
            {
                return new(localSettings, PromptSource.GlobalFallbackToLocal, fault.Reason);
            }

            // IsReachable said the file was there, but the read found nothing. The
            // share dropped in between, so these are built-in defaults wearing a
            // successful load's clothes — caching them would destroy the last known
            // good prompts (PR #46 review).
            if (!globalRepo.LastLoadReadFile)
            {
                return new(
                    localSettings,
                    PromptSource.GlobalFallbackToLocal,
                    "Die Datei war beim Lesen nicht mehr vorhanden.");
            }

            return new(globalSettings, PromptSource.Global);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new(localSettings, PromptSource.GlobalFallbackToLocal, ex.Message);
        }
    }
}
