using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;

namespace AspectUtils.Desktop;

public interface IShellHost
{
    AppSession Session { get; }

    Task<OperationOutcome> RunOperationAsync(
        string title,
        string description,
        Func<IProgress<string>, CancellationToken, Task<OperationOutcome>> work);

    bool Confirm(string title, string message, string confirmText, bool danger = false);

    void Navigate(string pageKey);

    void ShowMessage(string title, string message);
}
