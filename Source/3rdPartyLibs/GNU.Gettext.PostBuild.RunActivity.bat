rem Script starts in \OpenRails\Program\
..\Source\3rdPartyLibs\GNU.Gettext.Xgettext.exe -D ..\Source\RunActivity\ --recursive -o ..\Source\Locales\RunActivity.pot
..\Source\3rdPartyLibs\GNU.Gettext.Msgfmt.exe -l hu -r ORTS.Simulator -d .\ -L GNU.Gettext.dll ..\Source\Locales\RunActivity\hu.po
