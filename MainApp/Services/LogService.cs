using System.IO;
using System.Diagnostics;
using System.Text;

namespace AspectUtils.Desktop.Services;

public sealed class LogService
{
    private readonly object _gate = new();

    public LogService()
    {
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AspectUtils",
            "logs");
        Directory.CreateDirectory(LogDirectory);
        LogPath = Path.Combine(LogDirectory, $"aspectutils-desktop_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Info("A$PECT UTILS desktop started.");
    }

    public event Action<string>? LineAdded;

    public string LogDirectory { get; }
    public string LogPath { get; }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    public void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        Debug.WriteLine(line);
        lock (_gate)
        {
            try
            {
                File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Logging must never take the app down.
            }
        }

        LineAdded?.Invoke(line);
    }
}
