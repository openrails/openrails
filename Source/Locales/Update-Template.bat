@ECHO OFF
REM Script must be run from Locales directory.

PUSHD ..\3rdPartyLibs
FOR /D %%M IN (..\Locales\*) DO (
	GNU.Gettext.Xgettext.exe -D ..\%%~nxM --recursive -o ..\Locales\%%~nxM\%%~nxM.pot
)
POPD
IF "%~1"=="" PAUSE
