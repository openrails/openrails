.. _timetable:

**************
Timetable Mode
**************

Introduction
============

The timetable concept is not a replacement for the activity definition, but is 
an alternative way of defining both player and computer-controlled (AI and 
Static) trains.

In an activity, the player train is defined explicitly, and all AI trains are 
defined in a traffic definition. Static trains are defined separately.

In a timetable all trains are defined in a similar way. On starting a timetable 
run, the required player train is selected from the list of available trains. In 
the timetable definition itself, no distinction is made between running trains -- 
any of the running trains can be selected as player train, and if not selected 
as such they will be run as AI trains. Static trains are also defined in the 
same way but cannot be selected as the player train.

As a result, the number of different 'activities' that can be played using the 
same timetable file is equal to the number of trains which are defined in the 
timetable.

The development of the timetable concept is still very much a work in progress. 
This document details the state as it is at the moment, but also includes items 
yet to be produced, or items which have yet to be developed further.

To distinguish between these items, the following styles are used in the 
description of timetable mode.

*Items shown in black italics are available but only in a provisional 
implementation, or in a limited context. Further development of these items is 
still required.*

**Important aspects where the use of specific OR or MSTS items for timetables 
differs significantly from its use in an activity are shown in bold**.

Apart from the items indicated as above, it should be realised that as work 
continues, all items are still subject to change.

General
=======

Data definition
---------------

The timetable data is defined in a Spreadsheet, and saved as a \*.csv file 
(character separated file) in Unicode format. As the separation character, 
either ',' (comma) or ';' (semi-colon) must be used.

Do not select space or tab as the separation character.

As ';' or ',' are possible separation characters, these symbols must not be used 
anywhere within the actual data. Enclosure of text by quotes (either single or 
double) has no effect. Also, the character '#' should not be used in train 
names, since it is the prefix for reserved words in the Timetable.

File structure
--------------

The saved ``*.csv`` files must be renamed with the extension ``*.timetable_or``. 
The timetable files must be placed in a subdirectory named ``OpenRails`` created 
in the route's ``Activities`` directory.

File and train selection
------------------------

When starting a timetable run, the mode *Timetable* is selected in the menu. The 
desired timetable file must then be selected in the *Timetable set* display.

After selecting the required timetable, a list of all trains contained in that 
timetable is displayed and the required train can be selected.

Season and weather can also be selected; these are not preset within the 
timetable definition.

Timetable Definition
====================

General
-------

A timetable consists of a list of trains, and, per train, the required timing of 
these trains. The timing can be limited to just the start time, or it can 
include intermediate times as well.

*At present, intermediate timings are limited to 'platform' locations as created 
using the MSTS Route Editor.*

Each column in the spreadsheet contains data for a train and each row represents 
a location. A cell at the intersection of a train and location contains the 
timing data for that particular train at that location.

Special rows and columns can be defined for general information or control 
commands.

The first row for each column contains the train definition.

The first column for each row contains the location definition.

The cell at the intersection of the first row and first column **must be empty**.

This paragraph only lists the main outline, a fuller detailed description will 
follow in the next paragraphs.

Column definitions
------------------

A column is defined by the contents of the first row.

Default, the first row defines the train name.

Special columns can be defined using the following syntax :

    - ``#comment``: column contains comment only and is ignored when reading the 
      timetable.
    - <blank>: column is extension of preceding column.

Row definitions
---------------

A row is defined by the contents of the first column.

Default, the first column defines the stop location.

Special columns can be defined using the following syntax :

    - ``#comment``: row contains comment only and is ignored when reading the 
      timetable
    - <blank>:      row is extension of row above
    - ``#path``:    defines train path
    - ``#consist``: defines train consist
    - ``#start``:   defines time when train is started
    - ``#note``:    defines general notes for this train
    - ``#dispose``: defines how train is handled after it has terminated

Timing details
--------------

Each cell which is at an intersection of a train column and a location row, can 
contain timing details for that train at that location.

*Presently, only train stop details can be defined. Later on, passing times will 
also be defined; these passing times can be used to determine a train's delay.*

Control commands can be set at locations where the train stops, but can also be 
set for locations where no timing is inserted as the train passes through that 
location without stopping.

Timetable Data Details
======================

Timetable Description
---------------------

Although ``#comment`` rows and columns are generally ignored, the contents of 
the cell at the intersection of the first ``#comment`` row and first ``#comment`` 
column is used as the timetable description and appears as the timetable's name 
in the Open Rails menu.

Train Details
-------------

The train name as defined in the first row must be unique for each train in a 
timetable file. This name is also used when referencing this train in a train 
command; see details below.

The sequence of trains is not important.

Location Details
----------------

At present, the possible locations are restricted to 'platforms' as defined in 
the MSTS Route Editor.

Each location must be set to the 'Station Name' as defined in the platform 
definitions.

The name used in the timetable must exactly match the name as used in the route 
definition (\*.tdb file), otherwise the location cannot be found and therefore 
cannot be processed.

Also, each location name must be unique, as otherwise its position in the train 
path could be ambiguous.

