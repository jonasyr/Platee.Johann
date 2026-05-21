namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

public sealed partial class NewEntryViewModel : ObservableObject
{
    // Form fields
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private EntryType selectedType = EntryType.Projekt;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string projectName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;
    [ObservableProperty]
    private string validationMessage = string.Empty;

    public bool DialogResult { get; private set; }

    public Entry? CreatedEntry { get; private set; }

    // Binding-friendly list of available types
    public IReadOnlyList<EntryType> AvailableTypes { get; } =
        Enum.GetValues<EntryType>().ToArray();

    private readonly int sequenceNumber;
    private readonly DateTimeOffset createdAt;

    public NewEntryViewModel(int sequenceNumber, DateTimeOffset? createdAt = null)
    {
        this.sequenceNumber = sequenceNumber;
        this.createdAt = createdAt ?? DateTimeOffset.Now;
    }

    private bool CanSave =>
        !string.IsNullOrWhiteSpace(this.ProjectName) &&
        !string.IsNullOrWhiteSpace(this.TitleText);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var title = this.TitleText.Trim();
        var project = this.ProjectName.Trim();
        var jobId = BuildJobId(this.createdAt, this.sequenceNumber, project);

        this.CreatedEntry = new Entry
        {
            JobId = jobId,
            SequenceNumber = this.sequenceNumber,
            Type = this.SelectedType,
            ProjectName = project,
            Title = title,
            CreatedAt = this.createdAt,
            SourceType = "text",
            Status = ProcessingStatus.Empty,
            Transcript = string.IsNullOrWhiteSpace(this.Content) ? null : this.Content.Trim(),
        };

        this.DialogResult = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        this.DialogResult = false;
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
