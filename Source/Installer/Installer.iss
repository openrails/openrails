; Open Rails installer

; Build the installer by running `Build.cmd stable`:
; - Installer created in `./Output/OpenRailsSetup.exe`
; - `Build.cmd` will move that to `../OpenRails-<mode>-Setup.exe`.

; From https://github.com/DomGries/InnoDependencyInstaller
#include "CodeDependencies.iss"

#define MyAppName "Open Rails"
#include "Version.iss"  ; provides the version number
#define MyAppPublisher "Open Rails Project"
#define MyAppManualName "Open Rails manual"
#define MyAppSourceName "Download Open Rails source code"
#define MyAppBugName "Report a bug in Open Rails"

#define MyAppURL "https://openrails.org"
#define MyAppSourceURL "http://openrails.org/download/source/"
#define MyAppSupportURL "https://launchpad.net/or"

#define MyAppExeName "OpenRails.exe"
#define MyAppManual "Documentation\Manual.pdf"

#define MyAppProgPath "..\..\Program"
#define MyAppDocPath "..\..\Program\Documentation"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, use Tools > Generate GUID.)
AppId={{94E15E08-869D-4B69-B8D7-8C82075CB51C} ; Generated for OpenRails pre-v1.0

AppName         ={#MyAppName}
AppVersion      ={#MyAppVersion}
AppVerName      ={#MyAppName} {#MyAppVersion}
AppPublisher    ={#MyAppPublisher}
AppPublisherURL ={#MyAppURL}
AppSupportURL   ={#MyAppSupportURL}
AppUpdatesURL   ={#MyAppURL}
DefaultDirName  ={commonpf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons    =yes
LicenseFile     ={#MyAppProgPath}\Copying.txt
InfoBeforeFile  ={#MyAppProgPath}\Prerequisites.txt
InfoAfterFile   ={#MyAppProgPath}\Readme.txt

; Prompt for a destination folder
DisableDirPage  =no
; 32-bit is the default, installing in "Program Files (x86)" on 64-bit Windows. "x64compatible" uses Program Files on 64-bit Windows
ArchitecturesInstallIn64BitMode =x64compatible

; Default is admin install mode.
; Comment in the following line to run in non-administrative install mode, but that cannot create the directory C:\Program Files\Open Rails
; PrivilegesRequired=lowest

Compression     =lzma
SolidCompression=yes
WizardStyle     =modern
Uninstallable   =yes
UninstallDisplayIcon ={app}\{#MyAppExeName}
OutputBaseFilename =OpenRailsSetup

; Windows 10 Version 1909 (November 2019 Update)
MinVersion      =10.0.18363

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
;Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
; The game itself
; Readme.txt is copied from Source\RunActivity\Readme.txt
Source: {#MyAppProgPath}\*; Excludes: Readme*.txt; DestDir: {app}; Flags: ignoreversion recursesubdirs
Source: ..\..\Program\Readme.txt; DestDir: {app}; Flags: ignoreversion
Source: {#MyAppDocPath}\*; DestDir: {app}\Documentation; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppManualName}"; Filename: "{app}\{#MyAppManual}"
Name: "{group}\{#MyAppSourceName}"; Filename: "{#MyAppSourceURL}"
Name: "{group}\{#MyAppBugName}"; Filename: "{#MyAppSupportURL}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; StatusMsg: "Installing Open Rails ..."; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
 

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddDotNet60Desktop; // .NET Desktop Runtime 6.0

  Result := True;
end;
