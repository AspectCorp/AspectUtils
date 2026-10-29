using System.IO;
using System.Windows;
using System.Windows.Controls;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;
using Microsoft.Win32;

namespace AspectUtils.Desktop.Views;

public partial class UpdatesView : UserControl, IPageView
{
    private const string UpdatePolicyKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

    private readonly IShellHost _shell;
    private readonly List<UpdateProfile> _profiles;
    private bool _initialized;

    public UpdatesView(IShellHost shell)
    {
        InitializeComponent();
        _shell = shell;

        _profiles = new List<UpdateProfile>
        {
            new()
            {
                Key = "default",
                Title = "Windows default",
                Description = "Remove the update policies this app manages, restore update services and scheduled tasks, and let Windows Update behave the way Windows intends.",
                ScriptFile = "updates-default.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "Remove the Windows Update policies applied by A$PECT UTILS and restore Windows defaults? Windows Update will resume its normal schedule."
            },
            new()
            {
                Key = "recommended",
                Title = "Recommended",
                Description = "Keep security updates flowing, block driver updates, defer feature updates for 365 days and quality updates for 4 days, and download updates before asking to install.",
                ScriptFile = "updates-recommended.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "Apply the recommended policy? Security updates stay enabled; driver updates are blocked and feature/quality updates are deferred.",
                Recommended = true
            },
            new()
            {
                Key = "disable",
                Title = "Disable Windows Update",
                Description = "Turn off automatic updates, disable update services and scheduled tasks, and clear downloaded update files. Security updates stop until Windows default is restored. Not recommended.",
                ScriptFile = "updates-disable.ps1",
                RequiresConfirmation = true,
                ConfirmationText = "Disabling Windows Update stops update services, disables scheduled tasks, and clears downloaded updates. Security updates will not be installed until Windows default is restored. Continue?"
            }
        };

        ProfilesList.ItemsSource = _profiles;
        ProfilesList.SelectedIndex = 1;
        _initialized = true;
        RefreshPolicyText();
        UpdateApplyButton();
    }

    public void OnActivated() => RefreshPolicyText();

    private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialized)
        {
            UpdateApplyButton();
        }
    }

    private void UpdateApplyButton()
    {
        if (ProfilesList.SelectedItem is not UpdateProfile profile)
        {
            ApplyButton.IsEnabled = false;
            ApplyButton.Content = "Apply profile";
            return;
        }

        ApplyButton.IsEnabled = true;
        ApplyButton.Content = $"Apply {profile.Title.ToLowerInvariant()}";
    }

    private void RefreshPolicyText()
    {
        try
        {
            var noAutoUpdate = Registry.GetValue(UpdatePolicyKey + @"\AU", "NoAutoUpdate", null);
            if (Convert.ToInt32(noAutoUpdate ?? 0) == 1)
            {
                CurrentPolicyText.Text = "Automatic updates are disabled by policy (NoAutoUpdate = 1).";
                return;
            }

            var auOptions = Registry.GetValue(UpdatePolicyKey + @"\AU", "AUOptions", null);
            var featureDeferral = Registry.GetValue(UpdatePolicyKey, "DeferFeatureUpdatesPeriodInDays", null);
            var qualityDeferral = Registry.GetValue(UpdatePolicyKey, "DeferQualityUpdatesPeriodInDays", null);
            var driversBlocked = Registry.GetValue(UpdatePolicyKey, "ExcludeWUDriversInQualityUpdate", null);

            if (auOptions is null && featureDeferral is null && qualityDeferral is null && driversBlocked is null)
            {
                CurrentPolicyText.Text = "No A$PECT UTILS update policy is set. Windows defaults are in control.";
                return;
            }

            CurrentPolicyText.Text =
                $"AUOptions {auOptions ?? 0} · feature updates deferred {featureDeferral ?? 0} days · " +
                $"quality updates deferred {qualityDeferral ?? 0} days · driver updates " +
                $"{(Convert.ToInt32(driversBlocked ?? 0) == 1 ? "blocked" : "allowed")}.";
        }
        catch (Exception exception)
        {
            CurrentPolicyText.Text = $"Could not read the current policy: {exception.Message}";
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not UpdateProfile profile)
        {
            return;
        }

        var scriptPath = Path.Combine(_shell.Session.ScriptsDirectory, profile.ScriptFile);
        if (!File.Exists(scriptPath))
        {
            _shell.ShowMessage(
                BrandInfo.Name,
                $"The update script is missing:{Environment.NewLine}{scriptPath}{Environment.NewLine}{Environment.NewLine}" +
                "Rebuild or reinstall the app so the Scripts folder is present.");
            return;
        }

        if (profile.RequiresConfirmation &&
            !_shell.Confirm(
                profile.Title,
                profile.ConfirmationText,
                "Apply profile",
                danger: profile.Key == "disable"))
        {
            return;
        }

        var outcome = await _shell.RunOperationAsync(
            $"Windows Update: {profile.Title}",
            "Applying the selected update profile.",
            async (progress, token) =>
            {
                var script = await File.ReadAllTextAsync(scriptPath, token);
                var result = await PowerShellRunner.RunScriptAsync(script, progress.Report, token);

                return result.ExitCode == 0
                    ? OperationOutcome.Ok("Update profile applied. Restart Windows to finish.")
                    : OperationOutcome.Fail($"The update script exited with code {result.ExitCode}.");
            });

        RefreshPolicyText();
        if (!outcome.Success)
        {
            _shell.Session.Log.Warn($"Update profile {profile.Key} finished with problems: {outcome.Summary}");
        }
    }
}
