namespace Platee.Johann.Application.Services;

using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

/// <param name="Warning">
/// A ready-to-display explanation of why the team's prompts are not in use, or
/// <c>null</c> when they are.
/// </param>
public sealed record PromptStartupResult(PromptSettings Prompts, string? Warning);

/// <summary>
/// Decides which prompts a session runs on.
///
/// The team's global prompt file is the source of truth. Every successful load is
/// mirrored into a local cache, so an unreachable or corrupt share degrades to the
/// last known team prompts instead of silently reverting to the built-in defaults —
/// which made one colleague produce different GPT output than everyone else with
/// nothing on screen to say so (#45 H1).
/// </summary>
public static class PromptStartupResolver
{
    public static async Task<PromptStartupResult> ResolveAsync(
        IPromptSettingsRepository cacheRepo,
        IPromptSettingsRepository? globalRepo,
        string? globalPath,
        Action<Exception>? onCacheWriteError = null,
        CancellationToken ct = default)
    {
        var hadCache = cacheRepo.IsReachable;

        var load = await PromptSettingsLoader
            .LoadWithFallbackAsync(cacheRepo, globalRepo, ct)
            .ConfigureAwait(false);

        if (load.Source == PromptSource.Global)
        {
            try
            {
                await cacheRepo.SaveAsync(load.Settings, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A cache that cannot be written is not worth failing startup over,
                // but it must not disappear either — the next outage depends on it.
                onCacheWriteError?.Invoke(ex);
            }
        }

        if (load.Source != PromptSource.GlobalFallbackToLocal)
        {
            return new PromptStartupResult(load.Settings, null);
        }

        var used = hadCache
            ? "zuletzt geladene Team-Prompts (lokaler Zwischenspeicher)"
            : "eingebaute Standard-Prompts — die Ergebnisse weichen von denen der Kollegen ab";

        var warning = string.Join(
            Environment.NewLine,
            $"Zentrale Prompts ({globalPath}):",
            $"Grund: {load.FallbackReason}",
            $"Verwendet: {used}");

        return new PromptStartupResult(load.Settings, warning);
    }
}
