namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class ProcessLogItem(string message, DateTime timestamp, bool isRunning)
    : ObservableObject
{
    public string Key { get; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string message = message;

    public DateTime Timestamp { get; } = timestamp;

    [ObservableProperty]
    private bool isRunning = isRunning;
    [ObservableProperty]
    private string resultMessage = string.Empty;

    public string DisplayTime => this.Timestamp.ToString("HH:mm:ss");

    public void Complete(string result)
    {
        this.IsRunning = false;
        this.ResultMessage = result;
    }
}
