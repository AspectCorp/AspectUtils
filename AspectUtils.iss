; Inno Setup script for A$PECT UTILS Desktop App
; Build: publish\release\AspectUtils.exe

[Setup]
AppName=A$PECT UTILS
AppVersion=2.0.1
AppPublisher=A$PECT UTILS
AppPublisherURL=https://github.com/AspectCorp/AspectUtils
AppSupportURL=https://github.com/AspectCorp/AspectUtils
AppUpdatesURL=https://github.com/AspectCorp/AspectUtils
DefaultDirName={commonpf}\A$PECT UTILS
DisableProgramGroupPage=yes
UninstallDisplayIcon={commonpf}\A$PECT UTILS\AspectUtils.exe
PrivilegesRequired=admin
Uninstallable=yes
OutputDir="."
OutputBaseFilename=AspectUtils_Install

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: exclusive
Name: "startmenu"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: exclusive

[Files]
Source: "publish\release-selfcontained\AspectUtils.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\release-selfcontained\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "config\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "Scripts\*.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
Source: "publish\release-selfcontained\Theme\AspectTheme.xaml"; DestDir: "{app}\Theme"; Flags: ignoreversion
Source: "publish\release-selfcontained\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion

[Icons]
Name: "{group}\A$PECT UTILS"; Filename: "{app}\AspectUtils.exe"
Name: "{group}\A$PECT UTILS\Desktop"; Filename: "{app}\AspectUtils.exe"; Tasks: desktopicon

[UninstallRun]

[Messages]
