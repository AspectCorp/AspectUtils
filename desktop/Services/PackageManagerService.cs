using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.Services;

public sealed class PackageRunResult
{
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; } = new();
}

public sealed class PackageManagerService
{
    private readonly LogService _log;

    public PackageManagerService(LogService log)
    {
        _log = log;
    }

    public async Task<PackageRunResult> InstallAsync(
        IReadOnlyList<AppDefinition> apps,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var result = new PackageRunResult();
        var hasWinget = await PowerShellRunner.CommandExistsAsync("winget", cancellationToken);
        var hasChoco = await PowerShellRunner.CommandExistsAsync("choco", cancellationToken);

        if (!hasWinget && !hasChoco)
        {
            result.Failed = apps.Count;
            result.Errors.Add("Neither WinGet nor Chocolatey is available. Install App Installer from the Microsoft Store.");
            _log.Error(result.Errors[0]);
            return result;
        }

        for (var index = 0; index < apps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var app = apps[index];
            output?.Invoke($"[{index + 1}/{apps.Count}] Installing {app.Content}...");
            _log.Info($"Installing {app.Content} ({app.PackageId}).");

            var installed = false;
            if (hasWinget && !string.IsNullOrWhiteSpace(app.Winget))
            {
                installed = await RunWingetAsync("install", app.Winget!, output, cancellationToken);
            }

            if (!installed && hasChoco && !string.IsNullOrWhiteSpace(app.Choco))
            {
                output?.Invoke($"WinGet did not complete {app.Content}; trying Chocolatey...");
                installed = await RunChocoAsync("install", app.Choco!, output, cancellationToken);
            }

            if (installed)
            {
                result.Succeeded++;
                _log.Info($"Installed {app.Content}.");
            }
            else
            {
                result.Failed++;
                result.Errors.Add($"{app.Content} could not be installed.");
                _log.Error($"Could not install {app.Content}.");
            }
        }

        return result;
    }

    public async Task<PackageRunResult> UninstallAsync(
        IReadOnlyList<AppDefinition> apps,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var result = new PackageRunResult();
        var hasWinget = await PowerShellRunner.CommandExistsAsync("winget", cancellationToken);
        var hasChoco = await PowerShellRunner.CommandExistsAsync("choco", cancellationToken);

        for (var index = 0; index < apps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var app = apps[index];
            output?.Invoke($"[{index + 1}/{apps.Count}] Uninstalling {app.Content}...");
            _log.Info($"Uninstalling {app.Content} ({app.PackageId}).");

            var removed = false;
            if (hasWinget && !string.IsNullOrWhiteSpace(app.Winget))
            {
                removed = await RunWingetAsync("uninstall", app.Winget!, output, cancellationToken);
            }

            if (!removed && hasChoco && !string.IsNullOrWhiteSpace(app.Choco))
            {
                removed = await RunChocoAsync("uninstall", app.Choco!, output, cancellationToken);
            }

            if (removed)
            {
                result.Succeeded++;
            }
            else
            {
                result.Failed++;
                result.Errors.Add($"{app.Content} could not be uninstalled.");
                _log.Warn($"Could not uninstall {app.Content}.");
            }
        }

        return result;
    }

    private async Task<bool> RunWingetAsync(
        string verb,
        string packageId,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { verb, "--id", packageId, "--exact" };
        if (verb == "install")
        {
            arguments.Add("--silent");
            arguments.Add("--accept-package-agreements");
        }

        arguments.Add("--accept-source-agreements");
        arguments.Add("--disable-interactivity");

        var result = await PowerShellRunner.RunAsync("winget", arguments, output, cancellationToken);
        if (result.ExitCode == 0)
        {
            return true;
        }

        if (result.Output.Contains("already installed", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("No installed package found", StringComparison.OrdinalIgnoreCase))
        {
            output?.Invoke("WinGet reports there is nothing to change for this package.");
            return true;
        }

        _log.Warn($"WinGet {verb} for {packageId} exited with code {result.ExitCode}.");
        return false;
    }

    private async Task<bool> RunChocoAsync(
        string verb,
        string packageId,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        var result = await PowerShellRunner.RunAsync(
            "choco",
            new[] { verb, packageId, "-y", "--no-progress" },
            output,
            cancellationToken);

        if (result.ExitCode == 0)
        {
            return true;
        }

        _log.Warn($"Chocolatey {verb} for {packageId} exited with code {result.ExitCode}.");
        return false;
    }
}
