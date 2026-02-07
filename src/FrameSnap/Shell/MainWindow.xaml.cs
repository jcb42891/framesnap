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
            var selectedRatio = selectedFrameSpec.ToAspectRatio().ToString();
            if (RatioComboBox.Items.Contains(selectedRatio))
            {
                RatioComboBox.SelectedItem = selectedRatio;
            }
            else
            {
                RatioComboBox.SelectedItem = "Custom...";
            }
        }
        else
        {
            RatioComboBox.SelectedItem = "Custom...";
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
        var dialog = new CustomCaptureSizeDialog(_trayShell.SelectedFrameSpec)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true || dialog.SelectedFrameSpec is null)
        {
            SyncFromSettings();
            return;
        }

        _trayShell.UpdateFrameSpec(dialog.SelectedFrameSpec.Value);
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
}
