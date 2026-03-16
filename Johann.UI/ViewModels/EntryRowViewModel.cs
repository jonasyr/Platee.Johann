using CommunityToolkit.Mvvm.ComponentModel;
using Johann.Domain.Entities;
using Johann.Domain.Enums;

namespace Johann.UI.ViewModels;

public sealed partial class EntryRowViewModel : ObservableObject
{
    public Entry Entry { get; }

    public string    JobId          => Entry.JobId;
    public int       SequenceNumber => Entry.SequenceNumber;
    public EntryType Type           => Entry.Type;
    public string    ProjectName    => Entry.ProjectName;
    public string    Title          => Entry.Title;
    public string    TypeBadge      => Entry.Type.ToString();

    /// <summary>
    /// Format: NNN_Projektname_ErsteFünfWorteDesTitels
    /// e.g. "001_Johann_wir_müssen_Änderungen_vornehmen"
    /// </summary>
    public string DisplayName
    {
        get
        {
            var words = Entry.Title
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(5);
            return $"{Entry.SequenceNumber:D3}_{Entry.ProjectName}_{string.Join("_", words)}";
        }
    }

    public EntryRowViewModel(Entry entry)
    {
        Entry = entry;
    }
}
