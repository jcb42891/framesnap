using System.Windows;
using System.Windows.Controls;
using FrameSnap.Core;

namespace FrameSnap.Shell;

public partial class MainWindow : Window
{
    private readonly TrayShell _trayShell;
    private bool _isInitializing;

    public MainWindow(TrayShell trayShell)
    {
        InitializeComponent();
        _trayShell = trayShell;

        _isInitializing = true;
        PopulateRatioOptions();
        SyncFromSettings();
        _isInitializing = false;

        _trayShell.CaptureStatusChanged += OnCaptureStatusChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayShell.CaptureStatusChanged -= OnCaptureStatusChanged;
        base.OnClosed(e);
    }

    private void PopulateRatioOptions()
    {
        foreach (var ratio in AspectRatio.Presets)
        {
            RatioComboBox.Items.Add(ratio.ToString());
        }

        RatioComboBox.Items.Add("Custom...");
    }

    private void SyncFromSettings()
    {
        var selectedFrameSpec = _trayShell.SelectedFrameSpec;
        var selectedOutputMode = _trayShell.SelectedOutputMode;

        if (selectedFrameSpec.Mode == CaptureFrameMode.AspectRatio)
        {
            RatioComboBox.SelectedItem = selectedFrameSpec.ToAspectRatio().ToString();
        }
        else
        {
            RatioComboBox.SelectedItem = "Custom...";
            StatusText.Text = $"Using custom size: {selectedFrameSpec}";
        }

        OutputComboBox.SelectedIndex = selectedOutputMode == OutputMode.ClipboardOnly ? 0 : 1;
    }

    private void OnNewSnipClicked(object sender, RoutedEventArgs e)
    {
        _trayShell.RequestCapture();
    }

    private void OnRatioSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || RatioComboBox.SelectedItem is null)
        {
            return;
        }

        var selected = RatioComboBox.SelectedItem.ToString() ?? string.Empty;

        if (selected == "Custom...")
        {
            return;
        }

        if (!AspectRatio.TryParse(selected, out var ratio))
        {
            return;
        }

        _trayShell.UpdateFrameSpec(CaptureFrameSpec.FromRatio(ratio));
    }

    private void OnRatioDropDownClosed(object sender, EventArgs e)
    {
        if (_isInitializing || RatioComboBox.SelectedItem?.ToString() != "Custom...")
        {
            return;
        }

        PromptForCustomCaptureSize();
    }

    private void PromptForCustomCaptureSize()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Use W:H (example: 5:4) for ratio mode, or WxH (example: 1920x1080) for exact pixels.",
            "Custom Capture Size",
            _trayShell.SelectedFrameSpec.ToString());

        if (string.IsNullOrWhiteSpace(input))
        {
            SyncFromSettings();
            return;
        }

        if (!_trayShell.TryUpdateFrameSpec(input.Trim()))
        {
            System.Windows.MessageBox.Show(
                "Invalid format. Use W:H for ratio mode or WxH for exact pixel mode.",
                "Invalid Capture Format",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            SyncFromSettings();
            return;
        }

        StatusText.Text = $"Using custom size: {_trayShell.SelectedFrameSpec}";
    }

    private void OnOutputSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || OutputComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        var mode = selectedItem.Tag?.ToString() == "ClipboardAndSave"
            ? OutputMode.ClipboardAndSave
            : OutputMode.ClipboardOnly;

        _trayShell.UpdateOutputMode(mode);
    }

    private void OnCaptureStatusChanged(object? sender, string status)
    {
        Dispatcher.Invoke(() => StatusText.Text = status);
    }
}
