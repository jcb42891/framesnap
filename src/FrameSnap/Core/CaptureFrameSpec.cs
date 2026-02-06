namespace FrameSnap.Core;

public enum CaptureFrameMode
{
    AspectRatio,
    PixelSize
}

public readonly record struct CaptureFrameSpec(int Width, int Height, CaptureFrameMode Mode)
{
    public static CaptureFrameSpec FromRatio(AspectRatio ratio) => new(ratio.Width, ratio.Height, CaptureFrameMode.AspectRatio);

    public static CaptureFrameSpec FromPixelSize(int width, int height) => new(width, height, CaptureFrameMode.PixelSize);

    public AspectRatio ToAspectRatio() => new(Width, Height);

    public override string ToString() => Mode == CaptureFrameMode.PixelSize ? $"{Width}x{Height}" : $"{Width}:{Height}";

    public static bool TryParse(string input, out CaptureFrameSpec spec)
    {
        spec = default;
        if (AspectRatio.TryParse(input, out var ratio))
        {
            spec = FromRatio(ratio);
            return true;
        }

        if (!TryParsePixelSize(input, out var width, out var height))
        {
            return false;
        }

        spec = FromPixelSize(width, height);
        return true;
    }

    private static bool TryParsePixelSize(string input, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim().Replace('X', 'x');
        var parts = normalized.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height))
        {
            return false;
        }

        return width > 0 && height > 0;
    }
}
