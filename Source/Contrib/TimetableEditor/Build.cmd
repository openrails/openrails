@ECHO OFF
SETLOCAL
REM Script must be run from Contrib\TimetableEditor directory.

SET FindExecutable.Default=%SystemDrive%\Lazarus\lazbuild.exe
CALL :find-executable lazbuild.exe
IF NOT EXIST "%FindExecutable%" (
	ECHO Error: Lazarus compiler ^(lazbuild.exe^) not found.
	ECHO Expected location is %FindExecutable.Default% or on the PATH.
	GOTO :EOF
)
SET Lazarus.lazbuild=%FindExecutable%

IF EXIST lib RMDIR /S /Q lib
%Lazarus.lazbuild% timetableedit.lpi && MOVE /Y timetableedit.exe ..\..\..\Program\Contrib.TimetableEditor.exe && XCOPY /S /I /Y languages ..\..\..\Program\languages

ENDLOCAL
GOTO :EOF

:find-executable
SET FindExecutable=%~$PATH:1
IF "%FindExecutable%" == "" IF NOT "%FindExecutable.Default%" == "" SET FindExecutable=%FindExecutable.Default%
GOTO :EOF
