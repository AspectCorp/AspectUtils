# A$PECT UTILS

A$PECT UTILS is a Windows 11 setup utility for installing apps, applying reversible tweaks, running common fixes, and controlling Windows Update. It ships as a native WPF desktop app and as a PowerShell app, and both front ends read the same JSON catalogs.

![A$PECT UTILS](docs/public/social-preview.png)

## Quick start

### Desktop app

Download `AspectUtilsSetup.exe` from the [Releases page](https://github.com/AspectCorp/AspectUtils/releases/latest) and run it. The installer is a single self-contained file: the target PC does not need the .NET runtime installed, and there is no separate payload folder to manage.

To build it from source with the .NET 8 SDK:

```powershell
dotnet build MainApp\AspectUtils.Desktop.csproj -c Release
.\MainApp\bin\Release\net8.0-windows\AspectUtils.exe
```

The app requests administrator rights when it starts. See [MainApp/README.md](MainApp/README.md) for details.

To produce the installer, run:

```powershell
.\Installer\build.ps1
```

The single-file setup is written to `Installer\output\launcher\AspectUtilsSetup.exe`. Pass `-Inno` to also build the optional Inno Setup package, which requires [Inno Setup 6](https://jrsoftware.org/isdl.php).

### PowerShell

Run PowerShell or Terminal as Administrator, then:

```powershell
irm "https://github.com/AspectCorp/AspectUtils/releases/latest/download/AspectUtils.ps1" | iex
```

To run from a local checkout instead:

```powershell
.\PowerShellApp\Compile.ps1 -Run
```

### Presets

Apply a predefined configuration without manual selection:

```powershell
& ([ScriptBlock]::Create((irm "https://github.com/AspectCorp/AspectUtils/releases/latest/download/AspectUtils.ps1"))) -Preset Standard
```

| Preset | Description |
|--------|-------------|
| `Standard` | Balanced defaults for most users |
| `Minimal` | Minimal changes to suit every user |
| `Advanced` | Deep tweaks for power users |

The exact contents are defined in [`PowerShellApp/config/preset.json`](PowerShellApp/config/preset.json).

## What's included

- **Install apps** through WinGet and Chocolatey, including upgrade and uninstall flows.
- **Tweaks with undo**: reversible registry and service changes, plus one-time cleanup actions.
- **Windows features** such as Hyper-V, WSL, .NET, Sandbox, and legacy panels.
- **Fixes** for networking, NTP, system repair, WinGet, and Windows Update.
- **Windows Update profiles**: default, recommended, and disabled, with a restore path.
- **Quickres** for instant resolution switching, and a **Drivers** tab that detects hardware categories.
- **Activation tab** for Windows and Office via Microsoft Activation Scripts (MAS) in the PowerShell app.
- **Own branding and documentation** with a mint and gray theme.

The desktop app covers installs, tweaks and undo, fixes, features, update profiles, Quickres, and drivers. AppX package management, Win11 Creator, and activation are currently available only in the PowerShell app.

## Build and develop

The PowerShell app compiles from the sources in `PowerShellApp/`:

```powershell
.\PowerShellApp\Compile.ps1
```

The compiled `PowerShellApp/AspectUtils.ps1` is generated output and is not committed.

Desktop app:

```powershell
dotnet build MainApp\AspectUtils.Desktop.csproj -c Debug
```

Tests:

```powershell
Import-Module Pester -RequiredVersion 5.8.0 -Force
Invoke-Pester -Path 'PowerShellApp/pester/*.Tests.ps1' -Output Detailed -CI
```

Script Analyzer:

```powershell
Invoke-ScriptAnalyzer -Path .\PowerShellApp -Settings .\PowerShellApp\lint\PSScriptAnalyser.ps1 -Recurse
```

## Repository layout

| Path | Purpose |
|------|---------|
| `MainApp/` | Native .NET 8 WPF desktop app |
| `PowerShellApp/` | PowerShell app sources, shared JSON catalogs, tests, lint, and tools |
| `PowerShellApp/AspectUtils.ps1` | Compiled PowerShell app (generated, gitignored) |
| `PowerShellApp/config/` | Shared JSON catalogs for apps, tweaks, features, and presets |
| `Installer/` | WPF setup launcher, optional Inno Setup script, and the publish/package pipeline |
| `docs/` | Astro/Starlight documentation site |
| `PowerShellApp/tools/branding/` | Generates the logo, social preview, and app icon |

## Documentation

- [Documentation site source](docs/)
- [Desktop app notes](MainApp/README.md)
- [Installer notes](Installer/README.md)
- [Contributing guide](docs/src/content/docs/contributing.mdx)

## License

MIT. The original WinUtil copyright notice is preserved in [LICENSE](LICENSE). See [NOTICE.md](NOTICE.md) for attribution and a summary of changes.
