@ECHO OFF
SETLOCAL ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION

REM Get product version and code revision.
SET Version=
SET Revision=
FOR /F "usebackq tokens=1* delims=-" %%V IN (`git describe --first-parent --always --long`) DO (
	SET Version=%%V
	SET Revision=%%W
)
IF "%~1%" == "Unstable" (
	SET TZ=UTC
	FOR /F "usebackq tokens=1* delims=-" %%V IN (`git log -1 --pretty^=format:^%%ad --date^=format-local:^%%Y^.%%m^.%%d-^%%H^%%M`) DO (
		SET Version=%%V
		SET Revision=%%W
	)
)
IF "%Version%" == "" (
	>&2 ECHO WARNING: No Git repository found.
)

REM Output version numbers.
ECHO OpenRails_Version=%Version%
ECHO OpenRails_Revision=%Revision%
