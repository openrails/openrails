.. _options:

******************
Open Rails Options
******************

Clicking on the *Options* button opens a multi-panel window. The *Menu >
Options* panels contain the settings which remain in effect during your
simulation. Most of the options are self-explanatory; you may set them
according to your preference and system configuration. For example, you
can turn off dynamic shadowing if your system has low FPS
(frames-per-second) capability. The options configuration that you select
is saved when you click *OK*. When you restart OR, it will use the last
options configuration that you selected.

There are 10 option panels, described below.

.. _options-general:

General Options
===============

.. image:: images/options-general.png

Alerter in cab
--------------

As in real life, when this option is selected, the player driving the train
is required to perform specific actions to demonstrate that he is *alive*,
i.e. press the Alerter Button (or press the Key ``<Z>``). As the player may
sometimes use a view other than the cabview to follow the train, and
therefore will not see the alerter warning, selecting the related option
*Also in external views* enables the alerter in those views as well.


.. _options-control-confirmations:

Control confirmations
---------------------

Following MSTS practice, whenever you make adjustments to the train
controls (e.g. open the throttle) OR briefly shows a message near the
bottom of the screen.

.. image:: images/options-confirmations.png

This is helpful for operations that don't have visible feedback and also
allows you to control the train without being in the cab.

Uncheck this option if you prefer to monitor your cab instruments and
don't want to see these messages.

OR uses the same message scheme for system messages such as "Game saved"
or "Replay ended" but you cannot suppress these system messages.

Control confirmations can also be toggled on and off at runtime using the 
key combination Ctrl-Alt-F10. 


Graduated release air brakes
----------------------------

Selecting this option allows a partial release of the brakes. Generally
speaking, operating with the option checked is equivalent to passenger
standard and unchecked is equivalent to freight standard. A complete
description of this option can be found :ref:`here <physics-braking>`.



.. _options-retainers:

Retainer valve on all cars
--------------------------

The player can change the braking capability of all of the cars in the
simulation to include :ref:`Brake Retainers <physics-retainers>`. These
cause the brake cylinder on a car to retain some fixed pressure when the
train brakes are released; this causes the car to produce a constant
braking force. If this option is not checked, then brake retainers are
only found on cars that have an appropriate entry, as described
:ref:`here <physics-retainers>`, in their .wag files.

.. _options-brake-pipe-charging:

Brake pipe charging rate
------------------------

The Brake Pipe Charging Rate (psi/s) value controls the charging rate of
the main air brake pipe. Increasing the value will reduce the time
required to recharge the train (i.e. when releasing the brakes after a
brake application), while decreasing the value will slow the charging
rate. See also the :ref:`paragraphs <physics-hud-brake>` on the OR implementation of the braking
system.

If this parameter is set at 1000, a simplified, MSTS-like braking model is
implemented, providing for faster brake release and being less influenced
by incoherent braking parameters within .eng file.

Language
--------

OR is an internationalized package. It supports many languages, and others
can be added by following the instructions contained in the *Localization
Manual* which can be found in the Open Rails ``Documentation``
folder.

When *System* is selected, OR automatically selects the language of the
hosting OS, if the language is available.

.. _options-pressure:

Pressure unit
-------------

The player can select the unit of measure of brake pressure in the
:ref:`HUD display <driving-hud>`.

When set to *automatic* the unit of measure is the same as that used in
the cabview of the locomotive.

Other units
-----------

This selects the units displayed for length, mass, pressure, etc. in the
:ref:`F5 Train Driving Info Window and also the Alt+F5 HUD <driving-hud>` of the simulation.

The option *Player's Location* sets the units according to the Windows
*Language and Region* settings on the player's computer.

The option *Route* sets the units based on the data in the route files.
The other options are self-explanatory.

These windows use the abbreviations *t-us* for short tons (2000 lb),
*t-uk* for long tons (2240 lb) and *t* for metric tons (1000 kg).

Note: The units displayed by the :ref:`F4 Track Monitor <driving-track-monitor>` (e.g. velocity and
distance) are always based on data read from the route files.

Disable TCS scripts
-------------------

This option disables the train control system scripts for locomotives where
these have been implemented.

Web server port
-----------------

The web server can be accessed from a browser on the local machine at
``http://localhost:<port>``, where ``<port>`` is the specified port number.
Change the default value of 2150 if it conflicts with other services.

