namespace Platee.Johann.UI.Controls;

using System.Windows.Automation.Peers;
using System.Windows.Controls;

/// <summary>
/// The heading of one detail-view section. It is a plain <see cref="ContentControl"/> in every
/// visual respect; it exists only so the stable <c>Section.&lt;id&gt;</c> AutomationId (#111) is
/// visible to UI Automation. A plain <see cref="ContentControl"/> creates no automation peer,
/// so UIA never exposed that id (found live in Task 12, fixed in its first review round).
/// </summary>
public sealed class SectionHeaderControl : ContentControl
{
    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}
