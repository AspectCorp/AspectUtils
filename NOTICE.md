# NOTICE

A$PECT UTILS is a modified fork of **WinUtil**, originally published at
<https://github.com/ChrisTitusTech/winutil> by Chris Titus Tech and contributors.

WinUtil is licensed under the MIT License. The original copyright notice is
preserved in [LICENSE](LICENSE):

> Copyright (c) 2022 CT Tech Group LLC

A$PECT UTILS is not affiliated with or endorsed by Chris Titus Tech or the
WinUtil project.

## Changes in this fork

- Rebranded the application, documentation site, icons, and social preview as
  **A$PECT UTILS** with a mint and gray palette.
- Renamed the compiled PowerShell artifact from `winutil.ps1` to
  `AspectUtils.ps1` and moved runtime data to `%LocalAppData%\AspectUtils`.
- Added a native .NET 8 WPF desktop app under `desktop/` that reads the same
  `config/*.json` catalogs and implements installs, tweaks with undo, fixes,
  Windows features, and Windows Update profiles.
- Added helper scripts for fixes and update profiles, and a brand asset
  generator under `tools/branding/`.
- Rewrote the root README and parts of the documentation for the fork. The
  upstream function names and internal PowerShell identifiers are retained to
  keep the compiled script and its loader compatible.

Files inherited from WinUtil remain subject to the MIT License. New files added
by this fork are released under the same MIT License.
