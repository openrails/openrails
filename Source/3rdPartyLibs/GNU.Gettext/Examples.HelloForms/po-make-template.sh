#!bin/sh
## Extract messages to PO template file
mono ./../../Bin/Debug/GNU.Gettext.Xgettext.exe -D "./" --recursive -o "./po/Messages.pot"