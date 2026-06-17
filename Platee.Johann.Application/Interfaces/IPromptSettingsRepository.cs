namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Application.Settings;

public interface IPromptSettingsRepository
{
    bool IsReachable { get; }

    Task<PromptSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(PromptSettings settings, CancellationToken ct = default);
}
