using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AspectUtils.Installer;

public partial class MainWindow : Window
{
    private enum UiState
    {
        Idle,
        NeedsElevation,
        ConfirmRunning,
        Installing,
        Success,
        Error
    }

    private readonly InstallationService _service;
    private CancellationTokenSource? _cts;
    private DoubleAnimation? _spinnerAnimation;
    private UiState _state;
    private bool _installing;
    private bool _closing;
    private bool _canRetry = true;

    public MainWindow()
    {
        InitializeComponent();

        _service = new InstallationService();
        InstallPathText.Text = _service.TargetDirectory;
        DesktopShortcutCheck.IsChecked = true;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Shell.BeginAnimation(UIElement.OpacityProperty, fade);

        var slide = new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ShellTransform.BeginAnimation(TranslateTransform.YProperty, slide);

        if (!_service.PayloadAvailable)
        {
            ShowError(
                "The installer payload is missing.",
                $"Expected to find {AppInfo.ExecutableName} in '{_service.PayloadDirectory}'. " +
                "Rebuild the installer with Installer\\build.ps1 and keep the payload folder beside the setup executable.",
                canRetry: false);
            return;
        }

        if (!InstallationService.IsAdministrator())
        {
            ShowElevationState();
            return;
        }

        ShowIdle();
    }

    private void ShowIdle(string? status = null)
    {
        _state = UiState.Idle;
        _canRetry = true;
        ShowHosts(brand: true);

        TitleText.Text = "Ready to install";
        SetStatus(status ?? $"A$PECT UTILS {AppInfo.Version} installs for every account on this PC.");
        SetDetail(null);

        ResetProgress();
        InstallProgress.Visibility = Visibility.Collapsed;

        PrimaryActionButton.Content = "Install";
        PrimaryActionButton.IsEnabled = true;
        PrimaryActionButton.Visibility = Visibility.Visible;

        SecondaryActionButton.Visibility = Visibility.Collapsed;
        DesktopShortcutCheck.IsEnabled = true;
    }

    private void ShowElevationState()
    {
        _state = UiState.NeedsElevation;
        _canRetry = true;
        ShowHosts(brand: true);

        TitleText.Text = "Administrator approval needed";
        SetStatus("A$PECT UTILS installs for every account, so Windows needs your permission.");
        SetDetail("Choose Continue, then approve the Windows prompt. Nothing changes until you do.");

        ResetProgress();
        InstallProgress.Visibility = Visibility.Collapsed;

        PrimaryActionButton.Content = "Continue";
        PrimaryActionButton.IsEnabled = true;
        PrimaryActionButton.Visibility = Visibility.Visible;

        SecondaryActionButton.Content = "Quit";
        SecondaryActionButton.Visibility = Visibility.Visible;
        DesktopShortcutCheck.IsEnabled = true;
    }

    private void ShowConfirmRunningState(int runningCount)
    {
        _state = UiState.ConfirmRunning;
        _canRetry = true;
        ShowHosts(brand: true);

        TitleText.Text = "A$PECT UTILS is running";
        SetStatus(runningCount == 1
            ? "Close it to continue with the installation."
            : $"Close all {runningCount} open copies to continue with the installation.");
        SetDetail("The installer will close it for you. Unsaved selections in the app will be lost.");

        ResetProgress();
        InstallProgress.Visibility = Visibility.Collapsed;

        PrimaryActionButton.Content = "Close and install";
        PrimaryActionButton.IsEnabled = true;
        PrimaryActionButton.Visibility = Visibility.Visible;

        SecondaryActionButton.Content = "Cancel";
        SecondaryActionButton.Visibility = Visibility.Visible;
        DesktopShortcutCheck.IsEnabled = true;
    }

    private void ShowInstalling()
    {
        _state = UiState.Installing;
        _installing = true;
        _canRetry = true;
        ShowHosts(spinner: true);

        TitleText.Text = "Installing";
        SetStatus("Getting things ready…");
        SetDetail(null);

        ResetProgress();
        InstallProgress.Visibility = Visibility.Visible;

        PrimaryActionButton.Visibility = Visibility.Collapsed;
        SecondaryActionButton.Visibility = Visibility.Collapsed;
        DesktopShortcutCheck.IsEnabled = false;
    }

    private void ShowSuccess(InstallResult result)
    {
        _state = UiState.Success;
        _installing = false;
        _canRetry = true;
        ShowHosts(success: true);

        TitleText.Text = "You're all set";
        SetStatus(result.Message);

        var detail = $"Installed to {_service.TargetDirectory}";
        if (!string.IsNullOrWhiteSpace(result.Detail))
        {
            detail += " " + result.Detail;
        }
        SetDetail(detail);

        InstallProgress.Visibility = Visibility.Collapsed;
        ResetProgress();

        PrimaryActionButton.Content = "Launch A$PECT UTILS";
        PrimaryActionButton.IsEnabled = true;
        PrimaryActionButton.Visibility = Visibility.Visible;

        SecondaryActionButton.Content = "Close";
        SecondaryActionButton.Visibility = Visibility.Visible;
        DesktopShortcutCheck.IsEnabled = false;
    }

    private void ShowError(string message, string? detail = null, bool canRetry = true)
    {
        _state = UiState.Error;
        _installing = false;
        _canRetry = canRetry;
        ShowHosts(error: true);

        TitleText.Text = "Installation stopped";
        SetStatus(message);
        SetDetail(detail);

        InstallProgress.Visibility = Visibility.Collapsed;
        ResetProgress();

        PrimaryActionButton.Content = canRetry ? "Try again" : "Close";
        PrimaryActionButton.IsEnabled = true;
        PrimaryActionButton.Visibility = Visibility.Visible;

        SecondaryActionButton.Content = "Close";
        SecondaryActionButton.Visibility = canRetry ? Visibility.Visible : Visibility.Collapsed;
        DesktopShortcutCheck.IsEnabled = true;
    }

