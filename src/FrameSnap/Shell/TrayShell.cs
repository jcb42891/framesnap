using FrameSnap.Capture;
using FrameSnap.Core;
using FrameSnap.Input;
using FrameSnap.Overlay;
using FrameSnap.Output;
using WinForms = System.Windows.Forms;

namespace FrameSnap.Shell;

public sealed class TrayShell : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly HotkeyManager _hotkeyManager;
    private readonly CaptureEngine _captureEngine;
    private readonly ClipboardOutputService _clipboardOutputService;
    private OverlayWindow? _overlay;
    private AspectRatio _selectedRatio = AspectRatio.Presets[0];
    private bool _captureSessionActive;

    public event EventHandler? CaptureRequested;

    public TrayShell()
    {
        _captureEngine = new CaptureEngine();
        _clipboardOutputService = new ClipboardOutputService();
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        _notifyIcon = new WinForms.NotifyIcon
        {
            Text = "FrameSnap",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = false,
            ContextMenuStrip = BuildMenu()
        };
    }

    public void Start()
    {
        _hotkeyManager.RegisterDefaultHotkey();
        _notifyIcon.Visible = true;
    }

    public void ShowOverlay()
    {
        if (_overlay is not null || _captureSessionActive)
        {
            return;
        }

        _captureSessionActive = true;
        _overlay = new OverlayWindow(_selectedRatio);
        _overlay.CaptureConfirmed += OnCaptureConfirmed;
        _overlay.CaptureCancelled += OnCaptureCancelled;
        _overlay.Closed += OnOverlayClosed;
        _overlay.Show();
        _overlay.Activate();
        _overlay.Focus();
    }

    public void Dispose()
    {
        CloseOverlay();
        _hotkeyManager.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyManager.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private WinForms.ContextMenuStrip BuildMenu()
    {
        var menu = new WinForms.ContextMenuStrip();

        var captureItem = new WinForms.ToolStripMenuItem("Capture", null, (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty));
        var ratioItem = new WinForms.ToolStripMenuItem("Ratio");

        foreach (var ratio in AspectRatio.Presets)
        {
            var item = new WinForms.ToolStripMenuItem(ratio.ToString())
            {
                Checked = ratio == _selectedRatio
            };
            item.Click += (_, _) => SelectRatio(ratioItem, ratio);
            ratioItem.DropDownItems.Add(item);
        }

        var customRatioItem = new WinForms.ToolStripMenuItem("Custom...")
        {
            ToolTipText = "Enter ratio as W:H in the popup."
        };
        customRatioItem.Click += (_, _) => OpenCustomRatioPrompt(ratioItem);
        ratioItem.DropDownItems.Add(new WinForms.ToolStripSeparator());
        ratioItem.DropDownItems.Add(customRatioItem);

        var exitItem = new WinForms.ToolStripMenuItem("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());

        menu.Items.Add(captureItem);
        menu.Items.Add(ratioItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        CaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCaptureConfirmed(object? sender, CaptureRegionEventArgs e)
    {
        try
        {
            var image = _captureEngine.CaptureRegion(e.Region);
            _clipboardOutputService.CopyImage(image);
        }
        catch
        {
            // Keep MVP resilient: capture errors should not leave overlay stuck.
        }

        CloseOverlay();
    }

    private void OnCaptureCancelled(object? sender, EventArgs e)
    {
        CloseOverlay();
    }

    private void OnOverlayClosed(object? sender, EventArgs e)
    {
        CloseOverlay();
    }

    private void CloseOverlay()
    {
        if (_overlay is null)
        {
            _captureSessionActive = false;
            return;
        }

        _overlay.CaptureConfirmed -= OnCaptureConfirmed;
        _overlay.CaptureCancelled -= OnCaptureCancelled;
        _overlay.Closed -= OnOverlayClosed;

        if (_overlay.IsVisible)
        {
            _overlay.Close();
        }

        _overlay = null;
        _captureSessionActive = false;
    }

    private void SelectRatio(WinForms.ToolStripMenuItem ratioMenuItem, AspectRatio ratio)
    {
        _selectedRatio = ratio;
        foreach (WinForms.ToolStripItem item in ratioMenuItem.DropDownItems)
        {
            if (item is WinForms.ToolStripMenuItem menuItem && AspectRatio.TryParse(menuItem.Text ?? string.Empty, out var menuRatio))
            {
                menuItem.Checked = menuRatio == ratio;
            }
        }
    }

    private void OpenCustomRatioPrompt(WinForms.ToolStripMenuItem ratioMenuItem)
    {
        var result = WinForms.MessageBox.Show(
            "Use the format W:H (example: 5:4). Click OK to enter it now.",
            "Custom Ratio",
            WinForms.MessageBoxButtons.OKCancel,
            WinForms.MessageBoxIcon.Information);

        if (result != WinForms.DialogResult.OK)
        {
            return;
        }

        using var prompt = new WinForms.Form
        {
            Width = 280,
            Height = 140,
            Text = "Set Custom Ratio",
            StartPosition = WinForms.FormStartPosition.CenterScreen,
            FormBorderStyle = WinForms.FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false
        };
        var textBox = new WinForms.TextBox
        {
            Left = 20,
            Top = 20,
            Width = 220,
            Text = _selectedRatio.ToString()
        };
        var okButton = new WinForms.Button
        {
            Text = "OK",
            Left = 85,
            Width = 80,
            Top = 55,
            DialogResult = WinForms.DialogResult.OK
        };
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(okButton);
        prompt.AcceptButton = okButton;

        if (prompt.ShowDialog() != WinForms.DialogResult.OK)
        {
            return;
        }

        if (!AspectRatio.TryParse(textBox.Text, out var customRatio))
        {
            WinForms.MessageBox.Show("Invalid ratio format. Expected W:H with positive integers.", "Invalid Ratio", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
            return;
        }

        SelectRatio(ratioMenuItem, customRatio);
    }
}
