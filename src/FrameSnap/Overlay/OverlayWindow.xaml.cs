using System.Windows.Input;
using System.Windows.Media;
using FrameSnap.Core;
using FrameSnap.Platform;

namespace FrameSnap.Overlay;

public partial class OverlayWindow : Window
{
    private readonly MonitorInfoProvider _monitorInfoProvider = new();
    private readonly CaptureFrameSpec _frameSpec;
    private PixelRect _currentCaptureRect;
    private PixelRect _lastRenderedCaptureRect;
    private MonitorDetails _currentMonitor;
    private bool _hasRenderedCaptureRect;
    private System.Drawing.Point _lastCursorScreenPosition;
    private bool _hasLastCursorScreenPosition;

    public event EventHandler<CaptureRegionEventArgs>? CaptureConfirmed;
    public event EventHandler? CaptureCancelled;

    public OverlayWindow(CaptureFrameSpec frameSpec)
    {
        _frameSpec = frameSpec;
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        SizeChanged += (_, _) => UpdateShadeRegions();
        Loaded += (_, _) =>
        {
            CompositionTarget.Rendering += OnCompositionTargetRendering;
            MoveRectangleToCursor();
        };
        Closed += (_, _) => CompositionTarget.Rendering -= OnCompositionTargetRendering;
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
        if (_hasLastCursorScreenPosition && screenPosition == _lastCursorScreenPosition)
        {
            return;
        }

        _lastCursorScreenPosition = screenPosition;
        _hasLastCursorScreenPosition = true;

        if (!_monitorInfoProvider.TryGetMonitorForPoint(screenPosition.X, screenPosition.Y, out var monitor))
        {
            return;
        }
        _currentMonitor = monitor;

        _currentCaptureRect = CaptureRectangleCalculator.Calculate(_frameSpec, screenPosition.X, screenPosition.Y, monitor.Bounds);
        if (_hasRenderedCaptureRect && _currentCaptureRect == _lastRenderedCaptureRect)
        {
            return;
        }

        var topLeft = PointFromScreen(new Point(_currentCaptureRect.Left, _currentCaptureRect.Top));
        var bottomRight = PointFromScreen(new Point(_currentCaptureRect.Right, _currentCaptureRect.Bottom));
        var width = Math.Max(1, bottomRight.X - topLeft.X);
        var height = Math.Max(1, bottomRight.Y - topLeft.Y);

        CaptureRect.Width = width;
        CaptureRect.Height = height;
        var captureLeft = topLeft.X;
        var captureTop = topLeft.Y;
        System.Windows.Controls.Canvas.SetLeft(CaptureRect, captureLeft);
        System.Windows.Controls.Canvas.SetTop(CaptureRect, captureTop);

        UpdateShadeRegions(captureLeft, captureTop, width, height);
        _lastRenderedCaptureRect = _currentCaptureRect;
        _hasRenderedCaptureRect = true;
    }

    private void RaiseCaptureConfirmed()
    {
        CaptureConfirmed?.Invoke(this, new CaptureRegionEventArgs(_currentCaptureRect, _currentMonitor));
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        MoveRectangleToCursor();
    }

    private static System.Drawing.Point GetCursorScreenPosition()
    {
        GetCursorPos(out var point);
        return new System.Drawing.Point(point.X, point.Y);
    }

    private void UpdateShadeRegions()
    {
        var captureLeft = System.Windows.Controls.Canvas.GetLeft(CaptureRect);
        var captureTop = System.Windows.Controls.Canvas.GetTop(CaptureRect);
        if (double.IsNaN(captureLeft) || double.IsNaN(captureTop))
        {
            return;
        }

        UpdateShadeRegions(captureLeft, captureTop, CaptureRect.Width, CaptureRect.Height);
    }

    private void UpdateShadeRegions(double captureLeft, double captureTop, double captureWidth, double captureHeight)
    {
        var overlayWidth = Math.Max(0, ActualWidth);
        var overlayHeight = Math.Max(0, ActualHeight);
        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            return;
        }

        captureLeft = Math.Clamp(captureLeft, 0, overlayWidth);
        captureTop = Math.Clamp(captureTop, 0, overlayHeight);
        var captureRight = Math.Clamp(captureLeft + captureWidth, 0, overlayWidth);
        var captureBottom = Math.Clamp(captureTop + captureHeight, 0, overlayHeight);

        var geometry = new StreamGeometry
        {
            FillRule = FillRule.EvenOdd
        };

        using (var context = geometry.Open())
        {
            AddRectangle(context, 0, 0, overlayWidth, overlayHeight);
            AddRectangle(context, captureLeft, captureTop, captureRight - captureLeft, captureBottom - captureTop);
        }

        geometry.Freeze();
        ShadeMask.Data = geometry;
    }

    private static void AddRectangle(StreamGeometryContext context, double left, double top, double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var topLeft = new Point(left, top);
        var topRight = new Point(left + width, top);
        var bottomRight = new Point(left + width, top + height);
        var bottomLeft = new Point(left, top + height);

        context.BeginFigure(topLeft, isFilled: true, isClosed: true);
        context.LineTo(topRight, isStroked: false, isSmoothJoin: false);
        context.LineTo(bottomRight, isStroked: false, isSmoothJoin: false);
        context.LineTo(bottomLeft, isStroked: false, isSmoothJoin: false);
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
    public CaptureRegionEventArgs(PixelRect region, MonitorDetails monitor)
    {
        Region = region;
        Monitor = monitor;
    }

    public PixelRect Region { get; }

    public MonitorDetails Monitor { get; }
}
