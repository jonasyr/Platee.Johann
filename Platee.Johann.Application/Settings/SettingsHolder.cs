namespace Platee.Johann.Application.Settings;

/// <summary>
/// Mutable reference wrapper so live settings changes propagate to SummaryGenerator
/// without needing to recreate the entire DI graph.
/// Internally stores both values in a single immutable record so that
/// <see cref="Snapshot"/> always reads a consistent pair.
/// </summary>
public sealed class SettingsHolder
{
    private sealed record SettingsState(AppSettings Settings, PromptSettings Prompts);

    private volatile SettingsState _state;

    public AppSettings Current
    {
        get => _state.Settings;
        set => _state = new SettingsState(value, _state.Prompts);
    }

    public PromptSettings Prompts
    {
        get => _state.Prompts;
        set => _state = new SettingsState(_state.Settings, value);
    }

    public SettingsHolder(AppSettings initial, PromptSettings? prompts = null)
    {
        _state = new SettingsState(initial, prompts ?? PromptSettings.Default);
    }

    /// <summary>
    /// Atomically writes both <see cref="Current"/> and <see cref="Prompts"/>
    /// in a single reference swap, preventing torn reads from <see cref="Snapshot"/>.
    /// </summary>
    public void Update(AppSettings settings, PromptSettings prompts)
        => _state = new SettingsState(settings, prompts);

    /// <summary>
    /// Returns a new <see cref="SettingsHolder"/> with the current references captured
    /// in a single atomic read. The returned instance is isolated from later changes.
    /// </summary>
    public SettingsHolder Snapshot()
    {
        var s = _state; // single atomic reference read
        return new SettingsHolder(s.Settings, s.Prompts);
    }
}
