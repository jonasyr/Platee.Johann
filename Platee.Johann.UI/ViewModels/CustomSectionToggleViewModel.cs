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
    private readonly IDictionary<string, bool> target;

    public CustomSectionToggleViewModel(string id, string name, IDictionary<string, bool> target, bool isVisible)
    {
        this.Id = id;
        this.Name = name;
        this.target = target;
        this.isVisible = isVisible;
        target[id] = isVisible;
    }

    public string Id { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool isVisible;

    partial void OnIsVisibleChanged(bool value) => this.target[this.Id] = value;
}
