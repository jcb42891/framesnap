using System.Runtime.InteropServices;

namespace FrameSnap.Platform;

public sealed class MonitorInfoProvider
{
    private const uint MonitorDefaultToNearest = 2;

    public bool TryGetMonitorForPoint(int x, int y, out MonitorDetails monitorDetails)
    {
        monitorDetails = default;
        var point = new PointStruct(x, y);
        var monitorHandle = MonitorFromPoint(point, MonitorDefaultToNearest);
        if (monitorHandle == IntPtr.Zero)
        {
            return false;
        }

        var info = new MonitorInfoEx
        {
            cbSize = Marshal.SizeOf<MonitorInfoEx>()
        };

        if (!GetMonitorInfo(monitorHandle, ref info))
        {
            return false;
        }

        var bounds = new Core.PixelRect(
            info.rcMonitor.Left,
            info.rcMonitor.Top,
            info.rcMonitor.Right - info.rcMonitor.Left,
            info.rcMonitor.Bottom - info.rcMonitor.Top);
        monitorDetails = new MonitorDetails(monitorHandle, bounds);

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
