#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

#ifndef SourceDir
#define SourceDir "publish\" + AppVersion
#endif

#ifndef OutputDir
#define OutputDir "installer"
#endif

#define AppIdValue "{7D0AE770-57A4-4AA2-93C0-C9E389F1D5AC}"
#define AppName "CWS Office WPS Toolbox"
#define AppPublisher "CWS"
#define AppExeName "Gallery.exe"
#define AppInstallDirName "CWSTools"
#define AppGroupName "CWSTools"
#define AppShortcutName "CWSTools"

[Setup]
AppId={{#AppIdValue}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppInstallDirName}
DefaultGroupName={#AppGroupName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=CWSTools-{#AppVersion}-Setup
SetupIconFile=Assets\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
