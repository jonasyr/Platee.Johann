namespace Platee.Johann.Application.Settings;

/// <summary>
/// The one-time v1.4.0 transition to per-section generation modes.
/// <para>
/// Dropping from eight generated sections to four is a visible behaviour change nobody
/// asked for, so it is offered through a first-run prompt rather than applied silently.
/// </para>
/// </summary>
public static class SectionModeMigration
{
    /// <summary>Returns true while the user has not yet answered the first-run prompt.</summary>
    public static bool ShouldShow(AppSettings settings) => !settings.SectionModesMigrationDone;

    /// <summary>
    /// Returns a new <see cref="AppSettings"/> with the chosen preset applied and the
    /// migration marked done, so the prompt never reappears.
    /// </summary>
    public static AppSettings Apply(AppSettings settings, bool useRecommended) => settings with
    {
        SectionModes = new Dictionary<string, GenerationMode>(
            useRecommended ? SectionModeDefaults.Recommended : SectionModeDefaults.AllAuto,
            StringComparer.Ordinal),
        SectionModesMigrationDone = true,
    };
}
