using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace FrameSnap.Input;

public sealed class HotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const int VkS = 0x53;

    private readonly HwndSource _hwndSource;
    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public HotkeyManager()
    {
        var parameters = new HwndSourceParameters("FrameSnapHotkeySink")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    public void RegisterDefaultHotkey()
    {
        if (_registered)
        {
            return;
        }

        if (!RegisterHotKey(_hwndSource.Handle, 1, ModControl | ModShift, VkS))
        {
            return;
        }

        _registered = true;
    }

    public void UnregisterDefaultHotkey()
    {
        if (!_registered)
        {
            return;
        }

        UnregisterHotKey(_hwndSource.Handle, 1);
        _registered = false;
    }

    public void Dispose()
    {
        UnregisterDefaultHotkey();
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == 1)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
