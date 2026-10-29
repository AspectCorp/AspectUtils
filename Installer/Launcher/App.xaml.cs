using System.Windows;
using System.Windows.Threading;

namespace AspectUtils.Installer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        InstallLog.Write("Unhandled exception: " + e.Exception);

        MessageBox.Show(
            "The installer hit an unexpected problem.\n\n" +
            e.Exception.Message +
            "\n\nDetails were written to:\n" +
            InstallLog.FilePath,
            AppInfo.ProductName,
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
        Shutdown(1);
    }
}
