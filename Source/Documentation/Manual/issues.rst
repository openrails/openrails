.. _issues:

************************
Known Issues
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
end of a conditional statement within file sigscr.dat, e.g.::

    if ( next_hp ==# 0 && next_gue !=# 2 ) {

Therefore the file must be edited as follows to be correctly interpreted by Open Rails::

    if ( next_hp ==# 0 && next_gue !=# 2 )
    {

Spurious emergency braking in Timetable mode
=============================================

If, in Timetable mode, a speedplate with higher speed limit follows a signal with 
reduced speed limit, the allowed speed in the Trackmonitor rises to the speed 
shown on the speedplate. This occurs according to specs of Timetable mode 
(and differently from activity mode).

However the overspeedmonitor considers the reduced signal speed, coherently 
with activity mode. Therefore in this case if, in timetable mode, a train is 
accelerated above the signal speed, the overspeedmonitor may trigger 
emergency braking.