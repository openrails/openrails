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
IF EXIST ".git" (
	FOR /F "usebackq tokens=1" %%R IN (`git describe --first-parent --always`) DO SET Revision=%%R
)
IF "%Revision%" == "000" (
	>&2 ECHO WARNING: No Subversion or Git revision found.
)

REM Copy the Web content
IF EXIST "Program\Content\Web" RMDIR "Program\Content\Web" /S /Q
IF NOT EXIST "Program\Content\Web" MKDIR "Program\Content\Web"
XCOPY "Source\RunActivity\Viewer3D\WebServices\Web" "Program\Content\Web" /S /Y || GOTO :error

GOTO :EOF

REM Reports that an error occurred.
:error
>&2 ECHO ERROR: Failure during build ^(check the output above^). Error %ERRORLEVEL%.
EXIT /B 1