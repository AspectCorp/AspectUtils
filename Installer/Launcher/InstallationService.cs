using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;

namespace AspectUtils.Installer;

public sealed class InstallationService
{
    private const int CopyAttempts = 4;
    private const string EmbeddedPayloadResource = "AspectUtils.Payload.zip";

    private bool _payloadIsTemporary;

    public InstallationService(string? targetDirectory = null, string? payloadDirectory = null)
    {
        TargetDirectory = Normalize(targetDirectory) ?? ResolveTargetDirectory();
        PayloadDirectory = Normalize(payloadDirectory) ?? ResolvePayloadDirectory();
    }

    public string TargetDirectory { get; }

    public string PayloadDirectory { get; private set; }

    public bool PayloadAvailable =>
        File.Exists(Path.Combine(PayloadDirectory, AppInfo.ExecutableName)) || HasEmbeddedPayload;

    private static bool HasEmbeddedPayload =>
        typeof(InstallationService).Assembly.GetManifestResourceInfo(EmbeddedPayloadResource) is not null;

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static int CountRunningInstances(string targetDirectory)
    {
        var processes = FindRunningInstances(targetDirectory);

        try
        {
            return processes.Count;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    public async Task<InstallResult> InstallAsync(
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken,
        bool createDesktopShortcut = true,
        bool requireElevation = true,
        bool createShortcuts = true)
    {
        try
        {
            if (!PayloadAvailable)
            {
                return InstallResult.Fail(
                    "The installer payload is missing.",
                    $"Expected to find {AppInfo.ExecutableName} in '{PayloadDirectory}'. " +
                    "Rebuild the installer with Installer\\build.ps1 and keep the payload folder beside the setup executable.");
            }

            if (requireElevation && !IsAdministrator())
            {
                return InstallResult.Fail(
                    "Administrator rights are required.",
                    "Close the installer, start it again, and approve the Windows prompt.");
            }

            InstallLog.Write($"Installing {AppInfo.ProductName} {AppInfo.Version} to {TargetDirectory}.");
            progress.Report(new InstallProgress(2, "Preparing the installer…"));

            await PreparePayloadAsync(progress, cancellationToken).ConfigureAwait(false);

            progress.Report(new InstallProgress(6, "Checking the install folder…"));

            await Task.Run(() => EnsureWritable(TargetDirectory), cancellationToken).ConfigureAwait(false);

            progress.Report(new InstallProgress(7, "Closing A$PECT UTILS…"));
            var closed = await CloseRunningInstancesAsync(progress, cancellationToken).ConfigureAwait(false);
            if (!closed)
            {
                return InstallResult.Fail(
                    "A$PECT UTILS is still running.",
                    "Close it from Task Manager, then choose Try again.");
            }

            progress.Report(new InstallProgress(18, "Copying files…"));
            await CopyPayloadAsync(progress, cancellationToken).ConfigureAwait(false);

            progress.Report(new InstallProgress(88, "Creating shortcuts…"));
            var warnings = createShortcuts
                ? CreateShortcuts(createDesktopShortcut)
                : new List<string>();

            progress.Report(new InstallProgress(96, "Checking the result…"));
            var executablePath = Path.Combine(TargetDirectory, AppInfo.ExecutableName);
            if (!File.Exists(executablePath))
            {
                return InstallResult.Fail(
                    "The installation did not finish.",
                    $"Windows did not create '{executablePath}'. Check free disk space and try again.");
            }

            progress.Report(new InstallProgress(100, "Done."));
            InstallLog.Write("Installation completed.");
            CleanupTemporaryPayload();

            return warnings.Count == 0
                ? InstallResult.Ok("A$PECT UTILS is installed.")
                : InstallResult.Ok(
                    "A$PECT UTILS is installed.",
                    "Some shortcuts could not be created: " + string.Join(" ", warnings));
        }
        catch (OperationCanceledException)
        {
            InstallLog.Write("Installation cancelled.");
            throw;
        }
        catch (InstallPayloadException ex)
        {
            InstallLog.Write("Payload preparation failed: " + ex);
            return InstallResult.Fail(
                "The installer could not unpack its files.",
                "Windows could not write to the temporary folder. Check that the disk has free space, then try again. " + ex.InnerException?.Message);
        }
        catch (InstallFileException ex)
        {
            InstallLog.Write("File copy failed: " + ex);
            return InstallResult.Fail(
                $"Windows could not replace {Path.GetFileName(ex.FilePath)}.",
                $"The file is in use or protected. Close A$PECT UTILS and any Explorer window showing " +
                $"'{TargetDirectory}', then try again.");
        }
        catch (UnauthorizedAccessException ex)
        {
            InstallLog.Write("Access denied: " + ex);
            return InstallResult.Fail(
                "Windows denied access to the install folder.",
                "Approve the administrator prompt, or sign in with an account that can write to Program Files, then try again.");
        }
        catch (IOException ex)
        {
            InstallLog.Write("I/O failure: " + ex);
            return InstallResult.Fail(
                "The install folder is busy.",
                "Close A$PECT UTILS, check that the disk has free space, then try again.");
        }
        catch (Exception ex)
        {
            InstallLog.Write("Unexpected failure: " + ex);
            return InstallResult.Fail(
                "The installation stopped unexpectedly.",
                ex.Message);
        }
    }

    private async Task PreparePayloadAsync(
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        if (File.Exists(Path.Combine(PayloadDirectory, AppInfo.ExecutableName)))
        {
            return;
        }

        if (!HasEmbeddedPayload)
        {
            throw new InstallPayloadException(
                "The installer does not contain a payload.",
                new InvalidDataException("The embedded installer payload was not found."));
        }

        progress.Report(new InstallProgress(4, "Unpacking the installer…"));

        var payloadRoot = Path.Combine(
            Path.GetTempPath(),
            "AspectUtilsSetup",
            $"{AppInfo.Version}-{Environment.ProcessId}");

        try
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(payloadRoot))
                {
                    Directory.Delete(payloadRoot, recursive: true);
                }

                Directory.CreateDirectory(payloadRoot);

                using var stream = typeof(InstallationService).Assembly
                    .GetManifestResourceStream(EmbeddedPayloadResource)
                    ?? throw new InvalidDataException("The embedded installer payload could not be read.");

                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(payloadRoot, overwriteFiles: true);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            throw new InstallPayloadException(
                "The installer payload could not be unpacked.",
                ex);
        }

        var executablePath = Path.Combine(payloadRoot, AppInfo.ExecutableName);
        if (!File.Exists(executablePath))
        {
            throw new InstallPayloadException(
                "The installer payload is incomplete.",
                new FileNotFoundException($"Expected {AppInfo.ExecutableName} in the unpacked payload.", executablePath));
        }

        PayloadDirectory = payloadRoot;
        _payloadIsTemporary = true;
        InstallLog.Write("Unpacked the installer payload to " + payloadRoot);
    }

