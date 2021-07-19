@ECHO OFF
SETLOCAL ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION

SET Mode=%~1%
FOR /F "usebackq tokens=1-2 delims=-" %%A IN (`git describe --long --exclude=*-*`) DO (
	SET Git.Tag=%%A
	SET Git.Commits=%%B
)
FOR /F "usebackq tokens=1-4 delims=." %%A IN (`ECHO %Git.Tag%.0.0`) DO SET Revision=%%A.%%B.%%C.%Git.Commits%
GOTO %Mode%


:stable
FOR /F "usebackq tokens=* delims=-" %%A IN (`git describe`) DO SET Version=%%A
GOTO :done


:testing
FOR /F "usebackq tokens=* delims=-" %%A IN (`git describe --long --exclude=*-*`) DO SET Version=%Mode:~0,1%%%A
GOTO :done


:unstable
SET TZ=UTC
FOR /F "usebackq tokens=1-4 delims=." %%A IN (`git log -1 --pretty^=format:%%ad --date=format-local:%%Y.%%m.%%d.%%H%%M`) DO (
	SET Version=%Mode:~0,1%%%A.%%B.%%C-%%D
	SET Revision=0.%%A.%%B%%C.%%D
)
GOTO :done


:done
ECHO OpenRails_Version=%Version%
ECHO OpenRails_Revision=%Revision%
