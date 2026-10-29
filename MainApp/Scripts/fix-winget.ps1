$ErrorActionPreference = 'Continue'

Write-Host 'Installing the NuGet package provider...'
Install-PackageProvider -Name NuGet -Force | Out-Null

Write-Host 'Installing the Microsoft.WinGet.Client module...'
Install-Module -Name Microsoft.WinGet.Client -Force -AllowClobber | Out-Null

Write-Host 'Repairing WinGet for all users...'
Repair-WinGetPackageManager -AllUsers

Write-Host 'WinGet repair complete. Open a new terminal before using winget again.'
