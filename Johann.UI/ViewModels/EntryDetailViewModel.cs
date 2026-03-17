using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Johann.Application.Processing;
using Johann.Domain.Entities;

namespace Johann.UI.ViewModels;

public sealed partial class EntryDetailViewModel : ObservableObject
{
    private readonly IEnumerable<IEntryRenderer> _renderers;
    private readonly IEntryProcessor? _processor;
    private readonly IEntryRepository? _repository;
    private readonly string _outputRoot;
    private readonly SectionVisibilityViewModel _sections;

    [ObservableProperty] private Entry? _entry;
    [ObservableProperty] private bool _isTranscriptExpanded;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // Display helpers — show "—" for null fields
    public string DisplayAbstract => Entry?.Abstract ?? "—";
    public string DisplayLongSummary => Entry?.LongSummary ?? "—";
    public string DisplayProseSummary => Entry?.ProseSummary ?? "—";
    public string DisplayTranscript => Entry?.Transcript ?? "—";
    public string DisplayConversationNote => Entry?.ConversationNote ?? "—";
    public string DisplayTaskList => Entry?.TaskList ?? "—";
    public string DisplayEmailText         => Entry?.EmailText         ?? "—";
    public string DisplayStundenzettelText => Entry?.StundenzettelText ?? "—";
    public string DisplayAnalogText        => Entry?.AnalogText        ?? "—";
    public string DisplayTypeBadge => Entry?.Type.ToString() ?? string.Empty;
    public string DisplayProject => Entry?.ProjectName ?? string.Empty;
    public string DisplayDuration => Entry is null ? string.Empty : FormatDuration(Entry.DurationSeconds);
    public string DisplayDate => Entry?.CreatedAt.ToString("dd.MM.yyyy") ?? string.Empty;
    public string DisplayTitle => Entry?.Title ?? string.Empty;
    public bool DisplayIsDone => Entry?.IsDone ?? false;
    public string IsDoneButtonText => Entry?.IsDone == true ? "Erledigt aufheben" : "Als erledigt markieren";

    // Section visibility — true only when content exists AND checkbox is on
    public bool ShowLongSummarySection       => !string.IsNullOrWhiteSpace(Entry?.LongSummary)       && _sections.ShowLongSummary;
    public bool ShowProseSummarySection      => !string.IsNullOrWhiteSpace(Entry?.ProseSummary)      && _sections.ShowProseSummary;
    public bool ShowTaskListSection          => !string.IsNullOrWhiteSpace(Entry?.TaskList)          && _sections.ShowTaskList;
    public bool ShowConversationNoteSection  => !string.IsNullOrWhiteSpace(Entry?.ConversationNote)  && _sections.ShowConversationNote;
    public bool ShowStundenzettelSection     => !string.IsNullOrWhiteSpace(Entry?.StundenzettelText) && _sections.ShowStundenzettelText;
    public bool ShowAnalogSection            => !string.IsNullOrWhiteSpace(Entry?.AnalogText)        && _sections.ShowAnalogText;
    public bool ShowEmailSection             => !string.IsNullOrWhiteSpace(Entry?.EmailText)         && _sections.ShowEmailText;
    public bool ShowTranscriptSection        => !string.IsNullOrWhiteSpace(Entry?.Transcript)        && _sections.ShowTranscript;

    public bool HasEntry => Entry is not null;
    public bool HasNoEntry => Entry is null;
    public bool IsAudio => Entry?.SourceType == "audio";
    public bool CanReprocess => Entry is not null && _processor is not null;

    /// <summary>Raised after an entry's IsDone status is toggled so the list can refresh.</summary>
    public event Action<Entry>? EntryStatusChanged;