The sequence of the locations is not important, as the order in which the 
stations are passed by a train is defined in that train's path. For the same 
reason, a train's path can be set to just run in between some of the locations, 
or be set to bypass certain stations.

Timing Details
--------------

Each cell at an intersection of train and location can contain the timing 
details of that train at that location.

Times are defined as HH:mm, and the 24-hour clock must be used.

If a single time is inserted it is taken as the departure time (except at the 
final location).

If both arrival and departure time are to be defined, these must be separated by 
'-'.

Additional control commands can be included. Such commands can also be set for 
locations where the train does not stop and therefore has no timing details, but 
the train must pass through that location for the commands to be effective.

Although a location itself can be defined more than once in a timetable, it is 
not possible to define timing details for trains for a location more than once. 
If a train follows a route which takes it through the same location more than 
once, the train must be 'split' into separate train entries.

Special Columns
---------------

- ``#Comment`` column. 
  
  A column with the #comment definition in the first row is a comment column and 
  is ignored when reading the timetable, except for the cell at the intersection 
  of the first comment column and the first comment row.

- <Blank> column. 
  
  A column with a blank (empty) cell in the first row is taken as a continuation 
  of the preceding column. It can be used to insert control commands which apply 
  to the details in the preceding column. This can be useful when timings are 
  derived automatically through formulas in the spreadsheet as inserting 
  commands in the timing cell itself would exclude the use of such formulas.

Special rows
------------

- ``#Comment`` row. 
  
  A row with the #comment definition in the first column is a comment row and is 
  ignored when reading the timetable, except for the cell at the intersection of 
  the first comment column and the first comment row.

- <Blank> row. 
  
  A row with a blank (empty) cell in the first column is taken as a continuation 
  of the preceding row.
  
- ``#Path`` row. 
  
  The #path row defines the path of that train. The path must be a \*.pat file as 
  defined by the MSTS Activity Editor or by Trackviewer, and must be located
  in the route's Path 
  directory. This field is compulsory.
  
  The timetable uses the same paths as those defined for activities.

  **However, waiting points must not be defined in paths for use in timetables as 
  the processing of waiting points is not supported in the timetable concept. 
  Waiting points within a timetable must be defined using the specific control 
  commands.**
  
  The ``#path`` statement can take a qualifier: ``/binary``.
  
  Large timetables can require many paths, and loading those paths can take 
  considerable time (several minutes). To reduce this loading time, the paths 
  can be stored in a processed, binary format. This format is the same as used 
  in the 'save' command. Note that the binary path information cannot be 
  directly accessed by the user, either for reading or for writing. When 
  ``/binary`` is set, the program will check if a binary path exists. If so, it 
  will read that path. If not, it will read the 'normal' path, and will then 
  store this as binary for future use. Binary paths are stored in a subdirectory 
  named ``OpenRails`` which must be created in the Paths directory of the route.
  
  **Important:**

    - If a path is edited, the binary version must be deleted manually, otherwise the program will still use this older version.
    - If a route is edited, such that the .tdb might have been changed, all binary paths must be deleted.

- ``#Consist`` row 
  
  The ``#consist`` row defines the consist used for that train. This field is 
  compulsory.
  
  However, if the train is run as an AI train and it is 'formed' out of another 
  train (see below), the consist information is ignored and the train uses the 
  consist of the train out of which it was formed.

  For the player train, the consist is always used even if the train is formed 
  out of another train. The consist definition must be a \*.con file as defined 
  by the MSTS Activity Editor or by the TSRE5 consist editor,
  and must be stored in the defined consist 
  directory.

  Also a more complex syntax of the consist definition is possible, as described 
  below.

  This allows a consist definition to be not just a single string directly 
  referring to a file, but a combination of strings, with the possibility to use 
  (part of) the consist in reverse.

  The general syntax is::

    consist [$reverse] [+ consists [$reverse] [+ ...] ]
  
  Example: a loco-hauled train, using the same set of coaches, running in both 
  directions. Two consists are defined: ``c_loco`` and ``c_wagons``. The consist 
  definitions which can now be used are:
  
    ``c_loco + c_wagons``, and for reverse:
    
    ``c_loco $reverse + c_wagons $reverse``

  Please note that ``$reverse`` always applies only to the sub-consist with 
  which it is defined, not for the complete combined consist.

  If this train sometimes has some additional wagons, e.g. during rush hours, 
  the consists can be defined as follows (with ``c_add`` the definition of the 
  additional wagons):
  
    ``c_loco + c_wagons + c_add``, and for reverse: 
    
    ``c_loco $reverse + c_add $reverse + c_wagons $reverse``

  Clearly, this can save on the definition of the total required consists, and 
  in particular saves the tedious task of having to define 'reverse' consists. 
  When using multiple units, this is even more useful.

  Suppose there are two sets of multiple units, running either as single trains 
  or combined. Normally, six different consists would be required to cover all 
  trains, but now only two will suffice : ``set_a`` and ``set_b``. The various 
  combinations are:

    ``set_a``, reverse ``set_a $reverse``.

    ``set_b``, reverse ``set_b $reverse``.

    ``set_a + set_b``, reverse ``set_b $reverse + set_a $reverse``.

  Consist strings which contain  '+'  or  '$'  can be used in timetables but 
  must be enclosed by <  >. For instance :

  ``<loco+wagon>+<$loco+wagon>$reverse``

