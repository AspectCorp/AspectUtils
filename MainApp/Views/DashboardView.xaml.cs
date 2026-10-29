using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AspectUtils.Desktop.Services;
using Microsoft.Win32;

namespace AspectUtils.Desktop.Views;

public partial class DashboardView : UserControl, IPageView
{
    private readonly IShellHost _shell;
    private bool _toolCheckStarted;

    public DashboardView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;
    }

    public void OnActivated()
    {
        var session = _shell.Session;
        AppsCount.Text = session.Config.Apps.Count.ToString("N0");
        TweaksCount.Text = session.ToggleTweaks.Count.ToString("N0");
        FeaturesCount.Text = session.InstallableFeatures.Count.ToString("N0");
        AppliedCount.Text = session.Undo.All.Count.ToString("N0");
        UndoText.Text = session.Undo.All.Count == 0 ? "No tracked changes" : $"{session.Undo.All.Count} recorded";
        LogPathText.Text = session.Log.LogPath;

        OsText.Text = ReadWindowsVersion();

        if (!_toolCheckStarted)
        {
            _toolCheckStarted = true;
            _ = RefreshPackageManagersAsync();
        }
    }

    private async Task RefreshPackageManagersAsync()
    {
        try
        {
            var hasWinget = await PowerShellRunner.CommandExistsAsync("winget");
            var hasChoco = await PowerShellRunner.CommandExistsAsync("choco");
            ApplyToolStatus(WingetText, hasWinget);
            ApplyToolStatus(ChocoText, hasChoco);
        }
        catch (Exception exception)
        {
            _shell.Session.Log.Warn($"Could not check package managers: {exception.Message}");
        }
    }

    private void ApplyToolStatus(TextBlock target, bool available)
    {
        target.Text = available ? "Available" : "Not found";
        target.Foreground = (Brush)FindResource(available ? "MintBrush" : "DangerBrush");
    }

    private static string ReadWindowsVersion()
    {
        try
        {
            const string keyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            var product = Registry.GetValue(keyPath, "ProductName", null) as string;
            var display = Registry.GetValue(keyPath, "DisplayVersion", null) as string;
            var build = Registry.GetValue(keyPath, "CurrentBuildNumber", null) as string;

            // Windows 11 still reports "Windows 10 Pro" in ProductName.
            var name = int.TryParse(build, out var buildNumber) && buildNumber >= 22000
                ? "Windows 11"
                : product ?? "Windows";
            var version = string.IsNullOrWhiteSpace(display) ? string.Empty : $" {display}";
            var buildText = string.IsNullOrWhiteSpace(build) ? string.Empty : $" (build {build})";
            return $"{name}{version}{buildText}";
        }
        catch
        {
            return Environment.OSVersion.VersionString;
        }
    }

    private void GoToInstall_Click(object sender, RoutedEventArgs e) => _shell.Navigate("install");

    private void GoToTweaks_Click(object sender, RoutedEventArgs e) => _shell.Navigate("tweaks");

    private void GoToFixes_Click(object sender, RoutedEventArgs e) => _shell.Navigate("fixes");

    private void GoToFeatures_Click(object sender, RoutedEventArgs e) => _shell.Navigate("features");

    private void GoToUpdates_Click(object sender, RoutedEventArgs e) => _shell.Navigate("updates");

    private void GoToLog_Click(object sender, RoutedEventArgs e) => _shell.Navigate("log");

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _shell.Session.Log.LogDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _shell.ShowMessage(BrandInfo.Name, $"Could not open the log folder.\n\n{exception.Message}");
        }
    }

    private void OpenProjectPage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = BrandInfo.RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _shell.ShowMessage(BrandInfo.Name, $"Could not open the project page.\n\n{exception.Message}");
        }
    }
}
