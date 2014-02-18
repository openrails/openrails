REM Script must be run from Program directory, with the following two Visual Studio macros:
SET ProjectName=%~1
SET ProjectDir=%~2
SET ProjectDir=%ProjectDir:~0,-1%

..\Source\3rdPartyLibs\GNU.Gettext.Xgettext.exe -D "%ProjectDir%" --recursive -o ..\Source\Locales\%ProjectName%.pot
FOR %%L IN (..\Source\Locales\%ProjectName%\*.po) DO ..\Source\3rdPartyLibs\GNU.Gettext.Msgfmt.exe -l %%~nL -r %ProjectName% -d .\ -L GNU.Gettext.dll ..\Source\Locales\%ProjectName%\%%~nL.po
