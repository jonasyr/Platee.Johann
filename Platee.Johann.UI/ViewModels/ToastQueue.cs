namespace Platee.Johann.UI.ViewModels;

using System.Collections.ObjectModel;

public sealed class ToastQueue
{
    private static readonly TimeSpan DismissDelay = TimeSpan.FromSeconds(5.2);
    private readonly Action<TimeSpan, Action>? schedule;
    private readonly Dictionary<ToastItem, int> dismissGenerations = [];

    public ObservableCollection<ToastItem> Items { get; } = [];

    public ToastQueue(Action<TimeSpan, Action>? schedule = null)
    {
        this.schedule = schedule;
    }

    public ToastItem ShowRunning(string title)
    {
        ToastItem? item = null;
        item = new ToastItem(Guid.NewGuid(), ToastTone.Ok, title, null, isRunning: true, () => Remove(item!));
        this.Items.Add(item);
        return item;
    }

    public ToastItem Show(string title, ToastTone tone, string? message = null)
    {
        // A repeat of a toast that is still visible restarts its display time instead of
        // stacking a copy — clicking "kopieren" ten times must not fill the screen (#56).
        var visible = this.Items.FirstOrDefault(i =>
            !i.IsRunning && i.Tone == tone && i.Title == title && i.Message == message);
        if (visible is not null)
        {
            this.ScheduleDismiss(visible);
            return visible;
        }

        ToastItem? item = null;
        item = new ToastItem(Guid.NewGuid(), tone, title, message, isRunning: false, () => Remove(item!));
        this.Items.Add(item);
        this.ScheduleDismiss(item);
        return item;
    }

    public void Complete(ToastItem item, string resultTitle, ToastTone tone)
    {
        item.Title = resultTitle;
        item.Tone = tone;
        item.IsRunning = false;
        this.ScheduleDismiss(item);
    }

    public void Remove(ToastItem item)
    {
        this.Items.Remove(item);
        this.dismissGenerations.Remove(item);
    }

    private void ScheduleDismiss(ToastItem item)
    {
        // Scheduled timers cannot be cancelled; only the most recent one per toast may
        // dismiss it, so a restarted display time is not cut short by an older timer.
        var generation = this.dismissGenerations.TryGetValue(item, out var current) ? current + 1 : 1;
        this.dismissGenerations[item] = generation;

        this.schedule?.Invoke(DismissDelay, () =>
        {
            if (this.dismissGenerations.TryGetValue(item, out var latest) && latest != generation)
                return;

            if (item.IsHovered)
                this.ScheduleDismiss(item);
            else
                this.Remove(item);
        });
    }
}
