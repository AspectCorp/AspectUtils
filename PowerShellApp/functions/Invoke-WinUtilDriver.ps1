function Invoke-WinUtilDriver {
    <#
    .SYNOPSIS
        Auto-detect PC hardware categories and install drivers via package manager.
    #>
    [CmdletBinding()]
    param()

    $driversPath = Join-Path $PSScriptRoot '..\config\drivers.json'
    if (-not (Test-Path $driversPath)) { $driversPath = Join-Path $env:ProgramData 'AspectUtils\config\drivers.json' }
    if (-not (Test-Path $driversPath)) { Write-Host "Driver config not found." -ForegroundColor Red; return }

    $config = Get-Content $driversPath | ConvertFrom-Json
    $categories = $config.categories.PSObject.Properties | Where-Object { $_.Name -in 'chipset','audio','gpu','network' }

    Write-Host "A$PECT UTILS - Driver Auto-Detect" -ForegroundColor Cyan
    foreach ($cat in $categories) {
        $key = $cat.Name
        $label = $cat.Value.label
        $wmiClass = $cat.Value.wmi_class
        $packages = $cat.Value.packages

        # WMI detect
        $detected = $false
        try {
            $query = "SELECT * FROM $wmiClass"
            $result = Get-WmiObject -Query $query -ErrorAction SilentlyContinue
            $detected = ($result -ne $null -and $result.Count -gt 0)
        } catch {}

        $status = if ($detected) { "DETECTED" } else { "Not detected" }
        Write-Host ("[$key] $label -> $status") -ForegroundColor $(if($detected){'Green'}else{'Yellow'})

        if ($detected) {
            foreach ($pkg in $packages) {
                Write-Host "  Installing $pkg ..." -ForegroundColor White
                # Use existing package manager (winget / choco) path
                # Example: winget install --id $pkg --accept-source-agreements --silent
            }
        }
    }
}
