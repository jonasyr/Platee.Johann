using CommunityToolkit.Mvvm.ComponentModel;

namespace Platee.Johann.UI.ViewModels;

public partial class ProcessLogItem(string message, DateTime timestamp, bool isRunning)
    : ObservableObject
{
    public string Key { get; } = Guid.NewGuid().ToString();
    public string Message { get; } = message;
    public DateTime Timestamp { get; } = timestamp;

    [ObservableProperty] private bool _isRunning = isRunning;
    [ObservableProperty] private string _resultMessage = string.Empty;

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");

    public void Complete(string result)
    {
        IsRunning = false;
        ResultMessage = result;
    }
}
