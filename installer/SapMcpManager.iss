#ifndef BuildRoot
  #define BuildRoot "..\artifacts\publish"
#endif
#ifndef PayloadRoot
  #define PayloadRoot "..\payload"
#endif
#ifndef Offline
  #define Offline "0"
#endif
#if Offline == "1"
  #define PackageName "Offline"
#else
  #define PackageName "Online"
#endif

[Setup]
AppId={{950456B8-B9F0-443B-9CA9-A4F5FD067290}
AppName=SAP MCP Manager
AppVersion=0.1.0-preview.4
AppPublisher=SAP MCP Manager
DefaultDirName={localappdata}\Programs\SapMcpManager
DefaultGroupName=SAP MCP Manager
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.19045
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installers
OutputBaseFilename=SAP-MCP-Manager-0.1.0-preview.4-Windows-x64-{#PackageName}
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\SapMcpManager.exe
CloseApplications=yes
RestartApplications=no
AppMutex=SapMcpManager.Installation
LicenseFile=..\docs\DISTRIBUTION-NOTICE.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[CustomMessages]
english.Full=SAP RFC + SAP ADT (recommended)
turkish.Full=SAP RFC + SAP ADT (önerilen)
english.Custom=Choose components
turkish.Custom=Bileşenleri seç
english.Manager=Setup and Connection Manager
turkish.Manager=Kurulum ve Bağlantı Yöneticisi
english.Rfc=SAP RFC MCP — attach SDK later
turkish.Rfc=SAP RFC MCP — SDK sonradan bağlanabilir
english.Desktop=Create a desktop shortcut
turkish.Desktop=Masaüstü kısayolu oluştur
english.Open=Open SAP MCP Manager and complete prerequisites
turkish.Open=SAP MCP yöneticisini aç ve gereksinimleri tamamla
english.SelectionFailed=Could not save component selection.
turkish.SelectionFailed=Bileşen seçimi kaydedilemedi.
english.SelectOne=Select at least one MCP component: SAP RFC or SAP ADT.
turkish.SelectOne=En az bir MCP bileşeni seçin: SAP RFC veya SAP ADT.
english.KeepData=Keep connection profiles, DPAPI secrets, logs and runtimes? (Recommended: Yes)
turkish.KeepData=Bağlantı profilleri, DPAPI şifreleri, loglar ve çalışma zamanları korunsun mu? (Önerilen: Evet)
english.DeleteData=Permanently delete this user's SAP MCP Manager data. Continue?
turkish.DeleteData=Bu kullanıcıya ait SAP MCP Manager verileri kalıcı olarak silinecek. Devam edilsin mi?

[Types]
Name: "full"; Description: "{cm:Full}"
Name: "custom"; Description: "{cm:Custom}"; Flags: iscustom

[Components]
Name: "manager"; Description: "{cm:Manager}"; Types: full custom; Flags: fixed
Name: "rfc"; Description: "{cm:Rfc}"; Types: full
Name: "adt"; Description: "SAP ADT MCP"; Types: full

[Files]
Source: "{#BuildRoot}\*"; DestDir: "{app}"; Excludes: "payload\cache\*"; Flags: ignoreversion recursesubdirs createallsubdirs
#if Offline == "1"
Source: "{#PayloadRoot}\cache\*"; DestDir: "{app}\payload\cache"; Excludes: "*.download"; Flags: ignoreversion
#endif

[Icons]
Name: "{group}\SAP MCP Manager"; Filename: "{app}\SapMcpManager.exe"
Name: "{userdesktop}\SAP MCP Manager"; Filename: "{app}\SapMcpManager.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:Desktop}"; Flags: unchecked

[Run]
Filename: "{app}\SapMcpManager.exe"; Parameters: "{code:SelectedArguments}"; Description: "{cm:Open}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\SapMcpHost.exe"; Parameters: "--unregister"; Flags: runhidden skipifdoesntexist; RunOnceId: "UnregisterMcpClients"

[UninstallDelete]
Type: files; Name: "{app}\setup-selection.json"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  RfcSelected, AdtSelected, Selection: String;
begin
  if CurStep = ssPostInstall then begin
    RfcSelected := 'false'; AdtSelected := 'false';
    if WizardIsComponentSelected('rfc') then RfcSelected := 'true';
    if WizardIsComponentSelected('adt') then AdtSelected := 'true';
    Selection := '{"id":"' + GetDateTimeString('yyyymmddhhnnsszzz', '-', ':') + '","rfc":' + RfcSelected + ',"adt":' + AdtSelected + '}';
    if not SaveStringToFile(ExpandConstant('{app}\setup-selection.json'), Selection, False) then RaiseException(CustomMessage('SelectionFailed'));
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not WizardIsComponentSelected('rfc') and not WizardIsComponentSelected('adt') then
    Result := CustomMessage('SelectOne');
end;

function SelectedArguments(Param: String): String;
begin
  Result := '--rfc=0 --adt=0';
  if WizardIsComponentSelected('rfc') then Result := Result + ' --rfc=1';
  if WizardIsComponentSelected('adt') then Result := Result + ' --adt=1';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = wpSelectComponents) and not WizardSilent then
    if not WizardIsComponentSelected('rfc') and not WizardIsComponentSelected('adt') then begin
      MsgBox(CustomMessage('SelectOne'), mbInformation, MB_OK);
      Result := False;
    end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataRoot: String;
begin
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then begin
    if MsgBox(CustomMessage('KeepData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON1) = IDNO then begin
      if MsgBox(CustomMessage('DeleteData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then begin
        DataRoot := ExpandConstant('{localappdata}\SapMcpManager');
        if (DataRoot <> '') and (Pos('\SapMcpManager', DataRoot) > 0) then DelTree(DataRoot, True, True, True);
      end;
    end;
  end;
end;
