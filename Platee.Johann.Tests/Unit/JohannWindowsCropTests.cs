namespace Platee.Johann.Tests.Unit;

using System.Drawing;
using FluentAssertions;
using Platee.Johann.UiDriver.Automation;

public sealed class JohannWindowsCropTests
{
    [Fact]
    public void Crop_ReturnsBitmapSizedToElementBounds_WhenFullyInsideWindow()
    {
        using var windowBitmap = new Bitmap(400, 300);
        var windowBounds = new Rectangle(100, 100, 400, 300);
        var elementBounds = new Rectangle(150, 130, 80, 40);

        using var cropped = JohannWindows.Crop(windowBitmap, windowBounds, elementBounds);

        cropped.Width.Should().Be(80);
        cropped.Height.Should().Be(40);
    }

    [Fact]
    public void Crop_IsRelativeToWindowOrigin_NotAbsoluteScreenCoordinates()
    {
        using var windowBitmap = new Bitmap(400, 300);
        var windowBounds = new Rectangle(500, 200, 400, 300);
        var elementBounds = new Rectangle(500, 200, 50, 20);

        // The element sits exactly at the window's top-left corner, so the crop must start at (0,0)
        // of the captured bitmap regardless of the window's absolute screen position.
        using var cropped = JohannWindows.Crop(windowBitmap, windowBounds, elementBounds);

        cropped.Width.Should().Be(50);
        cropped.Height.Should().Be(20);
    }

    [Fact]
    public void Crop_ClampsToBitmapBounds_WhenElementRectExtendsBeyondIt()
    {
        using var windowBitmap = new Bitmap(200, 150);
        var windowBounds = new Rectangle(0, 0, 200, 150);
        var elementBounds = new Rectangle(180, 130, 100, 100);

        using var cropped = JohannWindows.Crop(windowBitmap, windowBounds, elementBounds);

        cropped.Width.Should().Be(20);
        cropped.Height.Should().Be(20);
    }

    [Fact]
    public void Crop_ReturnsFullClone_WhenElementRectIsEntirelyOutsideBitmap()
    {
        using var windowBitmap = new Bitmap(200, 150);
        var windowBounds = new Rectangle(0, 0, 200, 150);
        var elementBounds = new Rectangle(500, 500, 30, 30);

        using var cropped = JohannWindows.Crop(windowBitmap, windowBounds, elementBounds);

        cropped.Width.Should().Be(200);
        cropped.Height.Should().Be(150);
    }
}
