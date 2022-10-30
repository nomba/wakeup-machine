#define AppId "B787687-2834-4599-9721-C4F23BE2248B"
#define ServiceName "WakeUpMachine"
#define StageDir "..\bin\Release\net6.0\win-x64\publish"
#define ApplicationName "WakeUpMachine"
#define ApplicationExeName "WakeUpMachine.Service.exe"
#define ApplicationVersion GetStringFileInfo(AddBackslash(StageDir) + ApplicationExeName, 'ProductVersion')
#define ApplicationPublisher "Sergey Petrov"
#define ApplicationPublisherURL "https://github.com/nomba"
#define ApplicationURL "https://github.com/nomba/wakeup-machine"
#define ApplicationCopyright "© 2022 Sergey Petrov"

[Setup]

AppId={#AppId}
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany={#ApplicationPublisher}
AppPublisher={#ApplicationPublisher}
AppPublisherURL={#ApplicationPublisherURL}
AppSupportURL={#ApplicationURL}
AppUpdatesURL={#ApplicationURL}
DefaultDirName={pf}\{#ApplicationName}
DefaultGroupName={#ApplicationName}
AllowNoIcons=yes
OutputDir=..\..\..\artifacts
Compression=lzma
SolidCompression=yes
WizardImageStretch=False
AppCopyright={#ApplicationCopyright}
UninstallDisplayIcon={app}\{#ApplicationExeName}
UninstallDisplayName={#ApplicationName}
VersionInfoCopyright={#ApplicationCopyright}
VersionInfoProductVersion={#ApplicationVersion}
MinVersion=0,6.1
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename={#"WakeUpMachine.Setup." + ApplicationVersion}

[Run]
Check: IsNewInstallation; Filename: "{app}\{#ApplicationExeName}"; Parameters: "--configure --bottoken={code:GetBotToken}"; Flags: nowait runascurrentuser runhidden; StatusMsg: "Configuring service..."
Check: IsNewInstallation; Filename: {sys}\sc.exe; Parameters: "create {#ServiceName} start= auto binPath= ""{app}\{#ApplicationExeName}"""; Flags: runhidden
Check: ServiceExistsDelayed; Filename: {sys}\sc.exe; Parameters: "start {#ServiceName}" ; Flags: runhidden

[UninstallRun]
Filename: {sys}\sc.exe; Parameters: "stop {#ServiceName}" ; Flags: runhidden
Filename: {sys}\sc.exe; Parameters: "delete {#ServiceName}" ; Flags: runhidden

[Files]

Source: "{#StageDir}\WakeUpMachine.Service.exe"; DestDir: "{app}"; Flags: ignoreversion;
Source: "{#StageDir}\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion;

[UninstallDelete]

Type: filesandordirs; Name: "{app}"
Type: dirifempty; Name: "{app}"

[InstallDelete]

Type: files; Name: "{app}\WakeUpMachine.Service.exe"

[Code]
// https://stackoverflow.com/a/5416744
#include "services_unicode.iss"

var
  BotTokenPage: TInputQueryWizardPage;
  BotToken: String;
  IsAlreadyInstalled: Boolean;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  S: Longword;
begin
  //If service is installed, it needs to be stopped
  if ServiceExists('{#ServiceName}') then begin
    S:= SimpleQueryService('{#ServiceName}');
    if S <> SERVICE_STOPPED then begin
      SimpleStopService('{#ServiceName}', True, True);
    end;
  end;
end;

function InitializeSetup: boolean;
begin
  IsAlreadyInstalled := RegKeyExists(HKEY_LOCAL_MACHINE,'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppId}_is1');
  Result := True;
end;

procedure InitializeWizard();
begin
  if not IsAlreadyInstalled then
    begin
      BotTokenPage := CreateInputQueryPage(
      wpWelcome,
      'Telegram Bot Token',
      'It will be used to configure service to polling command from Telegram','');

      BotTokenPage.Add('Bot Token:', False);
    end;
end;

function GetBotToken(param: String): String;
begin
  Result := Trim(BotTokenPage.Values[0]);
end;

function IsNewInstallation: Boolean;
begin
  Result := not IsAlreadyInstalled;
end;

function IsUpgradeInstallation: Boolean;
begin
  Result := IsAlreadyInstalled;
end;

function ServiceExistsDelayed: Boolean;
begin
  Sleep(2500);
  Result := ServiceExists('{#ServiceName}');
end;
