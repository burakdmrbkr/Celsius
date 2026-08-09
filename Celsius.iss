; Celsius kurulum betiği — Inno Setup 6
; Kurulum: uygulamanın tek-dosya (self-contained) publish çıktısını kurar,
; ayrıca CPU sıcaklığı için gereken PawnIO çekirdek sürücüsünü KURULUM ANINDA bir kez kurar.
; Sürücü kurulduğunda kalıcıdır; sonraki kurulumlar atlar (kayıt defteri kontrolü).
; Ek güvenlik ağı: portable/single-exe kullananlar için uygulama ilk açılışta da kurabilir.

#define MyAppName "Celsius"
#ifndef MyAppVersion
  #define MyAppVersion "1.1-beta"
#endif
#define MyAppPublisher "burakdmrbkr"
#define MyAppExeName "Celsius.exe"

[Setup]
AppId={{8E3B9F2D-6B2D-4B1E-9C5A-3D6E7A8B9C0D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Celsius
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
OutputDir=dist
OutputBaseFilename=CelsiusSetup-{#MyAppVersion}-win-x64

[Tasks]
Name: "desktopicon"; Description: "Masaüstüne kısayol ekle"; GroupDescription: "Ek görevler:"

[Files]
Source: "publish-selfcontained\Celsius.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Celsius\Resources\PawnIO_setup.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName}'u şimdi başlat"; Flags: nowait postinstall skipifsilent

[Code]
const
  PAWN_UNINSTALL_KEY = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO';

// PawnIO zaten kurulu mu? (kalıcı — yeniden kurmaya gerek yok)
function PawnInstalled(): Boolean;
var
  DummyVersion: String;
begin
  Result := RegQueryStringValue(HKLM64, PAWN_UNINSTALL_KEY, 'DisplayVersion', DummyVersion);
end;

// Sürücüyü kur. Dönüş: 0=bitti, 1=zaten var, 2=kullanıcı reddetti, 3=hata
function EnsurePawnDriver(): Integer;
var
  ResultCode: Integer;
begin
  Result := 1;
  if PawnInstalled then
    exit;

  if MsgBox('Celsius, CPU sıcaklığını okuyabilmek için küçük bir çekirdek sürücüsü olan ' +
      'PawnIO''yu kullanır. Bu sürücü yalnızca BIR KEZ kurulacak ve bilgisayarda kalıcı olacak; ' +
      'sonraki kurulum ve güncellemelerde tekrar sorulmayacak.' + #13#10#13#10 +
      'Sürücü şimdi kurulsun mu?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := 2;
    exit;
  end;

  if Exec(ExpandConstant('{app}\PawnIO_setup.exe'), '-install', '', SW_HIDE,
      ewWaitUntilTerminated, ResultCode) then
  begin
    if PawnInstalled then
      Result := 0
    else
      Result := 3;
  end
  else
    Result := 3;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  R: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    R := EnsurePawnDriver;
    if R = 2 then
      MsgBox('PawnIO sürücüsü kurulmadı. CPU sıcaklığı görünmeyebilir; ' +
          'dilerseniz uygulama ilk açılışında size sorarak yeniden deneyecek.', mbInformation, MB_OK)
    else if R = 3 then
      MsgBox('PawnIO sürücüsü kurulamadı. Uygulama ilk açılışında otomatik tekrar deneyecek.', mbInformation, MB_OK);
  end;
end;