    private void CleanupTemporaryPayload()
    {
        if (!_payloadIsTemporary)
        {
            return;
        }

        try
        {
            if (Directory.Exists(PayloadDirectory))
            {
                Directory.Delete(PayloadDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            InstallLog.Write("Could not remove the temporary payload: " + ex.Message);
        }
    }

    private async Task<bool> CloseRunningInstancesAsync(
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        var processes = FindRunningInstances(TargetDirectory);
        if (processes.Count == 0)
        {
            return true;
        }

        InstallLog.Write($"Found {processes.Count} running instance(s); asking them to close.");

        try
        {
            foreach (var process in processes)
            {
                try
                {
                    if (process.HasExited)
                    {
                        continue;
                    }

                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        process.CloseMainWindow();
                    }
                    else
                    {
                        // Background copies have no window to close, so stop them directly.
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    InstallLog.Write($"Could not close PID {process.Id}: {ex.Message}");
                }
            }

            await WaitForExitAsync(processes, TimeSpan.FromSeconds(6), cancellationToken).ConfigureAwait(false);

            foreach (var process in processes.Where(process => !HasExited(process)))
            {
                try
                {
                    InstallLog.Write($"Force-closing PID {process.Id}.");
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    InstallLog.Write($"Could not force-close PID {process.Id}: {ex.Message}");
                }
            }

            await WaitForExitAsync(processes, TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            return processes.All(HasExited);
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static List<Process> FindRunningInstances(string targetDirectory)
    {
        var result = new List<Process>();
        var targetPrefix = NormalizeDirectoryPrefix(targetDirectory);

        Process[] candidates;
        try
        {
            candidates = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AppInfo.ExecutableName));
        }
        catch
        {
            return result;
        }

        foreach (var process in candidates)
        {
            try
            {
                if (process.Id == Environment.ProcessId)
                {
                    process.Dispose();
                    continue;
                }

                var path = TryGetProcessPath(process);
                if (path is null)
                {
                    // The name matches but Windows will not let us inspect the path.
                    // Keep it so a locked install is not missed.
                    result.Add(process);
                    continue;
                }

                var fullPath = Path.GetFullPath(path);
                if (fullPath.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(process);
                }
                else
                {
                    process.Dispose();
                }
            }
            catch
            {
                process.Dispose();
            }
        }

        return result;
    }

    private static string? TryGetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static async Task WaitForExitAsync(
        IReadOnlyCollection<Process> processes,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (processes.All(HasExited))
            {
                return;
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            // If Windows will not tell us, the copy step will surface a real lock.
            return true;
        }
    }

    private static void EnsureWritable(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException(
                $"Windows would not let the installer create '{directory}'. " +
                "Start the installer as administrator and try again.",
                ex);
        }

        var probe = Path.Combine(directory, $".aspectutils-write-{Guid.NewGuid():N}.tmp");

        try
        {
            using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.WriteByte(0);
        }
        finally
        {
            try
            {
                File.Delete(probe);
            }
            catch
            {
                // A leftover probe file is harmless.
            }
        }
    }

    private async Task CopyPayloadAsync(IProgress<InstallProgress> progress, CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(PayloadDirectory, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        if (files.Length == 0)
        {
            throw new InstallFileException(PayloadDirectory, new IOException("The payload folder is empty."));
        }

        var payloadRoot = PayloadDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetRoot = TargetDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var copied = 0;

        foreach (var source in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(payloadRoot, source);
            var destination = Path.Combine(targetRoot, relative);
            var destinationDirectory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            await CopyFileWithRetryAsync(source, destination, cancellationToken).ConfigureAwait(false);
            copied++;

            if (copied == files.Length || copied % 6 == 0)
            {
                var percent = 20 + (int)(66.0 * copied / files.Length);
                progress.Report(new InstallProgress(percent, $"Copying files… {copied} of {files.Length}"));
            }
        }
    }

    private static async Task CopyFileWithRetryAsync(
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= CopyAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                try
                {
                    if (File.Exists(destination))
                    {
                        File.SetAttributes(destination, FileAttributes.Normal);
                    }
                }
                catch
                {
                    // Let File.Copy report the real problem.
                }

                File.Copy(source, destination, overwrite: true);
                return;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && attempt < CopyAttempts)
            {
                InstallLog.Write($"Copy attempt {attempt} failed for '{destination}': {ex.Message}");
                await Task.Delay(250 * attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new InstallFileException(destination, ex);
            }
        }
    }

    private List<string> CreateShortcuts(bool createDesktopShortcut)
    {
        var warnings = new List<string>();
        var executablePath = Path.Combine(TargetDirectory, AppInfo.ExecutableName);

        try
        {
            var startMenu = ShortcutService.CreateStartMenuShortcut(executablePath, TargetDirectory);
            InstallLog.Write("Created Start menu shortcut: " + startMenu);
        }
        catch (Exception ex)
        {
            warnings.Add("Start menu (" + ex.Message + ")");
            InstallLog.Write("Start menu shortcut failed: " + ex);
        }

        if (createDesktopShortcut)
        {
            try
            {
                var desktop = ShortcutService.CreateDesktopShortcut(executablePath, TargetDirectory);
                InstallLog.Write("Created desktop shortcut: " + desktop);
            }
            catch (Exception ex)
            {
                warnings.Add("Desktop (" + ex.Message + ")");
                InstallLog.Write("Desktop shortcut failed: " + ex);
            }
        }

        return warnings;
    }

    private static string ResolveTargetDirectory()
    {
        var args = Environment.GetCommandLineArgs();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            if (argument.Equals("--target", StringComparison.OrdinalIgnoreCase) ||
                argument.Equals("-Target", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 < args.Length)
                {
                    var value = Normalize(args[index + 1]);
                    if (value is not null)
                    {
                        return value;
                    }
                }
            }

            if (argument.StartsWith("--target=", StringComparison.OrdinalIgnoreCase))
            {
                var value = Normalize(argument["--target=".Length..]);
                if (value is not null)
                {
                    return value;
                }
            }
        }

        var environmentOverride = Normalize(Environment.GetEnvironmentVariable("ASPECTUTILS_INSTALL_DIR"));
        if (environmentOverride is not null)
        {
            return environmentOverride;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            AppInfo.InstallFolderName);
    }

    private static string ResolvePayloadDirectory()
    {
        var candidates = new List<string>();
        var baseDirectory = AppContext.BaseDirectory;

        candidates.Add(Path.Combine(baseDirectory, "payload"));
        candidates.Add(Path.Combine(baseDirectory, "release-selfcontained"));

        var directory = new DirectoryInfo(baseDirectory);
        for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
        {
            candidates.Add(Path.Combine(directory.FullName, "payload"));
            candidates.Add(Path.Combine(directory.FullName, "release-selfcontained"));
            candidates.Add(Path.Combine(directory.FullName, "MainApp", "bin", "Release", "net8.0-windows"));
            candidates.Add(Path.Combine(directory.FullName, "MainApp", "bin", "Debug", "net8.0-windows"));
            candidates.Add(Path.Combine(directory.FullName, "Installer", "stage"));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(candidate, AppInfo.ExecutableName)))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path.Trim().Trim('"'));
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeDirectoryPrefix(string directory)
    {
        var normalized = Normalize(directory) ?? directory;
        return normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
               Path.DirectorySeparatorChar;
    }

    private sealed class InstallFileException : Exception
    {
        public InstallFileException(string filePath, Exception innerException)
            : base($"Could not replace '{filePath}'.", innerException)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }

    private sealed class InstallPayloadException : Exception
    {
        public InstallPayloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
