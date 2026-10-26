using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Dialogs;
using AspectUtils.Desktop.Services;
using AspectUtils.Desktop.Views;

namespace AspectUtils.Desktop;

public partial class MainWindow : AspectUtils.Desktop.Theme.ThemedWindow, IShellHost
{
    private static readonly Dictionary<string, (string Title, string Subtitle)> Headers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dashboard"] = ("Dashboard", "A snapshot of this PC and the jobs A$PECT UTILS can do."),
        ["install"] = ("Install apps", "Pick from the WinGet and Chocolatey catalogue. Nothing installs until you confirm."),
        ["tweaks"] = ("Tweaks", "Apply reversible system changes, or roll back the ones applied by this app."),
        ["fixes"] = ("Fixes", "Repair common Windows problems with focused, auditable scripts."),
        ["features"] = ("Windows features", "Enable or remove optional Windows components. A restart is often required."),
        ["updates"] = ("Windows Update", "Choose how this PC receives updates. Settings apply to the whole machine."),
        ["quickres"] = ("Quickres", "Instant resolution switching for stretched res."),
        ["log"] = ("Activity log", "Every operation is written to a session log on this PC.")
    };

    private readonly Dictionary<string, UserControl> _pages = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _operationCts;
    private bool _operationRunning;

    public MainWindow()
    {
        InitializeComponent();

        if (!IsAdministrator())
        {
            MessageBox.Show(
                "A$PECT UTILS needs administrator rights to change Windows settings.\n\n" +
                "Close this window and start the app again, then approve the elevation prompt.",
                BrandInfo.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Application.Current.Shutdown();
            Session = null!;
            return;
        }

        try
        {
            Session = AppSession.Create();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"A$PECT UTILS could not start.\n\n{exception.Message}",
                BrandInfo.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            Session = null!;
            return;
        }

        VersionText.Text = $"v{BrandInfo.Version}";
        Session.Log.Info("Desktop shell ready.");
        NavList.SelectedIndex = 0;
    }

    public AppSession Session { get; }

    public async Task<OperationOutcome> RunOperationAsync(
        string title,
        string description,
        Func<IProgress<string>, CancellationToken, Task<OperationOutcome>> work)
    {
        if (_operationRunning)
        {
            return OperationOutcome.Fail("Another operation is still running. Wait for it to finish first.");
        }

        _operationRunning = true;
        _operationCts = new CancellationTokenSource();

        OperationTitle.Text = title;
        OperationDescription.Text = description;
        OperationOutput.Clear();
        OperationProgress.Visibility = Visibility.Visible;
        OperationCancelButton.Visibility = Visibility.Visible;
        OperationCancelButton.IsEnabled = true;
        OperationCloseButton.Visibility = Visibility.Collapsed;
        OperationOverlay.Visibility = Visibility.Visible;

        var progress = new Progress<string>(AppendOperationLine);
        OperationOutcome outcome;

        try
        {
            outcome = await work(progress, _operationCts.Token);
        }
        catch (OperationCanceledException)
        {
            outcome = OperationOutcome.Fail("Cancelled.");
        }
        catch (Exception exception)
        {
            Session.Log.Error($"{title} failed: {exception.Message}");
            outcome = OperationOutcome.Fail(exception.Message);
        }
        finally
        {
            _operationRunning = false;
            _operationCts.Dispose();
            _operationCts = null;
            OperationProgress.Visibility = Visibility.Collapsed;
            OperationCancelButton.Visibility = Visibility.Collapsed;
            OperationCloseButton.Visibility = Visibility.Visible;
        }

        AppendOperationLine(outcome.Success ? $"Done: {outcome.Summary}" : $"Stopped: {outcome.Summary}");
        OperationTitle.Text = outcome.Success ? title : $"{title} — finished with problems";
        Session.Log.Info($"{title}: {(outcome.Success ? "completed" : "incomplete")} — {outcome.Summary}");
        return outcome;
    }

    public bool Confirm(string title, string message, string confirmText, bool danger = false) =>
        ConfirmDialog.Show(this, title, message, confirmText, danger);

    public void Navigate(string pageKey)
    {
        foreach (var item in NavList.Items.OfType<ListBoxItem>())
        {
            if (!string.Equals(item.Tag as string, pageKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (item.IsSelected)
            {
                ShowPage(pageKey);
            }
            else
            {
                item.IsSelected = true;
            }

            return;
        }
    }

    public void ShowMessage(string title, string message) =>
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is ListBoxItem { Tag: string pageKey })
        {
            ShowPage(pageKey);
        }
    }

    private void ShowPage(string pageKey)
    {
        if (!Headers.TryGetValue(pageKey, out var header))
        {
            return;
        }

        PageTitle.Text = header.Title;
        PageSubtitle.Text = header.Subtitle;

        if (!_pages.TryGetValue(pageKey, out var page))
        {
            page = pageKey switch
            {
                "dashboard" => new DashboardView(this),
                "install" => new InstallView(this),
                "tweaks" => new TweaksView(this),
                "fixes" => new FixesView(this),
                "features" => new FeaturesView(this),
                "updates" => new UpdatesView(this),
                "quickres" => new QuickresView(this),
                "log" => new LogView(this),
                _ => throw new ArgumentOutOfRangeException(nameof(pageKey), pageKey, null)
            };

            _pages[pageKey] = page;
        }

        ContentHost.Content = page;
        (page as IPageView)?.OnActivated();
    }

    private void OperationCancelButton_Click(object sender, RoutedEventArgs e)
    {
        OperationCancelButton.IsEnabled = false;
        AppendOperationLine("Cancelling...");
        _operationCts?.Cancel();
    }

    private void OperationCloseButton_Click(object sender, RoutedEventArgs e)
    {
        OperationOverlay.Visibility = Visibility.Collapsed;
        OperationOutput.Clear();
    }

    private void AppendOperationLine(string line)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action<string>(AppendOperationLine), line);
            return;
        }

        if (OperationOutput.Text.Length > 200_000)
        {
            OperationOutput.Text = OperationOutput.Text[^100_000..];
        }

        OperationOutput.AppendText(line + Environment.NewLine);
        OperationOutput.ScrollToEnd();
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
