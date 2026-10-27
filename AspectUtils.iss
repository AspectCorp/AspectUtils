; Inno Setup script for A$PECT UTILS Desktop App
; Generated for: AspectUtils.Desktop
; Build: publish\release\AspectUtils.exe

[Setup]
; Basic info
AppName=A$PECT UTILS
AppVersion=1.0.0
AppPublisher=A$PECT UTILS
AppPublisherURL=https://winutil.christitus.com
AppSupportURL=https://winutil.christitus.com/code-reference/
AppUpdatesURL=https://winutil.christitus.com/code-reference/
; Use {code:GetAdminFlags} for admin requirement
DefaultDirName={commonpf}\A$PECT UTILS
DisableProgramGroupPage=yes
DisableProgramsPage=yes
UninstallDisplayIcon={commondir}\A$PECT UTILS\AspectUtils.exe
UninstallAfterInstall=no
; Make installer run as admin
PrivilegesRequired=admin
; Create uninstaller
Uninstallable=yes
; Output file
OutputDir="."
OutputBaseFilename=AspectUtils_Install

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: skipchecked

[Tasks]
Name: "startmenu"; Description: "{cm:CreateStartMenuDir}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: skipchecked

[Files]
; Copy the executable
Source: "publish\release\AspectUtils.exe"; DestDir: "{app}"; Flags: ignoreversion
; Copy config files
Source: "config\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "Scripts\*.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
; Copy theme
Source: "desktop\Theme\AspectTheme.xaml"; DestDir: "{app}"; Flags: ignoreversion
; Copy resources
Source: "Assets\\*"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}A$PECT UTILS"; Filename: "{app}\AspectUtils.exe"
Name: "desktopicon"; Filename: "{app}\AspectUtils.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: defaultchecked

[UninstallRun]
; No specific uninstall steps needed - the exe doesn't install services etc
; Just remove the folder

[Messages]
; Custom messages if needed