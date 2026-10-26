using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.ViewModels;

namespace AspectUtils.Desktop.Views;

public partial class InstallView : UserControl, IPageView
{
    private const string AllCategories = "All categories";

    private readonly IShellHost _shell;
    private readonly List<AppRow> _rows = new();
    private readonly ObservableCollection<AppRow> _visibleRows = new();
    private bool _initialized;

    public InstallView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;

        foreach (var app in _shell.Session.Config.Apps.Values
                     .OrderBy(app => app.Category, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(app => app.Content, StringComparer.OrdinalIgnoreCase))
        {
            var row = new AppRow(app);
            row.PropertyChanged += Row_PropertyChanged;
            _rows.Add(row);
        }

        AppsList.ItemsSource = _visibleRows;

        CategoryBox.Items.Add(AllCategories);
        foreach (var category in _rows.Select(row => row.Category)
                     .Where(category => !string.IsNullOrWhiteSpace(category))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(category => category, StringComparer.OrdinalIgnoreCase))
        {
            CategoryBox.Items.Add(category);
        }

        CategoryBox.SelectedIndex = 0;
        _initialized = true;
        ApplyFilter();
        UpdateSelectionState();
    }

    public void OnActivated() => UpdateSelectionState();

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppRow.IsSelected))
        {
            UpdateSelectionState();
        }
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

    private void ApplyFilter()
    {
        var search = SearchBox.Text.Trim();
        var category = CategoryBox.SelectedItem as string ?? AllCategories;

        _visibleRows.Clear();
        foreach (var row in _rows)
        {
            if (!category.Equals(AllCategories, StringComparison.OrdinalIgnoreCase) &&
                !category.Equals(row.Category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (search.Length > 0 &&
                !row.Name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                !(row.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) &&
                !row.PackageId.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _visibleRows.Add(row);
        }

        EmptyText.Visibility = _visibleRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _visibleRows)
        {
            row.IsSelected = true;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows)
        {
            row.IsSelected = false;
        }
    }

    private void UpdateSelectionState()
    {
        if (!_initialized)
        {
            return;
        }

        var count = _rows.Count(row => row.IsSelected);
        InstallButton.IsEnabled = count > 0;
        UninstallButton.IsEnabled = count > 0;
        InstallButton.Content = count == 1 ? "Install 1 app" : $"Install {count} apps";
        UninstallButton.Content = count == 1 ? "Uninstall 1 app" : $"Uninstall {count} apps";
        SelectionText.Text = count == 0
            ? $"{_visibleRows.Count} apps shown"
            : $"{count} selected · {_visibleRows.Count} apps shown";
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        var apps = _rows.Where(row => row.IsSelected).Select(row => row.Definition).ToList();
        if (apps.Count == 0)
        {
            return;
        }

        string preview = string.Join(Environment.NewLine, apps.Take(10).Select(app => $"• {app.Content}"));
        if (apps.Count > 10)
        {
            preview += $"{Environment.NewLine}…and {apps.Count - 10} more.";
        }

        if (!_shell.Confirm(
                "Install apps?",
                $"Install {apps.Count} selected {(apps.Count == 1 ? "app" : "apps")} with WinGet, falling back to Chocolatey where configured?" +
                $"{Environment.NewLine}{Environment.NewLine}{preview}",
                "Install"))
        {
            return;
        }

        var outcome = await _shell.RunOperationAsync(
            "Installing apps",
            $"Installing {apps.Count} {(apps.Count == 1 ? "app" : "apps")}. Keep this window open.",
            async (progress, token) =>
            {
                var result = await _shell.Session.Packages.InstallAsync(apps, progress.Report, token);
                if (result.Failed == 0)
                {
                    return OperationOutcome.Ok($"{result.Succeeded} of {apps.Count} apps installed.");
                }

                var details = string.Join(" ", result.Errors.Take(5));
                return OperationOutcome.Fail(
                    $"{result.Succeeded} installed, {result.Failed} failed. {details}");
            });

        if (outcome.Success)
        {
            foreach (var row in _rows)
            {
                row.IsSelected = false;
            }
        }
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        var apps = _rows.Where(row => row.IsSelected).Select(row => row.Definition).ToList();
        if (apps.Count == 0)
        {
            return;
        }

        string preview = string.Join(Environment.NewLine, apps.Take(10).Select(app => $"• {app.Content}"));
        if (apps.Count > 10)
        {
            preview += $"{Environment.NewLine}…and {apps.Count - 10} more.";
        }

        if (!_shell.Confirm(
                "Uninstall apps?",
                $"Remove {apps.Count} selected {(apps.Count == 1 ? "app" : "apps")}? The package manager may leave user data behind." +
                $"{Environment.NewLine}{Environment.NewLine}{preview}",
                "Uninstall",
                danger: true))
        {
            return;
        }

        var outcome = await _shell.RunOperationAsync(
            "Uninstalling apps",
            $"Removing {apps.Count} {(apps.Count == 1 ? "app" : "apps")}.",
            async (progress, token) =>
            {
                var result = await _shell.Session.Packages.UninstallAsync(apps, progress.Report, token);
                if (result.Failed == 0)
                {
                    return OperationOutcome.Ok($"{result.Succeeded} of {apps.Count} apps removed.");
                }

                var details = string.Join(" ", result.Errors.Take(5));
                return OperationOutcome.Fail(
                    $"{result.Succeeded} removed, {result.Failed} failed. {details}");
            });

        if (outcome.Success)
        {
            foreach (var row in _rows)
            {
                row.IsSelected = false;
            }
        }
    }
}
