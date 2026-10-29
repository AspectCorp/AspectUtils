using System.Diagnostics;
using System.Windows;
using AspectUtils.Desktop.Theme;

namespace AspectUtils.Desktop;

public partial class UpdateWindow : ThemedWindow
{
    private readonly string _releaseUrl;

    public UpdateWindow(string latestVersion, string releaseUrl)
    {
        InitializeComponent();
        _releaseUrl = string.IsNullOrWhiteSpace(releaseUrl)
            ? BrandInfo.RepositoryUrl + "/releases/latest"
            : releaseUrl;

        var displayVersion = latestVersion.StartsWith('v') ? latestVersion : $"v{latestVersion}";
        HeadlineText.Text = $"A$PECT UTILS {displayVersion} is ready.";
        VersionText.Text = $"You are running v{BrandInfo.Version}. The update is a free download from GitHub.";
    }

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_releaseUrl) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(
                this,
                $"Open this page in a browser to download the update:\n\n{_releaseUrl}",
                BrandInfo.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e) => Close();
}
