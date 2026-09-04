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
