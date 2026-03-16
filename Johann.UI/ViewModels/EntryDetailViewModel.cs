using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Johann.Domain.Entities;

namespace Johann.UI.ViewModels;

public sealed partial class EntryDetailViewModel : ObservableObject
{
    private readonly IEnumerable<IEntryRenderer> _renderers;
    private readonly IEntryProcessor? _processor;
    private readonly string _outputRoot;

    [ObservableProperty] private Entry? _entry;
    [ObservableProperty] private bool   _isTranscriptExpanded;
    [ObservableProperty] private bool   _includeTranscript = true;
    [ObservableProperty] private string _statusMessage     = string.Empty;

    // Display helpers — show "—" for null fields
    public string DisplayAbstract         => Entry?.Abstract         ?? "—";
    public string DisplayLongSummary      => Entry?.LongSummary      ?? "—";
    public string DisplayProseSummary     => Entry?.ProseSummary     ?? "—";
    public string DisplayTranscript       => Entry?.Transcript        ?? "—";
    public string DisplayConversationNote => Entry?.ConversationNote  ?? "—";
    public string DisplayTaskList         => Entry?.TaskList          ?? "—";
    public string DisplayTypeBadge        => Entry?.Type.ToString()   ?? string.Empty;
    public string DisplayProject          => Entry?.ProjectName       ?? string.Empty;
    public string DisplayDuration         => Entry is null ? string.Empty : FormatDuration(Entry.DurationSeconds);
    public string DisplayDate             => Entry?.CreatedAt.ToString("dd.MM.yyyy") ?? string.Empty;

    /// <summary>
    /// Format: Projektname_ErsteFünfWorteDesTitels
    /// e.g. "Johann_wir_müssen_Änderungen_vornehmen"
    /// </summary>
    public string DisplayTitle
    {
        get
        {
            if (Entry is null) return string.Empty;
            var words = Entry.Title
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(5);
            return $"{Entry.ProjectName}_{string.Join("_", words)}";
        }
    }

    public bool HasEntry    => Entry is not null;
    public bool HasNoEntry  => Entry is null;
    public bool IsAudio     => Entry?.SourceType == "audio";
    public bool CanReprocess => Entry is not null && _processor is not null;

    public EntryDetailViewModel(IEnumerable<IEntryRenderer> renderers, string outputRoot,
                                IEntryProcessor? processor = null)
    {
        _renderers  = renderers;
        _outputRoot = outputRoot;
        _processor  = processor;
    }

    partial void OnEntryChanged(Entry? value)
    {
        IsTranscriptExpanded = false;
        StatusMessage        = string.Empty;
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
        OnPropertyChanged(nameof(CanReprocess));
        GeneratePdfCommand.NotifyCanExecuteChanged();
        GenerateHtmlCommand.NotifyCanExecuteChanged();
        GenerateEmailCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ReprocessCommand.NotifyCanExecuteChanged();
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

    /// <summary>
    /// Generates an email via GPT (if available) and copies it to the clipboard.
    /// Falls back to a simple plain-text composition if no processor is configured.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GenerateEmailAsync(CancellationToken ct)
    {
        if (Entry is null) return;

        try
        {
            StatusMessage = "E-Mail wird generiert…";

            string emailText;
            if (_processor is not null)
            {
                emailText = await _processor.GenerateEmailTextAsync(Entry, ct);
            }
            else
            {
                emailText = BuildBasicEmailText(Entry);
            }

            System.Windows.Clipboard.SetText(emailText);
            StatusMessage = "✓ E-Mail in Zwischenablage kopiert!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
    }

    /// <summary>
    /// Copies ALL content sections to the clipboard, with section headers.
    /// Transcript is included only when IncludeTranscript is true.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void Copy()
    {
        if (Entry is null) return;

        var sb = new StringBuilder();

        // Title
        sb.AppendLine(DisplayTitle);
        sb.AppendLine(new string('─', 60));
        sb.AppendLine();

        // Abstract
        if (!string.IsNullOrWhiteSpace(Entry.Abstract))
        {
            sb.AppendLine("ABSTRACT");
            sb.AppendLine(Entry.Abstract);
            sb.AppendLine();
        }

        // TaskList (Aufgabe)
        if (!string.IsNullOrWhiteSpace(Entry.TaskList))
        {
            sb.AppendLine("AUFGABEN");
            sb.AppendLine(Entry.TaskList);
            sb.AppendLine();
        }

        // ConversationNote (Gesprächsnotiz)
        if (!string.IsNullOrWhiteSpace(Entry.ConversationNote))
        {
            sb.AppendLine("GESPRÄCHSNOTIZ");
            sb.AppendLine(Entry.ConversationNote);
            sb.AppendLine();
        }

        // Zusammenfassung
        if (!string.IsNullOrWhiteSpace(Entry.LongSummary))
        {
            sb.AppendLine("ZUSAMMENFASSUNG");
            sb.AppendLine(Entry.LongSummary);
            sb.AppendLine();
        }

        // Ausführliche Zusammenfassung
        if (!string.IsNullOrWhiteSpace(Entry.ProseSummary))
        {
            sb.AppendLine("AUSFÜHRLICHE ZUSAMMENFASSUNG");
            sb.AppendLine(Entry.ProseSummary);
            sb.AppendLine();
        }

        // Transcript — only when checkbox is checked
        if (IncludeTranscript && !string.IsNullOrWhiteSpace(Entry.Transcript))
        {
            sb.AppendLine("ORIGINALTRANSKRIPT");
            sb.AppendLine(Entry.Transcript);
            sb.AppendLine();
        }

        sb.AppendLine(new string('─', 60));
        sb.AppendLine($"[Johann · {Entry.CreatedAt:dd.MM.yyyy} · {Entry.ProjectName}]");

        System.Windows.Clipboard.SetText(sb.ToString());
        StatusMessage = "✓ Alles kopiert!";
    }

    [RelayCommand(CanExecute = nameof(CanReprocess))]
    private async Task ReprocessAsync(CancellationToken ct)
    {
        if (Entry is null || _processor is null) return;

        try
        {
            StatusMessage = "Verarbeite…";

            var progress = new Progress<ProcessingProgress>(p =>
                StatusMessage = $"{p.Stage} ({p.StepIndex}/{p.TotalSteps})");

            var updated = await _processor.ReprocessAsync(Entry, progress, ct);
            Entry         = updated;
            StatusMessage = "Verarbeitung abgeschlossen!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
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

            var dateDir = Path.Combine(_outputRoot, Entry!.CreatedAt.ToString("yyyy-MM-dd"));
            var opts = new RenderOptions(
                OutputDirectory:   dateDir,
                OpenAfterRender:   true,
                IncludeTranscript: IncludeTranscript);

            var result   = await renderer.RenderAsync(Entry!, opts, ct);
            var filePath = Path.Combine(dateDir, result.SuggestedFilename);
            StatusMessage = $"Gespeichert: {result.SuggestedFilename}";

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
    }

    /// <summary>Fallback plain-text email when no GPT processor is available.</summary>
    private static string BuildBasicEmailText(Entry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Betreff: {entry.ProjectName}: {entry.Title}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(entry.ProseSummary))
            sb.AppendLine(entry.ProseSummary);
        else if (!string.IsNullOrWhiteSpace(entry.Abstract))
            sb.AppendLine(entry.Abstract);

        sb.AppendLine();
        sb.AppendLine($"[{entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
        return sb.ToString();
    }

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }
}
