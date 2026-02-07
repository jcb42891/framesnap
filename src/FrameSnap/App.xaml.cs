using System.Windows;
using System.Threading.Tasks;
using FrameSnap.Settings;
using FrameSnap.Shell;

namespace FrameSnap;

public partial class App : Application
{
    private TrayShell? _trayShell;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterGlobalExceptionHandlers();
        _trayShell = new TrayShell(new SettingsStore());
        _trayShell.CaptureRequested += OnCaptureRequested;
        _trayShell.Start();

        _mainWindow = new MainWindow(_trayShell);
        _mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnregisterGlobalExceptionHandlers();
        if (_trayShell is not null)
        {
            _trayShell.CaptureRequested -= OnCaptureRequested;
            _trayShell.Dispose();
        }

        _mainWindow = null;

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

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void UnregisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _trayShell?.ShowStatus("FrameSnap Error", e.Exception.Message, System.Windows.Forms.ToolTipIcon.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var message = e.ExceptionObject is Exception ex ? ex.Message : "Unhandled exception occurred.";
        _trayShell?.ShowStatus("FrameSnap Error", message, System.Windows.Forms.ToolTipIcon.Error);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _trayShell?.ShowStatus("FrameSnap Error", e.Exception.Message, System.Windows.Forms.ToolTipIcon.Error);
        e.SetObserved();
    }
}
