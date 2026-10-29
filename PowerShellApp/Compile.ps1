param (
    [switch]$Run
)

$ErrorActionPreference = 'Stop'
$OFS = "`r`n"

$sourceRoot = $PSScriptRoot

# Variable to sync between runspaces
$sync = [Hashtable]::Synchronized(@{})
$sync.configs = @{}

$script = (Get-Content -Path (Join-Path $sourceRoot 'scripts\start.ps1')) -replace '#{replaceme}', (Get-Date -Format 'yy.MM.dd')
$isLocalCompile = -not [string]::Equals($env:GITHUB_ACTIONS, "true", [StringComparison]::OrdinalIgnoreCase)
$script = $script -replace '#{islocalcompile}', $isLocalCompile.ToString().ToLowerInvariant()

$script += Get-ChildItem -Path (Join-Path $sourceRoot 'functions') -Recurse -File | ForEach-Object {
    Get-Content -Path $_.FullName -Raw
}

Get-ChildItem (Join-Path $sourceRoot 'config') -File | ForEach-Object {
    $obj = Get-Content -Path $_.FullName -Raw | ConvertFrom-Json

    if ($_.Name -eq "applications.json") {
        $fixed = [ordered]@{}
        foreach ($p in $obj.PSObject.Properties) {
            $fixed["WPFInstall$($p.Name)"] = $p.Value
        }
        $obj = [pscustomobject]$fixed
    }

    $json = $obj | ConvertTo-Json -Depth 10

    $sync.configs[$_.BaseName] = $obj
    $script += "`$sync.configs.$($_.BaseName) = @'`r`n$json`r`n'@ | ConvertFrom-Json"
}

$xaml = Get-Content -Path (Join-Path $sourceRoot 'xaml\inputXML.xaml') -Raw
$script += "`$inputXML = @'`r`n$xaml`r`n'@"

$autounattendXml = Get-Content -Path (Join-Path $sourceRoot 'tools\autounattend.xml') -Raw
$script += "`$WinUtilAutounattendXml = @'`r`n$autounattendXml`r`n'@"

$script += Get-Content -Path (Join-Path $sourceRoot 'scripts\main.ps1') -Raw

$outputPath = Join-Path $sourceRoot 'AspectUtils.ps1'
Set-Content -Path $outputPath -Value $script

Write-Host "Compiled $outputPath"

if ($Run) {
    & $outputPath
}
