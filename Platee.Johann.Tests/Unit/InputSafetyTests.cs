namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Automation;

public sealed class InputSafetyTests
{
    [Fact]
    public void IsForegroundSafe_SameProcess_IsSafe() =>
        InputSafety.IsForegroundSafe(foregroundProcessId: 4242, targetProcessId: 4242).Should().BeTrue();

    [Fact]
    public void IsForegroundSafe_DifferentProcess_IsUnsafe() =>
        InputSafety.IsForegroundSafe(foregroundProcessId: 1111, targetProcessId: 4242).Should().BeFalse();

    [Fact]
    public void IsForegroundSafe_NoForegroundWindow_IsUnsafe() =>

        // GetForegroundWindow can legitimately return no window (id 0, e.g. desktop has focus);
        // that must never be mistaken for "the target process owns the foreground".
        InputSafety.IsForegroundSafe(foregroundProcessId: 0, targetProcessId: 0).Should().BeFalse();
}
