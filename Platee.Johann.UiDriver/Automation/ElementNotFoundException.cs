namespace Platee.Johann.UiDriver.Automation;

/// <summary>
/// Thrown by <see cref="JohannSession.Find"/> when no element matches the requested AutomationId
/// or Name within the timeout. Carries the searched-for key and a dump of the tree that was
/// actually searched, so a failing script can print both without a second round-trip.
/// </summary>
public sealed class ElementNotFoundException : Exception
{
    public ElementNotFoundException(string key, string tree)
        : base($"Element '{key}' nicht gefunden")
    {
        this.Key = key;
        this.Tree = tree;
    }

    public string Key { get; }

    public string Tree { get; }
}
