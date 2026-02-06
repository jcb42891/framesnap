namespace FrameSnap.Core;

public static class CaptureRectangleCalculator
{
    private const int MinimumDimensionPx = 20;
    private const double DefaultSizeFactor = 0.4;

    public static PixelRect Calculate(
        AspectRatio ratio,
        int cursorX,
        int cursorY,
        PixelRect monitorBounds)
    {
        var monitorWidth = monitorBounds.Width;
        var monitorHeight = monitorBounds.Height;
        var maxDimension = Math.Max(MinimumDimensionPx, (int)(Math.Min(monitorWidth, monitorHeight) * DefaultSizeFactor));

        var rawWidth = maxDimension;
        var rawHeight = (int)Math.Round(rawWidth * (ratio.Height / (double)ratio.Width));

        if (rawHeight > monitorHeight)
        {
            rawHeight = monitorHeight;
            rawWidth = (int)Math.Round(rawHeight * (ratio.Width / (double)ratio.Height));
        }

        var width = Math.Clamp(rawWidth, MinimumDimensionPx, monitorWidth);
        var height = Math.Clamp(rawHeight, MinimumDimensionPx, monitorHeight);
        var left = cursorX - (width / 2);
        var top = cursorY - (height / 2);
        var clampedLeft = Math.Clamp(left, monitorBounds.Left, monitorBounds.Right - width);
        var clampedTop = Math.Clamp(top, monitorBounds.Top, monitorBounds.Bottom - height);

        return new PixelRect(clampedLeft, clampedTop, width, height);
    }
}
