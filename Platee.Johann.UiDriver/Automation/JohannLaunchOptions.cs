namespace Platee.Johann.UiDriver.Automation;

using Platee.Johann.UiDriver.Sandbox;

/// <summary>
/// Everything <see cref="JohannSession.Launch"/> needs to start Johann against an isolated
/// sandbox: which exe to run, which sandbox layout to point its env vars at, and the optional
/// stub OpenAI endpoint / API key overrides.
/// </summary>
public sealed record JohannLaunchOptions(
    string ExePath,
    SandboxLayout Sandbox,
    Uri? OpenAiRoot,
    string? ApiKey,
    bool SkipUpdateCheck = true);
