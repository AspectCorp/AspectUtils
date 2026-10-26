using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;
using AspectUtils.Desktop.ViewModels;

namespace AspectUtils.Desktop.Views;

public partial class FeaturesView : UserControl, IPageView
{
    private readonly IShellHost _shell;
    private readonly List<FeatureRow> _rows = new();
    private readonly List<FeatureActionRow> _actionRows = new();
    private bool _initialized;

    public FeaturesView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;

        foreach (var feature in _shell.Session.InstallableFeatures)
        {
            var row = new FeatureRow(feature);
            row.PropertyChanged += Row_PropertyChanged;
            _rows.Add(row);
        }

        foreach (var feature in _shell.Session.FeatureActions)
        {
            _actionRows.Add(new FeatureActionRow(feature));
        }

        FeaturesList.ItemsSource = _rows;
        ActionsList.ItemsSource = _actionRows;
        EmptyText.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        _initialized = true;
        UpdateSelectionState();
    }

    public void OnActivated()
    {
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FeatureRow.IsSelected))
        {
            UpdateSelectionState();
        }
    }

    private void UpdateSelectionState()
    {
        if (!_initialized)
        {
            return;
        }

        var count = _rows.Count(row => row.IsSelected);
        EnableButton.IsEnabled = count > 0;
        DisableButton.IsEnabled = count > 0;
        EnableButton.Content = count == 1 ? "Enable 1 feature" : $"Enable {count} features";
        DisableButton.Content = count == 1 ? "Disable 1 feature" : $"Disable {count} features";
        SelectionText.Text = count == 0
            ? $"{_rows.Count} features available"
            : $"{count} of {_rows.Count} selected";
    }

    private async void Enable_Click(object sender, RoutedEventArgs e) => await SetSelectedAsync(true);

    private async void Disable_Click(object sender, RoutedEventArgs e) => await SetSelectedAsync(false);

    private async Task SetSelectedAsync(bool enabled)
    {
        var rows = _rows.Where(row => row.IsSelected).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var names = rows
            .SelectMany(row => row.Definition.Feature ?? new List<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string preview = string.Join(Environment.NewLine, rows.Select(row => $"• {row.Name}"));
        string title = enabled ? "Enable Windows features?" : "Disable Windows features?";
        string message = enabled
            ? $"Enable {rows.Count} selected {(rows.Count == 1 ? "feature" : "features")}? Windows may need to download payloads and a restart is usually required." +
              $"{Environment.NewLine}{Environment.NewLine}{preview}"
            : $"Disable {rows.Count} selected {(rows.Count == 1 ? "feature" : "features")}? Apps that depend on them will stop working until they are enabled again." +
              $"{Environment.NewLine}{Environment.NewLine}{preview}";

        if (!_shell.Confirm(title, message, enabled ? "Enable features" : "Disable features", danger: !enabled))
        {
            return;
        }

        var outcome = await _shell.RunOperationAsync(
            enabled ? "Enabling Windows features" : "Disabling Windows features",
            $"Processing {names.Count} Windows {(names.Count == 1 ? "feature" : "features")}. This can take several minutes.",
            async (progress, token) =>
            {
                var succeeded = await _shell.Session.Features.SetEnabledAsync(names, enabled, progress.Report, token);
                return succeeded
                    ? OperationOutcome.Ok($"Windows features {(enabled ? "enabled" : "disabled")}. Restart Windows to finish.")
                    : OperationOutcome.Fail("One or more feature changes failed. Review the output above.");
            });

        if (outcome.Success)
        {
            foreach (var row in rows)
            {
                row.IsSelected = false;
            }
        }
    }

    private async void RunAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TweakDefinition definition })
        {
            return;
        }

        var script = string.Join(
            Environment.NewLine,
            definition.InvokeScript?.Where(line => !string.IsNullOrWhiteSpace(line)) ?? Array.Empty<string>());

        if (string.IsNullOrWhiteSpace(script))
        {
            _shell.ShowMessage(BrandInfo.Name, "This maintenance action does not have a script to run.");
            return;
        }

        if (!_shell.Confirm(
                "Run maintenance action?",
                definition.Content,
                "Run action",
                danger: true))
        {
            return;
        }

        await _shell.RunOperationAsync(
            definition.Content,
            "Running a Windows maintenance action.",
            async (progress, token) =>
            {
                var result = await PowerShellRunner.RunScriptAsync(
                    PowerShellCompat.Wrap(script),
                    progress.Report,
                    token);

                return result.ExitCode == 0
                    ? OperationOutcome.Ok("Maintenance action finished.")
                    : OperationOutcome.Fail($"The action exited with code {result.ExitCode}.");
            });
    }
}