- ``#Start`` row

  The ``#start`` row defines the time at which the train is started. It must be 
  defined as HH:mm, and the 24 hour clock must be used. This field is compulsory.

  Use of start time for AI trains :
  
    - When a train is formed out of another train and this other train is included to run in the timetable, the time defined in #start is only used to define when the train becomes active.

  Use of start time for player train :
    
    - The time as defined in ``#start`` is normally used as the start time of the 
      timetable 'activity'.

  If a train is formed out of another train and this train is included in the 
  timetable, then if this train is delayed and has not arrived before the 
  defined start time, the starting of this train is also delayed until the train 
  out of which it is formed has arrived. This applies to both AI and player 
  train. This means that the start of the player activity can be delayed.

  For details on starting and running of trains around midnight see the 
  paragraph :ref:`below <timetable-midnight>`.

  The ``#start`` field can also contain the following command::
  
    $create[=<time>] [/ahead=<train>]

  The ``$create`` command will create that train at the time as indicated. If no 
  time is set, the train will be created before the start of the first train. 
  The train will be 'static' until the time as set as start time. The normal 
  rules for train placement still apply, so a train cannot be placed 
  onto a section of track already occupied by another train.

  However, storage sidings often hold multiple trains. To allow for this, and to 
  ensure the trains are stored in proper order (first one out up front), the 
  parameter ``[/ahead=<train>]`` must be used.

  The train will now be placed ahead of the referenced train, in the direction 
  of the train's path. Multiple trains can be stored on a single siding, but 
  care must be taken to set the proper references. The reference must always be 
  to the previous train - two trains cannot reference the same train in the 
  ``/ahead`` parameter as that would cause conflict.

  If the total length of all trains exceeds the length of the sidings, the 
  trains will 'spill out' onto whatever lies beyond.

  Note that a train referenced in an ``/ahead`` parameter must be created before 
  or at the same time as the train which uses that reference.
  
- ``#Note`` row

  The ``#note`` row can be used to defined control commands which are not 
  location related but apply to the full run of the train. It can also be used 
  to set commands for trains which do not stop at or pass through any defined 
  location. This row is optional.

  The following commands can be inserted in the #note field of each train::
    
    $acc=n
    $dec=n
    
  These commands set multiplication factors for the acceleration (``$acc``) and 
  deceleration (``$dec``) values used for that train.

  The program uses average acceleration and deceleration values for all trains 
  (different values for freight, passenger and high speed trains). But these 
  values are not always adequate, especially for modern trains. This can lead to 
  delays when trying to run to a real life timetable.

  Using the ``$acc`` and ``$dec`` commands, the values used can be modified. 
  Note that these commands do not define an actual value, but define a factor; 
  the default value will be multiplied by this factor. However, setting a higher 
  value for acceleration and deceleration does not mean that the trains will 
  always accelerate and decelerate faster according to the set value. **Most of 
  the time, the train behaviour is controlled through the physics.** But 
  especially the ``$dec`` factor does have an important side effect. The 
  deceleration value is also used to calculate the expected required braking 
  distance. Setting a higher deceleration will reduce the required braking 
  distance, allowing the train to continue to run at maximum allowed speed for 
  longer distances. This can have a significant effect on the timing. Take care, 
  though, not to set the value too high - the calculated braking distance must 
  of course be sufficient to allow for proper braking, otherwise the train 
  cannot stop in time resulting in SPADs etc.

  A typical value for modern stock for the ``$dec`` command is 2 or 3.
  
- ``#Dispose`` row

  The ``#dispose`` row defines what happens to an AI train when it has reached 
  the end of its run, i.e. it has reached the end of the defined path. The 
  information in the ``#dispose`` row can detail if the train is to be formed 
  into another train, and, if so, how and where. For details see the commands as 
  described further down.

  This row is optional and if included, the use per train is also optional. If 
  the row is not included or the field is not set for a particular train, the 
  train is removed from the activity after it has terminated.

  *The #dispose row presently does not affect the end of the run for the player train*. 

Control commands
----------------

General
'''''''

Control commands can be set to control train and signaling behaviour and 
actions. There are four sets of commands available:

- Location commands
- Train control commands
- Create commands
- Dispose commands

Command syntax
''''''''''''''

All commands have the same basic syntax. A command consists of:

- Syntax name : defines the control command.
- Syntax value : set the value related to the command. Not all commands take a 
  value.
- Syntax qualifiers : adds additional information to the command. Not all 
  commands have qualifiers. Some qualifiers may be optional but others may be 
  compulsory, or compulsory only in combination with other qualifiers.
- Syntax qualifier values : a qualifier may require a value

Command syntax::

    $name = value /qualifier=value 

