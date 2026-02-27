using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Johann.Domain.Entities;

namespace Johann.UI.ViewModels;

public sealed partial class EntryDetailViewModel : ObservableObject
{
    private readonly IEnumerable<IEntryRenderer> _renderers;
    private readonly string _outputRoot;

    [ObservableProperty] private Entry? _entry;
    [ObservableProperty] private bool _isTranscriptExpanded;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // Display helpers — show "—" for null fields
    public string DisplayAbstract       => Entry?.Abstract       ?? "—";
    public string DisplayLongSummary    => Entry?.LongSummary    ?? "—";
    public string DisplayProseSummary   => Entry?.ProseSummary   ?? "—";
    public string DisplayTranscript     => Entry?.Transcript      ?? "—";
    public string DisplayConversationNote => Entry?.ConversationNote ?? "—";
    public string DisplayTaskList       => Entry?.TaskList        ?? "—";
    public string DisplayTypeBadge      => Entry?.Type.ToString() ?? string.Empty;
    public string DisplayProject        => Entry?.ProjectName     ?? string.Empty;
    public string DisplayTitle          => Entry?.Title           ?? string.Empty;
    public string DisplayDuration       => Entry is null ? string.Empty : FormatDuration(Entry.DurationSeconds);
    public string DisplayDate           => Entry?.CreatedAt.ToString("dd.MM.yyyy") ?? string.Empty;

    public bool HasEntry   => Entry is not null;
    public bool HasNoEntry => Entry is null;
    public bool IsAudio    => Entry?.SourceType == "audio";

    public EntryDetailViewModel(IEnumerable<IEntryRenderer> renderers, string outputRoot)
    {
        _renderers  = renderers;
        _outputRoot = outputRoot;
    }

    partial void OnEntryChanged(Entry? value)
    {
        IsTranscriptExpanded = false;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(DisplayAbstract));
        OnPropertyChanged(nameof(DisplayLongSummary));
        OnPropertyChanged(nameof(DisplayProseSummary));
        OnPropertyChanged(nameof(DisplayTranscript));
        OnPropertyChanged(nameof(DisplayConversationNote));
        OnPropertyChanged(nameof(DisplayTaskList));
        OnPropertyChanged(nameof(DisplayTypeBadge));
        OnPropertyChanged(nameof(DisplayProject));
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(DisplayDuration));
        OnPropertyChanged(nameof(DisplayDate));
        OnPropertyChanged(nameof(HasEntry));
        OnPropertyChanged(nameof(HasNoEntry));
        OnPropertyChanged(nameof(IsAudio));
        GeneratePdfCommand.NotifyCanExecuteChanged();
        GenerateHtmlCommand.NotifyCanExecuteChanged();
        GenerateEmailCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GeneratePdfAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        await RenderAsync("PDF", ct);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GenerateHtmlAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        await RenderAsync("HTML", ct);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GenerateEmailAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        await RenderAsync("Email", ct);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void Copy()
    {
        if (Entry is null) return;
        var text = $"{Entry.Title}\n\n{Entry.Abstract}\n\n{Entry.LongSummary}";
        System.Windows.Clipboard.SetText(text);
        StatusMessage = "Kopiert!";
    }

    private async Task RenderAsync(string rendererName, CancellationToken ct)
    {
        var renderer = _renderers.FirstOrDefault(r =>
            r.RendererName.Equals(rendererName, StringComparison.OrdinalIgnoreCase));

        if (renderer is null)
        {
            StatusMessage = $"{rendererName}-Renderer nicht verfügbar.";
            return;
        }

        try
        {
            StatusMessage = $"{rendererName} wird erstellt…";

            // Save into the entry's own date directory inside outputRoot
            var dateDir = Path.Combine(_outputRoot,
                Entry!.CreatedAt.ToString("yyyy-MM-dd"));
            var opts = new RenderOptions(OutputDirectory: dateDir, OpenAfterRender: true);

            var result = await renderer.RenderAsync(Entry!, opts, ct);

            var filePath = Path.Combine(dateDir, result.SuggestedFilename);
            StatusMessage = $"Gespeichert: {result.SuggestedFilename}";

            // Open the file with the default application
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(filePath)
                { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
    }

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }
}
