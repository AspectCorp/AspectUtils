$ErrorActionPreference = 'Continue'

Write-Host 'Repairing Windows Update...'

Write-Host 'Stopping update services...'
foreach ($service in @('BITS', 'wuauserv', 'appidsvc', 'cryptsvc')) {
    Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
}

Write-Host 'Removing queued BITS jobs...'
Remove-Item "$env:allusersprofile\Application Data\Microsoft\Network\Downloader\qmgr*.dat" -ErrorAction SilentlyContinue
Get-BitsTransfer -ErrorAction SilentlyContinue | Remove-BitsTransfer -ErrorAction SilentlyContinue

Write-Host 'Resetting the Windows Update download folder...'
Rename-Item "$env:systemroot\SoftwareDistribution\Download" Download.bak -ErrorAction SilentlyContinue
Remove-Item "$env:systemroot\WindowsUpdate.log" -ErrorAction SilentlyContinue

Write-Host 'Re-registering Windows Update libraries...'
$dlls = @(
    'atl.dll', 'urlmon.dll', 'mshtml.dll', 'shdocvw.dll', 'browseui.dll',
    'jscript.dll', 'vbscript.dll', 'scrrun.dll', 'msxml.dll', 'msxml3.dll',
    'msxml6.dll', 'actxprxy.dll', 'softpub.dll', 'wintrust.dll', 'dssenh.dll',
    'rsaenh.dll', 'gpkcsp.dll', 'sccbase.dll', 'slbcsp.dll', 'cryptdlg.dll',
    'oleaut32.dll', 'ole32.dll', 'shell32.dll', 'initpki.dll', 'wuapi.dll',
    'wuaueng.dll', 'wuaueng1.dll', 'wucltui.dll', 'wups.dll', 'wups2.dll',
    'wuweb.dll', 'qmgr.dll', 'qmgrprxy.dll', 'wucltux.dll', 'muweb.dll', 'wuwebv.dll'
)
Push-Location "$env:systemroot\system32"
try {
    foreach ($dll in $dlls) {
        Start-Process -NoNewWindow -FilePath 'regsvr32.exe' -ArgumentList '/s', $dll -Wait
    }
} finally {
    Pop-Location
}

Write-Host 'Clearing WSUS client settings...'
$wsus = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate'
foreach ($name in @('AccountDomainSid', 'PingID', 'SusClientId')) {
    Remove-ItemProperty -Path $wsus -Name $name -ErrorAction SilentlyContinue
}

Write-Host 'Removing Windows Update policies...'
$policyValues = @(
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'ExcludeWUDriversInQualityUpdate' },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'; Name = 'PreventDeviceMetadataFromNetwork' },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'; Name = 'DontPromptForWindowsUpdate' },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'; Name = 'DontSearchWindowsUpdate' },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'; Name = 'DriverUpdateWizardWuSearchEnabled' },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Name = 'NoAutoRebootWithLoggedOnUsers' },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Name = 'AUPowerManagement' },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Name = 'BranchReadinessLevel' },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Name = 'DeferFeatureUpdatesPeriodInDays' },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Name = 'DeferQualityUpdatesPeriodInDays' }
)
foreach ($value in $policyValues) {
    Remove-ItemProperty -Path $value.Path -Name $value.Name -ErrorAction SilentlyContinue
}

Write-Host 'Resetting network components used by Windows Update...'
Start-Process -NoNewWindow -FilePath 'netsh' -ArgumentList 'winsock', 'reset' -Wait
Start-Process -NoNewWindow -FilePath 'netsh' -ArgumentList 'winhttp', 'reset', 'proxy' -Wait
Start-Process -NoNewWindow -FilePath 'netsh' -ArgumentList 'int', 'ip', 'reset' -Wait

Write-Host 'Restoring update services...'
foreach ($service in @('BITS', 'wuauserv', 'CryptSvc')) {
    Set-Service -Name $service -StartupType Manual -ErrorAction SilentlyContinue
    Start-Service -Name $service -ErrorAction SilentlyContinue
}
Set-Service -Name AppIDSvc -StartupType Manual -ErrorAction SilentlyContinue
Start-Service -Name AppIDSvc -ErrorAction SilentlyContinue

Write-Host 'Triggering a Windows Update detection...'
try {
    (New-Object -ComObject Microsoft.Update.AutoUpdate).DetectNow()
} catch {
    Write-Warning "Could not trigger Windows Update detection: $($_.Exception.Message)"
}
Start-Process -NoNewWindow -FilePath 'wuauclt' -ArgumentList '/resetauthorization', '/detectnow' -Wait

Write-Host 'Windows Update reset complete. Restart Windows to finish.'
