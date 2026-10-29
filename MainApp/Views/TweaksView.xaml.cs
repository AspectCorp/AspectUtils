using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;
using AspectUtils.Desktop.ViewModels;

namespace AspectUtils.Desktop.Views;

public partial class TweaksView : UserControl, IPageView
{
    private const string AllCategories = "All categories";

    private readonly IShellHost _shell;
    private readonly List<TweakRow> _rows = new();
    private readonly List<TweakActionRow> _actionRows = new();
    private readonly ObservableCollection<TweakRow> _visibleRows = new();
    private bool _initialized;

    public TweaksView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;

        foreach (var tweak in _shell.Session.ToggleTweaks)
        {
            var row = new TweakRow(tweak);
            row.PropertyChanged += Row_PropertyChanged;
            _rows.Add(row);
        }

        foreach (var tweak in _shell.Session.ActionTweaks)
        {
            _actionRows.Add(new TweakActionRow(tweak));
        }

        TweaksList.ItemsSource = _visibleRows;
        ActionsList.ItemsSource = _actionRows;

        CategoryBox.Items.Add(AllCategories);
        foreach (var category in _rows.Select(row => row.Category)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(category => category, StringComparer.OrdinalIgnoreCase))
        {
            CategoryBox.Items.Add(category);
        }

