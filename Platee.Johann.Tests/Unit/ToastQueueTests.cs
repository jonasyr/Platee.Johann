namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.ViewModels;

public sealed class ToastQueueTests
{
    [Fact]
    public void ShowRunning_AddsRunningToast_NoTimerScheduled()
    {
        int scheduleCalls = 0;
        var queue = new ToastQueue((_, _) => scheduleCalls++);

        var item = queue.ShowRunning("Verarbeite…");

        queue.Items.Should().ContainSingle();
        item.IsRunning.Should().BeTrue();
        item.Tone.Should().Be(ToastTone.Ok);
        scheduleCalls.Should().Be(0, "running toasts are not auto-dismissed");
    }

    [Fact]
    public void Show_AddsToast_SchedulesDismiss()
    {
        int scheduleCalls = 0;
        var queue = new ToastQueue((_, _) => scheduleCalls++);

        queue.Show("Fertig", ToastTone.Ok);

        queue.Items.Should().ContainSingle();
        scheduleCalls.Should().Be(1);
    }

    [Fact]
    public void Complete_UpdatesRunningToast_SchedulesDismiss()
    {
        int scheduleCalls = 0;
        var queue = new ToastQueue((_, _) => scheduleCalls++);
        var item = queue.ShowRunning("Läuft…");

        queue.Complete(item, "Fertig!", ToastTone.Ok);

        item.Title.Should().Be("Fertig!");
        item.Tone.Should().Be(ToastTone.Ok);
        item.IsRunning.Should().BeFalse();
        scheduleCalls.Should().Be(1);
    }

    [Fact]
    public void DismissCommand_RemovesOnlyTargetedToast()
    {
        var queue = new ToastQueue(schedule: null);
        var a = queue.ShowRunning("A");
        var b = queue.ShowRunning("B");

        a.DismissCommand.Execute(null);

        queue.Items.Should().ContainSingle()
            .Which.Title.Should().Be("B");
    }

    [Fact]
    public void AutoDismiss_WhenNotHovered_RemovesItem()
    {
        Action? scheduledAction = null;
        var queue = new ToastQueue((_, action) => scheduledAction = action);
        queue.Show("Test", ToastTone.Ok);

        scheduledAction.Should().NotBeNull();
        scheduledAction!.Invoke();

        queue.Items.Should().BeEmpty();
    }

    [Fact]
    public void AutoDismiss_WhenHovered_Reschedules_AndDoesNotRemove()
    {
        var scheduledActions = new List<Action>();
        var queue = new ToastQueue((_, action) => scheduledActions.Add(action));
        var item = queue.Show("Test", ToastTone.Ok);
        item.IsHovered = true;

        scheduledActions[0].Invoke(); // fires while hovered

        queue.Items.Should().ContainSingle("item stays while hovered");
        scheduledActions.Should().HaveCount(2, "a new dismiss was scheduled");

        item.IsHovered = false;
        scheduledActions[1].Invoke(); // fires again, not hovered

        queue.Items.Should().BeEmpty();
    }

    [Fact]
    public void MultipleToasts_EachDismissedIndependently()
    {
        var queue = new ToastQueue(schedule: null);
        var a = queue.ShowRunning("A");
        var b = queue.ShowRunning("B");
        var c = queue.ShowRunning("C");

        b.DismissCommand.Execute(null);

        queue.Items.Should().HaveCount(2);
        queue.Items.Select(x => x.Title).Should().Equal("A", "C");
    }
}
