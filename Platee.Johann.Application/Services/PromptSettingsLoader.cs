using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Application.Services;

public enum PromptSource
{
    Local,
    Global,
    GlobalFallbackToLocal,
}

public sealed record PromptSettingsLoadResult(PromptSettings Settings, PromptSource Source);

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
            return new(localSettings, PromptSource.GlobalFallbackToLocal);
        }

        try
        {
            var globalSettings = await globalRepo.LoadAsync(ct).ConfigureAwait(false);
            return new(globalSettings, PromptSource.Global);
        }
        catch
        {
            return new(localSettings, PromptSource.GlobalFallbackToLocal);
        }
    }
}
