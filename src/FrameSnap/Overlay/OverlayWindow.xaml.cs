using System.Windows.Input;
using FrameSnap.Core;
using FrameSnap.Platform;

namespace FrameSnap.Overlay;

public partial class OverlayWindow : Window
{
    private readonly MonitorInfoProvider _monitorInfoProvider = new();
    private readonly AspectRatio _ratio;
    private PixelRect _currentCaptureRect;

    public event EventHandler<CaptureRegionEventArgs>? CaptureConfirmed;
    public event EventHandler? CaptureCancelled;

    public OverlayWindow(AspectRatio ratio)
    {
        _ratio = ratio;
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Loaded += (_, _) => MoveRectangleToCursor();
    }

    private void OnOverlayMouseMove(object sender, MouseEventArgs e)
    {
        MoveRectangleToCursor();
    }

    private void OnOverlayMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RaiseCaptureConfirmed();
    }

    private void OnOverlayKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CaptureCancelled?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (e.Key == Key.Enter)
        {
            RaiseCaptureConfirmed();
        }
    }

    private void MoveRectangleToCursor()
    {
        var screenPosition = GetCursorScreenPosition();
        if (!_monitorInfoProvider.TryGetMonitorBoundsForPoint(screenPosition.X, screenPosition.Y, out var monitorBounds))
        {
            return;
        }

        _currentCaptureRect = CaptureRectangleCalculator.Calculate(_ratio, screenPosition.X, screenPosition.Y, monitorBounds);

        var topLeft = PointFromScreen(new Point(_currentCaptureRect.Left, _currentCaptureRect.Top));
        var bottomRight = PointFromScreen(new Point(_currentCaptureRect.Right, _currentCaptureRect.Bottom));
        var width = Math.Max(1, bottomRight.X - topLeft.X);
        var height = Math.Max(1, bottomRight.Y - topLeft.Y);

        CaptureRect.Width = width;
        CaptureRect.Height = height;
        System.Windows.Controls.Canvas.SetLeft(CaptureRect, topLeft.X);
        System.Windows.Controls.Canvas.SetTop(CaptureRect, topLeft.Y);
    }

    private void RaiseCaptureConfirmed()
    {
        CaptureConfirmed?.Invoke(this, new CaptureRegionEventArgs(_currentCaptureRect));
    }

    private static System.Drawing.Point GetCursorScreenPosition()
    {
        GetCursorPos(out var point);
        return new System.Drawing.Point(point.X, point.Y);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint lpPoint);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}

public sealed class CaptureRegionEventArgs : EventArgs
{
    public CaptureRegionEventArgs(PixelRect region)
    {
        Region = region;
    }

    public PixelRect Region { get; }
}
