$ErrorActionPreference = 'Continue'

Write-Host 'Starting the Windows Time service...'
Start-Service w32time -ErrorAction SilentlyContinue

Write-Host 'Pointing Windows Time at pool.ntp.org...'
w32tm /config /update /manualpeerlist:"pool.ntp.org,0x8" /syncfromflags:MANUAL | Out-Host

Restart-Service w32time -ErrorAction SilentlyContinue
Write-Host 'Resynchronizing the clock...'
w32tm /resync | Out-Host

Write-Host 'NTP configuration complete.'
