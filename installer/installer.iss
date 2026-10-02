; ---------------------------------------------------------------------------
; Celsius installer (Inno Setup 6)
;
; Packages the self-contained Celsius build and bundles the PawnIO kernel
; driver setup. Before installing, it checks whether PawnIO is present and
; current, and warns the user when it is missing or outdated.
;
; Build with:
;   iscc /DAppVersion=0.1.0-beta ^
;        /DPublishDir=..\publish ^
;        /DPawnIoSetup=..\vendor\PawnIO_setup.exe ^
;        installer.iss
; ---------------------------------------------------------------------------

#ifndef AppVersion
  #define AppVersion "0.1.0-beta"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef PawnIoSetup
  #define PawnIoSetup "..\vendor\PawnIO_setup.exe"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

; Minimum PawnIO version Celsius requires. LibreHardwareMonitor 0.9.6 needs the
; PawnIO 2.x driver API; older 1.x installs do not expose the required device.
#define PawnIoMinVersion "2.0.0.0"

[Setup]
AppId={{7C2F5B4E-9A31-4D6E-8B0F-C1E5A9D3F210}
AppName=Celsius
AppVersion={#AppVersion}
AppVerName=Celsius {#AppVersion}
AppPublisher=burakdmrbkr
AppPublisherURL=https://github.com/burakdmrbkr/Celsius
AppSupportURL=https://github.com/burakdmrbkr/Celsius/issues
AppUpdatesURL=https://github.com/burakdmrbkr/Celsius/releases
DefaultDirName={autopf}\Celsius
DefaultGroupName=Celsius
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=Celsius-{#AppVersion}-win-x64-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Celsius ships a requireAdministrator manifest. Because reading temperatures
; needs the PawnIO kernel driver, the installer must be elevated too (it also
; installs the driver).
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
UninstallDisplayIcon={app}\Celsius.exe
; The bundled PawnIO_setup.exe is a signed third-party binary: keep it in the
; install payload so users can re-run it if the driver is ever removed.
DisableDirPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[CustomMessages]
english.PawnIoMissingBody=PawnIO driver not detected.%n%nCelsius needs the PawnIO kernel driver to read CPU and GPU temperatures.%n%nPawnIO was not detected on this computer. Setup will install it for you.%n%nWithout PawnIO, Celsius still runs, but temperatures and the thermal guard will be unavailable.
english.PawnIoOutdatedBody=PawnIO driver is outdated.%n%nAn older PawnIO driver (%1) is installed.%n%nCelsius requires PawnIO %2 or newer. Setup will update it for you.
english.PawnIoFailedBody=PawnIO installation failed.%n%nThe bundled PawnIO setup exited with code %1.%n%nCelsius was installed, but temperature sensors will not work until PawnIO is installed. You can retry later by running:%n%2
english.PawnIoInstallTask=Install the PawnIO kernel driver (required for temperature sensors)
english.PawnIoInstallTaskDesc=Installs PawnIO, the signed kernel driver Celsius uses to read CPU/GPU temperatures.
english.RunCelsius=Launch Celsius
turkish.PawnIoMissingBody=PawnIO sürücüsü bulunamadı.%n%nCelsius'un işlemci ve ekran kartı sıcaklıklarını okuyabilmesi için PawnIO çekirdek sürücüsüne ihtiyaç vardır.%n%nBu bilgisayarda PawnIO bulunamadı. Kurulum sizin için PawnIO'yu kuracaktır.%n%nPawnIO olmadan Celsius çalışmaya devam eder, ancak sıcaklıklar ve termal koruma kullanılamaz.
turkish.PawnIoOutdatedBody=PawnIO sürücüsü eski.%n%nEski bir PawnIO sürücüsü (%1) kurulu.%n%nCelsius, PawnIO %2 veya daha yenisini gerektirir. Kurulum sizin için güncelleyecektir.
turkish.PawnIoFailedBody=PawnIO kurulumu başarısız.%n%nPaketteki PawnIO kurulumu %1 koduyla sona erdi.%n%nCelsius kuruldu, ancak PawnIO kurulana kadar sıcaklık sensörleri çalışmayacaktır. Daha sonra şunu çalıştırarak tekrar deneyebilirsiniz:%n%2
turkish.PawnIoInstallTask=PawnIO çekirdek sürücüsünü kur (sıcaklık sensörleri için gerekli)
turkish.PawnIoInstallTaskDesc=PawnIO'yu kurar: Celsius'un CPU/GPU sıcaklıklarını okumak için kullandığı imzalı çekirdek sürücüsü.
turkish.RunCelsius=Celsius'u başlat

[Tasks]
Name: "installpawnio"; Description: "{cm:PawnIoInstallTask}"; GroupDescription: "{cm:PawnIoInstallTaskDesc}"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained Celsius payload (produced by `dotnet publish -o publish`).
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Bundled PawnIO installer, kept for manual re-install/repair.
Source: "{#PawnIoSetup}"; DestDir: "{app}\pawnio"; DestName: "PawnIO_setup.exe"; Flags: ignoreversion

[Icons]
Name: "{group}\Celsius"; Filename: "{app}\Celsius.exe"
Name: "{group}\Uninstall Celsius"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Celsius"; Filename: "{app}\Celsius.exe"; Tasks: desktopicon

[Run]
; PawnIO itself is installed from [Code] (CurStepChanged) so its exit code can
; be checked; only Celsius is launched from here.
Filename: "{app}\Celsius.exe"; Description: "{cm:RunCelsius}"; \
  Flags: nowait postinstall skipifsilent

[Code]
const
  PawnIoUninstallKey = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO';
  PawnIoRequired = '{#PawnIoMinVersion}';

{ Reads the installed PawnIO DisplayVersion from the 32-bit or 64-bit
  uninstall registry hive. Returns '' when PawnIO is not installed. }
function GetInstalledPawnIoVersion(): String;
var
  Version: String;
begin
  Version := '';
  if not RegQueryStringValue(HKLM, PawnIoUninstallKey, 'DisplayVersion', Version) then
  begin
    { The installer may live in the 64-bit hive on x64 systems. }
    if not RegQueryStringValue(HKLM64, PawnIoUninstallKey, 'DisplayVersion', Version) then
      Version := '';
  end;
  Result := Version;
end;

{ Parses up to four numeric components of a dotted version string into V. }
procedure ParseVersionParts(const S: String; var V1, V2, V3, V4: Integer);
var
  Idx, Start, Comp: Integer;
  Num: String;
  Values: array[0..3] of Integer;
begin
  for Idx := 0 to 3 do
    Values[Idx] := 0;

  Comp := 0;
  Start := 1;
  for Idx := 1 to Length(S) + 1 do
  begin
    if (Idx > Length(S)) or (S[Idx] = '.') then
    begin
      Num := Copy(S, Start, Idx - Start);
      if Comp <= 3 then
        Values[Comp] := StrToIntDef(Num, 0);
      Inc(Comp);
      Start := Idx + 1;
    end;
  end;

  V1 := Values[0];
  V2 := Values[1];
  V3 := Values[2];
  V4 := Values[3];
end;

{ Compares two dotted version strings. Returns >0 if A>B, 0 if equal, <0 if A<B.
  Missing components are treated as 0, so '2.2' == '2.2.0.0'. }
function CompareVersions(A, B: String): Integer;
var
  A1, A2, A3, A4: Integer;
  B1, B2, B3, B4: Integer;
begin
  ParseVersionParts(A, A1, A2, A3, A4);
  ParseVersionParts(B, B1, B2, B3, B4);

  if A1 <> B1 then
  begin
    Result := A1 - B1;
    Exit;
  end;
  if A2 <> B2 then
  begin
    Result := A2 - B2;
    Exit;
  end;
  if A3 <> B3 then
  begin
    Result := A3 - B3;
    Exit;
  end;
  Result := A4 - B4;
end;

{ Runs the bundled PawnIO setup and returns its exit code (-1 when it could not
  be started). 0 = success, 3010 = success but reboot required. }
function RunPawnIoSetup(): Integer;
var
  SetupPath: String;
  ResultCode: Integer;
begin
  SetupPath := ExpandConstant('{app}\pawnio\PawnIO_setup.exe');
  if not FileExists(SetupPath) then
  begin
    Result := -1;
    Exit;
  end;

  if not Exec(SetupPath, '-install', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := -1
  else
    Result := ResultCode;
end;

{ Minimal %1/%2 substitution because FmtMessage expects a dynamic array. }
function FillTemplate(const Template, A, B: String): String;
begin
  Result := Template;
  StringChangeEx(Result, '%1', A, True);
  StringChangeEx(Result, '%2', B, True);
end;

function InitializeSetup(): Boolean;
var
  Installed: String;
  Message: String;
begin
  Result := True;

  Installed := GetInstalledPawnIoVersion();

  if Installed = '' then
  begin
    { Not installed -> warn the user before continuing. }
    MsgBox(CustomMessage('PawnIoMissingBody'), mbInformation, MB_OK);
  end
  else if CompareVersions(Installed, PawnIoRequired) < 0 then
  begin
    { Installed but older than required -> warn. }
    Message := FillTemplate(CustomMessage('PawnIoOutdatedBody'), Installed, PawnIoRequired);
    MsgBox(Message, mbInformation, MB_OK);
  end;
end;

procedure ShowPawnIoFailure(const Rc: Integer);
var
  Message: String;
  ExePath: String;
begin
  ExePath := ExpandConstant('{app}\pawnio\PawnIO_setup.exe');
  Message := FillTemplate(CustomMessage('PawnIoFailedBody'), IntToStr(Rc), ExePath);
  MsgBox(Message, mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Installed: String;
  Rc: Integer;
begin
  if CurStep <> ssPostInstall then
    Exit;

  { The PawnIO install task is selected by default. Only run it when the driver
    is actually missing or out of date, so a current install is left untouched. }
  if not WizardIsTaskSelected('installpawnio') then
    Exit;

  Installed := GetInstalledPawnIoVersion();
  if (Installed <> '') and (CompareVersions(Installed, PawnIoRequired) >= 0) then
    Exit;

  Rc := RunPawnIoSetup();
  if (Rc <> 0) and (Rc <> 3010) then
    ShowPawnIoFailure(Rc);
end;