Multiple values may be set, separated by '+'. Note that any qualifiers always 
apply to all values.

Train Reference
'''''''''''''''

Many commands require a reference to another train. This reference is the other 
train's name as defined in the first row.

Location Commands
'''''''''''''''''

Location commands are::

    $hold
    $forcehold
    $nowaitsignal
    $terminal

These commands are also available as train control commands and are detailed in 
that paragraph.

Train control commands
''''''''''''''''''''''

All available train control commands are detailed below.

These commands can be set for each timing cell, i.e. at each intersection of 
train column and location row. The commands will apply at and from the location 
onward (if applicable).

Some commands can also be set in the #note row, in which case they apply from 
the start of the train. These commands are indicated below by an asterisk (*) 
behind the command name

The commands ``$hold`` and ``$nosignalwait`` can also be set as location commands.

``$hold, $nohold and $forcehold``

    If ``$hold`` is set, it defines that the exit signal for that location must 
    be held at danger up to 2 minutes before train departure.

    An exit signal is allocated to a platform if this signal is beyond the end platform marker (in the direction of travel), but is still within the same track node - so there must not be any points etc. between the platform marker and the signal.

    **By default, the signal will not be held.**

    If set per location, it will apply to all trains, but can be overridden for 
    any specific train by defining ``$nohold`` in that train's column. If set per 
    train, it will apply to that train only.

    ``$forcehold`` will set the first signal beyond the platform as the 'hold' signal, even if this signal is not allocated to the platform as exit signal. This can be useful at locations with complex layout where signals are not directly at the platform ends, but not holding the signals could lead to delay to other trains.

``$callon``

    This will allow a train to 'call on' into a platform occupied by another 
    train.

    For full details, see the :ref:`discussion above <timetable-callon>` on 
    the relationship between signalling and timetable.
    
``$connect``

    Syntax : ``$connect=<train> /maxdelay=n /hold=h``
    
    Defines that a train is to wait at a station until another train has 
    arrived, so as to let passengers make the connection between the trains.

    The train will be timetabled to allow this connection, and the ``$connect`` 
    command is set to maintain this connection if the arriving train is running 
    late.

    Note that the ``$connect`` command will not lock the signal. If the paths of 
    this train and the arriving train conflict before the arriving train reaches 
    the station, additional ``$wait`` or ``$hold`` commands must be set to avoid 
    deadlock.

    Command value: reference to train which is to be waited for, this is 
    compulsory.

    Command qualifiers :

        ``/maxdelay=n`` : n is the maximum delay (in minutes) of the arriving train for which this train is held.

            If the delay of the arriving train exceeds this value the train will 
            not wait. The maximum delay is independent from this train's own 
            delay.

            This qualifier and its  value are compulsory.

        ``/hold=h`` : h is the time (in minutes) the train is still held after the other train has arrived, and relates to the time required by the passengers to make the connection.

            This qualifier and its value are compulsory.

``$wait *``

    Syntax : ``$wait=<train> /maxdelay=n /notstarted /owndelay=n``

    Defines that a train is to wait for the referenced train to allow this train 
    to proceed first. The referenced train can be routed in the same or the 
    opposite direction as this train itself. A search is done for the first 
    track section which is common to both trains, starting at the location where 
    the ``$wait`` is defined, or at the start of the path if defined in the 
    ``#note`` row.

    If the start location is already common for both trains, then first a search 
    is done for the first section which is not common to both trains, and the 
    wait is applied to the next first common section beyond that.

    If the wait is set, the section will not be cleared for this train until the 
    referenced train has passed this section. This will force the train to wait. 
    The referenced train must exist for the wait to be valid.

    However, if /notstarted is set, the wait will also be set even if the 
    referenced train has not yet been started. This can be used where the wait 
    position is very close to the start position of the referenced train, and 
    there is a risk that the train may clear the section before the referenced 
    train is started.

    Care should be taken when defining a $wait at a location where the train is 
    to reverse. As the search is performed for the active subpath only, a $wait 
    defined at a location where the train is to reverse will not be effective as 
    the common section will be in the next subpath after the reversal. In such a 
    situation, the train should be 'split' into two separate definitions, one up 
    to the reversal location and another starting at that location.

    Command value : referenced train, this is compulsory.

    Command qualifiers :

        ``/maxdelay=n``: n is the maximum delay (in minutes) of the referenced train for which the wait is still valid.

            This delay is compensated for any delay of the train which is to 
            wait, e.g. if maxdelay is 5 minutes, the referenced train has a 
            delay of 8 minutes but this train itself has a delay of 4 minutes, 
            the compensated delay is 4 minutes and so the wait is still valid.

            This parameter is optional, if not set a maxdelay of 0 minutes is 
            set as default.

        ``/notstarted``: the wait will also be applied if the referenced train has not yet started.

        ``/owndelay=n`` (n is delay in minutes); the owndelay qualifier command makes the command valid only if the train in question is delayed by at least the total minutes as set for the owndelay qualifier.

            This can be used to hold a late-running train such that is does not 
            cause additional delays to other trains, in particular on single 
            track sections.

``$follow *``

    Syntax : ``$follow=<train> /maxdelay=n /owndelay=n``

    This command is very similar to the $wait command, but in this case it is 
    applied to each common section of both trains beyond a part of the route 
    which was not common. The train is controlled such that at each section 
    where the paths of the trains re-join after a section which was not common, 
    the train will only proceed if the referenced train has passed that 
    position. The command therefore works as a $wait which is repeated for each 
    such section.

    The command can only be set for trains routed in the same direction. When a 
    wait location is found and the train is due to be held, a special check is 
    performed to ensure the rear of the train is not in the path of the 
    referenced train or, if it is, the referenced train has already cleared that 
    position. Otherwise, a deadlock would result, with the referenced train not 
    being able to pass the train which is waiting for it.

    Command value: referenced train, this is compulsory.

    Command qualifiers:
    
        ``/maxdelay=n``: n is the maximum delay (in minutes) of the referenced train for which the wait is still valid. This delay is compensated by any delay of the train which is to wait, e.g. if maxdelay is 5 minutes, the referenced train has a delay of 8 minutes but this train itself has a delay of 4 minutes, the compensated delay is 4 minutes and thus the wait is still valid.

            This parameter is optional, if not set a maxdelay of 0 minutes is 
            set as default.
        
        ``/owndelay=n`` (n is delay in minutes): the owndelay qualifier  command makes the command valid only if the train in question is delayed by at least the total minutes as set for the owndelay qualifier.

            This can be used to hold a late-running train such that is does not 
            cause additional delays to other trains, in particular on single 
            track sections.

``$waitany *``

    Syntax : ``$waitany=<path> /both``

    This command will set a wait for any train which is on the path section as 
    defined.

    If the qualifier /both is set, the wait will be applied for any train 
    regardless of its direction, otherwise the wait is set only for trains 
    heading in the same direction as the definition of the path.

    The path defined in the waitany command must have a common section with the 
    path of the train itself, otherwise no waiting position can be found.

    This command can be set to control trains to wait beyond the normal signal 
    or deadlock rules. For instance, it can be used to perform a check for a 
    train which is to leave a siding or yard, checking the line the train is to 
    join for any trains approaching on that line, for a distance further back 
    than signalling would normally clear, so as to ensure it does not get into 
    the path of any train approaching on that line.

    With the /both qualifier set, it can be used at the terminating end of 
    single track lines to ensure a train does not enter that section beyond the 
    last passing loop if there is another train already in that section as this 
    could lead to irrecoverable deadlocks.

``$[no]waitsignal``

    Syntax: 
    
        ``$waitsignal``
        ``$nowaitsignal``


    Normally, if a train is stopped at a station and the next signal ahead is 
    still at danger, the train will not depart. But, there are situations where 
    this should be overruled.

    Some stations are 'free line' stations - that is, they are not controlled by 
    signals (usually small halts, without any switches). The next signal 
    probably is a 'normal' block signal and may be some distance from the 
    station. In that situation, the train does not have to wait for that signal 
    to clear in order to depart.

    Other situation are for freight trains, light engines and empty stock, which 
    also usually do not wait for the signal to clear but draw up to the signal 
    so as to take as little as time as possible to exit the station.

    The ``$nowaitsignal`` qualifier can be set per station (in the station column), 
    or per train. If set per station, it can be overruled by ``$waitsignal`` per 
    train.

``$terminal``

    The ``$terminal`` command changes the calculation of the stop position, and 
    makes the train stop at the terminating end of the platform. Whether the 
    platform is really a terminating platform, and at which end it terminates, 
    is determined by a check of the train's path.

    If the platform is in the first section of a train's path, or there are no 
    junctions in the path leading up to the section which holds the platform, it 
    is assumed the train starts at a terminal platform and the end of the train 
    is placed close to the start of the platform.

    If the platform is in the last section if the path or there are no junctions 
    beyond the section which holds the platform, it is assumed the platform is 
    at the end of the train's path and the train will run up to near the end of 
    the platform in its direction of travel.

    If neither condition is met, it is assumed it is not a terminal platform 
    after all, and the normal stop position is calculated.

    The ``$terminal`` option can be set for a station, or for individual trains. 
    If set for a station it cannot be overruled by a train.

    However, because of the logic as described above, if set for a station which 
    has both terminal platforms as well as through platforms, trains with paths 
    continuing through those platforms will have the normal stop positions.
    
Dispose Commands
----------------

Dispose commands can be set in the ``#dispose`` row to define what is to be done 
with the train after it has terminated. See special notes below on the behaviour 
of the player train when it is formed out of another train by a dispose command, 
or when the player train itself has a dispose command.

``$forms``

    Syntax : ``$forms=<train> /runround=<path> /rrime=time /setstop``

    ``$forms`` defines which new train is to be formed out of this train when 
    the train terminates. The consist of the new train is formed out of the 
    consist of the terminating train and any consist definition for the new 
    train is ignored. The new train will be 'static' until the time as defined 
    in #start row for that train. This means that the new train will not try to 
    clear its path, signals etc., and will not move even if it is not in a 
    station. 
    
    If the incoming train is running late, and its arrival time is later as the 
    start time of the new train, the start of the new train is also delayed but 
    the new train will immediately become active as soon as it is formed.

    For locomotive-hauled trains, it can be defined that the engine(s) must run 
    round the train in order for the train to move in the opposite direction. 
    The runround qualifier needs a path which defines the path the engine(s) is 
    to take when performing the runround. If the train has more than one leading 
    engine, all engines will be run round. Any other power units within the 
    train will not be moved.

    For specific rules and conditions for runround to work, see 
    :ref:`discussion <timetable-signalling>` on the relationship between 
    signalling and the timetable concept.

    If runround is defined, the time at which the runround is to take place can 
    be defined. If this time is not set, the runround will take place 
    immediately on termination of the incoming train.

    Command value : referenced train, this is compulsory.

    Command qualifiers:
    
        ``/runround=<path>``: <path> is the path to be used by the engine to perform the runround.

            This qualifier is optional; if set, the value is compulsory.
        
        ``/rrtime=time``: time is the definition of the time at which the runround is to take place. The time must be defined in HH:mm and must use the 24 hour clock.

            This qualifier is only valid in combination with the /runround 
            qualifier, is optional but if set, the value is compulsory.

        ``/setstop``: if this train itself has no station stops defined but the train it is to form starts at a station, this command will copy the details of the first station stop of the formed train, to ensure this train will stop at the correct location.

            For this qualifier to work correctly, the path of the incoming train 
            must terminate in the platform area of the departing train.
            
            This qualifier is optional and takes no values.

``$triggers``

    Syntax : ``$triggers=<train>``

    ``$triggers`` also defines which new train is to be formed out of this train 
    when the train terminates.

    However, when this command is used, the new train will be formed using the 
    consist definition of the new train and the existing consist is removed.

    Command value : referenced train, this is compulsory.

``$static``

    Syntax : ``$static``

    The train will become a 'static' train after it has terminated.
    
    Command value : none.

``$stable``

    Syntax: 
        ``$stable /out_path=<path> /out_time=time /in_path=<path> /in_time=time 
        /static /runround=<path> /rrtime= time /rrpos=<runround position> 
        /forms=<train> /triggers=<train>``

    ``$stable`` is an extended form of either $forms, $triggers or $static, 
    where the train is moved to another location before the related command is 
    performed. In case of /forms or /triggers, the train can move back to the 
    same or to another location where the new train actually starts. Note that 
    in these cases, the train has to make two moves, outward and inward.

    A runround can be performed in case /forms  is defined.

    If ``/triggers`` is defined, the change of consist will take place at the 
    'stable' position. Any reversal(s) in the inward path, or at the final 
    inward position, are taken into account when the new train is build, such 
    that the consist is facing the correct direction when the new train is 
    formed at the final inward position.

    The ``$stable`` can be used where a train forms another train but when the 
    train must clear the platform before the new train can be formed to allow 
    other trains to use that platform. It can also be used to move a train to a 
    siding after completing its last duty, and be 'stabled' there as static train.

    Separate timings can be defined for each move; if such a time is not 
    defined, the move will take place immediately when the previous move is 
    completed.

    If timings are defined, the train will be 'static' after completion of the 
    previous move until that required time.

    If the formed train has a valid station stop and the return path of the 
    stable command (in_path) terminates in the area of the platform of the first 
    station stop of the formed train, the 'setstop' check (see setstop qualifier 
    in ``$forms`` command) will automatically be added

    Command value : none.

    Command qualifiers :

        ``/out_path=<path>``: <path> is the path to be used by the train to move out to the 'stable' position. The start of the path must match the end of the path of the incoming train.

        ``/out_time = time``: time definition when the outward run must be started. Time is defined as HH:mm and must use the 24 hour clock.

        ``/in_path=<path>``: <path> is the path to be used by the train for the inward run from the 'stable' position to the start of the new train. The start of the path must match the end of the out_path, the end of the path must match the start of the path for the new train.

        ``/in_time = time``: time definition when the inward run must be started. Time is defined as HH:mm and must use the 24 hour clock.

        ``/runround=<path>``: <path> is the path to be used by the engine to perform the runround. For details, see the $forms command definition of the time at which the runround is to take place. The time must be defined in HH:mm and must use the 24 hour clock.

        ``/rrtime=time``: time is the definition of the time at which the runaround is to take place. The time must be defined in HH:mm and must use the 24 hour clock.

        ``/rrpos = <runround position>``: the position within the 'stable' move at which the runround is to take place.

            Possible values:

                - out: the runround will take place before the outward move is 
                  started.
                - stable: the runround will take place at the 'stable' position.
                - in: the runround will take place after completion of the 
                  inward move.

        ``/static``: train will become a 'static' train after completing the outward move.

        ``/forms=<train>``: train will form the new train after completion of the inward move. See the $forms command for details.

        ``/triggers=<train>``: train will trigger the new train after completion of the inward move. The train will change to the consist of the new train at the 'stable' position. See the $triggers command for details.

    Use of command qualifiers :

    In combination with /static:
    
        - /out_path: compulsory
        - /out_time: optional

    In combination with /forms:
    
        - /out_path: compulsory
        - /out_time: optional
        - /in_path: compulsory
        - /in_time: optional
        - /runround: optional
        - /rrtime: optional, only valid if /runround is set
        - /rrpos: compulsory if /runround is set, otherwise not valid

    In combination with /triggers :

        - /out_path: compulsory
        - /out_time: optional
        - /in_path: compulsory
        - /in_time: optional

Additional Notes on Timetables
==============================

Static Trains
-------------

A static train can be defined by setting ``$static`` in the top row (e.g. as the 
'name' of that train). Consist and path are still required - the path is used to 
determine where the consist is placed (rear end of train at start of path). No 
start-time is required. The train will be created from the start of the 
timetable - but it cannot be used for anything within a timetable. It cannot be 
referenced in any command etc., as it has no name. At present, it is also not 
possible to couple to a static train - see below for details.

Note that there are some differences between timetable and activity mode in the 
way that static trains are generated. In activity mode, the train is an instance 
of the Train class, with type STATIC.

In timetable mode, the train is an instance of the TTTrain class (as are all 
trains in timetable mode), with type AI, movement AI_STATIC. This difference may 
lead to different behaviour with respect to sound, smoke and lights.

Processing of #dispose Command For Player Train
-----------------------------------------------

When the player train terminates and a #dispose command is set for that train to 
form another train (either $form, $trigger or $stable), the train will indeed 
form the next train as detailed, and that next train will now be the new player 
train. So the player can continue with that train, for instance on a return 
journey.

On forming the new train, the train will become 'Inactive'. This is a new state, 
in which the train is not authorized to move.

Note that the :ref:`F4 Track Monitor <driving-track-monitor>` information is 
not updated when the train is 'Inactive'. The *Next Station* display in the 
:ref:`F10 Activity Monitor <driving-activity>` will show details on when the 
train is due to start. The train will become 'active' at the start-time as 
defined for the formed train. For information, the Activity Monitor window 
shows the name of the train which the player is running.

Termination of a Timetable Run
------------------------------

On reaching the end of a timetable run, the program will not be terminated 
automatically but has to be terminated by the player.

Calculation of Running Delay
----------------------------

An approximate value of the delay is continuously updated. This approximation 
is derived from the booked arrival time at the next station. If the present 
time is later as the booked arrival, and that difference exceeds the present 
delay, the delay is set to that difference. The time required to reach that 
station is not taken into account.

This approximation will result in better regulation where /maxdelay or 
/owndelay parameters are used.

No Automatic Coupling
---------------------

There is logic within the program which for any stopped train checks if it is 
close enough to another train to couple to this train. It is this logic which 
allows the player train to couple to any static train.

However, this logic contains some actions which do not match the processing of 
timetable trains.
*Therefore this has now been disabled for timetable mode*. 
*Presently, therefore, coupling of trains is not possible in timetable mode except for runround commands in dispose options*.

*Also uncoupling through the F9 window could be disabled in the near future for timetable mode*. *In due course, new attach/detach functions will be included in the timetable concept to replace the existing functions*.

.. _timetable-signalling:

Signalling Requirements and Timetable Concept
---------------------------------------------

General
'''''''

The timetable concept is more demanding of the performance of the signalling 
system than 'normal' activities. The main reason for this is that the timetable 
will often have AI trains running in both directions, including trains running 
ahead of the player train in the same direction as the player train. There are 
very few activities with such situations as no effort would of course be made 
to define trains in an activity which would never be seen, but also because 
MSTS could not always properly handle such a situation.

Any flaws in signalling, e.g. signals clearing the path of a train too far 
ahead, will immediately have an effect on the running of a timetable.

If signals clear too far ahead on a single track line, for instance, it means 
trains will clear through passing loops too early, which leads to very long 
waits for trains in the opposite direction. This, in turn, can lead to lock-ups 
as multiple trains start to converge on a single set of passing loops.

Similar situations can occur at large, busy stations - if trains clear their 
path through such a station too early, it will lead to other trains being kept 
waiting to enter or exit the station.

If ``$forms`` or ``$triggers`` commands are used to link reversing trains, the 
problem is exacerbated as any delays for the incoming train will work through 
on the return working.

.. _timetable-callon:

Call On Signal Aspect
'''''''''''''''''''''

