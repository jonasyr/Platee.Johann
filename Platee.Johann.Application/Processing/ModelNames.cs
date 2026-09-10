namespace Platee.Johann.Application.Processing;

/// <summary>
/// The OpenAI models Johann runs on, in one place.
/// <para>
/// Which model to use is an application decision; calling the SDK with it is
/// infrastructure. Keeping the names here lets the status bar name the model without the
/// view models reaching into <c>Infrastructure</c>, and stops the string being duplicated
/// — the same duplication that let the prompt constants drift from the team file.
/// </para>
/// <para>
/// #71 turns <see cref="Summaries"/> into a per-user setting. Until then it is the fixed
/// default and this is the only place to change it.
/// </para>
/// </summary>
public static class ModelNames
{
    /// <summary>Speech-to-text. Replaced <c>whisper-1</c> in v1.4.0.</summary>
    public const string Transcription = "gpt-transcribe";

    /// <summary>Summaries and all generated sections. Replaced <c>gpt-5-nano</c> in v1.4.0.</summary>
    public const string Summaries = "gpt-5.6-luna";

    /// <summary>
    /// What the status bar shows, e.g. <c>gpt-transcribe · gpt-5.6-luna</c>. The user asked
    /// to see which models are in use; naming both is more honest than naming one.
    /// </summary>
    public static string StatusBarLabel => $"{Transcription} · {Summaries}";
}
