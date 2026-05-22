namespace Platee.Johann.UI.ViewModels;

using System.Collections.ObjectModel;

public sealed class ToastQueue
{
    private static readonly TimeSpan DismissDelay = TimeSpan.FromSeconds(5.2);
    private readonly Action<TimeSpan, Action>? schedule;

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

    public void Remove(ToastItem item) => this.Items.Remove(item);

    private void ScheduleDismiss(ToastItem item)
    {
        this.schedule?.Invoke(DismissDelay, () =>
        {
            if (item.IsHovered)
                this.ScheduleDismiss(item);
            else
                this.Remove(item);
        });
    }
}
