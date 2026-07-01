#define MyAppName "AI Batch Renamer"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "AI Batch Renamer"
#define MyAppExeName "AiBatchRenamer.exe"

[Setup]
AppId={{A8562E35-C15D-4781-95A3-AE19D4AF2E05}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\AI Batch Renamer
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=AiBatchRenamer-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\src\AiBatchRenamer.App\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

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
      '.NET Framework 4.8 is required. Please install .NET Framework 4.8 and run this installer again.',
      mbError,
      MB_OK
    );
    Result := False;
  end;
end;
