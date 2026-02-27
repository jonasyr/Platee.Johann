using Johann.Application.Settings;

namespace Johann.Application.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