        CategoryBox.SelectedIndex = 0;
        _initialized = true;
        ApplyFilter();
        RefreshState();
    }

    public void OnActivated()
    {
        RefreshState();
        ApplyFilter();
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TweakRow.IsSelected))
        {
            UpdateSelectionState();
        }
    }

    private void RefreshState()
    {
        var appliedKeys = new HashSet<string>(
            _shell.Session.Undo.All.Select(record => record.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in _rows)
        {
            row.IsApplied = appliedKeys.Contains(row.Definition.Key);
            row.CanUndo = row.IsApplied || HasUndoData(row.Definition);
        }

        var tracked = _shell.Session.Undo.All.Count;
        AppliedSummary.Text = tracked == 0
            ? "No changes recorded by this app yet."
            : $"{tracked} change {(tracked == 1 ? "record" : "records")} stored for undo.";
        UndoAllButton.IsEnabled = tracked > 0;
        UndoAllButton.Content = tracked == 0 ? "Undo all tracked changes" : $"Undo all tracked changes ({tracked})";
        UndoAllButton.ToolTip = _shell.Session.Undo.StorePath;

        UpdateSelectionState();
    }

    private static bool HasUndoData(TweakDefinition tweak) =>
        AppSession.HasScript(tweak.UndoScript) ||
        tweak.Registry?.Any(change => change.OriginalValue is not null) == true ||
        tweak.Service?.Any(service => !string.IsNullOrWhiteSpace(service.OriginalType)) == true;

    private void UpdateSelectionState()
    {
        if (!_initialized)
        {
            return;
        }

        var selected = _rows.Where(row => row.IsSelected).ToList();
        var applicable = selected.Count(row => !row.IsApplied);
        var undoable = selected.Count(row => row.CanUndo);

        ApplyButton.IsEnabled = applicable > 0;
        ApplyButton.Content = applicable == 0
            ? "Apply selected"
            : applicable == 1 ? "Apply 1 tweak" : $"Apply {applicable} tweaks";

        UndoButton.IsEnabled = undoable > 0;
        UndoButton.Content = undoable == 0
            ? "Undo selected"
            : undoable == 1 ? "Undo 1 tweak" : $"Undo {undoable} tweaks";

        SelectionText.Text = selected.Count == 0
            ? $"{_visibleRows.Count} of {_rows.Count} tweaks shown"
            : $"{selected.Count} selected · {_visibleRows.Count} of {_rows.Count} tweaks shown";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_initialized)
        {
            ApplyFilter();
        }
    }

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialized)
        {
            ApplyFilter();
        }
    }

    private void AppliedOnlyBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        var search = SearchBox.Text.Trim();
        var category = CategoryBox.SelectedItem as string ?? AllCategories;
        var appliedOnly = AppliedOnlyBox.IsChecked == true;

        _visibleRows.Clear();
        foreach (var row in _rows)
        {
            if (appliedOnly && !row.IsApplied)
            {
                continue;
            }

            if (!category.Equals(AllCategories, StringComparison.OrdinalIgnoreCase) &&
                !category.Equals(row.Category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (search.Length > 0 &&
                !row.Name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                !(row.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) &&
                !(row.SupportText?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            _visibleRows.Add(row);
        }

        EmptyText.Visibility = _visibleRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateSelectionState();
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        var rows = _rows.Where(row => row.IsSelected && !row.IsApplied).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        string preview = string.Join(Environment.NewLine, rows.Take(10).Select(row => $"• {row.Name}"));
        if (rows.Count > 10)
        {
            preview += $"{Environment.NewLine}…and {rows.Count - 10} more.";
        }

        if (!_shell.Confirm(
                "Apply tweaks?",
                $"Apply {rows.Count} selected {(rows.Count == 1 ? "tweak" : "tweaks")}? Windows settings and services will change. " +
                $"You can undo these from this page." +
                $"{Environment.NewLine}{Environment.NewLine}{preview}",
                "Apply tweaks"))
        {
            return;
        }

        var tweaks = rows.Select(row => row.Definition).ToList();
        var outcome = await _shell.RunOperationAsync(
            "Applying tweaks",
            $"Applying {tweaks.Count} {(tweaks.Count == 1 ? "tweak" : "tweaks")}.",
            async (progress, token) =>
            {
                var result = await _shell.Session.Tweaks.ApplyAsync(tweaks, progress.Report, token);
                if (result.Failed == 0)
                {
                    return OperationOutcome.Ok($"{result.Applied} of {tweaks.Count} tweaks applied.");
                }

                return OperationOutcome.Fail(
                    $"{result.Applied} applied, {result.Failed} failed. {string.Join(" ", result.Errors.Take(5))}");
            });

        if (outcome.Success)
        {
            foreach (var row in rows)
            {
                row.IsSelected = false;
            }
        }

        RefreshState();
        ApplyFilter();
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        var rows = _rows.Where(row => row.IsSelected && row.CanUndo).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        string preview = string.Join(Environment.NewLine, rows.Take(10).Select(row => $"• {row.Name}"));
        if (rows.Count > 10)
        {
            preview += $"{Environment.NewLine}…and {rows.Count - 10} more.";
        }

        if (!_shell.Confirm(
                "Undo tweaks?",
                $"Restore the original values for {rows.Count} selected {(rows.Count == 1 ? "tweak" : "tweaks")}? " +
                "Snapshots captured by this app are used first; otherwise the original values in the shared config are restored." +
                $"{Environment.NewLine}{Environment.NewLine}{preview}",
                "Undo tweaks"))
        {
            return;
        }

        var tweaks = rows.Select(row => row.Definition).ToList();
        var outcome = await _shell.RunOperationAsync(
            "Undoing tweaks",
            $"Restoring {tweaks.Count} {(tweaks.Count == 1 ? "tweak" : "tweaks")}.",
            async (progress, token) =>
            {
                var result = await _shell.Session.Tweaks.UndoAsync(tweaks, progress.Report, token);
                if (result.Failed == 0)
                {
                    return OperationOutcome.Ok($"{result.Applied} of {tweaks.Count} tweaks undone.");
                }

                return OperationOutcome.Fail(
                    $"{result.Applied} undone, {result.Failed} failed. {string.Join(" ", result.Errors.Take(5))}");
            });

        if (outcome.Success)
        {
            foreach (var row in rows)
            {
                row.IsSelected = false;
            }
        }

        RefreshState();
        ApplyFilter();
    }

    private async void UndoAll_Click(object sender, RoutedEventArgs e)
    {
        var tracked = _shell.Session.Undo.All.Count;
        if (tracked == 0)
        {
            _shell.ShowMessage(BrandInfo.Name, "There are no changes recorded by this app to undo.");
            return;
        }

        if (!_shell.Confirm(
                "Undo all tracked changes?",
                $"Restore the original values for all {tracked} recorded {(tracked == 1 ? "change" : "changes")}? " +
                "This includes tweaks you did not select.",
                "Undo everything",
                danger: true))
        {
            return;
        }

        var outcome = await _shell.RunOperationAsync(
            "Undoing all tracked changes",
            $"Restoring {tracked} recorded {(tracked == 1 ? "change" : "changes")}.",
            async (progress, token) =>
            {
                var result = await _shell.Session.Tweaks.UndoAllAsync(progress.Report, token);
                if (result.Failed == 0)
                {
                    return OperationOutcome.Ok($"{result.Applied} of {tracked} changes undone.");
                }

                return OperationOutcome.Fail(
                    $"{result.Applied} undone, {result.Failed} failed. {string.Join(" ", result.Errors.Take(5))}");
            });

        RefreshState();
        ApplyFilter();
    }

    private void TweaksTab_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        TweaksPanel.Visibility = Visibility.Visible;
        ActionsPanel.Visibility = Visibility.Collapsed;
    }

    private void ActionsTab_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        TweaksPanel.Visibility = Visibility.Collapsed;
        ActionsPanel.Visibility = Visibility.Visible;
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
            _shell.ShowMessage(BrandInfo.Name, "This action does not have a script to run.");
            return;
        }

        if (!_shell.Confirm(
                "Run this action?",
                $"{definition.Content}{Environment.NewLine}{Environment.NewLine}" +
                "This runs immediately, is not recorded in the undo manifest, and cannot be reverted from A$PECT UTILS.",
                "Run action",
                danger: true))
        {
            return;
        }

        await _shell.RunOperationAsync(
            definition.Content,
            "Running a one-time action. This cannot be undone from the app.",
            async (progress, token) =>
            {
                var result = await PowerShellRunner.RunScriptAsync(
                    PowerShellCompat.Wrap(script),
                    progress.Report,
                    token);

                return result.ExitCode == 0
                    ? OperationOutcome.Ok("Action finished.")
                    : OperationOutcome.Fail($"The action exited with code {result.ExitCode}.");
            });
    }
}
