namespace Platee.Johann.UiDriver.Sandbox;

public sealed record SandboxLayout(string Root)
{
    public string Home => Path.Combine(this.Root, "home");

    public string Output => Path.Combine(this.Root, "output");

    public string Eingang => Path.Combine(this.Root, "eingang");

    public string Archiv => Path.Combine(this.Eingang, "Archiv");

    public string TeamPrompts => Path.Combine(this.Root, "team", "prompts.json");

    public string SettingsFile => Path.Combine(this.Home, "settings.json");
}
