@ECHO OFF
SETLOCAL ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION

REM Parse command line.
SET Mode=%~1

REM Get product version.
REM TODO!

REM Get code revision.
REM   For Subversion, this is a positive integer.
REM   For Git, this is the stable version, new commits, and latest commit ID hyphenated.
SET Revision=
IF EXIST ".svn" (
	FOR /F "usebackq tokens=1" %%R IN (`svn --non-interactive info --show-item revision .`) DO SET Revision=%%R
)
IF EXIST ".git" (
	FOR /F "usebackq tokens=1" %%R IN (`git describe --first-parent --always`) DO SET Revision=%%R
)
IF "%Revision%" == "" (
	>&2 ECHO WARNING: No Subversion or Git revision found.
)

REM Output version numbers.
ECHO OpenRails_Version=%Version%
ECHO OpenRails_Revision=%Revision%
