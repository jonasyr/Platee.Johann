using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #116 (Audit F03): the title invented a judgment („Brandschutznachweis unerquicklich“). The
/// prompt now says a title names the subject without judging it, with an example. Measured
/// against the old wording on 52 dictations (docs/prompting/titel-anrede-115-116.md).
/// </summary>
public sealed class SummaryGeneratorTitleTests
{
    [Fact]
    public async Task TitleRequest_ForbidsJudgments_AndEndsWithTheTranscript()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>()).Returns("Titel");
        var sut = new SummaryGenerator(llm, new SettingsHolder(AppSettings.Default));

        await sut.GenerateTitleAsync("Der Brandschutznachweis fehlt.");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(u => u.Contains("er bewertet nicht")
                             && u.StartsWith("Bitte formuliere einen sehr kurzen, prägnanten Titel", StringComparison.Ordinal)
                             && u.EndsWith("\n\nDer Brandschutznachweis fehlt.", StringComparison.Ordinal)),
            Arg.Any<LlmOptions>());
    }

    [Fact]
    public void TitleRequest_KeepsThePrefixTheUiTestStubRecognises() =>
        SummaryGenerator.TitleInstruction.Should().StartWith(
            "Bitte formuliere einen sehr kurzen, prägnanten Titel",
            "SectionPromptMatcher.TitleRequestPrefix in the UI suite routes title requests by it");

    [Fact]
    public void CostEstimate_CountsTheTitlePromptActuallySent()
    {
        // The estimator used to keep its own copy of the title prompt; a longer prompt must show
        // up in the estimate, so it now counts the one SummaryGenerator sends.
        var withTitle = DictationCostEstimator.TokensFor(SummaryGenerator.TitleInstruction);

        withTitle.Should().BeGreaterThan(DictationCostEstimator.TokensFor(
            "Bitte formuliere einen sehr kurzen, prägnanten Titel (maximal 3-7 Worte) für den "
            + "folgenden Text. Antworte NUR mit dem Titel, ohne Anführungszeichen oder Erklärungen:"));
    }
}
