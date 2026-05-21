namespace Platee.Johann.UI.ViewModels;

using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Platee.Johann.Application.Processing;
using Platee.Johann.Domain.Entities;

public sealed partial class EntryDetailViewModel : ObservableObject
{
    private readonly IEnumerable<IEntryRenderer> renderers;
    private readonly IEntryProcessor? processor;
    private readonly IEntryRepository? repository;
    private readonly string outputRoot;
    private readonly SectionVisibilityViewModel sections;
    private readonly Func<string, bool, ProcessLogItem>? addLog;
    private readonly Action<ProcessLogItem, string>? completeLog;
    private readonly Action<string>? updateStatus;

    [ObservableProperty]
    private Entry? entry;
    [ObservableProperty]
    private bool isTranscriptExpanded;

    // Zoom
    [ObservableProperty]
    private double detailZoom = 1.0;

    public string ZoomText => $"{(int)(this.DetailZoom * 100)} %";

    // Display helpers — show "—" for null fields
    public string DisplayAbstract => this.Entry?.Abstract ?? "—";

    public string DisplayLongSummary => this.Entry?.LongSummary ?? "—";

    public string DisplayProseSummary => this.Entry?.ProseSummary ?? "—";

    public string DisplayTranscript => this.Entry?.Transcript ?? "—";

    public string DisplayConversationNote => this.Entry?.ConversationNote ?? "—";

    public string DisplayTaskList => this.Entry?.TaskList ?? "—";

    public string DisplayEmailText => this.Entry?.EmailText ?? "—";

    public string DisplayStundenzettelText => this.Entry?.StundenzettelText ?? "—";

    public string DisplayAnalogText => this.Entry?.AnalogText ?? "—";

    public string DisplayTypeBadge => this.Entry?.Type.ToString() ?? string.Empty;

    public string DisplayProject => this.Entry?.ProjectName ?? string.Empty;

    public string DisplayDuration => this.Entry is null ? string.Empty : FormatDuration(this.Entry.DurationSeconds);

    public string DisplayDate => this.Entry?.CreatedAt.ToString("dd.MM.yyyy") ?? string.Empty;

    public string DisplayTitle => this.Entry?.Title ?? string.Empty;

    public bool DisplayIsDone => this.Entry?.IsDone ?? false;

    public string IsDoneButtonText => this.Entry?.IsDone == true ? "Erledigt aufheben" : "Als erledigt markieren";

    // Section visibility — true only when content exists AND checkbox is on
    public bool ShowLongSummarySection => !string.IsNullOrWhiteSpace(this.Entry?.LongSummary) && this.sections.ShowLongSummary;

    public bool ShowProseSummarySection => !string.IsNullOrWhiteSpace(this.Entry?.ProseSummary) && this.sections.ShowProseSummary;

    public bool ShowTaskListSection => !string.IsNullOrWhiteSpace(this.Entry?.TaskList) && this.sections.ShowTaskList;

    public bool ShowConversationNoteSection => !string.IsNullOrWhiteSpace(this.Entry?.ConversationNote) && this.sections.ShowConversationNote;

    public bool ShowStundenzettelSection => !string.IsNullOrWhiteSpace(this.Entry?.StundenzettelText) && this.sections.ShowStundenzettelText;

    public bool ShowAnalogSection => !string.IsNullOrWhiteSpace(this.Entry?.AnalogText) && this.sections.ShowAnalogText;

    public bool ShowEmailSection => !string.IsNullOrWhiteSpace(this.Entry?.EmailText) && this.sections.ShowEmailText;

    public bool ShowTranscriptSection => !string.IsNullOrWhiteSpace(this.Entry?.Transcript) && this.sections.ShowTranscript;

    public bool HasEntry => this.Entry is not null;

    public bool HasNoEntry => this.Entry is null;

    public bool IsAudio => this.Entry?.SourceType == "audio";

    public bool CanReprocess => this.Entry is not null && this.processor?.CanProcess == true;