    private void ShowHosts(bool brand = false, bool spinner = false, bool success = false, bool error = false)
    {
        BrandHost.Visibility = brand ? Visibility.Visible : Visibility.Collapsed;
        SpinnerHost.Visibility = spinner ? Visibility.Visible : Visibility.Collapsed;
        SuccessHost.Visibility = success ? Visibility.Visible : Visibility.Collapsed;
        ErrorHost.Visibility = error ? Visibility.Visible : Visibility.Collapsed;

        if (spinner)
        {
            StartSpinner();
        }
        else
        {
            StopSpinner();
        }
    }

    private void StartSpinner()
    {
        _spinnerAnimation = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.05))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, _spinnerAnimation);
    }

    private void StopSpinner()
    {
        SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        SpinnerRotate.Angle = 0;
        _spinnerAnimation = null;
    }

    private void SetStatus(string message, bool subtle = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = (Brush)FindResource(subtle ? "SubtleBrush" : "MutedBrush");

        var fade = new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        StatusText.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void SetDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            DetailText.Text = string.Empty;
            DetailText.Visibility = Visibility.Collapsed;
            return;
        }

        DetailText.Text = detail;
        DetailText.Visibility = Visibility.Visible;
    }

    private void AnimateProgress(double value)
    {
        var current = (double)InstallProgress.GetValue(ProgressBar.ValueProperty);
        var animation = new DoubleAnimation(current, value, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        InstallProgress.BeginAnimation(ProgressBar.ValueProperty, animation);
    }

    private void ResetProgress()
    {
        InstallProgress.BeginAnimation(ProgressBar.ValueProperty, null);
        InstallProgress.Value = 0;
    }

    private async Task BeginInstallAsync(bool confirmed = false)
    {
        if (!InstallationService.IsAdministrator())
        {
            ShowElevationState();
            return;
        }

        if (!confirmed)
        {
            var runningCount = InstallationService.CountRunningInstances(_service.TargetDirectory);
            if (runningCount > 0)
            {
                ShowConfirmRunningState(runningCount);
                return;
            }
        }

        ShowInstalling();
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<InstallProgress>(item =>
            {
                SetStatus(item.Message);
                AnimateProgress(item.Percent);
            });

            var result = await _service.InstallAsync(
                progress,
                _cts.Token,
                DesktopShortcutCheck.IsChecked == true);

            if (result.Success)
            {
                ShowSuccess(result);
            }
            else
            {
                ShowError(result.Message, result.Detail);
            }
        }
        catch (OperationCanceledException)
        {
            if (!_closing)
            {
                ShowIdle("Installation cancelled.");
            }
        }
        catch (Exception ex)
        {
            InstallLog.Write("Install failed: " + ex);
            if (!_closing)
            {
                ShowError("Installation stopped unexpectedly.", ex.Message);
            }
        }
        finally
        {
            _installing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            switch (_state)
            {
                case UiState.NeedsElevation:
                    TryRestartElevated();
                    break;
                case UiState.ConfirmRunning:
                    await BeginInstallAsync(confirmed: true);
                    break;
                case UiState.Success:
                    LaunchInstalledApp();
                    break;
                case UiState.Error:
                    if (_canRetry)
                    {
                        await BeginInstallAsync();
                    }
                    else
                    {
                        Close();
                    }
                    break;
                default:
                    await BeginInstallAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            InstallLog.Write("Unhandled UI error: " + ex);
            ShowError("Something went wrong.", ex.Message);
        }
    }

    private void SecondaryAction_Click(object sender, RoutedEventArgs e)
    {
        if (_state == UiState.ConfirmRunning)
        {
            ShowIdle();
            return;
        }

        Close();
    }

    private void TryRestartElevated()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            ShowError(
                "The installer could not restart.",
                "The executable path is unavailable. Run AspectUtilsSetup.exe from Explorer and approve the Windows prompt.",
                canRetry: false);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory
        };

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "AspectUtilsSetup.dll"));
        }

        startInfo.ArgumentList.Add("--target");
        startInfo.ArgumentList.Add(_service.TargetDirectory);

        try
        {
            Process.Start(startInfo);
            _closing = true;
            Close();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            InstallLog.Write("Elevation was cancelled by the user.");
            SetStatus("Administrator approval was cancelled. Choose Continue to try again.");
            SetDetail("Windows did not grant permission, so nothing was installed.");
        }
        catch (Exception ex)
        {
            InstallLog.Write("Elevation failed: " + ex);
            ShowError(
                "The installer could not restart with administrator rights.",
                ex.Message,
                canRetry: false);
        }
    }

    private void LaunchInstalledApp()
    {
        var executablePath = Path.Combine(_service.TargetDirectory, AppInfo.ExecutableName);
        if (!File.Exists(executablePath))
        {
            ShowError(
                "A$PECT UTILS could not be found.",
                $"Expected it at '{executablePath}'. Run the installer again.",
                canRetry: false);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = _service.TargetDirectory,
                UseShellExecute = true
            });

            _closing = true;
            Close();
        }
        catch (Exception ex)
        {
            InstallLog.Write("Could not launch the installed app: " + ex);
            ShowError("A$PECT UTILS could not start.", ex.Message, canRetry: false);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The mouse button was released between the event and the drag.
        }
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_installing)
        {
            var answer = MessageBox.Show(
                this,
                "Installation is still running. Close anyway?",
                AppInfo.ProductName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _cts?.Cancel();
        }

        _closing = true;
    }
}
