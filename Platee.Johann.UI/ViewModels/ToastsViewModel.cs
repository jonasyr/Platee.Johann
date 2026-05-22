namespace Platee.Johann.UI.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Threading;

public sealed class ToastsViewModel
{
    private readonly ToastQueue queue;

    public ObservableCollection<ToastItem> Toasts => this.queue.Items;

    public ToastsViewModel()
    {
        this.queue = new ToastQueue(ScheduleOnDispatcher);
    }

    public ToastItem ShowRunning(string title) => this.queue.ShowRunning(title);

    public ToastItem Show(string title, ToastTone tone, string? message = null) =>
        this.queue.Show(title, tone, message);

    public void Complete(ToastItem item, string resultTitle, ToastTone tone) =>
        this.queue.Complete(item, resultTitle, tone);

    public void Dismiss(ToastItem item) => this.queue.Remove(item);

    private static void ScheduleOnDispatcher(TimeSpan delay, Action action)
    {
        var timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher)
        {
            Interval = delay,
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
        };
        timer.Start();
    }
}
