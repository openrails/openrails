@ECHO OFF
REM Script must be run from Locales directory.

PUSHD ..\3rdPartyLibs
FOR /D %%M IN (..\Locales\*) DO (
	GNU.Gettext.Xgettext.exe -D ..\%%~nxM --recursive -o ..\Locales\%%~nxM\%%~nxM.pot
	FOR %%L IN (%%M\*.po) DO GNU.Gettext.Msgfmt.exe -l %%~nL -r %%~nxM -d ..\..\Program -L ..\..\Program %%L
)
POPD
IF "%~1"=="" PAUSE