Signalling systems may allow a train to 'call on', i.e. allow a train onto a 
section of track already occupied by another train (also known as permissive 
working).

The difference between 'call on' and 'permissive signals' (STOP and PROCEED 
aspects) is that the latter is also allowed if the train in the section is 
moving (in the same direction), but 'call on' generally is only allowed if the 
train in the section is at a standstill.

When a signal allows 'call on', AI trains will always pass this signal and run 
up to a pre-defined distance behind the train in the section.

In station areas, this can lead to real chaos as trains may run into platforms 
occupied by other trains such that the total length of both trains far exceeds 
the platform length, so the second train will block the 'station throat' 
stopping all other trains. This can easily lead to a complete lock-up of all 
traffic in and around the station.

To prevent this, calling on should be blocked in station areas even if the 
signalling would allow it. To allow a train to 'call on' when this is required 
in the timetable, the ``$callon`` command must be set which overrules the overall 
block. This applies to both AI and player train

In case the train is to attach to another train in the platform, calling on is 
automatically set.

Because of the inability of AI trains in MSTS to stop properly behind another 
train if 'called on' onto an occupied track, most signalling systems do not 
support 'call on' aspects but instead rely on the use of 'permission requests'. 
AI trains cannot issue such a request, therefore in such systems ``$callon`` 
will not work.

