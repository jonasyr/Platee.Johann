using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class EntryRowViewModel : ObservableObject
{
    public Entry Entry { get; }

    public string JobId => Entry.JobId;
    public int SequenceNumber => Entry.SequenceNumber;
    public EntryType Type => Entry.Type;
    public string ProjectName => Entry.ProjectName;
    public string Title => Entry.Title;
    public string TypeBadge => Entry.Type.ToString();

    public bool IsDone => Entry.IsDone;

    public string DisplayName => Entry.IsDone
        ? $"✓ {Entry.SequenceNumber:D3}_{Entry.ProjectName}_{Entry.Title}"
        : $"{Entry.SequenceNumber:D3}_{Entry.ProjectName}_{Entry.Title}";

    public EntryRowViewModel(Entry entry)
    {
        Entry = entry;
    }
}
