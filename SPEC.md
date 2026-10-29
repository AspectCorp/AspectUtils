# SPEC.md

Project contract for A$PECT UTILS — what the project is, how it is built, and how it runs. This file describes the project itself, not any particular contributor's workflow.

## Project Context

A$PECT UTILS is a Windows 11 setup utility. It ships in two forms that share one set of JSON catalogs:

- A PowerShell app that compiles into a single distributable script.
- A native .NET 8 WPF desktop app, packaged with a modern WPF setup launcher.

The project is a fork of WinUtil. The upstream MIT copyright notice remains in [LICENSE](LICENSE), and [NOTICE.md](NOTICE.md) records the attribution and the changes made in this fork.

### Stack

- PowerShell app: Windows PowerShell / PowerShell, WPF via `PowerShellApp/xaml/inputXML.xaml`.
- Desktop app: .NET 8 (`net8.0-windows`), WPF, `System.Management` for hardware detection.
- Configuration: shared JSON files under `PowerShellApp/config/`.
- Tests: Pester tests under `PowerShellApp/pester/`.
- Lint: PowerShell Script Analyzer with settings in `PowerShellApp/lint/PSScriptAnalyser.ps1`.
- Docs: Astro + Starlight site under `docs/`, built independently of the app compile.
- Installer: the WPF launcher in `Installer/Launcher`, driven by `Installer/build.ps1`. Inno Setup 6 remains an optional packaging path through `Installer/AspectUtils.iss`.
- Release artifacts: `PowerShellApp/AspectUtils.ps1`, `Installer/output/launcher/AspectUtilsSetup.exe`, and the portable `AspectUtils-Desktop.zip`.

### Repository Layout

| Path | Purpose |
|------|---------|
| `MainApp/` | Native .NET 8 WPF desktop app |
| `PowerShellApp/Compile.ps1` | Builds `PowerShellApp/AspectUtils.ps1` from the PowerShell sources |
| `PowerShellApp/scripts/start.ps1` | Startup/bootstrap segment used at the beginning of the compiled script |
| `PowerShellApp/scripts/main.ps1` | Main entrypoint appended at the end of the compiled script |
| `PowerShellApp/functions/public/` | Public/UI-facing PowerShell functions |
| `PowerShellApp/functions/private/` | Internal helper PowerShell functions |
| `PowerShellApp/config/` | Shared JSON catalogs, embedded into the PowerShell script and copied beside the desktop app |
| `PowerShellApp/xaml/inputXML.xaml` | PowerShell app WPF markup embedded into the compiled script |
| `PowerShellApp/tools/autounattend.xml` | Unattended setup XML embedded for Windows ISO workflows |
| `PowerShellApp/pester/` | Pester tests |
| `PowerShellApp/lint/PSScriptAnalyser.ps1` | PowerShell Script Analyzer settings |
| `PowerShellApp/tools/title-screen/` | uv project that captures the PowerShell UI for the title-screen composite |
| `PowerShellApp/tools/branding/` | Brand asset generator |
| `Installer/build.ps1` | Publishes `MainApp`, stages and verifies the output, packs the payload, and builds the single-file setup |
| `Installer/Launcher/` | WPF setup launcher with elevation, running-app detection, payload extraction, and shortcuts |
| `Installer/AspectUtils.iss` | Optional Inno Setup definition |
| `Installer/stage/` | Generated self-contained publish output (gitignored) |
| `Installer/output/` | Generated installer output (gitignored) |
| `docs/` | Astro + Starlight documentation site, with its own `package.json` and build |
| `.github/` | CI workflows and repository automation |
| `PowerShellApp/AspectUtils.ps1` | Generated PowerShell build artifact (gitignored) |

## Goals

- Provide a single-script Windows utility that can be launched from PowerShell.
- Provide a native desktop app with the same core workflows and shared configuration.
- Keep development modular enough for contributors to work on functions, config, UI, docs, and tooling independently.
- Make install, tweak, feature, repair, update, Quickres, and driver workflows discoverable from the UI.
- Keep common lists and options declarative in JSON config where possible.
- Preserve a repeatable build process so local builds and GitHub Actions builds produce the same artifacts from the same inputs.
- Keep every system change auditable and, where the schema supports it, reversible.

## Non-Goals