In this situation, attach commands can also not work in station areas.

Note that the 'runround' command also requires 'call on' ability for the final 
move of the engine back to the train to attach to it. Therefore, when performed 
in station areas, also the runround can only work if the signalling supports 
'call on'.

Special signalling functions are available to adapt signals to function as 
described above, which can be used in the scripts for relevant signals in the 
sigscr file.

The function "TRAINHASCALLON()" will return 'true' if the section beyond the 
signal up to the next signal includes a platform where the train is booked to 
stop, and the train has the 'callon' flag set. This function will also return 
'true' if  there is no platform in the section beyond the signal.

The function "TRAINHASCALLON_RESTRICTED" returns 'true' in similar conditions, 
except that it always returns 'false' if there is no platform in the section 
beyond the signal.

Both functions must be used in combination with BLOCK_STATE = BLOCK_OCCUPIED.

Wait Commands and Passing Paths
'''''''''''''''''''''''''''''''

From the location where the 'wait' or 'follow' is defined, a search is made for 
the first common section for both trains, following on from a section where the 
paths are not common.

However, on single track routes with passing loops where 'passing paths' are 
defined for both trains, the main path of the trains will run over the same 
tracks in the passing loops and therefore no not-common sections will be found. 
As a result, the waiting point cannot find a location for the train to wait and 
therefore the procedure will not work.

