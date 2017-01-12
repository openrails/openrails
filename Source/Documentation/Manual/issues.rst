.. _issues:

************************
Version 1.2 Known Issues
************************

Empty Effects Section in .eng File
==================================

If an .eng file is used that has an ``Effects()`` section that contains no 
data, the engine will not be loaded by ORTS. In this case it is suggested to 
fully delete the ``Effects()`` section.

Curly brackets in file sigscr.dat
=================================

Open Rails does not correctly handle, and also generates a misleading error 
message in file OpenRailsLog.txt file, when there is a curly bracket at the 
end of a conditional statement, e.g.::

    if ( next_hp ==# 0 && next_gue !=# 2 ) {

Therefore the file must be edited as follows to be correctly interpreted by Open Rails::

    if ( next_hp ==# 0 && next_gue !=# 2 )
    {