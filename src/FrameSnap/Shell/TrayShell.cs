using FrameSnap.Capture;
using FrameSnap.Core;
using FrameSnap.Input;
using FrameSnap.Overlay;
using FrameSnap.Output;
using FrameSnap.Settings;
using Microsoft.Win32;
using System.Threading.Tasks;
using WinForms = System.Windows.Forms;

namespace FrameSnap.Shell;

public sealed class TrayShell : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly HotkeyManager _hotkeyManager;
    private readonly CaptureEngine _captureEngine;
    private readonly ClipboardOutputService _clipboardOutputService;
    private readonly FileOutputService _fileOutputService;
    private readonly SettingsStore _settingsStore;
    private readonly CaptureSettings _settings;
    private OverlayWindow? _overlay;
    private AspectRatio _selectedRatio;
    private OutputMode _outputMode;
    private WinForms.ToolStripMenuItem? _ratioMenuItem;
    private WinForms.ToolStripMenuItem? _outputMenuItem;
    private bool _captureSessionActive;

    public event EventHandler? CaptureRequested;

    public TrayShell(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _settings = _settingsStore.Load();
        _selectedRatio = AspectRatio.TryParse(_settings.AspectRatio, out var parsedRatio) ? parsedRatio : AspectRatio.Presets[0];
        _outputMode = _settings.OutputMode;

        _captureEngine = new CaptureEngine();
        _clipboardOutputService = new ClipboardOutputService();
        _fileOutputService = new FileOutputService();
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
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
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
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _hotkeyManager.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private WinForms.ContextMenuStrip BuildMenu()
    {
        var menu = new WinForms.ContextMenuStrip();

        var captureItem = new WinForms.ToolStripMenuItem("Capture", null, (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty));
        _ratioMenuItem = new WinForms.ToolStripMenuItem("Ratio");
        _outputMenuItem = new WinForms.ToolStripMenuItem("Output");

        foreach (var ratio in AspectRatio.Presets)
        {
            var item = new WinForms.ToolStripMenuItem(ratio.ToString())
            {
                Checked = ratio == _selectedRatio
            };
            item.Click += (_, _) => SelectRatio(ratio);
            _ratioMenuItem.DropDownItems.Add(item);
        }

        var customRatioItem = new WinForms.ToolStripMenuItem("Custom...")
        {
            ToolTipText = "Enter ratio as W:H in the popup."
        };
        customRatioItem.Click += (_, _) => OpenCustomRatioPrompt();
        _ratioMenuItem.DropDownItems.Add(new WinForms.ToolStripSeparator());
        _ratioMenuItem.DropDownItems.Add(customRatioItem);

        var clipboardOnlyItem = new WinForms.ToolStripMenuItem("Clipboard Only")
        {
            Checked = _outputMode == OutputMode.ClipboardOnly
        };
        clipboardOnlyItem.Click += (_, _) => SelectOutputMode(OutputMode.ClipboardOnly);

        var clipboardAndSaveItem = new WinForms.ToolStripMenuItem("Clipboard + Save")
        {
            Checked = _outputMode == OutputMode.ClipboardAndSave
        };
        clipboardAndSaveItem.Click += (_, _) => SelectOutputMode(OutputMode.ClipboardAndSave);

        _outputMenuItem.DropDownItems.Add(clipboardOnlyItem);
        _outputMenuItem.DropDownItems.Add(clipboardAndSaveItem);

        var exitItem = new WinForms.ToolStripMenuItem("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());

        menu.Items.Add(captureItem);
        menu.Items.Add(_ratioMenuItem);
        menu.Items.Add(_outputMenuItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        CaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void OnCaptureConfirmed(object? sender, CaptureRegionEventArgs e)
    {
        string? savedPath = null;
        var success = false;
        var errorMessage = string.Empty;

        try
        {
            CloseOverlay();
            await Task.Delay(50);

            var image = _captureEngine.CaptureRegion(e.Region, e.Monitor);
            _clipboardOutputService.CopyImage(image);
            if (_outputMode == OutputMode.ClipboardAndSave)
            {
                savedPath = _fileOutputService.SavePng(image);
            }

            success = true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        ShowCaptureStatus(success, savedPath, errorMessage);
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

    private void SelectRatio(AspectRatio ratio)
    {
        _selectedRatio = ratio;
        _settings.AspectRatio = ratio.ToString();
        SaveSettings();

        if (_ratioMenuItem is null)
        {
            return;
        }

        foreach (WinForms.ToolStripItem item in _ratioMenuItem.DropDownItems)
        {
            if (item is WinForms.ToolStripMenuItem menuItem && AspectRatio.TryParse(menuItem.Text ?? string.Empty, out var menuRatio))
            {
                menuItem.Checked = menuRatio == ratio;
            }
        }
    }

    private void OpenCustomRatioPrompt()
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

        SelectRatio(customRatio);
    }

    private void SelectOutputMode(OutputMode outputMode)
    {
        _outputMode = outputMode;
        _settings.OutputMode = outputMode;
        SaveSettings();

        if (_outputMenuItem is null)
        {
            return;
        }

        foreach (WinForms.ToolStripItem item in _outputMenuItem.DropDownItems)
        {
            if (item is not WinForms.ToolStripMenuItem menuItem)
            {
                continue;
            }

            menuItem.Checked = (outputMode == OutputMode.ClipboardOnly && menuItem.Text == "Clipboard Only")
                || (outputMode == OutputMode.ClipboardAndSave && menuItem.Text == "Clipboard + Save");
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch
        {
            // Non-fatal: app should continue even if settings cannot persist.
        }
    }

    private void ShowCaptureStatus(bool success, string? savedPath, string errorMessage)
    {
        if (success)
        {
            if (string.IsNullOrWhiteSpace(savedPath))
            {
                ShowStatus("FrameSnap", "Copied to clipboard.");
                return;
            }

            ShowStatus("FrameSnap", $"Copied and saved to {savedPath}");
            return;
        }

        var message = string.IsNullOrWhiteSpace(errorMessage) ? "Capture failed." : $"Capture failed: {errorMessage}";
        ShowStatus("FrameSnap Error", message, WinForms.ToolTipIcon.Error);
    }

    public void ShowStatus(string title, string text, WinForms.ToolTipIcon icon = WinForms.ToolTipIcon.Info)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(2500);
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        _hotkeyManager.UnregisterDefaultHotkey();
        _hotkeyManager.RegisterDefaultHotkey();
    }
}
