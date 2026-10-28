; Custom Installer Script for A$PECT UTILS
; Features: MUImodern style, self-contained .NET 8 bundling, config in subfolder

[Setup]
AppName=A$PECT UTILS Custom
AppVersion=2.0.1
AppPublisher=A$PECT UTILS
AppPublisherURL=https://github.com/AspectCorp/AspectUtils
AppSupportURL=https://github.com/AspectCorp/AspectUtils
AppUpdatesURL=https://github.com/AspectCorp/AspectUtils
DefaultDirName={autopf}\A$PECT UTILS CUSTOM
DisableProgramGroupPage=yes
Uninstallable=yes
OutputDir="."
OutputBaseFilename=Custom_AspectUtils_Install
PrivilegesRequired=admin
UninstallDisplayIcon={app}\A$PECT UTILS CUSTOM\AspectUtils.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: exclusive
Name: "startmenu"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: exclusive

[Files]
; Self-contained build - bundles .NET 8 runtime so no separate runtime needed on target
Source: "publish\release-selfcontained\AspectUtils.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\release-selfcontained\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "config\*.json"; DestDir: "{app}\config"; Flags: ignoreversion
Source: "Scripts\*.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
Source: "publish\release-selfcontained\Theme\AspectTheme.xaml"; DestDir: "{app}\Theme"; Flags: ignoreversion
Source: "publish\release-selfcontained\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion

[Icons]
Name: "{group}\A$PECT UTILS Custom"; Filename: "{app}\AspectUtils.exe"
Name: "{group}\A$PECT UTILS Custom\Desktop"; Filename: "{app}\AspectUtils.exe"; Tasks: desktopicon

[UninstallRun]

[Messages]