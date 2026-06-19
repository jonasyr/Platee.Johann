namespace Platee.Johann.Application.Settings;

/// <summary>
/// Mutable reference wrapper so live settings changes propagate to SummaryGenerator
/// without needing to recreate the entire DI graph.
/// </summary>
public sealed class SettingsHolder
{
    public AppSettings Current { get; set; }

    public PromptSettings Prompts { get; set; }

    public SettingsHolder(AppSettings initial, PromptSettings? prompts = null)
    {
        this.Current = initial;
        this.Prompts = prompts ?? PromptSettings.Default;
    }

    /// <summary>
    /// Returns a new SettingsHolder with the current references captured.
    /// The returned instance is isolated from later changes to this holder.
    /// </summary>
    public SettingsHolder Snapshot() => new(this.Current, this.Prompts);
}
