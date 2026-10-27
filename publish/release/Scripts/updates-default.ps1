$ErrorActionPreference = 'Continue'

Write-Host 'Restoring Windows Update to the Windows default settings...'

$registryValues = @(
    @{
        Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
        Names = @('NoAutoUpdate', 'AUOptions', 'NoAutoRebootWithLoggedOnUsers', 'AUPowerManagement')
    },
    @{
        Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
        Names = @('ExcludeWUDriversInQualityUpdate', 'DeferFeatureUpdates', 'DeferFeatureUpdatesPeriodInDays', 'DeferQualityUpdates', 'DeferQualityUpdatesPeriodInDays')
    },
    @{
        Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'
        Names = @('BranchReadinessLevel', 'DeferFeatureUpdatesPeriodInDays', 'DeferQualityUpdatesPeriodInDays')
    },
    @{
        Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'
        Names = @('PreventDeviceMetadataFromNetwork')
    },
    @{
        Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'
        Names = @('DontPromptForWindowsUpdate', 'DontSearchWindowsUpdate', 'DriverUpdateWizardWuSearchEnabled')
    },
    @{
        Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config'
        Names = @('DODownloadMode')
    }
)

foreach ($registryEntry in $registryValues) {
    foreach ($valueName in $registryEntry.Names) {
        Remove-ItemProperty -Path $registryEntry.Path -Name $valueName -ErrorAction SilentlyContinue
    }
}

$explorerPolicyPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'
$settingsPageVisibility = (Get-ItemProperty -Path $explorerPolicyPath -Name 'SettingsPageVisibility' -ErrorAction SilentlyContinue).SettingsPageVisibility
if ($settingsPageVisibility -eq 'hide:windowsupdate') {
    Write-Host 'Removing the legacy Windows Update settings page restriction...'
    Remove-ItemProperty -Path $explorerPolicyPath -Name 'SettingsPageVisibility' -ErrorAction SilentlyContinue
}

Write-Host 'Restoring Windows Update services...'
Set-Service -Name BITS -StartupType Manual
Set-Service -Name wuauserv -StartupType Manual
Set-Service -Name UsoSvc -StartupType Automatic
Start-Service -Name UsoSvc -ErrorAction SilentlyContinue

Write-Host 'Enabling update-related scheduled tasks...'
$tasks = @(
    '\Microsoft\Windows\InstallService\*',
    '\Microsoft\Windows\UpdateOrchestrator\*',
    '\Microsoft\Windows\UpdateAssistant\*',
    '\Microsoft\Windows\WaaSMedic\*',
    '\Microsoft\Windows\WindowsUpdate\*',
    '\Microsoft\WindowsUpdate\*'
)

foreach ($task in $tasks) {
    Get-ScheduledTask -TaskPath $task -ErrorAction SilentlyContinue | Enable-ScheduledTask -ErrorAction SilentlyContinue
}

Write-Host 'Windows Update defaults restored. Restart Windows for every change to take effect.'
