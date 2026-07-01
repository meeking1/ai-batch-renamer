#define MyAppName "AI Batch Renamer"
#define MyAppVersion "0.1.5"
#define MyAppExeName "AiBatchRenamer.exe"

[Setup]
AppId={{B49E45E9-B731-4FC9-98B4-725E71F1C817}
AppName={#MyAppName} Portable
AppVersion={#MyAppVersion}
CreateAppDir=no
Uninstallable=no
OutputDir=output
OutputBaseFilename=AiBatchRenamer-Portable-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
DisableWelcomePage=yes
DisableFinishedPage=yes
WizardStyle=modern

[Files]
Source: "..\src\AiBatchRenamer.App\bin\Release\*"; DestDir: "{tmp}\AiBatchRenamer"; Flags: ignoreversion recursesubdirs createallsubdirs deleteafterinstall

[Run]
Filename: "{tmp}\AiBatchRenamer\{#MyAppExeName}"; Flags: skipifdoesntexist

[Code]
function IsDotNet48Installed(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(
    HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
    'Release',
    Release
  ) and (Release >= 528040);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet48Installed() then
  begin
    MsgBox(
      '.NET Framework 4.8 is required. Please install .NET Framework 4.8 and run this portable package again.',
      mbError,
      MB_OK
    );
    Result := False;
  end;
end;
