;Open Rails installer include file
;17-Apr-2014
;Chris Jakeman

; Included from "OpenRails from DVD\OpenRails from DVD.iss" or from "OpenRails from download\OpenRails from download.iss"

#define MyAppName "Open Rails"
#include "Version.iss"
#define MyAppPublisher "Open Rails"
#define MyAppManualName "Open Rails manual"
#define MyAppSourceName "Download Open Rails source code"
#define MyAppBugName "Report a bug in Open Rails"

#define DotNETName "Microsoft .NET Framework 3.5 SP1"
#define XNAName "Microsoft XNA Framework 3.1"

#define MyAppURL "http://openrails.org"
#define MyAppSourceURL "http://openrails.org/download/source/"
#define MyAppSupportURL "http://launchpad.net/or"

#define MyAppExeName "OpenRails.exe"
#define MyAppManual "Documentation\Manual.pdf"

#define XNARedistPath "..\..\..\Microsoft XNA Framework Redistributable 3.1"
#define XNARedist "xnafx31_redist.msi"
#define MyAppProgPath "..\..\..\Open Rails\Program"
#define MyAppDocPath "..\..\..\Open Rails\Documentation"


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
DefaultDirName  ={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons    =yes
LicenseFile     ={#MyAppProgPath}\Copying.txt
InfoAfterFile   =..\Readme.txt
OutputDir       =Output
OutputBaseFilename={#OutputBaseFilename}
Compression     =lzma
SolidCompression=yes
Uninstallable   =yes
UninstallDisplayIcon={app}\{#MyAppExeName}
; Windows XP SP2
MinVersion      =5.1sp2

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "greek"; MessagesFile: "compiler:Languages\Greek.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "serbiancyrillic"; MessagesFile: "compiler:Languages\SerbianCyrillic.isl"
Name: "serbianlatin"; MessagesFile: "compiler:Languages\SerbianLatin.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]

; Don't install these prerequisites until after the licence file has been accepted.
; .NET Framework redistributable
Source: {#NetRedistPath}\{#NetRedist}; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallFrameworkNet35SP1; Check: IsNotInstalledFrameworkNet35SP1

; XNA Framework redistributable
; Can't make this work in the same way as installing .NET Framework. Keep getting error code 2
;Source: {#XNARedistPath}\{#XNARedist}; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallFrameworkXNA31; Check: IsNotInstalledFrameworkXNA31
; Instead, use the clumsier mechanism below: Unpack XNA always, then delete if not needed. Install of XNA is skipped if file is missing.

; Unpack XNA always, then delete if not needed.
Source: {#XNARedistPath}\{#XNARedist}; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: CheckFrameworkXNA31; 

; The game itself
Source: {#MyAppProgPath}\*; Excludes: Readme*.txt; DestDir: {app}; Flags: ignoreversion recursesubdirs
Source: ..\Readme.txt; DestDir: {app}; Flags: ignoreversion
Source: {#MyAppDocPath}\*; DestDir: {app}\Documentation; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppManualName}"; Filename: "{app}\{#MyAppManual}"
Name: "{group}\{#MyAppSourceName}"; Filename: "{#MyAppSourceURL}"
Name: "{group}\{#MyAppBugName}"; Filename: "{#MyAppSupportURL}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; 'skipifdoesntexist' ensures that install of XNA is skipped if file is missing.
Filename: msiexec.exe; StatusMsg: "Installing {#XNAName} (takes about 1 min) ..."; Parameters: "/qn /i ""{tmp}\{#XNARedist}"""; Flags: skipifdoesntexist;

Filename: "{app}\{#MyAppExeName}"; StatusMsg: "Installing Open Rails ..."; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
 
[Code]
function IsNotInstalledFrameworkNet35SP1: Boolean;
var
  data: Cardinal;
  StatusText: string;
begin
  // Gets left on screen while file is unpacked.
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Unpacking {#DotNETName}...';
  result := true;
  if (RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v3.5', 'Install', data)) then begin
    if (data = 1) then begin
      if (RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v3.5', 'SP', data)) then begin
        if (data = 1) then begin
          result := false
          // Prompt is repeated. Help suggests a way around this.
          //MsgBox('{#DotNETName} is already installed', mbError, MB_OK);
        end;
      end;
    end;
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;

procedure CheckFrameworkXNA31;
var
    key: string;
    data: cardinal;
    StatusText: string;
begin
  // Gets left on screen while file is unpacked.
  if IsWin64 then begin
    key := 'SOFTWARE\Wow6432Node\Microsoft\XNA\Framework\v3.1';
  end else begin
    key := 'SOFTWARE\Microsoft\XNA\Framework\v3.1';
  end;
  if RegQueryDWordValue(HKLM, key, 'Installed', data) and (data = 1) then begin
    DeleteFile(ExpandConstant('{tmp}\{#XNARedist}')); // So the [Run]Filename is skipped.
    //MsgBox('{#XNAName} is already installed', mbError, MB_OK);
  end;                                                              
end;

(*
function IsNotInstalledFrameworkXNA31: Boolean;
var
    key: string;
    data: cardinal;
begin
  // Gets left on screen while file is unpacked.
  WizardForm.StatusLabel.Caption := 'Unpacking {#XNAName}...';
  if IsWin64 then begin
    key := 'SOFTWARE\Wow6432Node\Microsoft\XNA\Framework\v3.1';
  end else begin
    key := 'SOFTWARE\Microsoft\XNA\Framework\v3.1';
  end;
  result := true;
  if RegQueryDWordValue(HKLM, key, 'Installed', data) and (data = 1) then begin
    result := false;
    //MsgBox('{#XNAName} is already installed', mbError, MB_OK);
  end;                                                              
end;

procedure InstallFrameworkXNA31;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Installing {#XNAName}...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    // XNA setup execution code
    begin
      // Note: msiexec uses slightly different parameters from dotnetfx35.exe
      //if not Exec(ExpandConstant('msiexec.exe /package {tmp}\xnafx31_redist.msi'), ' /q /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      //if not Exec(ExpandConstant('msiexec.exe /package {tmp}\xnafx31_redist.msi'), '/q /norestart /l C:\logfile.txt', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      if not ShellExec('', ExpandConstant('msiexec.exe /package {tmp}\xnafx31_redist.msi'), '/q /norestart /l C:\logfile.txt', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      //if not ShellExec('', ExpandConstant('{app}\README.txt'), '', '', SW_SHOW, ewNoWait, ResultCode) then
           begin
        // Tell the user why the installation failed
        MsgBox('Installing {#XNAName} failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
      end;
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;
*)