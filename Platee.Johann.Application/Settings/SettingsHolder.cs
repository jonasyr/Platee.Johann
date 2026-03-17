namespace Platee.Johann.Application.Settings;

/// <summary>
/// Mutable reference wrapper so live settings changes propagate to SummaryGenerator
/// without needing to recreate the entire DI graph.
/// </summary>
public sealed class SettingsHolder
{
    public AppSettings Current { get; set; }

    public SettingsHolder(AppSettings initial) => Current = initial;
}
