namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class ToastItem : ObservableObject
{
    public Guid Id { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetailsLink))]
    private ToastTone tone;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string? message;

    [ObservableProperty]
    private bool isRunning;

    public bool IsHovered { get; set; }

    public bool ShowDetailsLink => this.Tone == ToastTone.Error;

    public IRelayCommand DismissCommand { get; }

    public ToastItem(Guid id, ToastTone tone, string title, string? message, bool isRunning, Action dismiss)
    {
        this.Id = id;
        this.Tone = tone;
        this.Title = title;
        this.Message = message;
        this.IsRunning = isRunning;
        this.DismissCommand = new RelayCommand(dismiss);
    }
}
