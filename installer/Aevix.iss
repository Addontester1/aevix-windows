; ============================================================================
;  Aevix for Windows — Inno Setup script
;
;  Produces a single setup.exe that installs Aevix into
;  %ProgramFiles%\Aevix, creates Start Menu + Desktop shortcuts, and
;  registers an uninstaller in Add/Remove Programs.
;
;  Build:
;    1. Install Inno Setup 6 from https://jrsoftware.org/isdl.php
;    2. From a Developer Command Prompt or just Explorer, right-click this
;       file → Compile.  Or from a terminal:
;          "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\Aevix.iss
;    3. The setup.exe lands in publish\installer\Aevix-Setup-v1.0.0.exe
;
;  Re-run `dotnet publish` first (see README) to refresh publish\win-x64\
;  with the latest build before recompiling the installer.
; ============================================================================

#define AppName        "Aevix"
#define AppVersion     "1.0.0"
#define AppPublisher   "Aevix"
#define AppURL         "https://github.com/Addontester1/aevix-windows"
#define AppExeName     "Aevix.App.exe"
#define SourceFolder   "..\publish\win-x64"

[Setup]
AppId={{C5E2A1D3-3F9E-4F5E-A2D7-9B41E8C3F411}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
VersionInfoVersion={#AppVersion}.0

; Per-machine install into Program Files. Use {autopf} so x86 vs x64 works
; even if you ever publish a 32-bit build.
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; Only allow installing on 64-bit Windows because we publish win-x64.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Output: publish\installer\Aevix-Setup-v1.0.0.exe
OutputDir=..\publish\installer
OutputBaseFilename=Aevix-Setup-v{#AppVersion}

Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
; Pull in everything from the publish folder. recursesubdirs picks up
; the libvlc\ native binaries and any other resource trees.
Source: "{#SourceFolder}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";       Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; "Launch Aevix" checkbox on the final wizard page.
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Leave the user's library in %LOCALAPPDATA%\Aevix alone — playlists +
; settings + crash.log survive an uninstall. If you want to wipe them
; uncomment the next line.
; Type: filesandordirs; Name: "{localappdata}\Aevix"
