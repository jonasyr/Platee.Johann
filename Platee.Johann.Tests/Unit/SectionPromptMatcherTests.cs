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
    public void MatchSection_prefixes_are_mutually_unique()
    {
        var prefixes = PromptsByKey.Values.Select(SectionPromptMatcher.PrefixOf).ToList();

        prefixes.Should().OnlyHaveUniqueItems();
        prefixes.Should().OnlyContain(p => p.Length > 0);
    }

    [Fact]
    public void MatchSection_returns_null_for_unrelated_text()
    {
        SectionPromptMatcher.MatchSection("Irgendein anderer Text ohne Bezug.", PromptsByKey).Should().BeNull();
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
}
