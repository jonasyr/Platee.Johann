namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.Services;

public sealed partial class EntryRowViewModel : ObservableObject
{
    public Entry Entry { get; private set; }

    public string JobId => this.Entry.JobId;

    public int SequenceNumber => this.Entry.SequenceNumber;

    public EntryType Type => this.Entry.Type;

    public string ProjectName => this.Entry.ProjectName;

    public string Title => this.Entry.Title;

    public string TypeBadge => this.Entry.Type.ToString();

    public string FormattedDuration => DurationFormatter.Format(this.Entry.DurationSeconds);

    public bool IsDone => this.Entry.IsDone;

    public string DisplayName => this.Entry.IsDone
        ? $"✓ {this.Entry.SequenceNumber:D3} {this.Entry.ProjectName} {this.Entry.Title}"
        : $"{this.Entry.SequenceNumber:D3} {this.Entry.ProjectName} {this.Entry.Title}";

    public EntryRowViewModel(Entry entry)
    {
        this.Entry = entry;
    }

    /// <summary>
    /// Swaps in a newer version of the same entry.
    /// <para>
    /// The detail view is re-seeded from this instance on every re-selection, so a row
    /// that keeps a pre-edit <see cref="Entities.Entry"/> makes persisted work look lost.
    /// </para>
    /// </summary>
    public void UpdateEntry(Entry entry)
    {
        this.Entry = entry;
        this.OnPropertyChanged(nameof(this.Entry));
        this.OnPropertyChanged(nameof(this.JobId));
        this.OnPropertyChanged(nameof(this.SequenceNumber));
        this.OnPropertyChanged(nameof(this.Type));
        this.OnPropertyChanged(nameof(this.ProjectName));
        this.OnPropertyChanged(nameof(this.Title));
        this.OnPropertyChanged(nameof(this.TypeBadge));
        this.OnPropertyChanged(nameof(this.FormattedDuration));
        this.OnPropertyChanged(nameof(this.IsDone));
        this.OnPropertyChanged(nameof(this.DisplayName));
    }
}
