namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Application.Settings;

public interface IPromptSettingsRepository
{
    bool IsReachable { get; }

    /// <summary>
    /// Set by the last <see cref="LoadAsync"/> when an existing file could not be
    /// read. <c>null</c> means the load reflected the file's real content.
    /// </summary>
    SettingsFileFault? LastLoadFault { get; }

    Task<PromptSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(PromptSettings settings, CancellationToken ct = default);
}