If waiting points are used on single track lines, the trains must have their 
paths running over different tracks through the passing loop in order for the 
waiting points to work properly.

It is a matter of choice by the timetable creator to either pre-set passing 
locations using the wait commands, or let the system work out the passing 
locations using the passing paths.

Wait Commands and Permissive Signals
''''''''''''''''''''''''''''''''''''

The 'wait' and 'follow' commands are processed through the 'blockstate' of the 
signal control. If at the location where the train is to wait permissive 
signals are used, and these signals allow a 'proceed' aspect on blockstate 
JN_OBSTRUCTED, the 'wait' or 'follow' command will not work as the train will 
not be stopped.

.. _timetable-midnight:

11.5.6.5 Running Trains Around Midnight.

A timetable can be defined for a full 24 hour day, and  so would include trains 
running around midnight.

The following rules apply for the player train:

- Train booked to start before midnight will be started at the end of the day, 
  but will continue to run if terminating after midnight.
- Trains formed out of other trains starting before midnight will NOT be 
  started if the incoming train is delayed and as a result the start time is 
  moved after midnight. In this situation, the activity is aborted.
- Trains booked to start after midnight will instead be started at the 
  beginning of the day.

The following rules apply for AI trains :

- Trains booked to start before midnight will be started at the end of the day, 
  but will continue to run if terminating after midnight.