If you `open
<https://www.howtogeek.com/394735/how-do-i-open-a-port-on-windows-firewall/>`_
the web server port (just granting RunActivity.exe an exemption is not
sufficient) in Windows Firewall, the server can also be accessed from a device
on the local network, such as a smartphone, tablet or another PC, using your
system's `IP address
<https://support.microsoft.com/en-us/windows/find-your-ip-address-f21a9bbc-c582-55cd-35e0-73431160a1b9>`_.
E.g.: If your Open Rails PC is at IP address 192.168.0.99, browse to
``http://192.168.0.99:<port>``, where ``<port>`` is the specified port number.

:ref:`Sample web pages <sample-web-pages>` are included in the Open Rails
installation and the browser will show a menu of sample pages.

As well as a web browser, data from the web server can also be fetched by any
program which can make a web request, such as C# or Python, using the
:ref:`Application Programming Interface <web-server-api>` (API).

Overspeed Monitor
-----------------

If a Train Control Script (TCS) is specified for the loco and not disabled, then that takes priority.
Otherwise, if the loco has an Overspeed Monitor specified in its ENG file, then that monitor will detect excessive speed and respond as it was specified, e.g. by applying emergency braking.

This monitor is enabled by checking the option.

Audio Options
=============

.. image:: images/options-audio.png

Except for very slow computers, it is suggested that you set the Sound detail level to 5.

The *% sound volume* scroll button allows adjustment of the volume of OR
sound. Default is 40.

The *% external sound heard internally* scroll button allows to define the percentage
of the original volume of external sounds heard in cab and passenger views. In fact
in real world external sounds are attenuated when heard within a trainset.
This percentage may be overridden trainset by trainset as defined
:ref:`here <sound-external>`.

Video Options
=============

.. image:: images/options-video.png

Dynamic shadows
---------------

Check this option to cast shadows from movable objects such as trains.

The default setting is unchecked.

Note: This may reduce the frame rate.

Shadow for all shapes
---------------------

Check this option to cast shadows from static objects.

The default setting is unchecked.

Note: This may reduce the frame rate.

Note: Static objects provided with shadows (in the World files) 
will cast shadows anyway. This option adds shadows for other static objects.

Glass on in-game windows
------------------------

When this option is checked, the in-game windows are displayed in a
semitransparent mode.

Model instancing
----------------

When the option is checked, in cases where multiple instances of the same 
object have to be drawn, only a single draw call is sent to the GPU. 
Uncheck this option to avoid the graphical glitches which appear on some 
hardware, but this may reduce the frame rate.

The default setting is checked.

Overhead wire
-------------

This option will enable or disable display of the overhead wire.

.. _options-double-overhead-wires:

Double overhead wires
---------------------

MSTS uses a single wire for electrified routes; you may check this box so
that OR will show the two overhead wires that are more common.

.. _options-vsync:

Vertical sync
-------------

Vertical Sync (VSync) attempts to lock Open Rails’ output frame rate 
to your monitor's refresh rate for the smoothest image and to resist 
image "tearing”.
VSync may help keep the frame rate more stable on complex routes, 
reducing sudden frame rate drops and apparent control lag in some cases.
If Open Rails' frame rate drops below your monitor's frame rate, you 
may see stuttering or image "tearing". To prevent this, either turn off 
the VSync option or reduce the values for video options such as view 
distance, anti-aliasing, or world object density.

Viewing distance
----------------

This option defines the maximum distance at which terrain is displayed. 
Where the content provides "Distant Mountains", these are displayed independently (see below).

Note: Some routes are optimized for the standard MSTS maximum viewing distance (2km).

Note: When the option to tune settings automatically is applied, then this 
value will be overridden and dynamically changed to maintain a target frame rate.

The default distance is 2km.

Distant mountains
-----------------

This option defines the maximum distance at which "Distant Mountains" are displayed. 

Note: "Distant Mountains" are present in the route if it has a folder called LO_TILE. 

The default setting is checked.

The default distance is 40km

.. image:: images/options-mountains.png

Viewing vertical FOV
--------------------

This value defines the vertical angle of the world that is shown. Higher
values correspond roughly to a zoom out effect. The default is 45 degrees.

World object density
--------------------

This value can be set from 0 to 99 and the default value is 49.
When 49 is selected, all content defined in the route files and intended for the player to see is visible. 
Lower values will hide some categories of objects which tends to increase frame rates.

In legacy routes, all the content was assigned to categories 0-10.
In more modern routes, content may be assigned to categories between 0 and 49.
Content builders are advised to reserve values 50 to 99 for objects used in building the route.

Window size
-----------

This pair of values defines the size of the OR window. There are some
preconfigured pairs of values, however you may also manually enter a
different size to be used.

Ambient daylight brightness
---------------------------

With this slider you can set the daylight brightness.

Anti-aliasing
-------------

