; Inno Setup script for A$PECT UTILS.
; Build with Installer\build.ps1 so the staged app is always complete.

#define MyAppName "A$PECT UTILS"
#ifndef MyAppVersion
  #define MyAppVersion "2.1.1"
#endif
#define MyAppExeName "AspectUtils.exe"
#define MyAppMutex "AspectUtils.SingleInstance"

[Setup]
AppId={{8F3B6C2A-7D41-4E8B-9A55-2C6E1F0D4B77}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=A$PECT UTILS
AppPublisherURL=https://github.com/AspectCorp/AspectUtils
AppSupportURL=https://github.com/AspectCorp/AspectUtils
AppUpdatesURL=https://github.com/AspectCorp/AspectUtils
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
PrivilegesRequired=admin
OutputDir=output
OutputBaseFilename=AspectUtils_Install
SetupIconFile=..\MainApp\Assets\AspectUtils.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=A$PECT UTILS
VersionInfoDescription={#MyAppName} installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
SetupLogging=yes
UsePreviousAppDir=yes

; Let Inno close a running copy and never restart it behind the user's back.
AppMutex={#MyAppMutex}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Copy the complete publish output. Never list individual runtime files here:
; the apphost cannot start without AspectUtils.dll and the .NET runtime files.
Source: "stage\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';

  if CheckForMutexes('{#MyAppMutex}') then
  begin
    if MsgBox(
      'A$PECT UTILS is still running. Setup needs to close it before replacing the current version.' + #13#10 +
      'Close A$PECT UTILS now?',
      mbConfirmation,
      MB_YESNO) = IDYES then
    begin
      Exec(
        ExpandConstant('{sys}\taskkill.exe'),
        '/F /IM {#MyAppExeName} /T',
        '',
        SW_HIDE,
        ewWaitUntilTerminated,
        ResultCode);
      Sleep(1500);

      if CheckForMutexes('{#MyAppMutex}') then
      begin
        Result := 'A$PECT UTILS is still running. Close it from Task Manager, then run setup again.';
      end;
    end
    else
    begin
      Result := 'Close A$PECT UTILS before installing, then run setup again.';
    end;
  end;
end;
