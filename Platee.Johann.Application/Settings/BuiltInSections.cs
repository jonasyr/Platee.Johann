namespace Platee.Johann.Application.Settings;

/// <summary>
/// Stable identifiers for the seven displayable built-in sections.
/// <para>
/// Title and Abstract are intrinsics that drive the entry list; they are deliberately
/// absent because they are never toggleable and never on-demand.
/// </para>
/// </summary>
public static class BuiltInSections
{
    /// <summary>Maps to <c>Entry.LongSummary</c>.</summary>
    public const string LongSummary = "builtin.longSummary";

    /// <summary>Maps to <c>Entry.ProseSummary</c>.</summary>
    public const string ProseSummary = "builtin.proseSummary";

    /// <summary>Maps to <c>Entry.TaskList</c>.</summary>
    public const string TaskList = "builtin.taskList";

    /// <summary>Maps to <c>Entry.ConversationNote</c>.</summary>
    public const string ConversationNote = "builtin.conversationNote";

    /// <summary>Maps to <c>Entry.EmailText</c>. Derived from ProseSummary, not the transcript.</summary>
    public const string EmailText = "builtin.emailText";

    /// <summary>Maps to <c>Entry.StundenzettelText</c>.</summary>
    public const string Stundenzettel = "builtin.stundenzettel";

    /// <summary>Maps to <c>Entry.AnalogText</c>.</summary>
    public const string Analog = "builtin.analog";

    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        [LongSummary] = "Zusammenfassung",
        [ProseSummary] = "Ausführliche Zusammenfassung",
        [TaskList] = "Aufgaben",
        [ConversationNote] = "Gesprächsnotiz",
        [EmailText] = "E-Mail",
        [Stundenzettel] = "Stundenzettel",
        [Analog] = "Analog",
    };

    private static readonly Dictionary<string, string> LegacyNames =
        Names.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>Gets every displayable built-in section id, in detail-view order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        LongSummary,
        ProseSummary,
        TaskList,
        ConversationNote,
        EmailText,
        Stundenzettel,
        Analog,
    ];

    /// <summary>
    /// Returns the German display name for a section id, falling back to the id itself
    /// so an unknown (e.g. custom) id never renders as an empty heading.
    /// </summary>
    public static string DisplayNameOf(string id) =>
        Names.TryGetValue(id, out var name) ? name : id;

    /// <summary>
    /// Maps the legacy German <c>CommandParameter</c> strings used by MainWindow.xaml onto
    /// stable ids.
    /// <para>
    /// Note the inversion inherited from the original <c>ReprocessSectionAsync</c> switch:
    /// "Zusammenfassung" is <see cref="LongSummary"/> and "Ausführliche Zusammenfassung" is
    /// <see cref="ProseSummary"/>. The mapping reproduces that exactly rather than silently
    /// correcting it, so behaviour is preserved while the XAML migrates.
    /// </para>
    /// </summary>
    public static string? FromLegacyName(string legacyName) =>
        LegacyNames.TryGetValue(legacyName, out var id) ? id : null;
}
