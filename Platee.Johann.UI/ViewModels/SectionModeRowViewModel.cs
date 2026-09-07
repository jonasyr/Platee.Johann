namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Application.Settings;

/// <summary>
/// The Auto / „Auf Knopfdruck“ toggle for one built-in section.
/// <para>
/// Custom categories carry their own <see cref="CategoryEditorViewModel.Mode"/>, so this
/// row exists only for the seven built-ins — which have no editable definition of their
/// own but must be toggleable exactly like a custom category.
/// </para>
/// </summary>
public sealed partial class SectionModeRowViewModel : ObservableObject
{
    public SectionModeRowViewModel(string id, string name, GenerationMode mode)
    {
        this.Id = id;
        this.Name = name;
        this.mode = mode;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuto))]
    private GenerationMode mode;

    /// <summary>Gets the stable section id the mode is stored under.</summary>
    public string Id { get; }

    /// <summary>Gets the German display name of the section.</summary>
    public string Name { get; }

    /// <summary>Gets or sets a value indicating whether the section is generated automatically.</summary>
    public bool IsAuto
    {
        get => this.Mode == GenerationMode.Auto;
        set => this.Mode = value ? GenerationMode.Auto : GenerationMode.OnDemand;
    }
}
