rem Script starts in \OpenRails\Program\
..\Source\3rdPartyLibs\GNU.Gettext.Xgettext.exe -D ..\Source\Menu\ -D ..\Source\ORTS\Menu\ --recursive -o ..\Source\Locales\Menu.pot
..\Source\3rdPartyLibs\GNU.Gettext.Msgfmt.exe -l fr -r ORTS.Menu -d .\ -L GNU.Gettext.dll ..\Source\Locales\Menu\fr.po
..\Source\3rdPartyLibs\GNU.Gettext.Msgfmt.exe -l hu -r ORTS.Menu -d .\ -L GNU.Gettext.dll ..\Source\Locales\Menu\hu.po
