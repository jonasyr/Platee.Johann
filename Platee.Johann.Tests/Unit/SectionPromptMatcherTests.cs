namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.UiTests;

/// <summary>
/// Guards the OpenAI-stub request matcher used by the (unrun) UI test suite against the real
/// <see cref="SummaryPrompts"/> wording: if a prompt is edited so its fixed prefix collides with
/// another section's, or drops its placeholder entirely, this fails here — in the normal test
/// suite — instead of silently breaking the stub the next time someone runs the UI tests.
/// </summary>
public sealed class SectionPromptMatcherTests
{
    private static readonly IReadOnlyDictionary<string, string> PromptsByKey = new Dictionary<string, string>
    {
        ["abstract"] = SummaryPrompts.Abstract,
        ["longSummary"] = SummaryPrompts.Structured,
        ["proseSummary"] = SummaryPrompts.Prose,
        ["emailText"] = SummaryPrompts.Email,
        ["taskList"] = SummaryPrompts.Aufgabe,
        ["conversationNote"] = SummaryPrompts.Gespraechsnotiz,
        ["stundenzettelText"] = SummaryPrompts.Stundenzettel,
        ["analogText"] = SummaryPrompts.Analog,
    };

    public static IEnumerable<object[]> AllSectionKeys() => PromptsByKey.Keys.Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(AllSectionKeys))]
    public void MatchSection_finds_the_right_key_once_placeholders_are_filled(string key)
    {
        var filledRequest = FillPlaceholders(PromptsByKey[key]);

        var result = SectionPromptMatcher.MatchSection(filledRequest, PromptsByKey);

        result.Should().Be(key);
    }

    [Fact]
    public void MatchSection_prefixes_are_mutually_unique_and_not_nested()
    {
        var prefixes = PromptsByKey.Values.Select(SectionPromptMatcher.PrefixOf).ToList();

        prefixes.Should().OnlyHaveUniqueItems();
        prefixes.Should().OnlyContain(p => p.Length > 0);

        // A future prompt edit that makes one section's prefix a proper prefix of another's would
        // make MatchSection depend on longest-match tie-breaking between real sections instead of
        // between a real section and unrelated text — still handled, but worth failing loudly here
        // so it is a deliberate choice, not an accident.
        foreach (var a in prefixes)
        {
            foreach (var b in prefixes)
            {
                if (ReferenceEquals(a, b) || a == b)
                {
                    continue;
                }

                a.StartsWith(b, StringComparison.Ordinal).Should().BeFalse(
                    $"'{Shorten(b)}' should not be a proper prefix of '{Shorten(a)}'");
            }
        }
    }

    [Fact]
    public void MatchSection_prefers_the_longest_matching_prefix_when_one_prefix_nests_another()
    {
        var prompts = new Dictionary<string, string>
        {
            ["short"] = "Hallo {x}",
            ["long"] = "Hallo Welt, wie geht es dir {x}",
        };
        var userContent = "Hallo Welt, wie geht es dir heute?";

        // Both prefixes ("Hallo " and "Hallo Welt, wie geht es dir ") are proper prefixes of
        // userContent — the matcher must pick the longer, more specific one regardless of
        // dictionary enumeration order.
        SectionPromptMatcher.MatchSection(userContent, prompts).Should().Be("long");
    }

    [Fact]
    public void MatchSection_returns_null_for_unrelated_text()
    {
        SectionPromptMatcher.MatchSection("Irgendein anderer Text ohne Bezug.", PromptsByKey).Should().BeNull();
    }

    [Fact]
    public void MatchSection_ignores_an_empty_prefix_instead_of_matching_everything()
    {
        // A prompt whose placeholder is its very first character (PrefixOf returns "") must never
        // swallow unrelated requests just because "" is trivially a prefix of anything — this is
        // the N1 regression from fix round 1 (bestLength started at -1 with no length > 0 guard).
        var prompts = new Dictionary<string, string>
        {
            ["empty"] = "{transcript}",
            ["real"] = "Erstelle etwas Bestimmtes.\n{transcript}",
        };

        SectionPromptMatcher.MatchSection("Irgendein anderer Text ohne Bezug.", prompts).Should().BeNull();
        SectionPromptMatcher.MatchSection("Erstelle etwas Bestimmtes.\nDer Rest.", prompts).Should().Be("real");
    }

    [Fact]
    public void IsTitleRequest_recognises_the_real_title_prompt_prefix()
    {
        var titleRequest = "Bitte formuliere einen sehr kurzen, prägnanten Titel für dieses Diktat:\n\nEs geht um...";

        SectionPromptMatcher.IsTitleRequest(titleRequest).Should().BeTrue();
        SectionPromptMatcher.MatchSection(titleRequest, PromptsByKey).Should().BeNull();
    }

    [Fact]
    public void IsTitleRequest_is_false_for_a_section_request()
    {
        SectionPromptMatcher.IsTitleRequest(FillPlaceholders(SummaryPrompts.Abstract)).Should().BeFalse();
    }

    private static string FillPlaceholders(string prompt) => prompt
        .Replace("{word_limit}", "40")
        .Replace("{transcript}", "Ein Beispieltranskript für den Test.")
        .Replace("{prose_summary}", "Eine Beispielzusammenfassung für den Test.");

    private static string Shorten(string s) => s.Length <= 60 ? s : s[..60] + "…";
}