Controls the anti-aliasing method used by Open Rails. Anti-aliasing is a
computer graphics technique that smooths any harsh edges, otherwise known as
"jaggies," present in the video image. Currently, Open Rails only supports the
multisample anti-aliasing (MSAA) method. Higher applications of anti-aliasing
will require exponentially more graphics computing power.

The default setting is MSAA with 2x sampling.

.. _options-simulation:

Simulation Options
==================

The majority of these options define train physics behavior.

.. image:: images/options-simulation.png

.. _options-advanced-adhesion:

Advanced adhesion model
-----------------------

OR supports two adhesion models: the basic one is similar to the one used
by MSTS, while the advanced one is based on a model more similar to reality.

For more information read the section on :ref:`Adhesion Models <physics-adhesion>` later in this
manual.

Adhesion moving average filter size
-----------------------------------

The computations related to adhesion are passed through a moving average
filter. Higher values cause smoother operation, but also less
responsiveness. 10 is the default filter size.

Break couplers
--------------

When this option is selected, if the force on a coupler is higher than the
threshold set in the .eng file, the coupler breaks and the train is
divided into two parts. OR will display a message to report this.

.. _options-curve-resistance:

Curve dependent speed limit
---------------------------

When this option is selected, OR computes whether the train is running too
fast on curves, and if so, a warning message is logged and displayed on
the monitor. Excessive speed may lead to overturn of cars, this is also
displayed as a message. This option is described in detail
:ref:`here <physics-curve-speed-limit>` (theory) and also
:ref:`here <physics-curve-speed-limit-application>` (OR application).
OR does not display the damage.


Steam locomotive hot start
--------------------------

With this option selected, the temperature and pressure of steam in the boiler is ready to pull the train.
If not, the boiler pressure will be at 2/3 of maximum, which is only adequate for light work.
If your schedule gives you time to raise the pressure close to maximum, then 
switch from AI Firing to Manual Firing (Ctrl+F) and increase the Blower (N) to 100% to raise a draught. 
Replenish the coal using R and Shift+R to keep the fire mass close to 100%.
Full pressure may be reached in 5 minutes or so.

The default setting is checked.

.. _options-forced-red:

Forced red at station stops
---------------------------

In case a signal is present beyond a station platform and in the same
track section (no switches in between), OR will set the signal to red
until the train has stopped and then hold it as red from that time up to
two minutes before starting time. This is useful in organizing train meets
and takeovers, however it does not always correspond to reality nor to
MSTS operation. So with this option the player can decide which behavior
the start signal will have. 

This option is checked by default. 

Note: Unchecking the option has no effect when in 
:ref:`Timetable mode <timetable>`.

.. _options-open-doors-ai:

Open/close doors on AI trains
-----------------------------

This option enables door open/close at station stops on AI trains having passenger
trainsets with door animation. Doors are opened 4 seconds after train stop and closed
10 seconds before train start. Due to the fact that not all routes have been built with
correct indication of the platform side with respect to the track, this option can be
individually disabled or enabled on a per-route basis, as explained
:ref:`here <features-route-open-doors-ai>`.
With option enabled, doors open and
close automatically also when a player train is in :ref:`autopilot mode <driving-autopilot>`.
The option is active only in activity mode.

.. _options-location-linked-passing-path:

Location-linked passing path processing
---------------------------------------

When this option is NOT selected, ORTS acts similarly to MSTS. That is, if
two trains meet whose paths share some track section in a station, but are
both provided with passing paths as defined with the MSTS Activity Editor,
one of them will run through the passing path, therefore allowing the
meet. Passing paths in this case are only available to the trains whose
path has passing paths.

When this option is selected, ORTS makes available to all trains the main
and the passing path of the player train. Moreover, it takes into account
the train length in selecting which path to assign to a train in case of a
meet.

.. admonition:: For content developers

    A more detailed description of this feature can be
    found under :ref:`Location-Linked Passing Path Processing <operation-locationpath>`
    in the chapter  *Open Rails Train Operation*.

Simple control and physics
--------------------------

This is an option which players can set to simplify either the train controls or physics. 
This feature is intended for players who want to focus on "running" trains and don't want to be bothered 
by complex controls or prototypical physics which may require some additional expertise to operate.

Initially this option affects only trains that use vacuum braking but other controls may be added in future versions.

With vacuum braking, it is sometimes necessary to operate two different controls to apply and release the brakes. 
With "Simple control and physics" checked, the player is able to operate the brakes just with the brake valve 
and doesn't need to consider the steam ejector separately.

Diesel engines stopped at simulation start
------------------------------------------

When this option is unchecked, stationary diesel locos start the simulation with their engines running.
Check this option for a more detailed behaviour in which the player has to start the loco's engine.

