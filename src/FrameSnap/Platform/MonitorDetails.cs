using FrameSnap.Core;

namespace FrameSnap.Platform;

public readonly record struct MonitorDetails(IntPtr Handle, PixelRect Bounds);
