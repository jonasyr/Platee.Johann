namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// One checkbox in the „Im Eintrag anzeigen" panel for a user-defined category.
/// <para>
/// The built-in sections have a fixed property each; custom categories cannot, because
/// they are created at runtime. Each toggle writes straight through to the shared
/// visibility map the renderers and the clipboard read.
/// </para>
/// </summary>
public sealed partial class CustomSectionToggleViewModel : ObservableObject
{
    private readonly Action<string, bool> onChanged;

    public CustomSectionToggleViewModel(
        string id, string name, string group, bool isVisible, Action<string, bool> onChanged)
    {
        this.Id = id;
        this.Name = name;
        this.Group = group;
        this.isVisible = isVisible;
        this.onChanged = onChanged;
        onChanged(id, isVisible);
    }

    public string Id { get; }

    public string Name { get; }

    /// <summary>Gets the heading this toggle is listed under: personal, team, or orphaned.</summary>
    public string Group { get; }

    [ObservableProperty]
    private bool isVisible;

    partial void OnIsVisibleChanged(bool value) => this.onChanged(this.Id, value);
}
