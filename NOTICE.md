# Attribution

A$PECT UTILS is a fork of [WinUtil](https://github.com/ChrisTitusTech/winutil), originally created by Chris Titus and the WinUtil contributors.

- Original project: WinUtil, Copyright (c) 2022 CT Tech Group LLC.
- Original license: MIT. The full text is preserved in [LICENSE](LICENSE).
- Fork maintained by AspectCorp: <https://github.com/AspectCorp/AspectUtils>.

## Summary of changes

- Rebranded the UI, assets, documentation, and release pipeline as A$PECT UTILS with a mint and gray palette.
- Added a native .NET 8 WPF desktop app (`MainApp/`) that shares the PowerShell app's JSON catalogs.
- Reorganized the PowerShell sources, configuration, tests, and tooling under `PowerShellApp/`.
- Added an Inno Setup packaging pipeline under `Installer/` that publishes a self-contained Windows x64 build.
- Added reversible tweak tracking, update-profile controls, Quickres, driver detection, and in-app update checking to the desktop app.
- Updated documentation, CI workflows, and release automation for the new layout.

Upstream WinUtil remains available under its original license at the project linked above.
