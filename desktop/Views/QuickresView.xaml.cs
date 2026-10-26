using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Services;

namespace AspectUtils.Desktop.Views;

public partial class QuickresView : UserControl, IPageView
{
    private readonly MainWindow _host;
    private int _originalWidth;
    private int _originalHeight;
    private bool _haveOriginal;

    private static readonly (string Label, int Width, int Height)[] Presets =
    {
        ("1920x1440 (stretch)", 1920, 1440),
        ("1440x1080 (stretch)", 1440, 1080),
        ("1600x1080 (stretch)", 1600, 1080),
        ("1280x1080 (stretch)", 1280, 1080),
        ("1280x1024 (stretch)", 1280, 1024),
        ("1280x960 (stretch)", 1280, 960),
        ("1568x1080 (stretch)", 1568, 1080),
        ("1920x1080 (native)", 1920, 1080),
        ("2560x1440 (native)", 2560, 1440),
        ("3840x2160 (native)", 3840, 2160)
    };

    public QuickresView(MainWindow host)
    {
        _host = host;
        InitializeComponent();

        foreach (var preset in Presets)
        {
            var button = new Button
            {
                Content = preset.Label,
                Tag = preset,
                Style = (Style)FindResource("PrimaryButton"),
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 6, 12, 6)
            };
            button.Click += (s, e) =>
            {
                if (button.Tag is (string _, int w, int h))
                {
                    Apply(w, h);
                }
            };
            PresetsPanel.Children.Add(button);
        }

        var current = ResolutionService.GetCurrent();
        CurrentResolutionText.Text = $"{current.Width}x{current.Height}";
        _originalWidth = current.Width;
        _originalHeight = current.Height;
        _haveOriginal = true;
        StatusText.Text = "Ready. Select a preset or enter a custom resolution, then apply.";
    }

    public void OnActivated()
    {
        var current = ResolutionService.GetCurrent();
        CurrentResolutionText.Text = $"{current.Width}x{current.Height}";
    }

    private void ApplyCustomButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthBox.Text, out var w) || !int.TryParse(HeightBox.Text, out var h))
        {
            StatusText.Text = "Enter valid numeric width and height.";
            return;
        }
        Apply(w, h);
    }

    private void Apply(int width, int height)
    {
        if (!_haveOriginal)
        {
            var current = ResolutionService.GetCurrent();
            _originalWidth = current.Width;
            _originalHeight = current.Height;
            _haveOriginal = true;
        }

        var ok = ResolutionService.TrySet(width, height, out var message);
        StatusText.Text = message;
        if (ok)
        {
            CurrentResolutionText.Text = $"{width}x{height}";
        }
    }

    private void RevertButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_haveOriginal)
        {
            StatusText.Text = "No original resolution recorded yet.";
            return;
        }
        var ok = ResolutionService.TrySet(_originalWidth, _originalHeight, out var message);
        StatusText.Text = ok ? $"Reverted to {_originalWidth}x{_originalHeight}." : message;
        if (ok)
        {
            CurrentResolutionText.Text = $"{_originalWidth}x{_originalHeight}";
        }
    }

    private void OpenSnapResButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/bkuwu/SnapRes/releases",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not open the link: {exception.Message}";
        }
    }
}
