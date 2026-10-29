[CmdletBinding()]
param (
    [switch]$SkipPublish,
    [switch]$Inno,
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$installerRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent $installerRoot
$appProject = Join-Path $repoRoot 'MainApp\AspectUtils.Desktop.csproj'
$launcherProject = Join-Path $installerRoot 'Launcher\AspectUtils.Installer.csproj'
$brandInfoPath = Join-Path $repoRoot 'MainApp\Models\BrandInfo.cs'
$stagePath = Join-Path $installerRoot 'stage'
$outputPath = Join-Path $installerRoot 'output'
$launcherOutput = Join-Path $outputPath 'launcher'
$payloadZipPath = Join-Path $outputPath 'payload.zip'
$scriptPath = Join-Path $installerRoot 'AspectUtils.iss'

$brandInfo = Get-Content -LiteralPath $brandInfoPath -Raw
if ($brandInfo -notmatch 'Version\s*=\s*"([^"]+)"') {
    throw "Could not read the app version from $brandInfoPath"
}
$appVersion = $Matches[1]

function Stop-RunningApp {
    $processes = Get-Process -Name 'AspectUtils' -ErrorAction SilentlyContinue
    if (-not $processes) {
        return
    }

    Write-Host 'Closing a running copy of A$PECT UTILS...'

    foreach ($process in $processes) {
        try {
            if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
                $process.CloseMainWindow() | Out-Null
            }
        }
        catch {
            Write-Verbose "Could not ask PID $($process.Id) to close: $($_.Exception.Message)"
        }
    }

    $deadline = (Get-Date).AddSeconds(6)
    while ((Get-Date) -lt $deadline -and ($processes | Where-Object { -not $_.HasExited })) {
        Start-Sleep -Milliseconds 250
        $processes | ForEach-Object { $_.Refresh() }
    }

    foreach ($process in ($processes | Where-Object { -not $_.HasExited })) {
        try {
            Write-Host "Stopping PID $($process.Id)..."
            $process.Kill()
            $process.WaitForExit(5000) | Out-Null
        }
        catch {
            Write-Warning "Could not close AspectUtils.exe (PID $($process.Id)). Close it manually if publishing fails."
        }
    }
}

function Find-Iscc {
    $candidates = @(
        (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    return $candidates | Select-Object -First 1
}

Write-Host "Building A`$PECT UTILS $appVersion"

Stop-RunningApp

if (-not $SkipPublish) {
    foreach ($path in @($stagePath, $launcherOutput, $payloadZipPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Force -Path $stagePath | Out-Null
New-Item -ItemType Directory -Force -Path $launcherOutput | Out-Null

if (-not $SkipPublish) {
    Write-Host 'Publishing the desktop app...'
    & dotnet publish $appProject `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --nologo `
        -o $stagePath

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

$requiredFiles = @(
    'AspectUtils.exe',
    'AspectUtils.dll',
    'AspectUtils.runtimeconfig.json',
    'AspectUtils.deps.json',
    'coreclr.dll',
    'hostfxr.dll',
    'Theme\AspectTheme.xaml',
    'Assets\AspectUtils.ico',
    'config\tweaks.json',
    'config\applications.json',
    'Scripts\fix-network.ps1'
)

$missing = $requiredFiles | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $stagePath $_))
}

if ($missing) {
    throw "The publish output is incomplete. Missing: $($missing -join ', ')"
}

Write-Host 'Packing the installer payload...'
if (Test-Path -LiteralPath $payloadZipPath) {
    Remove-Item -LiteralPath $payloadZipPath -Force
}

[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $stagePath,
    $payloadZipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)

if (-not (Test-Path -LiteralPath $payloadZipPath)) {
    throw "The installer payload archive was not created at $payloadZipPath"
}

Write-Host 'Publishing the WPF launcher...'
& dotnet publish $launcherProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PayloadZip=$payloadZipPath `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:Version=$appVersion `
    --nologo `
    -o $launcherOutput

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish (launcher) failed with exit code $LASTEXITCODE"
}

# The payload is embedded in the launcher. Remove any folder left by an older build.
$stalePayload = Join-Path $launcherOutput 'payload'
if (Test-Path -LiteralPath $stalePayload) {
    Remove-Item -LiteralPath $stalePayload -Recurse -Force
}

$launcherPath = Join-Path $launcherOutput 'AspectUtilsSetup.exe'

if (-not (Test-Path -LiteralPath $launcherPath)) {
    throw "The launcher was not created at $launcherPath"
}

$launcher = Get-Item -LiteralPath $launcherPath
$hash = (Get-FileHash -LiteralPath $launcherPath -Algorithm SHA256).Hash
Write-Host ("Launcher: {0} ({1:N1} MB)" -f $launcherPath, ($launcher.Length / 1MB))
Write-Host "SHA256:   $hash"

if ($Inno) {
    $iscc = Find-Iscc
    if (-not $iscc) {
        throw 'Inno Setup 6 was not found. Install it from https://jrsoftware.org/isdl.php or build without -Inno.'
    }

    Write-Host 'Compiling the Inno Setup package...'
    & $iscc "/DMyAppVersion=$appVersion" $scriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup failed with exit code $LASTEXITCODE"
    }

    $innoInstaller = Join-Path $outputPath 'AspectUtils_Install.exe'
    if (-not (Test-Path -LiteralPath $innoInstaller)) {
        throw "Installer was not created at $innoInstaller"
    }

    Write-Host "Inno installer: $innoInstaller"
}

if ($Zip) {
    $zipPath = Join-Path $outputPath 'AspectUtils-Desktop.zip'
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Write-Host 'Compressing the portable app...'
    Compress-Archive -Path (Join-Path $stagePath '*') -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Portable app: $zipPath"
}

Write-Host ''
Write-Host 'Build complete.'
Write-Host "Installer: $launcherPath"
if ($Inno) {
    Write-Host "Inno installer:  $(Join-Path $outputPath 'AspectUtils_Install.exe')"
}
