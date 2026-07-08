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
#define AppName "CWS Tool"
#define AppPublisher "CWS"
#define AppExeName "CWSTool.exe"
#define HostExeName "CWSOpenHost.exe"
#define AppInstallDirName "CWSTools"
#define AppGroupName "CWS Tool"
#define AppShortcutName "CWS Tool"

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
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#AppName}"; ValueData: "Software\CWS Tool\Capabilities"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\CWS Tool"; Flags: uninsdeletekeyifempty
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Routes Office, WPS, and PDF files through CWS Tool preferences."
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: "{app}\{#AppExeName},0"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".doc"; ValueData: "CWSTool.Doc"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".docx"; ValueData: "CWSTool.Docx"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xls"; ValueData: "CWSTool.Xls"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xlsx"; ValueData: "CWSTool.Xlsx"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ppt"; ValueData: "CWSTool.Ppt"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pptx"; ValueData: "CWSTool.Pptx"
Root: HKLM; Subkey: "Software\CWS Tool\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pdf"; ValueData: "CWSTool.Pdf"

Root: HKCR; Subkey: "Applications\{#HostExeName}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Applications\{#HostExeName}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".doc"; ValueData: ""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".docx"; ValueData: ""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".xls"; ValueData: ""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".xlsx"; ValueData: ""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".ppt"; ValueData: ""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".pptx"; ValueData: ""
Root: HKCR; Subkey: "Applications\{#HostExeName}\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""

Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#AppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#AppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#HostExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#HostExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#HostExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"

Root: HKCR; Subkey: "cwstool"; ValueType: string; ValueName: ""; ValueData: "URL:CWS Tool Protocol"; Flags: uninsdeletekey
Root: HKCR; Subkey: "cwstool"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCR; Subkey: "cwstool\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "cwstool\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""

Root: HKCR; Subkey: ".doc\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Doc"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".docx\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Docx"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".xls\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Xls"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".xlsx\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Xlsx"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".ppt\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Ppt"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".pptx\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Pptx"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".pdf\OpenWithProgids"; ValueType: string; ValueName: "CWSTool.Pdf"; ValueData: ""; Flags: uninsdeletevalue

Root: HKCR; Subkey: "CWSTool.Doc"; ValueType: string; ValueName: ""; ValueData: "CWS Tool DOC File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Doc"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool DOC File"
Root: HKCR; Subkey: "CWSTool.Doc\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Doc\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\wordicon\#203.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\wordicon\#203.ico'))
Root: HKCR; Subkey: "CWSTool.Doc\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "CWSTool.Docx"; ValueType: string; ValueName: ""; ValueData: "CWS Tool DOCX File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Docx"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool DOCX File"
Root: HKCR; Subkey: "CWSTool.Docx\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Docx\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\wordicon\#203.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\wordicon\#203.ico'))
Root: HKCR; Subkey: "CWSTool.Docx\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "CWSTool.Xls"; ValueType: string; ValueName: ""; ValueData: "CWS Tool XLS File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Xls"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool XLS File"
Root: HKCR; Subkey: "CWSTool.Xls\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Xls\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\xlicons\#260.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\xlicons\#260.ico'))
Root: HKCR; Subkey: "CWSTool.Xls\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "CWSTool.Xlsx"; ValueType: string; ValueName: ""; ValueData: "CWS Tool XLSX File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Xlsx"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool XLSX File"
Root: HKCR; Subkey: "CWSTool.Xlsx\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Xlsx\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\xlicons\#260.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\xlicons\#260.ico'))
Root: HKCR; Subkey: "CWSTool.Xlsx\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "CWSTool.Ppt"; ValueType: string; ValueName: ""; ValueData: "CWS Tool PPT File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Ppt"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool PPT File"
Root: HKCR; Subkey: "CWSTool.Ppt\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Ppt\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\pptico\#1303.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\pptico\#1303.ico'))
Root: HKCR; Subkey: "CWSTool.Ppt\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "CWSTool.Pptx"; ValueType: string; ValueName: ""; ValueData: "CWS Tool PPTX File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Pptx"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool PPTX File"
Root: HKCR; Subkey: "CWSTool.Pptx\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Pptx\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\pptico\#1303.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\pptico\#1303.ico'))
Root: HKCR; Subkey: "CWSTool.Pptx\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""
Root: HKCR; Subkey: "CWSTool.Pdf"; ValueType: string; ValueName: ""; ValueData: "CWS Tool PDF File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "CWSTool.Pdf"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "CWS Tool PDF File"
Root: HKCR; Subkey: "CWSTool.Pdf\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCR; Subkey: "CWSTool.Pdf\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Assets\Icons\wpsofficeicon\IDI_60APPLICATION.ico"; Check: FileExists(ExpandConstant('{app}\Assets\Icons\wpsofficeicon\IDI_60APPLICATION.ico'))
Root: HKCR; Subkey: "CWSTool.Pdf\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#HostExeName}"" ""%1"""

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
