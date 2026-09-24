namespace Platee.Johann.UiDriver.Automation;

/// <summary>
/// One node of a captured UI Automation tree, shaped for JSON output. Optional fields are
/// <c>null</c> when the underlying element carries no value, and <see cref="Children"/> is
/// empty for leaves.
/// </summary>
public sealed record UiNode(
    string ControlType,
    string? AutomationId,
    string? Name,
    bool IsEnabled,
    bool IsOffscreen,
    string Bounds,
    IReadOnlyList<UiNode> Children);
