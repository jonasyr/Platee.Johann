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

    /// <summary>
    /// True when the last <see cref="LoadAsync"/> actually read a file. False means
    /// it answered with built-in defaults because no file was there.
    ///
    /// <see cref="IsReachable"/> cannot stand in for this: the file can disappear
    /// between the reachability probe and the read — exactly what a dropping network
    /// share does — and the result is then indistinguishable from a successful load
    /// of an empty configuration.
    /// </summary>
    bool LastLoadReadFile { get; }

    Task<PromptSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(PromptSettings settings, CancellationToken ct = default);
}
