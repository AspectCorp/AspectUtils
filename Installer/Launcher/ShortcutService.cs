using System.IO;
using System.Runtime.InteropServices;

namespace AspectUtils.Installer;

internal static class ShortcutService
{
    public static string CreateStartMenuShortcut(string executablePath, string workingDirectory)
    {
        var programs = InstallationService.IsAdministrator()
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
            : Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var folder = Path.Combine(programs, AppInfo.InstallFolderName);
        Directory.CreateDirectory(folder);

        var linkPath = Path.Combine(folder, $"{AppInfo.ProductName}.lnk");
        CreateShortcut(linkPath, executablePath, workingDirectory);
        return linkPath;
    }

    public static string CreateDesktopShortcut(string executablePath, string workingDirectory)
    {
        var desktop = InstallationService.IsAdministrator()
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var linkPath = Path.Combine(desktop, $"{AppInfo.ProductName}.lnk");
        CreateShortcut(linkPath, executablePath, workingDirectory);
        return linkPath;
    }

    private static void CreateShortcut(string linkPath, string executablePath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows Script Host could not be started.");

            dynamic dynamicShell = shell;
            shortcut = dynamicShell.CreateShortcut(linkPath);

            dynamic link = shortcut;
            link.TargetPath = executablePath;
            link.WorkingDirectory = workingDirectory;
            link.IconLocation = $"{executablePath},0";
            link.Description = $"{AppInfo.ProductName} — Windows setup utility";
            link.Save();
        }
        finally
        {
            if (shortcut is not null)
            {
                _ = Marshal.FinalReleaseComObject(shortcut);
            }

            if (shell is not null)
            {
                _ = Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
