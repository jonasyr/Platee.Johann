namespace Platee.Johann.Application.Settings;

/// <summary>
/// The two generation-mode presets offered by the v1.4.0 first-run prompt.
/// </summary>
public static class SectionModeDefaults
{
    /// <summary>
    /// Gets the recommended preset: four sections generated automatically, the rest on
    /// demand. This is the change that takes a typical entry from eight GPT round-trips
    /// down to four.
    /// <para>
    /// E-Mail is listed as OnDemand to be explicit, matching what the pipeline already did —
    /// it has never been generated automatically.
    /// </para>
    /// </summary>
    public static IReadOnlyDictionary<string, GenerationMode> Recommended { get; } =
        new Dictionary<string, GenerationMode>(StringComparer.Ordinal)
        {
            [BuiltInSections.LongSummary] = GenerationMode.Auto,
            [BuiltInSections.ProseSummary] = GenerationMode.Auto,
            [BuiltInSections.TaskList] = GenerationMode.Auto,
            [BuiltInSections.ConversationNote] = GenerationMode.Auto,
            [BuiltInSections.Stundenzettel] = GenerationMode.OnDemand,
            [BuiltInSections.Analog] = GenerationMode.OnDemand,
            [BuiltInSections.EmailText] = GenerationMode.OnDemand,
        };

    /// <summary>
    /// Gets the "wie bisher" preset for users who would rather keep the previous
    /// always-generate-everything behaviour.
    /// </summary>
    public static IReadOnlyDictionary<string, GenerationMode> AllAuto { get; } =
        BuiltInSections.All.ToDictionary(id => id, _ => GenerationMode.Auto, StringComparer.Ordinal);
}
