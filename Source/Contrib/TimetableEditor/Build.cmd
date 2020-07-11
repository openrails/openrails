@ECHO OFF
SETLOCAL
REM Script must be run from Contrib\TimetableEditor directory.

SET FindExecutable.Default=%SystemDrive%\Lazarus\lazbuild.exe
CALL :find-executable lazbuild.exe
IF NOT EXIST "%FindExecutable%" (
	ECHO Error: Lazarus compiler ^(lazbuild.exe^) not found.
	ECHO Expected location is %FindExecutable.Default% or on the PATH.
	EXIT /B 1
)
SET Lazarus.lazbuild=%FindExecutable%

SET FindExecutable.Default=%SystemDrive%\Lazarus\fpc\2.6.4\bin\i386-win32\strip.exe
CALL :find-executable strip.exe
IF NOT EXIST "%FindExecutable%" (
	ECHO Error: Lazarus compiler ^(strip.exe^) not found.
	ECHO Expected location is %FindExecutable.Default% or on the PATH.
	EXIT /B 1
)
SET Lazarus.strip=%FindExecutable%

IF EXIST lib RMDIR /S /Q lib
%Lazarus.lazbuild% timetableedit.lpi && %Lazarus.strip% --strip-all timetableedit.exe && MOVE /Y timetableedit.exe ..\..\..\Program\Contrib.TimetableEditor.exe && XCOPY /S /I /Y languages ..\..\..\Program\languages || EXIT /B 1

ENDLOCAL
GOTO :EOF

:find-executable
SET FindExecutable=%~$PATH:1
IF "%FindExecutable%" == "" IF NOT "%FindExecutable.Default%" == "" SET FindExecutable=%FindExecutable.Default%
GOTO :EOF
