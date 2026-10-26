using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.Services;

public sealed class FeatureService
{
    private readonly LogService _log;

    public FeatureService(LogService log)
    {
        _log = log;
    }

    public async Task<string?> GetStateAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var escaped = featureName.Replace("'", "''");
        var script = $@"
$feature = Get-WindowsOptionalFeature -Online -FeatureName '{escaped}' -ErrorAction Stop
Write-Output $feature.State
";
        var result = await PowerShellRunner.RunScriptAsync(script, null, cancellationToken);
        if (result.ExitCode != 0)
        {
            _log.Warn($"Could not read the state of Windows feature {featureName}.");
            return null;
        }

        return result.Output.Trim();
    }

    public async Task<bool> SetEnabledAsync(
        IEnumerable<string> featureNames,
        bool enabled,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var features = featureNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        if (features.Count == 0)
        {
            return true;
        }

        var commands = features.Select(name =>
        {
            var escaped = name.Replace("'", "''");
            return enabled
                ? $@"try {{
    Write-Host 'Enabling {escaped}...'
    Enable-WindowsOptionalFeature -Online -FeatureName '{escaped}' -All -NoRestart -ErrorAction Stop | Out-Null
}} catch {{
    Write-Error $_.Exception.Message
    $script:failed = $true
}}"
                : $@"try {{
    Write-Host 'Disabling {escaped}...'
    Disable-WindowsOptionalFeature -Online -FeatureName '{escaped}' -NoRestart -ErrorAction Stop | Out-Null
}} catch {{
    Write-Error $_.Exception.Message
    $script:failed = $true
}}";
        });

        var script = "$failed = $false" + Environment.NewLine +
                     string.Join(Environment.NewLine, commands) + Environment.NewLine +
                     "if ($failed) { exit 1 }" + Environment.NewLine +
                     "Write-Host 'Windows feature changes complete.'";

        var result = await PowerShellRunner.RunScriptAsync(script, output, cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> RestoreStateAsync(
        FeatureSnapshot snapshot,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(snapshot.State))
        {
            _log.Warn($"No original state recorded for Windows feature {snapshot.Name}.");
            return false;
        }

        var shouldEnable = snapshot.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
        var shouldDisable = snapshot.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
        if (!shouldEnable && !shouldDisable)
        {
            _log.Warn($"Windows feature {snapshot.Name} was '{snapshot.State}' before the tweak; leaving it alone.");
            return false;
        }

        return await SetEnabledAsync(new[] { snapshot.Name }, shouldEnable, output, cancellationToken);
    }
}
