using System.Runtime.InteropServices;

namespace FrameSnap.Platform;

public sealed class MonitorInfoProvider
{
    private const uint MonitorDefaultToNearest = 2;

    public bool TryGetMonitorBoundsForPoint(int x, int y, out Core.PixelRect bounds)
    {
        bounds = default;
        var point = new PointStruct(x, y);
        var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var info = new MonitorInfoEx
        {
            cbSize = Marshal.SizeOf<MonitorInfoEx>()
        };

        if (!GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        bounds = new Core.PixelRect(
            info.rcMonitor.Left,
            info.rcMonitor.Top,
            info.rcMonitor.Right - info.rcMonitor.Left,
            info.rcMonitor.Bottom - info.rcMonitor.Top);

        return true;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(PointStruct pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectStruct
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public RectStruct rcMonitor;
        public RectStruct rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int X;
        public int Y;

        public PointStruct(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
