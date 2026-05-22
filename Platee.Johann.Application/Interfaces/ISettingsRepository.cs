namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Application.Settings;

public interface ISettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
