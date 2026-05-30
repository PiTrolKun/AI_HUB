#ifndef AppVersion
#error AppVersion is required. Pass /DAppVersion=...
#endif

#ifndef PublishDir
#error PublishDir is required. Pass /DPublishDir=...
#endif

#ifndef OutputDir
#error OutputDir is required. Pass /DOutputDir=...
#endif

#ifndef SetupIconFile
#error SetupIconFile is required. Pass /DSetupIconFile=...
#endif

#define AppName "AI HUB"
#define AppPublisher "AI_HUB"
#define AppExeName "AIHub.exe"

[Setup]
AppId={{85E9F5C5-2B18-43B1-84E2-A99B25E9B9E8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/PiTrolKun/AI_HUB
AppSupportURL=https://github.com/PiTrolKun/AI_HUB
AppUpdatesURL=https://github.com/PiTrolKun/AI_HUB
DefaultDirName={localappdata}\Programs\AI HUB
DefaultGroupName=AI HUB
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=AI_HUB_Setup_{#AppVersion}
SetupIconFile={#SetupIconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\AI HUB"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\AI HUB"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Запустить AI HUB"; Flags: nowait postinstall skipifsilent
