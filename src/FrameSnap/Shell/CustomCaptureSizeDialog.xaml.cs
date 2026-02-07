using FrameSnap.Core;

namespace FrameSnap.Shell;

public partial class CustomCaptureSizeDialog : Window
{
    public CaptureFrameSpec? SelectedFrameSpec { get; private set; }

    public CustomCaptureSizeDialog(CaptureFrameSpec initialFrameSpec)
    {
        InitializeComponent();

        WidthTextBox.Text = initialFrameSpec.Width.ToString();
        HeightTextBox.Text = initialFrameSpec.Height.ToString();

        if (initialFrameSpec.Mode == CaptureFrameMode.PixelSize)
        {
            PixelModeRadioButton.IsChecked = true;
        }
        else
        {
            RatioModeRadioButton.IsChecked = true;
        }
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        ValidationText.Visibility = Visibility.Collapsed;
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthTextBox.Text.Trim(), out var width) || width <= 0)
        {
            ShowValidation("Width must be a positive whole number.");
            return;
        }

        if (!int.TryParse(HeightTextBox.Text.Trim(), out var height) || height <= 0)
        {
            ShowValidation("Height must be a positive whole number.");
            return;
        }

        SelectedFrameSpec = PixelModeRadioButton.IsChecked == true
            ? CaptureFrameSpec.FromPixelSize(width, height)
            : CaptureFrameSpec.FromRatio(new AspectRatio(width, height));

        DialogResult = true;
    }

    private void ShowValidation(string message)
    {
        ValidationText.Text = message;
        ValidationText.Visibility = Visibility.Visible;
    }
}