The default setting is unchecked.


.. _options-keyboard:

Keyboard Options
================

.. image:: images/options-keyboard.png

In this panel you will find listed the keyboard keys that are associated
with all OR commands.

You can modify them by clicking on a field and pressing the new desired
key. Three symbols will appear at the right of the field: with the first
one you validate the change, with the second one you cancel it, with the
third one you return to the default value.

By clicking on *Check* OR verifies that the changes made are compatible,
that is, that there is no key that is used for more than one command.

By clicking on *Defaults* all changes that were made are reset, and the
default values are reloaded.

By clicking on *Export* a printable text file ``Open Rails
Keyboard.txt`` is generated on the desktop, showing all links between
commands and keys.

Data Logger Options
===================

.. image:: images/options-logger.png

By selecting the option *Start logging with the simulation start* or by
pressing ``<F12>`` a file with the name dump.csv is generated in the
configured Open Rails logging folder (placed on the Desktop by default).
This file can be used for later analysis.

Evaluation Options
==================

.. image:: images/options-evaluation.png

When data logging is started (see preceding paragraph), data selected in
this panel are logged, allowing a later evaluation on how the activity was
executed by the player.

.. _options-Content:

Content Options
===============

.. image:: images/options-content.png

This window allows you to add, remove or modify access to additional MSTS
installations or miniroute installations for Open Rails. Installations
located on other drives, or on a USB key, can be added even if they are
not always available.

Click on the *Add* button, and locate the desired installation. OR will
automatically enter a proposed name in the *Name:* window that will
appear in the *Installation set:* window on the main menu form. Modify
the name if desired, then click *OK* to add the new path and name to
Open Rails.

To remove an entry (note that this does not remove the installation
itself!) select the entry in the window, and click *Delete*, then *OK*
to close the window. To modify an entry, use the *Change...* button to
access the location and make the necessary changes.

.. _options-updater:

Updater Options
===============

.. image:: images/options-updater.png

These options control which OR version update channel is active (see also
:ref:`here <updating-or>`). The various options available are self-explanatory.

.. _options-experimental:

Experimental Options
====================

.. image:: images/options-experimental.png

Some experimental features being introduced in Open Rails may be turned on
and off through the *Experimental* tab of the Options window, as
described below.

Super-elevation
---------------

If the value set for *Level* is greater than zero, OR supports super-elevation 
for long curved tracks. The value *Minimum Length* determines
the length of the shortest curve to have super-elevation. You need to
choose the correct gauge for your route, otherwise some tracks may not be
properly shown.

When super-elevation is selected, two viewing effects occur at runtime:

1. If an external camera view is selected, the tracks and the running
   train will be shown inclined towards the inside of the curve.
2. When the cab view is selected, the external world will be
   shown as inclined towards the outside of the curve.

.. image:: images/options-superelevation_1.png
.. image:: images/options-superelevation_2.png

OR implements super-elevated tracks using Dynamic Tracks. You can change
the appearance of tracks by creating a ``<route folder>/TrackProfiles/
TrProfile.stf`` file. The document ``How to Provide Track Profiles for
Open Rails Dynamic Track.pdf`` describing the creation of track profiles
can be found in the *Menu > Documents* drop-down or the 
Open Rails ``/Source/Documentation/`` folder. Forum
discussions about track profiles can also be found on `Elvas Tower
<http://www.elvastower.com/forums/index.php?/topic/21119-superelevation/
page__view__findpost__p__115247>`_.

Automatically tune settings to keep performance level
-----------------------------------------------------

When this option is selected OR attempts to maintain the selected Target
frame rate FPS ( Frames per second). To do this it decreases or increases
the viewing distance of the standard terrain. If the option is selected,
also select the desired FPS in the *Target frame rate* window.

.. _options-shape-warnings:

Show shape warnings
-------------------

When this option is selected, when OR is loading the shape (.s) files it
will report errors in syntax and structure (even if these don't cause
runtime errors) in the :ref:`Log file <driving-logfile>` ``OpenRailsLog.txt`` on the desktop.

Signal light glow
-----------------

When this option is set, a glowing effect is added to signal semaphores
when seen at distance, so that they are visible at a greater distance.
There are routes where this effect has already been natively introduced;
for these, this option is not recommended.

Correct questionable braking parameters
---------------------------------------

When this option is selected, Open Rails corrects some braking parameters
if they are out of a reasonable range or if they are incoherent. This is
due to the fact that many existing .eng files have such issues, that are
not a problem for MSTS, which has a much simpler braking model, but that
are a problem for OR, which has a more sophisticated braking model. The
problem usually is that the train brakes require a long time to release,
and in some times do not release at all.

