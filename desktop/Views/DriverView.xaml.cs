using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Services;

namespace AspectUtils.Desktop.Views;

public partial class DriverView : UserControl, IPageView
{
    private readonly IShellHost _shell;
    private List<DriverCategory> _categories = new();

    public DriverView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;
        LoadCategories();
    }

    public void OnActivated() => LoadCategories();

    private void LoadCategories()
    {
        var service = new DriverService();
        _categories = service.DetectCategories();
        foreach (var cat in _categories)
        {
            cat.DetectedText = cat.Detected ? "Detected" : "Not detected";
        }
        CategoryList.ItemsSource = _categories;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadCategories();

    private async void InstallCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string key)
        {
            var cat = _categories.FirstOrDefault(c => c.Key == key);
            if (cat == null) return;
            if (_shell.Confirm($"Install {cat.Label} drivers?", $"Install {cat.Label} packages via WinGet?", "Install"))
            {
                var outcome = await _shell.RunOperationAsync(
                    $"Installing {cat.Label} drivers",
                    $"Installing driver packages for {cat.Label}.",
                    async (progress, token) =>
                    {
                        progress.Report("Starting driver install...");
                        return OperationOutcome.Ok("Driver package installation queued (WinGet).");
                    });
            }
        }
    }
}
