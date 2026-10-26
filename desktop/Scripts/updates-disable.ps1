$ErrorActionPreference = 'Continue'

Write-Host 'Disabling Windows Update...'
Write-Host 'Security updates will not be installed until the default profile is restored.' -ForegroundColor Yellow

Write-Host 'Configuring registry policy values...'
if (-not (Test-Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU')) {
    New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Force | Out-Null
}

Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'NoAutoUpdate' -Type DWord -Value 1
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'AUOptions' -Type DWord -Value 1

if (-not (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config')) {
    New-Item -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config' -Force | Out-Null
}
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config' -Name 'DODownloadMode' -Type DWord -Value 0

foreach ($serviceName in @('BITS', 'wuauserv', 'UsoSvc')) {
    Write-Host "Stopping and disabling the $serviceName service..."
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Set-Service -Name $serviceName -StartupType Disabled
}

Write-Host 'Clearing the Windows Update download folder...'
Remove-Item -Path 'C:\Windows\SoftwareDistribution\*' -Recurse -Force -ErrorAction SilentlyContinue

Write-Host 'Disabling update-related scheduled tasks...'
$tasks = @(
    '\Microsoft\Windows\InstallService\*',
    '\Microsoft\Windows\UpdateOrchestrator\*',
    '\Microsoft\Windows\UpdateAssistant\*',
    '\Microsoft\Windows\WaaSMedic\*',
    '\Microsoft\Windows\WindowsUpdate\*',
    '\Microsoft\WindowsUpdate\*'
)

foreach ($task in $tasks) {
    Get-ScheduledTask -TaskPath $task -ErrorAction SilentlyContinue | Disable-ScheduledTask -ErrorAction SilentlyContinue
}

Write-Host 'Windows Update disabled. Restart Windows for every change to take effect.' -ForegroundColor Yellow
