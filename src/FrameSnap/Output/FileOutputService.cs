using System.IO;
using System.Windows.Media.Imaging;

namespace FrameSnap.Output;

public sealed class FileOutputService
{
    public string SavePng(BitmapSource image)
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var monthFolder = DateTime.Now.ToString("yyyy-MM");
        var outputFolder = Path.Combine(pictures, "AspectSnips", monthFolder);
        Directory.CreateDirectory(outputFolder);

        var fileName = $"snip_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var fullPath = Path.Combine(outputFolder, fileName);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = File.Create(fullPath);
        encoder.Save(stream);

        return fullPath;
    }
}
