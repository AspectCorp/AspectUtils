using System.IO;
using System.Diagnostics;
using System.Text;

namespace AspectUtils.Desktop.Services;

public sealed class ProcessResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;

    public bool Succeeded => ExitCode == 0;
}

public static class PowerShellRunner
{
    public static async Task<ProcessResult> RunScriptAsync(
        string script,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var scriptDirectory = Path.Combine(Path.GetTempPath(), "AspectUtils");
        Directory.CreateDirectory(scriptDirectory);
        var scriptPath = Path.Combine(scriptDirectory, $"aspectutils-{Guid.NewGuid():N}.ps1");

        try
        {
            await File.WriteAllTextAsync(
                scriptPath,
                "\uFEFF" + script,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken);

            return await RunAsync(
                "powershell.exe",
                new[]
                {
                    "-NoLogo",
                    "-NoProfile",
                    "-NonInteractive",
                    "-ExecutionPolicy", "Bypass",
                    "-File", scriptPath
                },
                output,
                cancellationToken);
        }
        finally
        {
            try
            {
                File.Delete(scriptPath);
            }
            catch
            {
                // The temp file can outlive a hard-killed process; nothing to recover.
            }
        }
    }

    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        var captured = new StringBuilder();

        void Capture(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            lock (captured)
            {
                captured.AppendLine(line);
            }

            output?.Invoke(line);
        }

        process.OutputDataReceived += (_, args) => Capture(args.Data);
        process.ErrorDataReceived += (_, args) => Capture(args.Data);

        if (!process.Start())
        {
            return new ProcessResult { ExitCode = -1, Output = $"Could not start {fileName}." };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Already exited.
            }

            throw;
        }

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = captured.ToString()
        };
    }

    public static async Task<bool> CommandExistsAsync(string command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await RunAsync("where.exe", new[] { command }, null, cancellationToken);
            return result.Succeeded;
        }
        catch
        {
            return false;
        }
    }
}
