;Open Rails installer                                 
;12-Jul-2021
;Chris Jakeman

; Assuming that Build.cmd is run from its directory then, in the same directory, this installer for "stable" requires file:
;   ".NET Framework 4.7.2 web installer\ndp472-kb4054531-web.exe"
; which can be downloaded from:
;   http://go.microsoft.com/fwlink/?LinkId=863262
; and creates:
;   Open Rails/Program/*
;   Source/Installer/Output/OpenRailsSetup.exe
; Build.cmd for "stable" will move OpenRailSetup.exe back into .\OpenRails-<mode>-Setup.exe.

#define MyAppName "Open Rails"
#include "Version.iss"  ; provides the version number
#define MyAppPublisher "Open Rails Project"
#define MyAppManualName "Open Rails manual"
#define MyAppSourceName "Download Open Rails source code"
#define MyAppBugName "Report a bug in Open Rails"

#define DotNETName "Microsoft .NET Framework 4.7.2"

#define MyAppURL "http://openrails.org" ; Not yet HTTPS
#define MyAppSourceURL "http://openrails.org/download/source/"
#define MyAppSupportURL "https://launchpad.net/or"

#define MyAppExeName "OpenRails.exe"
#define MyAppManual "Documentation\Manual.pdf"

#define MyAppProgPath "..\..\Program"
#define MyAppDocPath "..\..\Program\Documentation"

#define NetRedistPath "..\..\.NET Framework 4.7.2 web installer"
#define NetRedist "NDP472-KB4054531-Web.exe"

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
InfoBeforeFile   ={#MyAppProgPath}\Prerequisites.txt
InfoAfterFile   ={#MyAppProgPath}\Readme.txt

; Remove the following line to run in administrative install mode (install for all users.)
; PrivilegesRequired=lowest ; Cannot create the directory C:\Program Files\Open Rails

Compression     =lzma
SolidCompression=yes
WizardStyle=modern
Uninstallable   =yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename=OpenRailsSetup

; Windows 7 SP1
MinVersion      =6.1sp1

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
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
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
; Don't install these prerequisites until after the licence file has been accepted.
; .NET Framework redistributable
Source: {#NetRedistPath}\{#NetRedist}; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallFrameworkNet472; Check: IsNotInstalledFrameworkNet472

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
function IsNotInstalledFrameworkNet472: Boolean;
var
  data: Cardinal;
  StatusText: string;
begin
  // Gets left on screen while file is unpacked.
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Checking for prerequisite {#DotNETName}...';
  Result := true; // Result is a pre-declared return value
  if (RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', data)) then begin
    // "or" operator doesn't work
    if (IntToStr(data) = '461808') then Result := false; // v4.7.2
    if (IntToStr(data) = '461814') then Result := false; // v4.7.2
    if (IntToStr(data) = '528040') then Result := false; // v4.8
    if (IntToStr(data) = '528372') then Result := false; // v4.8
    if (IntToStr(data) = '528049') then Result := false; // v4.8
  end;
  if (Result = true) then 
    WizardForm.StatusLabel.Caption := 'Installing Open Rails ...';
end;

procedure InstallFrameworkNet472;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Installing {#DotNETName} (takes about 8 mins and downloads 82MB)...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    begin
      // Install the package
      if not Exec(ExpandConstant('{tmp}\{#NetRedist}'), ' /q /noreboot', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      begin
        // Tell the user why the installation failed
        MsgBox('Installing {#DotNETName} failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
      end;
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;
