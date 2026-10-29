using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace AspectUtils.Desktop;

public partial class App : Application
{
    private const string InstanceMutexName = "AspectUtils.SingleInstance";
    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _instanceMutex = new Mutex(true, InstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            MessageBox.Show(
                "A$PECT UTILS is already running.",
                BrandInfo.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

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

    protected override void OnExit(ExitEventArgs e)
    {
        if (_instanceMutex is not null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
            }
            catch
            {
                // The mutex was already released during shutdown.
            }

            _instanceMutex.Dispose();
            _instanceMutex = null;
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var message = $"{e.Exception.GetType().Name}: {e.Exception.Message}\nInner: {e.Exception.InnerException}\nStack: {e.Exception.StackTrace}";

        try
        {
            File.WriteAllText(@"C:\Users\Public\aspect_error.log", message);
        }
        catch
        {
            // Logging must never hide the original failure.
        }

        MessageBox.Show(
            message,
            BrandInfo.Name,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
