@ECHO OFF
SETLOCAL ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION

ECHO  ##############################################################################
ECHO  #         ___                             ____            _   _              #
ECHO  #        / _ \   _ __     ___   _ __     ^|  _ \    __ _  (_) ^| ^|  ___        #
ECHO  #       ^| ^| ^| ^| ^| '_ \   / _ \ ^| '_ \    ^| ^|_) ^|  / _` ^| ^| ^| ^| ^| / __^|       #
ECHO  #       ^| ^|_^| ^| ^| ^|_) ^| ^|  __/ ^| ^| ^| ^|   ^|  _ ^<  ^| (_^| ^| ^| ^| ^| ^| \__ \       #
ECHO  #        \___/  ^| .__/   \___^| ^|_^| ^|_^|   ^|_^| \_\  \__,_^| ^|_^| ^|_^| ^|___/       #
ECHO  #               ^|_^|                                                          #
ECHO  ##############################################################################
ECHO.
ECHO This script will build Open Rails. Syntax:
ECHO.
ECHO %0 MODE
ECHO.
ECHO   MODE          Selects the mode to build with:
ECHO     unstable      Doesn't include documentation or installers
ECHO     testing       Includes documentation but not installers
ECHO     stable        Includes documentation and installers
ECHO.

REM Check for necessary tools.
ECHO The following tools must be available in %%PATH%% for the build to work:
ECHO [UTS] indicates which build modes need the tool: unstable, testing, and stable.
SET CheckToolInPath.Missing=0
SET CheckToolInPath.Check=0
:check-tools
CALL :list-or-check-tool "git.exe" "[UTS] Git version control tool"
CALL :list-or-check-tool "nuget.exe" "[UTS] .NET package manager tool"
CALL :list-or-check-tool "MSBuild.exe" "[UTS] Microsoft Visual Studio build tool"
CALL :list-or-check-tool "xunit.console.x86.exe" "[UTS] XUnit tool"
CALL :list-or-check-tool "rcedit-x86.exe" "[UTS] Electron rcedit tool"
CALL :list-or-check-tool "7za.exe" "[UTS] 7-zip tool"
CALL :list-or-check-tool "OfficeToPDF.exe" "[TS] Office-to-PDF conversion tool"
CALL :list-or-check-tool "iscc.exe" "[S] Inno Setup 5 compiler"
IF "%CheckToolInPath.Check%" == "0" (
	ECHO.
	SET CheckToolInPath.Check=1
	GOTO :check-tools
)

REM Parse command line
SET Mode=-
SET Flag.Changelog=0
SET Flag.Updater=0
:parse-command-line
IF /I "%~1" == "unstable" SET Mode=Unstable
IF /I "%~1" == "testing"  SET Mode=Testing
IF /I "%~1" == "stable"   SET Mode=Stable
SHIFT /1
IF NOT "%~1" == "" GOTO :parse-command-line
IF "%Mode%" == "-" (
	>&2 ECHO ERROR: No build mode specified.
	ECHO Run "Build.cmd MODE" where MODE is "unstable", "testing" or "stable".
	EXIT /B 1
)
IF %CheckToolInPath.Missing% GTR 0 (
	TIMEOUT /T 10
)

REM Check for necessary directory.
IF NOT EXIST "Source\ORTS.sln" (
	>&2 ECHO ERROR: Unexpected current directory.
	ECHO Run "Build.cmd" in the parent directory of "ORTS.sln" ^(the directory "Build.cmd" is in^).
	EXIT /B 1
)

IF "%Mode%" == "Stable" (
	CALL :create ".NET Framework 4.7.2 web installer"
	IF NOT EXIST ".NET Framework 4.7.2 web installer\ndp472-kb4054531-web.exe" (
		>&2 ECHO ERROR: Missing required file for "%Mode%" build: ".NET Framework 4.7.2 web installer\ndp472-kb4054531-web.exe".
		>&2 ECHO "Download from http://go.microsoft.com/fwlink/?LinkId=863262"
		EXIT /B 1
	)
)