    /// <summary>Raised after an entry's IsDone status is toggled so the list can refresh.</summary>
    public event Action<Entry>? EntryStatusChanged;

    public EntryDetailViewModel(IEnumerable<IEntryRenderer> renderers, string outputRoot,
                                IEntryProcessor? processor = null,
                                IEntryRepository? repository = null,
                                SectionVisibilityViewModel? sections = null,
                                Func<string, bool, ProcessLogItem>? addLog = null,
                                Action<ProcessLogItem, string>? completeLog = null,
                                Action<string>? updateStatus = null)
    {
        this.renderers = renderers;
        this.outputRoot = outputRoot;
        this.processor = processor;
        this.repository = repository;
        this.sections = sections ?? new SectionVisibilityViewModel();
        this.addLog = addLog;
        this.completeLog = completeLog;
        this.updateStatus = updateStatus;
        this.sections.PropertyChanged += (_, _) => this.RefreshSectionVisibility();
    }

    partial void OnEntryChanged(Entry? value)
    {
        IsTranscriptExpanded = false;
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
        OnPropertyChanged(nameof(DisplayEmailText));
        OnPropertyChanged(nameof(DisplayStundenzettelText));
        OnPropertyChanged(nameof(DisplayAnalogText));
        OnPropertyChanged(nameof(DisplayIsDone));
        OnPropertyChanged(nameof(IsDoneButtonText));
        OnPropertyChanged(nameof(HasEntry));
        OnPropertyChanged(nameof(HasNoEntry));
        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(CanReprocess));
        RefreshSectionVisibility();
        GeneratePdfCommand.NotifyCanExecuteChanged();
        GenerateHtmlCommand.NotifyCanExecuteChanged();
        CopyEmailCommand.NotifyCanExecuteChanged();
        OpenInOutlookCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ReprocessCommand.NotifyCanExecuteChanged();
        CopyPdfCommand.NotifyCanExecuteChanged();
        CopyHtmlCommand.NotifyCanExecuteChanged();
        ToggleDoneCommand.NotifyCanExecuteChanged();
        ReprocessSectionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GeneratePdfAsync(CancellationToken ct)
    {
        if (this.Entry is null)
        {
            return;
        }

        var filePath = await this.RenderToFileAsync("PDF", ct);
        if (filePath is null)
        {
            return;
        }

        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GenerateHtmlAsync(CancellationToken ct)
    {
        if (this.Entry is null)
        {
            return;
        }

        var filePath = await this.RenderToFileAsync("HTML", ct);
        if (filePath is null)
        {
            return;
        }

        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task CopyPdfAsync(CancellationToken ct)
    {
        if (this.Entry is null)
        {
            return;
        }

        var filePath = await this.RenderToFileAsync("PDF", ct);
        if (filePath is null)
        {
            return;
        }

        var sc = new System.Collections.Specialized.StringCollection();
        sc.Add(filePath);
        System.Windows.Clipboard.SetFileDropList(sc);
        System.Windows.MessageBox.Show("PDF in Zwischenablage kopiert.", "Platé.Johann",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task CopyHtmlAsync(CancellationToken ct)
    {
        if (this.Entry is null)
        {
            return;
        }

        var filePath = await this.RenderToFileAsync("HTML", ct);
        if (filePath is null)
        {
            return;
        }

        var sc = new System.Collections.Specialized.StringCollection();
        sc.Add(filePath);
        System.Windows.Clipboard.SetFileDropList(sc);
        System.Windows.MessageBox.Show("HTML in Zwischenablage kopiert.", "Platé.Johann",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task ToggleDoneAsync()
    {
        if (this.Entry is null || this.repository is null)
        {
            return;
        }

        var updated = this.Entry with { IsDone = !this.Entry.IsDone };
        await this.repository.SaveAsync(updated);
        this.Entry = updated;
        this.addLog?.Invoke(this.Entry.IsDone ? "✓ Als erledigt markiert." : "Erledigt-Markierung aufgehoben.", false);
        this.EntryStatusChanged?.Invoke(updated);
    }

    /// <summary>
    /// Copies the pre-generated email text directly to the clipboard.
    /// Falls back to a plain-text composition when no EmailText is stored.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void CopyEmail()
    {
        if (this.Entry is null)
        {
            return;
        }

        var text = !string.IsNullOrWhiteSpace(this.Entry.EmailText)
            ? this.Entry.EmailText
            : BuildBasicEmailText(this.Entry);
        System.Windows.Clipboard.SetText(text);
        this.addLog?.Invoke("✓ E-Mail in Zwischenablage kopiert!", false);
    }

    /// <summary>
    /// Opens the default mail client (Outlook) with subject and body pre-filled via mailto:.
    /// Subject is extracted from the "Betreff:" line of the email text when present.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void OpenInOutlook()
    {
        if (this.Entry is null)
        {
            return;
        }

        var emailText = !string.IsNullOrWhiteSpace(this.Entry.EmailText) ? this.Entry.EmailText : BuildBasicEmailText(this.Entry);
        var subject = Uri.EscapeDataString(ExtractBetreff(emailText) ?? $"{this.Entry.ProjectName}: {this.Entry.Title}");
        var body = Uri.EscapeDataString(StripBetreffLine(emailText));
        var mailto = $"mailto:?subject={subject}&body={body}";
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(mailto) { UseShellExecute = true });
            this.addLog?.Invoke("✓ Outlook geöffnet.", false);
        }
        catch (Exception ex)
        {
            this.addLog?.Invoke($"Fehler: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Copies ALL content sections to the clipboard, with section headers.
    /// Transcript is included only when IncludeTranscript is true.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void Copy()
    {
        if (this.Entry is null)
        {
            return;
        }

        var sb = new StringBuilder();

        // Title
        sb.AppendLine(this.DisplayTitle);
        sb.AppendLine(new string('─', 60));
        sb.AppendLine();

        // Abstract
        if (!string.IsNullOrWhiteSpace(this.Entry.Abstract))
        {
            sb.AppendLine("ABSTRACT");
            sb.AppendLine(this.Entry.Abstract);
            sb.AppendLine();
        }

        // TaskList (Aufgabe)
        if (!string.IsNullOrWhiteSpace(this.Entry.TaskList))
        {
            sb.AppendLine("AUFGABEN");
            sb.AppendLine(this.Entry.TaskList);
            sb.AppendLine();
        }

        // ConversationNote (Gesprächsnotiz)
        if (!string.IsNullOrWhiteSpace(this.Entry.ConversationNote))
        {
            sb.AppendLine("GESPRÄCHSNOTIZ");
            sb.AppendLine(this.Entry.ConversationNote);
            sb.AppendLine();
        }

        // Zusammenfassung
        if (!string.IsNullOrWhiteSpace(this.Entry.LongSummary))
        {
            sb.AppendLine("ZUSAMMENFASSUNG");
            sb.AppendLine(this.Entry.LongSummary);
            sb.AppendLine();
        }

        // Ausführliche Zusammenfassung
        if (!string.IsNullOrWhiteSpace(this.Entry.ProseSummary))
        {
            sb.AppendLine("AUSFÜHRLICHE ZUSAMMENFASSUNG");
            sb.AppendLine(this.Entry.ProseSummary);
            sb.AppendLine();
        }

        // Transcript — only when checkbox is checked
        if (this.sections.ShowTranscript && !string.IsNullOrWhiteSpace(this.Entry.Transcript))
        {
            sb.AppendLine("ORIGINALTRANSKRIPT");
            sb.AppendLine(this.Entry.Transcript);
            sb.AppendLine();
        }

        sb.AppendLine(new string('─', 60));
        sb.AppendLine($"[Johann · {this.Entry.CreatedAt:dd.MM.yyyy} · {this.Entry.ProjectName}]");

        System.Windows.Clipboard.SetText(sb.ToString());
        this.addLog?.Invoke("✓ Alles kopiert!", false);
    }

    partial void OnDetailZoomChanged(double value) => OnPropertyChanged(nameof(ZoomText));

    [RelayCommand]
    private void ZoomIn()
    {
        if (this.DetailZoom >= 2.0)
        {
            return;
        }

        this.DetailZoom = Math.Round(this.DetailZoom + 0.1, 1);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (this.DetailZoom <= 0.5)
        {
            return;
        }

        this.DetailZoom = Math.Round(this.DetailZoom - 0.1, 1);
    }

    [RelayCommand(CanExecute = nameof(CanReprocess))]
    private async Task ReprocessSectionAsync(string section, CancellationToken ct)
    {
        if (this.Entry is null || this.processor is null)
        {
            return;
        }

        var logItem = this.addLog?.Invoke($"'{section}' wird neu generiert…", true);
        try
        {
            var progress = new Progress<ProcessingProgress>(p =>
                this.updateStatus?.Invoke(p.Stage));
            var updated = await this.processor.ReprocessSectionAsync(this.Entry, section, progress, ct);
            this.Entry = updated;
            if (logItem is not null)
            {
                this.completeLog?.Invoke(logItem, $"'{section}' aktualisiert");
            }
            else
            {
                this.addLog?.Invoke($"✓ '{section}' aktualisiert", false);
            }
        }
        catch (Exception ex)
        {
            if (logItem is not null)
            {
                this.completeLog?.Invoke(logItem, $"Fehler: {ex.Message}");
            }
            else
            {
                this.addLog?.Invoke($"Fehler: {ex.Message}", false);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanReprocess))]
    private async Task ReprocessAsync(CancellationToken ct)
    {
        if (this.Entry is null || this.processor is null)
        {
            return;
        }

        if (!this.processor.CanProcess)
        {
            this.addLog?.Invoke("Kein API-Schlüssel konfiguriert. .env-Datei in Dokumente\\Johann ablegen.", false);
            return;
        }

        var logItem = this.addLog?.Invoke("Alle Abschnitte werden neu generiert…", true);
        try
        {
            var progress = new Progress<ProcessingProgress>(p =>
                this.updateStatus?.Invoke(p.Stage));

            var updated = await this.processor.ReprocessAsync(this.Entry, progress, ct);
            this.Entry = updated;
            if (logItem is not null)
            {
                this.completeLog?.Invoke(logItem, "Verarbeitung abgeschlossen!");
            }
            else
            {
                this.addLog?.Invoke("Verarbeitung abgeschlossen!", false);
            }
        }
        catch (Exception ex)
        {
            if (logItem is not null)
            {
                this.completeLog?.Invoke(logItem, $"Fehler: {ex.Message}");
            }
            else
            {
                this.addLog?.Invoke($"Fehler: {ex.Message}", false);
            }
        }
    }

    /// <summary>
    /// Renders a PDF for the given entry using current section visibility and returns
    /// the absolute file path. Returns null on failure. Used by drag-and-drop.
    /// </summary>
    public async Task<string?> RenderPdfForDragAsync(Entry entry, CancellationToken ct)
    {
        var renderer = this.renderers.FirstOrDefault(r =>
            r.RendererName.Equals("PDF", StringComparison.OrdinalIgnoreCase));
        if (renderer is null)
        {
            return null;
        }

        try
        {
            var dateDir = Path.Combine(this.outputRoot, entry.CreatedAt.ToString("yyyy-MM-dd"));
            var opts = new RenderOptions(
                OutputDirectory: dateDir,
                OpenAfterRender: false,
                IncludeTranscript: this.sections.ShowTranscript,
                Sections: this.sections.ToSectionVisibility());

            var result = await renderer.RenderAsync(entry, opts, ct);
            return Path.Combine(dateDir, result.SuggestedFilename);
        }
        catch
        {
            return null;
        }
    }

    private void RefreshSectionVisibility()
    {
        this.OnPropertyChanged(nameof(this.ShowLongSummarySection));
        this.OnPropertyChanged(nameof(this.ShowProseSummarySection));
        this.OnPropertyChanged(nameof(this.ShowTaskListSection));
        this.OnPropertyChanged(nameof(this.ShowConversationNoteSection));
        this.OnPropertyChanged(nameof(this.ShowStundenzettelSection));
        this.OnPropertyChanged(nameof(this.ShowAnalogSection));
        this.OnPropertyChanged(nameof(this.ShowEmailSection));
        this.OnPropertyChanged(nameof(this.ShowTranscriptSection));
    }

    private async Task<string?> RenderToFileAsync(string rendererName, CancellationToken ct)
    {
        var renderer = this.renderers.FirstOrDefault(r =>
            r.RendererName.Equals(rendererName, StringComparison.OrdinalIgnoreCase));

        if (renderer is null)
        {
            this.addLog?.Invoke($"{rendererName}-Renderer nicht verfügbar.", false);
            return null;
        }

        var logMessage = rendererName switch
        {
            "PDF" => "PDF-Export läuft…",
            "HTML" => "HTML-Export läuft…",
            _ => $"{rendererName} wird erstellt…",
        };
        var logItem = this.addLog?.Invoke(logMessage, true);
        try
        {
            var dateDir = Path.Combine(this.outputRoot, this.Entry!.CreatedAt.ToString("yyyy-MM-dd"));
            var opts = new RenderOptions(
                OutputDirectory: dateDir,
                OpenAfterRender: false,
                IncludeTranscript: this.sections.ShowTranscript,
                Sections: this.sections.ToSectionVisibility());

            var result = await renderer.RenderAsync(this.Entry!, opts, ct);
            var filePath = Path.Combine(dateDir, result.SuggestedFilename);
            if (logItem is not null)
            {
                this.completeLog?.Invoke(logItem, $"Gespeichert: {result.SuggestedFilename}");
            }
            else
            {
                this.addLog?.Invoke($"Gespeichert: {result.SuggestedFilename}", false);
            }

            return filePath;
        }
        catch (Exception ex)
        {
            if (logItem is not null)
            {
                this.completeLog?.Invoke(logItem, $"Fehler: {ex.Message}");
            }
            else
            {
                this.addLog?.Invoke($"Fehler: {ex.Message}", false);
            }

            return null;
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
        {
            sb.AppendLine(entry.ProseSummary);
        }
        else if (!string.IsNullOrWhiteSpace(entry.Abstract))
        {
            sb.AppendLine(entry.Abstract);
        }

        sb.AppendLine();
        sb.AppendLine($"[{entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
        return sb.ToString();
    }

    /// <summary>Removes the "Betreff: ..." line (and any immediately following blank line) from the body.</summary>
    private static string StripBetreffLine(string emailText)
    {
        var lines = emailText.Split('\n').ToList();
        var idx = lines.FindIndex(l => l.Trim().StartsWith("Betreff:", StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            return emailText;
        }

        lines.RemoveAt(idx);

        // Also remove the blank line that typically follows the Betreff line
        if (idx < lines.Count && string.IsNullOrWhiteSpace(lines[idx]))
        {
            lines.RemoveAt(idx);
        }

        return string.Join('\n', lines).TrimStart();
    }

    /// <summary>Returns the text after "Betreff:" from the first matching line, or null.</summary>
    private static string? ExtractBetreff(string? emailText)
    {
        if (string.IsNullOrWhiteSpace(emailText))
        {
            return null;
        }

        foreach (var line in emailText.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("Betreff:", StringComparison.OrdinalIgnoreCase))
            {
                return t["Betreff:".Length..].Trim();
            }
        }

        return null;
    }

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }
}
