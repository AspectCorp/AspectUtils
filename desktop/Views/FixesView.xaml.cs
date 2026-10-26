using System.IO;
using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;

namespace AspectUtils.Desktop.Views;

public partial class FixesView : UserControl, IPageView
{
    private readonly IShellHost _shell;
    private readonly List<FixAction> _fixes;

    public FixesView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;

        _fixes = new List<FixAction>
        {
            new()
            {
                Key = "network",
                Title = "Reset the network stack",
                Description = "Resets Winsock and TCP/IP, then flushes the DNS cache. Use this when connections fail but the adapter looks healthy. A restart is recommended.",
                ScriptFile = "fix-network.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "This resets Winsock and the TCP/IP stack. Network connections drop while it runs, and Windows should be restarted afterwards."
            },
            new()
            {
                Key = "ntp",
                Title = "Repair time synchronization",
                Description = "Starts the Windows Time service, points it at pool.ntp.org, and resynchronizes the clock. Useful when certificates or sign-ins fail because the clock drifted.",
                ScriptFile = "fix-ntp.ps1"
            },
            new()
            {
                Key = "system-repair",
                Title = "Scan and repair system files",
                Description = "Runs CHKDSK, SFC, and DISM restore-health in sequence. This is the longest fix and can take 15–30 minutes.",
                ScriptFile = "fix-system-repair.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "This runs CHKDSK, SFC, and DISM. It can take 15–30 minutes and must not be interrupted."
            },
            new()
            {
                Key = "winget",
                Title = "Repair WinGet",
                Description = "Installs the NuGet provider and Microsoft.WinGet.Client module, then repairs WinGet for all users. Use this when installs fail with a package manager error.",
                ScriptFile = "fix-winget.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "This installs PowerShell modules from the PowerShell Gallery and repairs WinGet for all users. It requires internet access."
            },
            new()
            {
                Key = "update-reset",
                Title = "Reset Windows Update",
                Description = "Stops update services, clears queued downloads, re-registers update libraries, removes stale policies, and triggers a new detection. A restart is recommended.",
                ScriptFile = "fix-update-reset.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "This clears downloaded updates, re-registers system libraries, and resets update policies. Windows Update will download everything again, and a restart is recommended."
            }
        };

        FixesList.ItemsSource = _fixes;
    }

    public void OnActivated()
    {
    }

    private async void RunFix_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FixAction fix })
        {
            return;
        }

        var scriptPath = Path.Combine(_shell.Session.ScriptsDirectory, fix.ScriptFile);
        if (!File.Exists(scriptPath))
        {
            _shell.ShowMessage(
                BrandInfo.Name,
                $"The fix script is missing:{Environment.NewLine}{scriptPath}{Environment.NewLine}{Environment.NewLine}" +
                "Rebuild or reinstall the app so the Scripts folder is present.");
            return;
        }

        if (fix.RequiresConfirmation &&
            !_shell.Confirm(fix.Title, fix.ConfirmationText, "Run fix", danger: true))
        {
            return;
        }

        await _shell.RunOperationAsync(
            fix.Title,
            "Running the repair script. Keep this window open.",
            async (progress, token) =>
            {
                var script = await File.ReadAllTextAsync(scriptPath, token);
                var result = await PowerShellRunner.RunScriptAsync(script, progress.Report, token);

                return result.ExitCode == 0
                    ? OperationOutcome.Ok("Repair finished. Restart if the script recommended it.")
                    : OperationOutcome.Fail($"The repair script exited with code {result.ExitCode}.");
            });
    }
}