REM Get product version and code revision.
FOR /F "usebackq tokens=1* delims==" %%A IN (`CALL GetVersion.cmd %Mode%`) DO SET %%A=%%B

REM Restore NuGet packages.
nuget restore Source\ORTS.sln || GOTO :error

REM Recreate Program directory for output and delete previous build files.
CALL :recreate "Program" || GOTO :error
CALL :delete "OpenRails-%Mode%*" || GOTO :error

REM Build main program.
REM Disable warning CS1591 "Missing XML comment for publicly visible type or member".
SET BuildConfiguration=Release
IF "%Mode%" == "Unstable" SET BuildConfiguration=Debug
MSBuild Source\ORTS.sln /t:Clean;Build /p:Configuration=%BuildConfiguration% /p:NoWarn=1591 || GOTO :error

REM Set update channel.
>>Program\Updater.ini ECHO Channel=string:%Mode% || GOTO :error
ECHO Set update channel to "%Mode%".

REM Build locales.
PUSHD Source\Locales && CALL Update.bat non-interactive && POPD || GOTO :error

REM Run unit tests (9009 means XUnit itself wasn't found, which is an error).
xunit.console.x86 Program\Tests.dll -nunit xunit.xml
IF "%ERRORLEVEL%" == "9009" GOTO :error

REM Copy the web content
ROBOCOPY /MIR /NJH /NJS "Source\RunActivity\Viewer3D\WebServices\Web" "Program\Content\Web"
IF %ERRORLEVEL% GEQ 8 GOTO :error

REM Copy version number from OpenRails.exe into all other 1st party files
FOR %%F IN ("Program\*.exe", "Program\Orts.*.dll", "Program\Contrib.*.dll", "Program\Tests.dll") DO (
	rcedit-x86.exe "%%~F" --set-product-version %OpenRails_Revision% --set-version-string ProductVersion %OpenRails_Version% || GOTO :error
)
ECHO Set product version information to "%OpenRails_Version%".

REM *** Special build step: signs binaries ***
IF NOT "%JENKINS_TOOLS%" == "" (
	FOR /R "Program" %%F IN (*.exe *.dll) DO CALL "%JENKINS_TOOLS%\sign.cmd" "%%~F" || GOTO :error
)

IF NOT "%Mode%" == "Unstable" (
	REM Restart the Office Click2Run service as this frequently breaks builds.
	NET stop ClickToRunSvc
	NET start ClickToRunSvc

	REM Create the documentation folders for output.
	CALL :create "Program\Documentation" || GOTO :error
	CALL :create "Program\Documentation\Online" || GOTO :error
	CALL :create "Program\Documentation\es" || GOTO :error

	REM Compile the documentation.
	FOR %%E IN (doc docx docm xls xlsx xlsm odt) DO FOR %%F IN ("Source\Documentation\*.%%E") DO ECHO %%~F && OfficeToPDF.exe /bookmarks /print "%%~F" "Program\Documentation\%%~nF.pdf" || GOTO :error
	FOR %%E IN (doc docx docm xls xlsx xlsm odt) DO FOR %%F IN ("Source\Documentation\Online\*.%%E") DO ECHO %%~F && OfficeToPDF.exe /bookmarks /print "%%~F" "Program\Documentation\Online\%%~nF.pdf" || GOTO :error
	>"Source\Documentation\Manual\version.py" ECHO version = '%OpenRails_Version%' || GOTO :error
	>>"Source\Documentation\Manual\version.py" ECHO release = '%OpenRails_Revision%' || GOTO :error
	PUSHD "Source\Documentation\Manual" && CALL make.bat clean & POPD || GOTO :error
	PUSHD "Source\Documentation\Manual" && CALL make.bat latexpdf && POPD || GOTO :error

	REM Copy the documentation.
	FOR %%F IN ("Source\Documentation\*.pdf") DO CALL :copy "%%~F" "Program\Documentation\%%~nF.pdf" || GOTO :error
	CALL :copy "Source\Documentation\Manual\_build\latex\Manual.pdf" "Program\Documentation\Manual.pdf" || GOTO :error
	CALL :copy "Source\Documentation\Manual\es\Manual.pdf" "Program\Documentation\es\Manual.pdf" || GOTO :error
	ROBOCOPY /MIR /NJH /NJS "Source\Documentation\SampleFiles" "Program\Documentation\SampleFiles"
	IF %ERRORLEVEL% GEQ 8 GOTO :error

	REM Copy the documentation separately.
	FOR %%F IN ("Program\Documentation\*.pdf") DO CALL :copy "%%~F" "OpenRails-%Mode%-%%~nxF" || GOTO :error
	FOR %%F IN ("Program\Documentation\Online\*.pdf") DO CALL :copy "%%~F" "OpenRails-%Mode%-%%~nxF" || GOTO :error
)

IF "%Mode%" == "Stable" (
	ROBOCOPY /MIR /NJH /NJS "Program" "Open Rails\Program" /XD Documentation
	IF %ERRORLEVEL% GEQ 8 GOTO :error
	ROBOCOPY /MIR /NJH /NJS "Program\Documentation" "Open Rails\Documentation"
	IF %ERRORLEVEL% GEQ 8 GOTO :error
	>"Source\Installer\Version.iss" ECHO #define MyAppVersion "%OpenRails_Version%" || GOTO :error
	iscc "Source\Installer\Installer.iss" || GOTO :error
	CALL :move "Source\Installer\Output\OpenRailsSetup.exe" "OpenRails-%Mode%-Setup.exe" || GOTO :error
	REM *** Special build step: signs binaries ***
	IF NOT "%JENKINS_TOOLS%" == "" CALL "%JENKINS_TOOLS%\sign.cmd" "OpenRails-%Mode%-Setup.exe" || GOTO :error
)

REM Create binary and source zips.
PUSHD "Program" && 7za.exe a -r -tzip -x^^!*.xml -x^^!Online "..\OpenRails-%Mode%.zip" . && POPD || GOTO :error
7za.exe a -r -tzip -x^^!.* -x^^!obj -x^^!lib -x^^!_build -x^^!*.bak "OpenRails-%Mode%-Source.zip" "Source" || GOTO :error

ENDLOCAL
GOTO :EOF

REM Lists or checks for a single tool
:list-or-check-tool
IF "%CheckToolInPath.Check%" == "0" GOTO :list-tool
IF "%CheckToolInPath.Check%" == "1" GOTO :check-tool
GOTO :EOF

REM Lists a tool using the same arguments as :check-tool
:list-tool
SETLOCAL
SET Tool.File=%~1                      .
SET Tool.Name=%~2                                                  .
ECHO   - %Tool.File:~0,22% %Tool.Name:~0,52%
ENDLOCAL
GOTO :EOF

REM Checks for a tool (%1) exists in %PATH% and reports a warning otherwise (%2 is descriptive name for tool).
:check-tool
IF "%~$PATH:1" == "" (
	>&2 ECHO WARNING: %~1 ^(%~2^) is not found in %%PATH%% - the build may fail.
	SET /A CheckToolInPath.Missing=CheckToolInPath.Missing+1
)
GOTO :EOF

REM Utility for creating a directories with logging.
:create
ECHO Create "%~1"
IF NOT EXIST "%~1" MKDIR "%~1"
GOTO :EOF

REM Utility for recreating a directories with logging.
:recreate
ECHO Recreate "%~1"
(IF EXIST "%~1" RMDIR "%~1" /S /Q) && MKDIR "%~1"
GOTO :EOF

REM Utility for moving files with logging.
:move
ECHO Move "%~1" "%~2"
1>nul MOVE /Y "%~1" "%~2"
GOTO :EOF

REM Utility for copying files with logging.
:copy
ECHO Copy "%~1" "%~2"
1>nul COPY /Y "%~1" "%~2"
GOTO :EOF

:delete
ECHO Delete "%~1"
IF EXIST "%~1" DEL /F /Q "%~1"
GOTO :EOF

REM Reports that an error occurred.
:error
>&2 ECHO ERROR: Failure during build ^(check the output above^). Error %ERRORLEVEL%.
EXIT /B 1