- The compiled PowerShell script is not hand-maintained.
- The PowerShell app is not structured as a PowerShell module at runtime.
- Generated build output is not reviewed as source changes.
- The desktop app does not replace the PowerShell-only workflows (AppX management, Win11 Creator, activation) in this release.
- Windows 10 is not a supported target.

## PowerShell Build Model

`PowerShellApp/Compile.ps1` combines the PowerShell sources into `PowerShellApp/AspectUtils.ps1` in this order:

1. Read `scripts/start.ps1` and replace `#{replaceme}` with the current `yy.MM.dd` build date.
2. Append every file under `functions/` recursively.
3. Convert each `config/*.json` file into embedded `$sync.configs` objects.
4. Special-case `config/applications.json` so keys receive the `WPFInstall` prefix in compiled config.
5. Embed `xaml/inputXML.xaml` into `$inputXML`.
6. Embed `tools/autounattend.xml` into `$WinUtilAutounattendXml`.
7. Append `scripts/main.ps1`.
8. Write the result to `PowerShellApp/AspectUtils.ps1`.

The script resolves its sources relative to `$PSScriptRoot`, so it can be run from any working directory. Because the final script is concatenated, code cannot rely on runtime module imports or source-relative dot-sourcing unless the compiled script will also contain the required code or data.

## Desktop Build And Packaging Model

The desktop app is built from `MainApp/AspectUtils.Desktop.csproj`. It requires administrator rights through `MainApp/app.manifest` and uses the named mutex `AspectUtils.SingleInstance` for single-instance detection and installer hand-off.

`Installer/build.ps1` is the canonical packaging pipeline:

1. Stop any running `AspectUtils.exe` so the publish output is not locked.
2. Read the version from `MainApp/Models/BrandInfo.cs`.
3. Publish `MainApp/AspectUtils.Desktop.csproj` as a self-contained win-x64 app into `Installer/stage`.
4. Verify the apphost, managed assembly, runtime config, deps file, .NET runtime, theme, assets, shared config, and helper scripts are present.
5. Compress the verified stage into `Installer/output/payload.zip`.
6. Publish `Installer/Launcher/AspectUtils.Installer.csproj` as a self-contained single-file app with the payload embedded.
7. Write `Installer/output/launcher/AspectUtilsSetup.exe` and print its SHA256 hash.

The launcher unpacks the embedded payload to `%TEMP%\AspectUtilsSetup` during installation, verifies the extracted apphost, and removes the temporary folder after a successful install. The release is one executable; users do not manage a payload folder.

The launcher installs to `%ProgramFiles%\A$PECT UTILS` by default, finds running copies by process path, asks before closing them, copies files with retries, and creates Start menu and desktop shortcuts. `--target` and `ASPECTUTILS_INSTALL_DIR` override the install folder for automation.

The optional Inno package (`Installer/AspectUtils.iss`) copies `stage\*` recursively. Individual runtime files must not be listed in its `[Files]` section: the app cannot start without `AspectUtils.dll`, `AspectUtils.runtimeconfig.json`, `AspectUtils.deps.json`, and the bundled .NET runtime. It uses `AppMutex=AspectUtils.SingleInstance` to detect a running app.

## Runtime Model

- The PowerShell app runs in PowerShell on Windows and uses WPF for the UI.
- Shared mutable state is stored in `$sync`, including configs, UI element references, runspace state, selections, and progress.
- Long-running operations use runspaces or existing async patterns so the UI remains responsive.
- UI updates from background work are dispatched back to the WPF UI thread.
- The desktop app hosts each page in a WPF shell, runs helper scripts through Windows PowerShell 5.1, and records operation output in a session log.
- Both front ends resolve the shared configuration beside the executable first, then fall back to the repository layout during development.
- Declarative features such as apps, tweaks, presets, DNS providers, and navigation stay in `config/*.json` unless code is required.

## UI And Event Contract

- PowerShell UI layout lives in `PowerShellApp/xaml/inputXML.xaml`.
- Named WPF controls are discovered and stored in `$sync`.
- Button/action wiring follows a naming convention: an element named like `WPFThingButton` maps to a function named like `Invoke-WPFThingButton`.
- Desktop pages implement `IPageView` and are created by the shell's navigation map in `MainApp/MainWindow.xaml.cs`.
- Desktop operations run through `IShellHost.RunOperationAsync`, which owns the operation overlay, cancellation, progress output, and final outcome.

