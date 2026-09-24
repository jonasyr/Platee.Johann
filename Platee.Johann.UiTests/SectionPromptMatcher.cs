namespace Platee.Johann.UiTests;

/// <summary>
/// Identifies which section (or the title request) a chat request sent to the stubbed OpenAI
/// endpoint is asking for, purely from the prompt text — the stub has no access to
/// <c>SummaryGenerator</c>'s internals, only the finished request body.
/// <para>
/// Every <c>SummaryPrompts</c> constant is a fixed instruction followed by exactly one
/// placeholder (<c>{transcript}</c> or, for one section, <c>{word_limit}</c> before it); the text
/// up to the first <c>{</c> therefore never changes once the placeholder is substituted, and is
/// unique per section — see <c>docs</c>/the <c>SummaryPrompts</c> constants themselves. Matching
/// on that prefix lets the stub answer correctly without parsing the model's actual output.
/// </para>
/// <para>
/// This file is duplicated into <c>Platee.Johann.Tests</c> via a linked <c>&lt;Compile&gt;</c>
/// item (the project's existing convention for sharing UI-side pure helpers) so a change to the
/// real <c>SummaryPrompts</c> wording that breaks this matcher fails the normal test suite, not
/// just an unrun UI smoke test.
/// </para>
/// </summary>
public static class SectionPromptMatcher
{
    /// <summary>
    /// Prefix of every title-generation request (see <c>SummaryGenerator</c>'s title prompt).
    /// </summary>
    public const string TitleRequestPrefix = "Bitte formuliere einen sehr kurzen, prägnanten Titel";

    /// <summary>
    /// True when <paramref name="userContent"/> is a title request rather than a section request.
    /// </summary>
    public static bool IsTitleRequest(string userContent) =>
        userContent.StartsWith(TitleRequestPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Returns the key of the entry in <paramref name="promptsByKey"/> whose fixed prefix
    /// (<see cref="PrefixOf"/>) starts <paramref name="userContent"/>, or <see langword="null"/>
    /// when none matches (e.g. a title request, or a prompt not covered by the caller's map).
    /// </summary>
    public static string? MatchSection(string userContent, IReadOnlyDictionary<string, string> promptsByKey)
    {
        foreach (var (key, prompt) in promptsByKey)
        {
            var prefix = PrefixOf(prompt);
            if (prefix.Length > 0 && userContent.StartsWith(prefix, StringComparison.Ordinal))
            {
                return key;
            }
        }

        return null;
    }

    /// <summary>
    /// The fixed instruction text of a <c>SummaryPrompts</c> constant, up to (excluding) its
    /// first placeholder. Equals the whole string when it has no placeholder at all.
    /// </summary>
    public static string PrefixOf(string prompt)
    {
        var braceIndex = prompt.IndexOf('{');
        return braceIndex < 0 ? prompt : prompt[..braceIndex];
    }
}
