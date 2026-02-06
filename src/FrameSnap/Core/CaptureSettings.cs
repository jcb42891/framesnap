namespace FrameSnap.Core;

public sealed class CaptureSettings
{
    public string AspectRatio { get; set; } = "1:1";

    public OutputMode OutputMode { get; set; } = OutputMode.ClipboardOnly;
}
