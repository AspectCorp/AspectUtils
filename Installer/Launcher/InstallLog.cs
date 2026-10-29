using System.IO;

namespace AspectUtils.Installer;

internal static class InstallLog
{
    private static readonly object Gate = new();

    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        AppInfo.InstallFolderName);

    public static string FilePath { get; } = Path.Combine(DirectoryPath, "install.log");

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}";

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(DirectoryPath);
                File.AppendAllText(FilePath, line);
            }
        }
        catch
        {
            // Logging must never prevent the installer from finishing.
        }
    }
}
