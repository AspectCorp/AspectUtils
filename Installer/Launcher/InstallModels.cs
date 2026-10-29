namespace AspectUtils.Installer;

public sealed record InstallProgress(int Percent, string Message);

public sealed record InstallResult(bool Success, string Message, string? Detail = null)
{
    public static InstallResult Ok(string message, string? detail = null) => new(true, message, detail);

    public static InstallResult Fail(string message, string? detail = null) => new(false, message, detail);
}
