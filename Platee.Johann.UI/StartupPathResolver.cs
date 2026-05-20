using System.IO;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.UI;

public sealed record StartupPathIssue(
    string Label,
    string ConfiguredPath,
    string FallbackPath,
    string Reason);

internal sealed record StartupPathResolution(
    AppSettings PersistedSettings,
    AppSettings EffectiveSettings,
    IReadOnlyList<StartupPathIssue> Issues);

internal readonly record struct DirectoryValidationResult(bool IsUsable, string? ErrorMessage = null);

internal static class StartupPathResolver
{
    public static StartupPathResolution Resolve(
        AppSettings persistedSettings,
        string[] startupArgs,
        Func<string, DirectoryValidationResult> validateDirectory,
        Func<string> resolveDefaultInputRoot,
        Func<string> resolveDefaultOutputRoot)
    {
        var issues = new List<StartupPathIssue>();

        var effectiveInput = ResolveConfiguredPath(
            label: "Quellverzeichnis",
            configuredPath: persistedSettings.Quellverzeichnis,
            fallbackPathFactory: resolveDefaultInputRoot,
            validateDirectory: validateDirectory,
            issues: issues);

        var effectiveOutput = ResolveConfiguredPath(
            label: "Ausgabeverzeichnis",
            configuredPath: persistedSettings.Ausgabeverzeichnis,
            fallbackPathFactory: () => ResolveOutputFallback(startupArgs, validateDirectory, resolveDefaultOutputRoot),
            validateDirectory: validateDirectory,
            issues: issues);

        var effectiveArchive = ResolveConfiguredPath(
            label: "Archivverzeichnis",
            configuredPath: persistedSettings.Archivverzeichnis,
            fallbackPathFactory: () =>
            {
                var path = Path.Combine(effectiveInput, "Archiv");
                EnsureUsableOrThrow(path, validateDirectory);
                return path;
            },
            validateDirectory: validateDirectory,
            issues: issues);

        var effectiveSettings = persistedSettings with
        {
            Quellverzeichnis = effectiveInput,
            Ausgabeverzeichnis = effectiveOutput,
            Archivverzeichnis = effectiveArchive,
        };

        return new StartupPathResolution(persistedSettings, effectiveSettings, issues);
    }

    private static string ResolveConfiguredPath(
        string label,
        string configuredPath,
        Func<string> fallbackPathFactory,
        Func<string, DirectoryValidationResult> validateDirectory,
        List<StartupPathIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var configuredResult = validateDirectory(configuredPath);
            if (configuredResult.IsUsable)
                return configuredPath;
        }

        var fallbackPath = fallbackPathFactory();
        var configuredDisplay = string.IsNullOrWhiteSpace(configuredPath) ? "(leer)" : configuredPath;
        var reason = string.IsNullOrWhiteSpace(configuredPath)
            ? "Pfad ist leer."
            : validateDirectory(configuredPath).ErrorMessage ?? "Pfad konnte nicht erstellt oder verwendet werden.";

        issues.Add(new StartupPathIssue(label, configuredDisplay, fallbackPath, reason));
        return fallbackPath;
    }

    private static string ResolveOutputFallback(
        string[] startupArgs,
        Func<string, DirectoryValidationResult> validateDirectory,
        Func<string> resolveDefaultOutputRoot)
    {
        if (startupArgs.Length > 0 && !string.IsNullOrWhiteSpace(startupArgs[0]))
        {
            var cliPath = startupArgs[0];
            var cliResult = validateDirectory(cliPath);
            if (cliResult.IsUsable)
                return cliPath;
        }

        return resolveDefaultOutputRoot();
    }

    private static void EnsureUsableOrThrow(string path, Func<string, DirectoryValidationResult> validateDirectory)
    {
        var result = validateDirectory(path);
        if (!result.IsUsable)
            throw new IOException(result.ErrorMessage ?? $"Pfad konnte nicht verwendet werden: {path}");
    }
}
