.. _compatibility:

*******************************
Use of MSTS Files by Open Rails
*******************************

Overview
========

Your MSTS Installation and Custom Installations for Open Rails
--------------------------------------------------------------

Open Rails reads only the content folders in each of the MSTS installations 
you choose to identify for it and will do so without modifying any of those 
files. None of the MSTS program folders are used and no changes to the MSTS 
directory tree are required.  

Open Rails may also be used to read a non-MSTS directory structure that you 
create.

This document uses the term Root Folder to designate the parent folder of any 
MSTS or OR-Specific directory tree (.e.g, ``\Train Simulator`` is the 
*Root Folder* for MSTS).

MSTS Directories Used by Open Rails
===================================

Open Rails software reads and uses all of the data found in many MSTS 
directories::

    \Consists
    \Paths
    \Services
    \Shapes
    \Sounds
    \Textures
    \Terrtex
    \Tiles
    \Traffic
    \Trainset
    \World

Open Rails uses a file parser to read the MSTS files and will locate many 
errors that are missed or unreported by the MSTS software or by other 
utilities. In most cases, the Open Rails software will ignore the error in the 
file and run properly. Open Rails software logs these errors in a log file on 
the user's desktop. This log file may be used to correct problems identified 
by the Open Rails software. The parser will also correct some of the problems 
that stumped MSTS.  For example, if a texture is missing Open Rails will 
substitute a neutral gray texture and continue.

MSTS Files Used in Whole or Part by Open Rails
==============================================

Route Files
-----------

Open Rails software uses some of the data in several MSTS Route files, 
depending on the MSTS features supported by Open Rails:

- Route Database file (.rdb) -- CarSpawner is supported.
- Reference File (.ref) -- a Route Editor is well under way.
- Track Database file (.tdb) -- supported
- Route File (.trk) -- Level Crossings and overhead wires are supported.
- Sigcfg (.dat) file -- Signal & scripting capabilities are supported.
- Sigscr (.dat) file -- Signal & scripting capabilities are supported.
- Speedpost (.dat) file -- Supported
- Spotter (.dat) file -- Supported
- Ssource (.dat) file -- Supported
- Telepole (.dat) file -- Supported
- Tsection (.dat) file -- Supported
- Ttype (.dat)  file -- Supported
- Hazards (.haz) file -- Supported

Environment .env files
----------------------

Open Rails software does not support advanced water dynamic effects.

OR Defined Weather
''''''''''''''''''

Open Rails uses its own sky, cloud, sun, moon and precipitation effects 
developed exclusively for it. When using the *Explore Route* feature you may 
choose season, weather, and time of day. When using the *Run Activity* feature 
they are read from the activity file.

OR Weather using MSTS Compatibility
'''''''''''''''''''''''''''''''''''

Open Rails can replace MSTS Environmental displays by its own (e.g., Kosmos) 

Activities
----------

Many passenger and freight activities created using the MSTS activity editor 
run without problems in Open Rails.

Some Activities created using the MSTS activity editor will have slightly 
different behavior as compared to running in MSTS. This is often due to 
slightly different train performance resulting from differences in how each 
simulator handles train physics.

A few activities fail to run at all. This appears to be due to the creativity 
of Activity Designers who have found ways to do things wholly unanticipated by 
the Open Rails Team.  As these are discovered the Open Rails team will record 
the bug for future correction.

.. _compatibility-folders:

Using a Non-MSTS Folder Structure
=================================

Open Rails uses a subset of the MSTS folder structure to run.
You must create a root folder of any suitable name and it must contain four 
folders, together with their related sub-folders::

    \GLOBAL
    \ROUTES
    \TRAINS
    \SOUND

No other files or folders are required in the root folder.
Within the ``\GLOBAL`` folder two sub-folders are required::

    \SHAPES
    \TEXTURES

Within the ``\TRAINS`` folder two subfolders are required::

    \CONSISTS
    \TRAINSETS

Original MSTS Files Usually Needed for Added MSTS-Compatible Content
====================================================================

Original MSTS Files Usually Needed for a Non-MSTS-Folder Structure
------------------------------------------------------------------

A number of MSTS folders and files must be placed into any OR-Specific 
installation you have created. These may be obtained from your own MSTS 
Installation or, as noted below, from Train Sim Forums

``\GLOBAL``
'''''''''''

Within the ``\GLOBAL`` folder only the file tsection.dat is required. The most 
current version is best and it can be downloaded from many Train Sim forums. 
Files sigcfg.dat and sigscr.dat are needed if there are routes that don't 
have their own specific files with the same names in their root folder.

``\GLOBAL\SHAPES``
''''''''''''''''''

Many routes use specific track sets, like XTRACKS, UK-finescale etc.
  
Routes which solely use such sets do not need any of the original MSTS 
files from GLOBAL, as all required files come from the relevant track set. 
These sets can be downloaded from many Train Sim forums. There are also many 
routes using super-sets of the original MSTS track sets. These routes will 
need some or all the files contained in the ``SHAPES`` and ``TEXTURES`` 
subfolders within the ``GLOBAL`` folder of your MSTS installation.

``\TRAINS``
'''''''''''
  
Requirements are similar to routes. Again, only the folders for the 
trainsets which are actually used are required, but many third-party 
trainsets refer to original MSTS files like cabviews and, in particular, 
sound files. Many consists refer to engines or wagons from the original 
MSTS routes but those can be easily replaced with other engines or wagons.

``\SOUND``
''''''''''
  
Only very few routes provide a full new sound set, so the original files 
included in this folder are usually needed.

``\ROUTES``
'''''''''''

Once all the above directories are populated with files you need only the 
specific route folder placed into ``\Routes`` to run Open Rails from a 
non-MSTS directory.

Note that many routes -- in particular freeware routes -- use content from the 
original MSTS routes, and therefore when installing new routes you may find 
their installation requires files from the original MSTS routes in order to be 
properly installed.
