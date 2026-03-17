using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class NewEntryViewModel : ObservableObject
{
    // Form fields
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private EntryType _selectedType = EntryType.Projekt;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _titleText = string.Empty;

    [ObservableProperty] private string _content = string.Empty;
    [ObservableProperty] private string _validationMessage = string.Empty;

    public bool DialogResult { get; private set; }
    public Entry? CreatedEntry { get; private set; }

    // Binding-friendly list of available types
    public IReadOnlyList<EntryType> AvailableTypes { get; } =
        Enum.GetValues<EntryType>().ToArray();

    private readonly int _sequenceNumber;
    private readonly DateTimeOffset _createdAt;

    public NewEntryViewModel(int sequenceNumber, DateTimeOffset? createdAt = null)
    {
        _sequenceNumber = sequenceNumber;
        _createdAt = createdAt ?? DateTimeOffset.Now;
    }

    private bool CanSave =>
        !string.IsNullOrWhiteSpace(ProjectName) &&
        !string.IsNullOrWhiteSpace(TitleText);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var title = TitleText.Trim();
        var project = ProjectName.Trim();
        var jobId = BuildJobId(_createdAt, _sequenceNumber, project);

        CreatedEntry = new Entry
        {
            JobId = jobId,
            SequenceNumber = _sequenceNumber,
            Type = SelectedType,
            ProjectName = project,
            Title = title,
            CreatedAt = _createdAt,
            SourceType = "text",
            Status = ProcessingStatus.Empty,
            Transcript = string.IsNullOrWhiteSpace(Content) ? null : Content.Trim(),
        };

        DialogResult = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
    }

    private static string BuildJobId(DateTimeOffset dt, int seq, string project)
    {
        var datePart = dt.ToString("yyMMdd");
        var seqPart = $"{seq:D3}";
        var projectPart = SanitizeForId(project);
        var hash = Math.Abs(Guid.NewGuid().GetHashCode() & 0xFFFFF).ToString("x5");
        return $"{datePart}_{seqPart}_{projectPart}_{hash}";
    }

    private static string SanitizeForId(string s)
        => new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray())
               .TrimStart('_');
}
