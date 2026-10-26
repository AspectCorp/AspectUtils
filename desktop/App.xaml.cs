using System.Windows;
using System.Windows.Threading;

namespace AspectUtils.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var splash = new SplashWindow();
        splash.Show();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var main = new MainWindow();
                main.Show();
            }
            finally
            {
                splash.Close();
            }
        }), DispatcherPriority.Background);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            e.Exception.Message,
            "A$PECT UTILS",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
