using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FrameSnap.Core;
using FrameSnap.Platform;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace FrameSnap.Capture;

public sealed class CaptureEngine
{
    private readonly IDirect3DDevice _direct3DDevice;

    public CaptureEngine()
    {
        _direct3DDevice = CreateDirect3DDevice();
    }

    public BitmapSource CaptureRegion(PixelRect region, MonitorDetails monitor)
    {
        try
        {
            return CaptureRegionWithWindowsGraphicsCapture(region, monitor);
        }
        catch
        {
            return CaptureRegionWithGdi(region);
        }
    }

    private BitmapSource CaptureRegionWithWindowsGraphicsCapture(PixelRect region, MonitorDetails monitor)
    {
        var item = CreateCaptureItemForMonitor(monitor.Handle);
        using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _direct3DDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            item.Size);
        using var session = framePool.CreateCaptureSession(item);
        var tcs = new TaskCompletionSource<SoftwareBitmap>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            using var frame = sender.TryGetNextFrame();
            if (frame is null)
            {
                return;
            }

            _ = SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface).AsTask().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    tcs.TrySetResult(task.Result);
                    return;
                }

                if (task.Exception is not null)
                {
                    tcs.TrySetException(task.Exception);
                    return;
                }

                tcs.TrySetCanceled();
            }, TaskScheduler.Default);
        }

        framePool.FrameArrived += OnFrameArrived;
        try
        {
            session.StartCapture();
            using var softwareBitmap = tcs.Task.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            return CropAndConvert(softwareBitmap, region, monitor.Bounds);
        }
        finally
        {
            framePool.FrameArrived -= OnFrameArrived;
        }
    }

    private static BitmapSource CropAndConvert(SoftwareBitmap bitmap, PixelRect region, PixelRect monitorBounds)
    {
        using var converted = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var width = converted.PixelWidth;
        var height = converted.PixelHeight;
        var stride = width * 4;
        var buffer = new Windows.Storage.Streams.Buffer((uint)(stride * height));
        converted.CopyToBuffer(buffer);
        var bytes = new byte[buffer.Length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(bytes);

        var fullFrame = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            bytes,
            stride);
        fullFrame.Freeze();

        var relativeLeft = Math.Clamp(region.Left - monitorBounds.Left, 0, Math.Max(0, width - 1));
        var relativeTop = Math.Clamp(region.Top - monitorBounds.Top, 0, Math.Max(0, height - 1));
        var cropWidth = Math.Clamp(region.Width, 1, width - relativeLeft);
        var cropHeight = Math.Clamp(region.Height, 1, height - relativeTop);
        var cropped = new CroppedBitmap(fullFrame, new Int32Rect(relativeLeft, relativeTop, cropWidth, cropHeight));
        cropped.Freeze();

        return cropped;
    }

    private static BitmapSource CaptureRegionWithGdi(PixelRect region)
    {
        using var bitmap = new Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(region.Left, region.Top, 0, 0, new System.Drawing.Size(region.Width, region.Height));
        }

        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    private static GraphicsCaptureItem CreateCaptureItemForMonitor(IntPtr monitorHandle)
    {
        var interop = GetCaptureItemInterop();
        var itemGuid = typeof(GraphicsCaptureItem).GUID;
        var hr = interop.CreateForMonitor(monitorHandle, ref itemGuid, out var itemPtr);
        if (hr < 0 || itemPtr == IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        try
        {
            return (GraphicsCaptureItem)Marshal.GetObjectForIUnknown(itemPtr);
        }
        finally
        {
            Marshal.Release(itemPtr);
        }
    }

    private static IDirect3DDevice CreateDirect3DDevice()
    {
        const uint d3d11SdkVersion = 7;
        var hr = D3D11CreateDevice(
            IntPtr.Zero,
            D3DDriverType.Hardware,
            IntPtr.Zero,
            0,
            IntPtr.Zero,
            0,
            d3d11SdkVersion,
            out var d3dDevice,
            out _,
            out var d3dContext);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        try
        {
            var idxgiDeviceGuid = new Guid("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
            hr = Marshal.QueryInterface(d3dDevice, ref idxgiDeviceGuid, out var dxgiDevice);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            try
            {
                hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out var inspectableDevice);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                try
                {
                    return (IDirect3DDevice)Marshal.GetObjectForIUnknown(inspectableDevice);
                }
                finally
                {
                    Marshal.Release(inspectableDevice);
                }
            }
            finally
            {
                Marshal.Release(dxgiDevice);
            }
        }
        finally
        {
            if (d3dContext != IntPtr.Zero)
            {
                Marshal.Release(d3dContext);
            }

            if (d3dDevice != IntPtr.Zero)
            {
                Marshal.Release(d3dDevice);
            }
        }
    }

    private static IGraphicsCaptureItemInterop GetCaptureItemInterop()
    {
        var classId = "Windows.Graphics.Capture.GraphicsCaptureItem";
        var interopGuid = typeof(IGraphicsCaptureItemInterop).GUID;
        var hr = RoGetActivationFactory(classId, ref interopGuid, out var factoryPtr);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        try
        {
            return (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
        }
        finally
        {
            Marshal.Release(factoryPtr);
        }
    }

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        [PreserveSig]
        int CreateForWindow(IntPtr window, ref Guid iid, out IntPtr result);

        [PreserveSig]
        int CreateForMonitor(IntPtr monitor, ref Guid iid, out IntPtr result);
    }

    private enum D3DDriverType : uint
    {
        Hardware = 1
    }

    [DllImport("combase.dll", CharSet = CharSet.Unicode)]
    private static extern int RoGetActivationFactory(
        [MarshalAs(UnmanagedType.HString)] string activatableClassId,
        ref Guid iid,
        out IntPtr factory);

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int D3D11CreateDevice(
        IntPtr adapter,
        D3DDriverType driverType,
        IntPtr software,
        uint flags,
        IntPtr featureLevels,
        uint featureLevelsCount,
        uint sdkVersion,
        out IntPtr device,
        out uint featureLevel,
        out IntPtr immediateContext);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
