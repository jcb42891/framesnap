using System.Windows.Media.Imaging;

namespace FrameSnap.Output;

public sealed class ClipboardOutputService
{
    public void CopyImage(BitmapSource image)
    {
        Clipboard.SetImage(image);
    }
}
