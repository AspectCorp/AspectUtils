# A$PECT UTILS installer

The primary installer is the WPF setup app in `Launcher/`. It uses the same
mint-and-gray visual language as the desktop app and embeds the complete
self-contained desktop build, so the release is one `AspectUtilsSetup.exe` file.

`AspectUtils.iss` remains available for an optional Inno Setup package.

## Build

From the repository root:

```powershell
.\Installer\build.ps1
```

The script:

1. Stops any running `AspectUtils.exe` so the publish output is not locked.
2. Publishes `MainApp\AspectUtils.Desktop.csproj` as a self-contained win-x64
   app into `Installer\stage`.
3. Verifies that the apphost, managed assembly, runtime config, .NET runtime,
   theme, assets, shared config, and helper scripts are present.
4. Compresses the stage into `Installer\output\payload.zip`.
5. Publishes the WPF launcher with the payload embedded and writes
   `Installer\output\launcher\AspectUtilsSetup.exe`.
6. Prints the installer size and SHA256 hash.

Useful switches:

| Switch | Effect |
|--------|--------|
| `-SkipPublish` | Reuse `Installer\stage` and rebuild only the installer |
| `-Inno` | Also compile `Installer\output\AspectUtils_Install.exe` (requires Inno Setup 6) |
| `-Zip` | Also write the portable app to `Installer\output\AspectUtils-Desktop.zip` |

## Install behaviour

- The launcher requests administrator rights through `app.manifest` and installs
  to `C:\Program Files\A$PECT UTILS` by default.
- On first run it unpacks the embedded payload to `%TEMP%\AspectUtilsSetup` and
  removes that folder after a successful install.
- It finds running copies of `AspectUtils.exe` by process path, asks before
  closing them, then copies the new files with retries.
- Start menu and desktop shortcuts are created for every account.
- After installation, the launcher can start the new build from the installed
  folder instead of leaving the old build running.
- Errors are shown in the launcher with a plain-language reason, and details are
  written to `C:\ProgramData\A$PECT UTILS\install.log`.

For unattended or test installs, the launcher accepts a target directory:

```powershell
.\AspectUtilsSetup.exe --target "D:\Tools\A$PECT UTILS"
```

`ASPECTUTILS_INSTALL_DIR` provides the same override for automation.

## Optional Inno package

`AspectUtils.iss` copies the verified `Installer\stage` output directly. It
detects a running app through `AppMutex=AspectUtils.SingleInstance`, offers to
close it before replacing files, and does not relaunch the old build after an
upgrade.

```powershell
.\Installer\build.ps1 -Inno
```

The Inno installer is written to `Installer\output\AspectUtils_Install.exe`.
