using System.Globalization;
using System.Text.RegularExpressions;

namespace AspectUtils.Desktop.Services;

public sealed partial class ServiceManager
{
    private readonly LogService _log;

    public ServiceManager(LogService log)
    {
        _log = log;
    }

    public async Task<string?> QueryStartupTypeAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = await PowerShellRunner.RunAsync("sc.exe", new[] { "qc", name }, null, cancellationToken);
        if (result.ExitCode != 0 ||
            result.Output.Contains("FAILED 1060", StringComparison.OrdinalIgnoreCase) ||
            result.Output.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var match = StartTypeRegex().Match(result.Output);
        if (!match.Success)
        {
            return null;
        }

        var code = int.Parse(match.Groups["code"].Value, CultureInfo.InvariantCulture);
        var delayed = result.Output.Contains("DELAYED", StringComparison.OrdinalIgnoreCase);
        return code switch
        {
            2 when delayed => "AutomaticDelayedStart",
            2 => "Automatic",
            3 => "Manual",
            4 => "Disabled",
            0 => "Boot",
            1 => "System",
            _ => null
        };
    }

    public async Task SetStartupTypeAsync(string name, string? startupType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(startupType))
        {
            _log.Warn($"No startup type supplied for service {name}.");
            return;
        }

        var startValue = NormalizeStartValue(startupType);
        if (startValue is null)
        {
            _log.Warn($"Unsupported startup type '{startupType}' for service {name}.");
            return;
        }

        var result = await PowerShellRunner.RunAsync(
            "sc.exe",
            new[] { "config", name, "start=", startValue },
            null,
            cancellationToken);

        if (result.ExitCode != 0)
        {
            _log.Warn($"Could not set service {name} to {startupType}: {result.Output.Trim()}");
            return;
        }

        _log.Info($"Set service {name} startup type to {startupType}.");
    }

    public static string? NormalizeStartValue(string? startupType) =>
        (startupType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "automatic" => "auto",
            "automaticdelayedstart" => "delayed-auto",
            "manual" => "demand",
            "disabled" => "disabled",
            "boot" => "boot",
            "system" => "system",
            _ => null
        };

    [GeneratedRegex(@"START_TYPE\s*:\s*(?<code>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex StartTypeRegex();
}