.. index::
   single: AirBrakesAirCompressorPowerRating

The following checks and corrections are performed if the option is
checked (only for single-pipe brake system):

- if the compressor restart pressure is smaller or very near to the max
  system pressure, the compressor restart pressure and if necessary the max
  main reservoir pressure are increased;
- if the main reservoir volume is smaller than 0.3 m\ :sup:`3` and the
  engine mass is higher than 20 tons, the reservoir volume is raised to 0.78
  m\ :sup:`3`;
- the charging rate of the reservoir is derived from the .eng parameter
  ``AirBrakesAirCompressorPowerRating`` (if this generates a value greater
  than 0.5 psi/s) instead of using a default value.

For a full list of parameters, see :ref:`Developing OR Content - Parameters and Tokens<parameters_and_tokens>`

.. _options-act-randomization:

Activity randomization
----------------------
The related ``Level`` box may be set to integer values from zero to three.
When a level of zero is selected, no randomization is inserted.
When a level greater than zero is selected, some activity parameters are randomly
changed, therefore causing different behaviors of the activity at every run.
Level 1 generates a moderate randomization, level 2 a significant randomization
and level 3 a high randomization, that may be unrealistic in some cases.
This feature is described in greater detail :ref:`here
<driving-act-randomization>`.

.. _options-actweather-randomization:

Activity weather randomization
------------------------------

The ``Level`` box works as the one for activity randomization, and has the
same range. When a level greater than zero is selected, the initial weather is
randomized, and moreover it changes during activity execution.
The randomization is not performed if at activity start the train is within a
lat/lon rectangle corresponding to the arid zone of North America (lat from
105 to 120 degrees west and lon from 30 to 45 degrees north).
The randomization is not performed either if the activity contains weather
change events.

.. _options-dds-textures:

Load DDS textures in preference to ACE
--------------------------------------

Open Rails is capable of loading both ACE and DDS textures. If only one of
the two is present, it is loaded. If both are present, the ACE texture is
loaded unless this option has been selected.


MSTS Environments
-----------------

By default ORTS uses its own environment files and algorithms, e.g. for
night sky and for clouds.

With this option selected, ORTS applies the MSTS environment files. This
includes support of Kosmos environments, even if the final effect may be
different from the current MSTS one.

Adhesion factor correction
--------------------------

The adhesion is multiplied by this percentage factor. Therefore lower
values of the slider reduce adhesion and cause more frequent wheel slips
and therefore a more difficult, but more challenging driving experience.

Level of detail bias
--------------------

Many visual objects are modelled at more than one level of detail (LOD) so, 
when they are seen at a distance, Open Rails can switch to the lesser level 
of detail without compromising the view. This use of multiple LODs reduces 
the processing load and so may increase frame rates.

Lowering the LOD Bias setting below 0 reduces the distance at which a lower 
level of detail comes into view, and so boosts frame rates but there may be 
some loss of sharpness.

Raising the LOD Bias setting above 0 increases the distance at which a lower 
level of detail comes into view. This may be useful to sharpen distant content 
that was created for a smaller screen or a wider field of view than you are 
currently using.

The default setting is 0.

Note: If your content does not use multiple LODs, then this option will have no effect.


Adhesion proportional to rain/snow/fog
--------------------------------------

When this option is selected, adhesion becomes dependent on the intensity
of rain and snow and the density of fog. Intensities and density can be
modified at runtime by the player.

Adhesion factor random change
-----------------------------

This factor randomizes the adhesion factor corrector by the entered
percentage. The higher the value, the higher the adhesion variations.

Precipitation Box Size
----------------------

Open Rails will simulate precipitation -- i.e. rain or snow, as falling
individual particles. This represents a significant computing and display
system load, especially for systems with limited resources. Therefore, the
region in which the precipitation particles are visible, the
*Precipitation Box*, is limited in size and moves with the camera. The
size of the box can be set by the entries in the height, width and length
boxes. The X and Z values are centered on the camera location, and falling
particles *spawn* and fall from the top of the box.

The max size for both length and width is 3000 meters or 9,842ft. Due to possibe
resource issues, the ability to use max length and width may not be possible.  The
best way to use the precipitation box is to define a square around your entire train
if small enough or around most of your train.  Keep track on how your resources are 
being used since snow will take up the most resources so you will have to adjust the
size until you are satisified with the results.

The reason for defining a square around your train is to minimize the moments when your train
is approaching the edge of the precipitation box.  Worst case is to save the activity,
exit and re-enter the activity since doing this will set your train back in the middle of the
precipitation box.

