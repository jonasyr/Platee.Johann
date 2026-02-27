using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Johann.Application.Processing;
using Johann.Application.Settings;

namespace Johann.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _repository;
    private readonly SettingsHolder _holder;

    [ObservableProperty] private string _systemMessage    = string.Empty;
    [ObservableProperty] private string _abstractPrompt   = string.Empty;
    [ObservableProperty] private string _structuredPrompt = string.Empty;
    [ObservableProperty] private string _prosePrompt      = string.Empty;
    [ObservableProperty] private string _emailPrompt      = string.Empty;
    [ObservableProperty] private string _statusMessage    = string.Empty;

    public SettingsViewModel(ISettingsRepository repository, SettingsHolder holder)
    {
        _repository = repository;
        _holder     = holder;
        LoadFromHolder();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var updated = new AppSettings
        {
            SystemMessage    = SystemMessage.Trim(),
            AbstractPrompt   = AbstractPrompt.Trim(),
            StructuredPrompt = StructuredPrompt.Trim(),
            ProsePrompt      = ProsePrompt.Trim(),
            EmailPrompt      = EmailPrompt.Trim(),
        };

        await _repository.SaveAsync(updated);
        _holder.Current = updated;           // live propagation to SummaryGenerator
        StatusMessage   = "✓ Einstellungen gespeichert.";
    }

    [RelayCommand]
    private void Reset()
    {
        SystemMessage    = SummaryPrompts.SystemMessage;
        AbstractPrompt   = SummaryPrompts.Abstract;
        StructuredPrompt = SummaryPrompts.Structured;
        ProsePrompt      = SummaryPrompts.Prose;
        EmailPrompt      = SummaryPrompts.Email;
        StatusMessage    = "Standard-Prompts wiederhergestellt – noch nicht gespeichert.";
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void LoadFromHolder()
    {
        var s            = _holder.Current;
        SystemMessage    = s.SystemMessage;
        AbstractPrompt   = s.AbstractPrompt;
        StructuredPrompt = s.StructuredPrompt;
        ProsePrompt      = s.ProsePrompt;
        EmailPrompt      = s.EmailPrompt;
    }
}
