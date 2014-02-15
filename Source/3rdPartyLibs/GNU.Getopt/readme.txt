Gnu.Getopt.Net 0.9.2
--------------------

This is a Getopt C#.NET port based on version 1.0.9 of Aaron M. Renn's Java
Getopt [1].

Please send recommendations and bug reports to this port to
klaus.prueckl@aon.at.


Usage:
For the correct usage take a look at the documentation or the demo
applications. You can access the documentation online, download, or build
yourself from the C# XML comments with Visual Studio or NDoc [3].

Settings:
If you want to use the POSIX behavior add
	<add key="Gnu.PosixlyCorrect" value="true">
to your <appSettings> section of your App.config file.
This setting changes the error message language to English if you are using
a UI culture that corresponds with one of the supported translations.
In general, on machines with another UI culture than mentioned below, the
error message language is always English.


Klaus Prückl
klaus.prueckl@aon.at
http://www.codeplex.com/getopt

---

SOURCE package contents:

COPYING.LIB          GNU LIBRARY GENERAL PUBLIC LICENSE 
Gnu.Getopt.FxCop     FxCop project file (for FxCop code analysis tool [2])
Gnu.Getopt.nunit     NUnit project file (for automated tests [3])
Gnu.Getopt.sln       Visual Studio .NET 2003 solution file
Gnu.Getopt.snk       Strong name key pair (for signing the library)

Gnu.Getopt/*         Sourcecode of the library including Visual Studio .NET
                     2003 project file
Gnu.Getopt.Tests/*   Sourcecode of the test application and automated NUnit
                     tests including Visual Studio .NET 2003 project file


BINARIES package contents:

Gnu.Getopt.dll       Release build of the library with the
                     English language error messages
                     Copyright (c) 1998 by William King (wrking@eng.sun.com) 
                     and Aaron M. Renn (arenn@urbanophile.com)
                              
Gnu.Getopt.resources.dll satellite assemblies with the message translations:

cs/*                 Czech language error messages
                     Copyright (c) 1998 by Roman Szturc (Roman.Szturc@vsb.cz)
de/*                 German language error messages
                     Copyright (c) 1999 by Bernhard Bablok (bablokb@gmx.net)
fr/*                 French language error messages
                     Copyright (c) 1999 Free Software Foundation, Inc.
                     Michel Robitaille <robitail@IRO.UMontreal.CA>, 1996,
                     Edouard G. Parmelan <edouard.parmelan@quadratec.fr>, 1999.
hu/*                 Hungarian language error messages
                     Copyright (c) 2001 by Gyula Csom (csom@informix.hu)
ja/*                 Japanese language error messages
                     Copyright (c) 2001 by Yasuoka Masahiko
                     (yasuoka@yasuoka.net)
nl/*                 Dutch language error messages
                     Copyright (c) 1999 by Ernst de Haan (ernst@jollem.com)
no/*                 Norwegian language error messages
                     Copyright (c) 1999 by Bjørn-Ove Heimsund (s811@ii.uib.no)

---

References

[1] http://www.urbanophile.com/arenn/coding/download.html
[2] http://www.gotdotnet.com/team/fxcop/
[3] http://www.nunit.org/

---

Changelog:


Version 0.9.2 - minor changes release (2007/06/??)

changed: switched from VS.NET 2003 to VS 2005 projects and to .NET 2.0
changed: switched from NDoc to Sandcastle
changed: japanese messages now with unicode characters instead of \uXXXX codes for each caracter.
todo: Add permission assembly attributes.
todo: Test MessagesBundle translations (encoding) - especially in japanese.
todo: Write automated test scenarios for the class Getopt.



Version 0.9.1 - minor changes release (2004/05/25)

changed: for POSIX behavior the UI culture of the actual thread is not changed
         anymore - an own CultureInfo object is used
changed: revised documentation
todo: Add permission assembly attributes.
todo: Test MessagesBundle translations (encoding).
todo: Write automated test scenarios for the class Getopt.



Version 0.9 - initial release (2004/01/28)

todo: Add permission assembly attributes.
todo: Test MessagesBundle translations (encoding).
todo: Write automated test scenarios for the class Getopt.