## Configuration Contract

- Config files must remain valid JSON and compile cleanly through `ConvertFrom-Json`.
- Both front ends read the same files from `PowerShellApp/config/` in the repository and from `config\` beside a published executable.
- `config/dns.json` opts unfiltered providers into Fastest selection with `BenchmarkEligible: true`; missing or false values exclude a provider from the TCP latency benchmark.
- `config/applications.json` defines installable applications; each entry includes the fields expected by tests and UI code, such as package manager IDs, category, display content, description, and link.
- `config/tweaks.json` defines Windows tweaks; registry and service changes include original values or original states when applicable so undo workflows can restore user systems.
- `config/drivers.json` defines the hardware categories and WinGet packages used by the desktop Drivers tab.
- Preset and navigation files reference valid config keys. Renaming a config key requires updating all presets, UI references, docs, and code paths together.

## Safety Requirements

- Registry, service, package manager, Windows Update, AppX removal, driver, and ISO operations affect the host system and are treated as high-risk.
- Tweak changes include undo metadata when the schema supports it, so changes stay reversible.
- The desktop app writes a registry snapshot before applying a reversible tweak and records the snapshot in its undo manifest.
- ISO workflows never modify the user's original ISO file; they work on copied/mounted content.
- The installer must never replace a running executable silently. It detects the mutex and asks before force-closing the app.

## Docs Site (Astro)

- `docs/` is an Astro + Starlight site, independent of the app builds, deployed through GitHub Actions.
- Pages live under `docs/src/content/docs/` (`.mdx`), organized into `guides/`, `code-reference/`, plus top-level pages such as `faq.mdx`, `knownissues.mdx`, `contributing.mdx`, and `index.mdx`.
- `docs/src/content/docs/code-reference/tweaks/` and `.../features/` are auto-generated by `PowerShellApp/tools/devdocs-generator.ps1` from `PowerShellApp/config/tweaks.json` and `PowerShellApp/config/feature.json` plus the relevant PowerShell function files. Other pages under `code-reference/` are hand-written and untouched by the generator.
- Sidebar entries in `docs/astro.config.mjs` must match actual page slugs under `docs/src/content/docs/`.
- `docs/public/` is tracked source for static assets, not generated output. Generated/ignored paths are listed in `docs/.gitignore`.
- `docs/src/assets/branding/title-screen.png` is a tracked generated image used by the repository README and docs homepage.
- `docs/Dockerfile` and `docs/docker-compose.yml` containerize the site's npm tooling. Use the container workflow when Docker is available.

## Testing And CI

- `.\PowerShellApp\Compile.ps1` verifies the compiler can generate `PowerShellApp/AspectUtils.ps1`.
- `.\PowerShellApp\Compile.ps1 -Run` compiles and launches the generated utility for manual GUI verification.
- Pester 5.8.0 runs the suite under `PowerShellApp/pester/*.Tests.ps1`. GitHub Actions installs Pester 5.8.0 fresh and runs with `-CI`, which produces `testResults.xml` and exits non-zero on failure.
- GitHub Actions runs PowerShell Script Analyzer against `PowerShellApp/` with `PowerShellApp/lint/PSScriptAnalyser.ps1`.
- The desktop workflow builds `MainApp/AspectUtils.Desktop.csproj` and uploads a self-contained artifact.
- The pre-release workflow compiles the PowerShell app and calls `Installer/build.ps1` to produce the release installer.
- The manually triggered title-screen workflow compiles the PowerShell app from `main` and opens an image-only pull request when the generated composite changes.

## Release Artifacts

GitHub Actions produces the release artifacts from repository sources:

- `AspectUtils.ps1` — compiled PowerShell app.
- `AspectUtilsSetup.exe` — self-contained single-file desktop installer with an embedded payload.
- `AspectUtils-Desktop.zip` — portable self-contained desktop build.
- `AspectUtils_Install.exe` — optional Inno Setup package, published only when built with `-Inno`.

A release is valid only if the PowerShell script came from the compile process and the setup executable came from `Installer/build.ps1`; neither may be replaced by hand-edited or stale binaries.
