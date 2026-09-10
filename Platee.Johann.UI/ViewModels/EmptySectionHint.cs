namespace Platee.Johann.UI.ViewModels;

using Platee.Johann.Domain.Entities;

/// <summary>
/// Decides when to explain that ticking a section in the sidebar shows nothing.
/// <para>
/// The list on the left controls <b>visibility</b>, not <b>generation</b>. Ticking a section
/// the entry never had generated therefore does exactly nothing, which reads as a broken
/// checkbox. Until #65 marks such sections as "nicht umgesetzt" in the entry itself, a
/// one-off hint says so and names the workaround.
/// </para>
/// </summary>
public static class EmptySectionHint
{
    public const string Title = "Vorlage wurde für diesen Eintrag nicht erzeugt";

    public const string Message =
        "Die Liste links blendet Abschnitte nur ein und aus — sie erzeugt keine.\n\n" +
        "Für diesen Eintrag wurde die Vorlage nie erzeugt, deshalb bleibt der Abschnitt " +
        "leer, auch wenn er angehakt ist.\n\n" +
        "So erzeugen Sie ihn jetzt: Rechtsklick auf „Neu generieren\" unten rechts, dann " +
        "die gewünschte Vorlage auswählen.\n\n" +
        "In einer der nächsten Versionen wird direkt im Eintrag stehen, welche Vorlagen " +
        "noch nicht erzeugt wurden, mit einem Knopf zum Nachholen.";

    /// <summary>
    /// Returns <c>true</c> when the hint should be shown for the section that was just
    /// toggled.
    /// </summary>
    /// <param name="content">The entry's text for that section, if any.</param>
    /// <param name="suppressed">Whether the user ticked "nicht mehr anzeigen".</param>
    public static bool ShouldShow(string? content, bool suppressed)
    {
        if (suppressed)
        {
            return false;
        }

        // Only empty sections are confusing. Toggling one that has text does what the user
        // expects, so saying anything would be noise.
        return string.IsNullOrWhiteSpace(content);
    }

    /// <summary>
    /// Returns the entry's text for a built-in section, or <c>null</c> when the entry has
    /// none. Custom sections are looked up by id in <see cref="Entry.CustomSections"/>.
    /// </summary>
    public static string? ContentFor(Entry? entry, string sectionKey) => entry is null
        ? null
        : sectionKey switch
        {
            nameof(Entry.LongSummary) => entry.LongSummary,
            nameof(Entry.ProseSummary) => entry.ProseSummary,
            nameof(Entry.TaskList) => entry.TaskList,
            nameof(Entry.ConversationNote) => entry.ConversationNote,
            nameof(Entry.EmailText) => entry.EmailText,
            nameof(Entry.StundenzettelText) => entry.StundenzettelText,
            nameof(Entry.AnalogText) => entry.AnalogText,
            nameof(Entry.Transcript) => entry.EffectiveTranscript,
            _ => entry.CustomSections.TryGetValue(sectionKey, out var text) ? text : null,
        };
}
