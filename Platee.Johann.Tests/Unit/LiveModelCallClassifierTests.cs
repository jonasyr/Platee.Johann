namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Xunit;

/// <summary>
/// Die Auswertung der Live-Pruefung ohne Netz: welcher Fehler beweist, dass ein Modell
/// fehlt, welcher, dass es existiert, und welcher gar nichts ueber das Modell sagt (#84).
/// </summary>
public sealed class LiveModelCallClassifierTests
{
    private const string TokenLimitMessage =
        "HTTP 400 (invalid_request_error: )\n"
        + "Could not finish the message because max_tokens or model output limit was reached.";

    [Fact]
    public void Token_limit_400_proves_the_model_answered()
    {
        LiveModelCallClassifier.Classify(400, TokenLimitMessage)
            .Should().Be(LiveModelCallOutcome.Exists);
    }

    [Fact]
    public void Not_found_means_the_model_is_missing()
    {
        LiveModelCallClassifier.Classify(404, "HTTP 404 (invalid_request_error: model_not_found)")
            .Should().Be(LiveModelCallOutcome.Missing);
    }

    [Fact]
    public void Model_not_found_code_counts_as_missing_even_without_404()
    {
        LiveModelCallClassifier.Classify(400, "HTTP 400 (invalid_request_error: model_not_found)")
            .Should().Be(LiveModelCallOutcome.Missing);
    }

    [Fact]
    public void Other_400_says_nothing_about_the_model()
    {
        LiveModelCallClassifier.Classify(400, "HTTP 400 (invalid_request_error: unsupported_parameter)")
            .Should().Be(LiveModelCallOutcome.Unexpected);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(500)]
    public void Key_quota_and_server_errors_are_not_mistaken_for_an_existing_model(int status)
    {
        // Ein falscher Schluessel darf den Test nicht still gruen machen.
        LiveModelCallClassifier.Classify(status, TokenLimitMessage)
            .Should().Be(LiveModelCallOutcome.Unexpected);
    }
}
