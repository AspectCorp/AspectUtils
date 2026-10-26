using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace AspectUtils.Desktop.Views;

public partial class LogView : UserControl, IPageView
{
    private readonly IShellHost _shell;
    private int _lineCount;

    public LogView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;

        LogPathBox.Text = _shell.Session.Log.LogPath;

        try
        {
            var existing = File.ReadAllText(_shell.Session.Log.LogPath);
            LogOutput.Text = existing;
            _lineCount = existing.Length == 0 ? 0 : existing.Split('\n').Length - 1;
        }
        catch (Exception exception)
        {
            _shell.Session.Log.Warn($"Could not read the existing log: {exception.Message}");
        }

        _shell.Session.Log.LineAdded += OnLogLine;
        UpdateLineCount();
        ScrollToEnd();
    }

    public void OnActivated() => ScrollToEnd();

    private void OnLogLine(string line)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action<string>(OnLogLine), line);
            return;
        }

        if (LogOutput.Text.Length > 200_000)
        {
            LogOutput.Text = LogOutput.Text[^100_000..];
        }

        LogOutput.AppendText(line + Environment.NewLine);
        _lineCount++;
        UpdateLineCount();
        ScrollToEnd();
    }

    private void UpdateLineCount() => LineCountText.Text = $"{_lineCount:N0} lines";

    private void ScrollToEnd() => LogOutput.ScrollToEnd();

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_shell.Session.Log.LogPath);
        }
        catch (Exception exception)
        {
            _shell.ShowMessage(BrandInfo.Name, $"Could not copy the log path.\n\n{exception.Message}");
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _shell.Session.Log.LogDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _shell.ShowMessage(BrandInfo.Name, $"Could not open the log folder.\n\n{exception.Message}");
        }
    }

    private void ClearView_Click(object sender, RoutedEventArgs e)
    {
        LogOutput.Clear();
        _lineCount = 0;
        UpdateLineCount();
    }
}
