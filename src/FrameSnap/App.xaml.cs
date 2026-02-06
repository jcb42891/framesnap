using System.Windows;
using FrameSnap.Shell;

namespace FrameSnap;

public partial class App : Application
{
    private TrayShell? _trayShell;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _trayShell = new TrayShell();
        _trayShell.CaptureRequested += OnCaptureRequested;
        _trayShell.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayShell is not null)
        {
            _trayShell.CaptureRequested -= OnCaptureRequested;
            _trayShell.Dispose();
        }

        base.OnExit(e);
    }

    private void OnCaptureRequested(object? sender, EventArgs e)
    {
        if (_trayShell is null)
        {
            return;
        }

        _trayShell.ShowOverlay();
    }
}
