#!bin/sh

# Compile PO files to satellite assemblies
mono ./../../Bin/Debug/GNU.Gettext.Msgfmt.exe -l fr-FR -d ./bin/Debug -r Examples.Hello  -L ./../../Bin/Debug -v ./po/fr.po
mono ./../../Bin/Debug/GNU.Gettext.Msgfmt.exe -l ru-RU -d ./bin/Debug -r Examples.Hello  -L ./../../Bin/Debug -v ./po/ru.po
