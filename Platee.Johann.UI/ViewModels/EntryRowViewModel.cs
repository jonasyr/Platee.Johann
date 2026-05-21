namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;

public sealed partial class EntryRowViewModel : ObservableObject
{
    public Entry Entry { get; }

    public string JobId => this.Entry.JobId;

    public int SequenceNumber => this.Entry.SequenceNumber;

    public EntryType Type => this.Entry.Type;

    public string ProjectName => this.Entry.ProjectName;

    public string Title => this.Entry.Title;

    public string TypeBadge => this.Entry.Type.ToString();

    public bool IsDone => this.Entry.IsDone;

    public string DisplayName => this.Entry.IsDone
        ? $"✓ {this.Entry.SequenceNumber:D3} {this.Entry.ProjectName} {this.Entry.Title}"
        : $"{this.Entry.SequenceNumber:D3} {this.Entry.ProjectName} {this.Entry.Title}";

    public EntryRowViewModel(Entry entry)
    {
        this.Entry = entry;
    }
}
