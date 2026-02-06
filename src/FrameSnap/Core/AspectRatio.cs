namespace FrameSnap.Core;

public readonly record struct AspectRatio(int Width, int Height)
{
    public override string ToString() => $"{Width}:{Height}";

    public static bool TryParse(string input, out AspectRatio ratio)
    {
        ratio = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
        {
            return false;
        }

        if (width <= 0 || height <= 0)
        {
            return false;
        }

        ratio = new AspectRatio(width, height);
        return true;
    }

    public static IReadOnlyList<AspectRatio> Presets { get; } =
    [
        new(1, 1),
        new(16, 9),
        new(4, 3),
        new(3, 2),
        new(9, 16),
        new(21, 9)
    ];
}
