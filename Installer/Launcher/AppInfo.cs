using System.Reflection;

namespace AspectUtils.Installer;

internal static class AppInfo
{
    public const string ProductName = "A$PECT UTILS";
    public const string InstallFolderName = "A$PECT UTILS";
    public const string ExecutableName = "AspectUtils.exe";

    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return "2.1.1";
        }

        var build = Math.Max(version.Build, 0);
        return $"{version.Major}.{version.Minor}.{build}";
    }
}
