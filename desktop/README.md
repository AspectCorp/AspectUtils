# A$PECT UTILS Desktop

Native WPF desktop app for A$PECT UTILS. It targets .NET 8 (`net8.0-windows`), has no NuGet dependencies, and shares `../config/*.json` with the PowerShell app.

## Requirements

- Windows 11 (Windows 10 is not supported, matching the PowerShell app)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build
- Administrator rights to run. `app.manifest` requests elevation, so Windows shows a UAC prompt at launch.

## Build and run

From the repository root:

```powershell
dotnet build desktop\AspectUtils.Desktop.csproj -c Debug
.\desktop\bin\Debug\net8.0-windows\AspectUtils.exe
```

The build copies `config\*.json` and `Scripts\*.ps1` into the output folder. If you edit the shared config while the app is running, restart it to reload the catalogs.

## Publish

Self-contained, single-file build for another Windows 11 x64 PC:

```powershell
dotnet publish desktop\AspectUtils.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts\desktop
```

Framework-dependent build (smaller, requires the .NET Desktop Runtime 8):

```powershell
dotnet publish desktop\AspectUtils.Desktop.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o artifacts\desktop
```

Both publish modes keep `config\` and `Scripts\` next to the executable. Include them when you copy the app to another machine.

## What the desktop app covers

| Area | Desktop app | Notes |
|------|-------------|-------|
| Install / upgrade / uninstall apps | Yes | WinGet and Chocolatey |
| Reversible tweaks | Yes | Registry snapshots are written before changes |
| One-time tweaks | Yes | Widgets removal, Disk Cleanup, temporary files |
| Fixes | Yes | Networking, NTP, system repair, WinGet, Windows Update reset |
| Windows features | Yes | Uses DISM through PowerShell |
| Windows Update profiles | Yes | Default, recommended, disabled |
| AppX package management | No | Use the PowerShell app |
| Win11 Creator | No | Use the PowerShell app |
| Windows/Office activation | No | Use the PowerShell app's Activation tab |

Tweak classification comes from the shared catalogs:

- 60 reversible toggles
- 3 one-time actions
- 5 entries that are only supported by the PowerShell app (DNS, Multiplane Overlay, O&O ShutUp10, and the Ultimate Performance enable/disable pair)

Unsupported entries are hidden from the desktop lists rather than shown as broken actions.

## Data and logs

- Logs: `%LocalAppData%\AspectUtils\logs\aspectutils-desktop_*.log`
- Undo manifest: `%LocalAppData%\AspectUtils\state\applied-tweaks.json`
- PowerShell app logs: `%LocalAppData%\AspectUtils\logs\aspectutils_*.log`

Deleting the undo manifest does not undo applied tweaks; it only removes the desktop app's record of which snapshots it can restore.

## Project structure

| Path | Purpose |
|------|---------|
| `MainWindow.xaml` / `.xaml.cs` | Shell, sidebar navigation, operation overlay, admin check |
| `Theme/AspectTheme.xaml` | Mint and gray styles, including scrollbars and cards |
| `Services/` | Config loading, logging, PowerShell runner, packages, tweaks, features, undo |
| `Views/` | Dashboard, Install, Tweaks, Fixes, Features, Updates, Activity log |
| `Scripts/` | Fix and update-profile PowerShell scripts |
| `Assets/` | App icon and logo |

## Troubleshooting

**The build fails with access denied.** A copy of `AspectUtils.exe` is still running. Close it (approve the UAC prompt if it was launched elevated) and rebuild.

**The app opens and closes immediately.** Check the desktop log under `%LocalAppData%\AspectUtils\logs\`. A malformed config file or a missing `config` folder beside the executable is the usual cause.

**A tweak fails or the log shows a missing PowerShell command.** The desktop app runs helper scripts with Windows PowerShell 5.1. Make sure execution policy allows local scripts, or run the PowerShell app once to verify the environment.

## License and attribution

MIT. See the repository's `LICENSE`.
