namespace Platee.Johann.Application.Services;

using System.Security.Cryptography;
using System.Text;
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
    /// <summary>
    /// File name for the local cache belonging to <paramref name="globalPath"/>.
    ///
    /// The name carries a digest of the path, so pointing the app at a different
    /// team share cannot silently serve the previous share's prompts from cache
    /// (PR #46 review).
    /// </summary>
    public static string CacheFileNameFor(string globalPath)
    {
        var normalised = globalPath.Trim().Replace('/', '\\').ToLowerInvariant();
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return $"prompts.cache.{Convert.ToHexString(digest)[..8].ToLowerInvariant()}.json";
    }

    public static async Task<PromptStartupResult> ResolveAsync(
        IPromptSettingsRepository cacheRepo,
        IPromptSettingsRepository? globalRepo,
        string? globalPath,
        Action<Exception>? onCacheWriteError = null,
        CancellationToken ct = default)
    {
        // No team file configured means the user opted out of team prompts. The
        // cache only exists to survive an outage of a configured share, so reading
        // it here would silently resurrect prompts the user just switched off
        // (PR #46 review).
        if (globalRepo is null)
        {
            return new PromptStartupResult(PromptSettings.Default, null);
        }

        var load = await PromptSettingsLoader
            .LoadWithFallbackAsync(cacheRepo, globalRepo, ct)
            .ConfigureAwait(false);

        // Existence is not the same as usable: a corrupt cache still leaves us on
        // built-in defaults, and saying otherwise would hide the very output
        // divergence this warning exists to expose (PR #46 review).
        var cacheUsable = cacheRepo.LastLoadReadFile && cacheRepo.LastLoadFault is null;

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

        var used = cacheUsable
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