    public EntryDetailViewModel(IEnumerable<IEntryRenderer> renderers, string outputRoot,
                                IEntryProcessor? processor = null,
                                IEntryRepository? repository = null,
                                SectionVisibilityViewModel? sections = null)
    {
        _renderers = renderers;
        _outputRoot = outputRoot;
        _processor = processor;
        _repository = repository;
        _sections = sections ?? new SectionVisibilityViewModel();
        _sections.PropertyChanged += (_, _) => RefreshSectionVisibility();
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
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GeneratePdfAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        var filePath = await RenderToFileAsync("PDF", ct);
        if (filePath is null) return;
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task GenerateHtmlAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        var filePath = await RenderToFileAsync("HTML", ct);
        if (filePath is null) return;
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task CopyPdfAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        var filePath = await RenderToFileAsync("PDF", ct);
        if (filePath is null) return;
        var sc = new System.Collections.Specialized.StringCollection();
        sc.Add(filePath);
        System.Windows.Clipboard.SetFileDropList(sc);
        System.Windows.MessageBox.Show("PDF in Zwischenablage kopiert.", "Platé.Johann",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task CopyHtmlAsync(CancellationToken ct)
    {
        if (Entry is null) return;
        var filePath = await RenderToFileAsync("HTML", ct);
        if (filePath is null) return;
        var sc = new System.Collections.Specialized.StringCollection();
        sc.Add(filePath);
        System.Windows.Clipboard.SetFileDropList(sc);
        System.Windows.MessageBox.Show("HTML in Zwischenablage kopiert.", "Platé.Johann",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand(CanExecute = nameof(HasEntry))]
    private async Task ToggleDoneAsync()
    {
        if (Entry is null || _repository is null) return;
        var updated = Entry with { IsDone = !Entry.IsDone };
        await _repository.SaveAsync(updated);
        Entry = updated;
        StatusMessage = Entry.IsDone ? "✓ Als erledigt markiert." : "Erledigt-Markierung aufgehoben.";
        EntryStatusChanged?.Invoke(updated);
    }

    /// <summary>
    /// Copies the pre-generated email text directly to the clipboard.
    /// Falls back to a plain-text composition when no EmailText is stored.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void CopyEmail()
    {
        if (Entry is null) return;
        var text = !string.IsNullOrWhiteSpace(Entry.EmailText)
            ? Entry.EmailText
            : BuildBasicEmailText(Entry);
        System.Windows.Clipboard.SetText(text);
        StatusMessage = "✓ E-Mail in Zwischenablage kopiert!";
    }

    /// <summary>
    /// Opens the default mail client (Outlook) with subject and body pre-filled via mailto:.
    /// Subject is extracted from the "Betreff:" line of the email text when present.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasEntry))]
    private void OpenInOutlook()
    {
        if (Entry is null) return;
        var emailText = !string.IsNullOrWhiteSpace(Entry.EmailText) ? Entry.EmailText : BuildBasicEmailText(Entry);
        var subject   = Uri.EscapeDataString(ExtractBetreff(emailText) ?? $"{Entry.ProjectName}: {Entry.Title}");
        var body      = Uri.EscapeDataString(StripBetreffLine(emailText));
        var mailto    = $"mailto:?subject={subject}&body={body}";
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(mailto) { UseShellExecute = true });
            StatusMessage = "✓ Outlook geöffnet.";
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
        if (_sections.ShowTranscript && !string.IsNullOrWhiteSpace(Entry.Transcript))
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
            Entry = updated;
            StatusMessage = "Verarbeitung abgeschlossen!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
    }

    /// <summary>
    /// Renders a PDF for the given entry using current section visibility and returns
    /// the absolute file path. Returns null on failure. Used by drag-and-drop.
    /// </summary>
    public async Task<string?> RenderPdfForDragAsync(Entry entry, CancellationToken ct)
    {
        var renderer = _renderers.FirstOrDefault(r =>
            r.RendererName.Equals("PDF", StringComparison.OrdinalIgnoreCase));
        if (renderer is null) return null;

        try
        {
            var dateDir = Path.Combine(_outputRoot, entry.CreatedAt.ToString("yyyy-MM-dd"));
            var opts = new RenderOptions(
                OutputDirectory: dateDir,
                OpenAfterRender: false,
                IncludeTranscript: _sections.ShowTranscript,
                Sections: _sections.ToSectionVisibility());

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
        OnPropertyChanged(nameof(ShowLongSummarySection));
        OnPropertyChanged(nameof(ShowProseSummarySection));
        OnPropertyChanged(nameof(ShowTaskListSection));
        OnPropertyChanged(nameof(ShowConversationNoteSection));
        OnPropertyChanged(nameof(ShowStundenzettelSection));
        OnPropertyChanged(nameof(ShowAnalogSection));
        OnPropertyChanged(nameof(ShowEmailSection));
        OnPropertyChanged(nameof(ShowTranscriptSection));
    }

    private async Task<string?> RenderToFileAsync(string rendererName, CancellationToken ct)
    {
        var renderer = _renderers.FirstOrDefault(r =>
            r.RendererName.Equals(rendererName, StringComparison.OrdinalIgnoreCase));

        if (renderer is null)
        {
            StatusMessage = $"{rendererName}-Renderer nicht verfügbar.";
            return null;
        }

        try
        {
            StatusMessage = $"{rendererName} wird erstellt…";

            var dateDir = Path.Combine(_outputRoot, Entry!.CreatedAt.ToString("yyyy-MM-dd"));
            var opts = new RenderOptions(
                OutputDirectory: dateDir,
                OpenAfterRender: false,
                IncludeTranscript: _sections.ShowTranscript,
                Sections: _sections.ToSectionVisibility());

            var result = await renderer.RenderAsync(Entry!, opts, ct);
            var filePath = Path.Combine(dateDir, result.SuggestedFilename);
            StatusMessage = $"Gespeichert: {result.SuggestedFilename}";
            return filePath;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
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
            sb.AppendLine(entry.ProseSummary);
        else if (!string.IsNullOrWhiteSpace(entry.Abstract))
            sb.AppendLine(entry.Abstract);

        sb.AppendLine();
        sb.AppendLine($"[{entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
        return sb.ToString();
    }

    /// <summary>Removes the "Betreff: ..." line (and any immediately following blank line) from the body.</summary>
    private static string StripBetreffLine(string emailText)
    {
        var lines  = emailText.Split('\n').ToList();
        var idx    = lines.FindIndex(l => l.Trim().StartsWith("Betreff:", StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return emailText;
        lines.RemoveAt(idx);
        // Also remove the blank line that typically follows the Betreff line
        if (idx < lines.Count && string.IsNullOrWhiteSpace(lines[idx]))
            lines.RemoveAt(idx);
        return string.Join('\n', lines).TrimStart();
    }

    /// <summary>Returns the text after "Betreff:" from the first matching line, or null.</summary>
    private static string? ExtractBetreff(string? emailText)
    {
        if (string.IsNullOrWhiteSpace(emailText)) return null;
        foreach (var line in emailText.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("Betreff:", StringComparison.OrdinalIgnoreCase))
                return t["Betreff:".Length..].Trim();
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
