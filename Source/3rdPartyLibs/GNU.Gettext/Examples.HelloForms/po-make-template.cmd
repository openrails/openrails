@echo off

%~d0
cd "%~dp0"
"%~dp0..\..\Bin\Debug\GNU.Gettext.Xgettext.exe" -D "%~dp0." --recursive -o "%~dp0.\po\Messages.pot"
if errorlevel 1 exit /b 1
exit /b 0