- Trains formed out of other trains starting before midnight will still be 
  started if the incoming train is delayed and as a result the start time is 
  moved after midnight.
- Trains booked to start after midnight will instead be started at the 
  beginning of the day.

As a result of these rules, it is not really possible to run an activity around 
or through midnight with all required AI trains included.

Viewing the Other Active Trains in the Timetable
''''''''''''''''''''''''''''''''''''''''''''''''

To change the train that is shown in the external views, click ``<Alt+F9>`` 
to display the :ref:`Train List <driving-trainlist>` and select the desired 
train from the list of active trains, or click ``<Alt+9>`` as described in 
:ref:`Changing the View <driving-changing-view>` to cycle through the active 
trains.

Known Problems
--------------

- If a #dispose command is processed for the player train , and the new train 
  runs in the opposite direction, the reverser will 'jump' to the reverse state 
  on forming that new train.
- A run-round command defined in a #dispose command cannot yet be processed. It 
  will be necessary to switch to Manual to perform that run-round.
- If two trains are to be placed on a single siding using $create with /ahead 
  qualifier, but the trains have paths in opposite directions, the trains may be 
  placed in incorrect positions.
- If the /binary qualifier is set for #path, but the OpenRails subdirectory in 
  the Paths directory does not exist, the program will not be able to load any 
  paths.

Example of a Timetable File
===========================

Here is an excerpt of a timetable file (shown in Excel):

.. image:: images/timetable.png

What tools are available to develop a Timetable?
================================================

It is recommended to use a powerful stand-alone program (Excel is not required), 
called Timetable Editor. It is included in the OR pack, and accessed from the 
*Tools* button on the OR menu.
