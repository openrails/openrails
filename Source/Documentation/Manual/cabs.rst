.. _cabs:

***************
Open Rails Cabs
***************

OR supports both MSTS-compatible 2D cabs as well as native 3D cabs, even on 
the same locomotive.

For a full list of parameters, see :ref:`Developing OR Content - Parameters and Tokens<parameters_and_tokens>`

2D Cabs
=======

OR supports with a high degree of compatibility all functions available in 
MSTS for 2D cabs, and provides some significant enhancements described in the 
next paragraphs.

OR adds support for the ETCS circular speed gauge, as described 
:ref:`here <options-etcs>`.

.. _cabs-battery-switch:

Battery switch
--------------

.. index::
   single: ORTS_BATTERY_SWITCH_COMMAND_SWITCH
   single: ORTS_BATTERY_SWITCH_COMMAND_BUTTON_CLOSE
   single: ORTS_BATTERY_SWITCH_ON

The :ref:`battery switch <physics-battery-switch>` controls the low voltage power supply of the locomotive.

The following controls are available for the cabview:

- ``ORTS_BATTERY_SWITCH_COMMAND_SWITCH`` can be used if the switch is directly controlled from the cab
- ``ORTS_BATTERY_SWITCH_COMMAND_BUTTON_CLOSE`` and ``ORTS_BATTERY_SWITCH_COMMAND_BUTTON_OPEN`` can be used if the switch is controlled with two pushbuttons (one to close the switch and the other to open it)
- ``ORTS_BATTERY_SWITCH_ON`` can be used to control a light on the cab showing the state of the battery switch

Other controls can be hidden if the low voltage power supply is not available using the following parameter::

      TwoState (
         Type ( ORTS_CIRCUIT_BREAKER_CLOSED TWO_STATE)
         ...
         DisabledIfLowVoltagePowerSupplyOff ( 1 )
      )

.. _cabs-master-key:

Master key
----------

.. index::
   single: ORTS_MASTER_KEY
   single: ORTS_CURRENT_CAB_IN_USE
   single: ORTS_OTHER_CAB_IN_USE

The :ref:`master key <physics-master-key>` controls the power supply of the cab.

The following controls are available for the cabview:

- ``ORTS_MASTER_KEY`` can be used in order to control the master key
- ``ORTS_CURRENT_CAB_IN_USE`` can be used to indicate that the current cab is active
- ``ORTS_OTHER_CAB_IN_USE`` can be used to indicate that another cab of the train is active

Other controls can be hidden if the cab power supply is not available using the following parameter::

      TwoState (
         Type ( ORTS_CIRCUIT_BREAKER_CLOSED TWO_STATE)
         ...
         DisabledIfCabPowerSupplyOff ( 1 )
      )

.. _cabs-service-retention:

Service retention
-----------------

.. index::
   single: ORTS_SERVICE_RETENTION_BUTTON
   single: ORTS_SERVICE_RETENTION_CANCELLATION_BUTTON

The :ref:`service retention <physics-service-retention>` can be used to disable a cab without cutting the power on the train.
It can only be used with a power supply script that uses this functionality.

The following controls are available for the cabview:

- ``ORTS_SERVICE_RETENTION_BUTTON`` can be used in order to enable the service retention
- ``ORTS_SERVICE_RETENTION_CANCELLATION_BUTTON`` can be used in order to cancel the service retention

.. _cabs-electric-train-supply:

Electric train supply
---------------------

.. index::
   single: ORTS_ELECTRIC_TRAIN_SUPPLY_COMMAND_SWITCH
   single: ORTS_ELECTRIC_TRAIN_SUPPLY_ON

The :ref:`electric train supply <physics-electric-train-supply>` controls the power line that supplies the passenger cars with electricity.

The following controls are available for the cabview:

- ``ORTS_ELECTRIC_TRAIN_SUPPLY_COMMAND_SWITCH`` can be used to control the electric train supply switch
- ``ORTS_ELECTRIC_TRAIN_SUPPLY_ON`` can be used to indicate that the electric train supply line is powered on

.. _cabs-dieselenginesonoff:

Controls to switch on and off diesel engines
--------------------------------------------

The keyboard keys to switch on and off diesel engines are following ones:

  - Ctrl+Y switches on and off the first diesel engine of the player locomotive 
  - Shift+Y switches on and off the other diesel engines of the player locomotive, plus all diesel engines of the further locomotives in the train, if they are MUed, (that is under control of the player locomotive) which is the default.

  
Following cabview controls are available:

.. index::
   single: ORTS_PLAYER_DIESEL_ENGINE

ORTS_PLAYER_DIESEL_ENGINE: the first frame is displayed when the diesel engine 
of the player locomotive is in states stopped or stopping, while the second 
frame is displayed when it is in states running or started. The control may 
be used with the mouse and starts/stops the (first) diesel engine of the 
player locomotive, and is useful when a single two state lever is used to start/stop 
the engine.

Example::

                TwoState (
                        Type ( ORTS_PLAYER_DIESEL_ENGINE TWO_STATE)
                        Position ( 150 446 27 26 )
                        Graphic ( graphic1.ace )
                        NumFrames ( 2 2 1 )
                        Style ( ONOFF )
                        MouseControl ( 1 )
                )



.. index::
   single: ORTS_HELPERS_DIESEL_ENGINES

ORTS_HELPERS_DIESEL_ENGINES: the first frame is displayed when further diesel 
engines of the player locomotive and/or the diesel engines of the helper 
locomotives are in states stopped or stopping, while the second frame is 
displayed when they are in states running or started. The control may be used 
with the mouse and starts/stops further diesel engines of the player locomotive 
and the diesel engines of the helper locomotives, and is useful when a two 
state button or lever is used. Note therefore that this command can be used 
also for player locomotives with more than one engine.

Example::

                TwoState (
                        Type ( ORTS_HELPERS_DIESEL_ENGINES TWO_STATE)
                        Position ( 190 446 27 26 )
                        Graphic ( graphics2.ace )
                        NumFrames ( 2 2 1 )
                        Style ( ONOFF )
                        MouseControl ( 1 )
                )


.. index::
   single: ORTS_PLAYER_DIESEL_ENGINE_STATE

ORTS_PLAYER_DIESEL_ENGINE_STATE: this control respectively selects frames 0, 
1, 2, 3 for the player locomotive engine states Stopped, Starting, Running and 
Stopping. It is a display-only control.

Example::

                MultiState (
                        Type ( ORTS_PLAYER_DIESEL_ENGINE_STATE TRI_STATE)
                        Position ( 270 446 39 40 )
                        Graphic ( cd_363_zberace.ace )
                        NumFrames ( 4 4 1 )
                        Style ( NONE )
                        MouseControl ( 1 )
                        Orientation ( 0 )
                        DirIncrease ( 1 )
                )


.. index::
   single: ORTS_PLAYER_DIESEL_ENGINE_STARTER

ORTS_PLAYER_DIESEL_ENGINE_STARTER: it displays the second frame when the 
player diesel engine is in starting status, and the first one in all other 
cases. It may be used with the mouse and it can only start the engine, 
therefore it is useful in conjunction with ORTS_PLAYER_DIESEL_ENGINE_STOPPER 
when starting and stopping the engine is done with separate commands 
(e.g. 2 buttons).

Example::

                TwoState (
                        Type ( ORTS_PLAYER_DIESEL_ENGINE_STARTER TWO_STATE)
                        Position ( 310 446 27 26 )
                        Graphic ( graphics3.ace )
                        NumFrames ( 2 2 1 )
                        Style ( PRESSED )
                        MouseControl ( 1 )
                )


.. index::
   single: ORTS_PLAYER_DIESEL_ENGINE_STOPPER

ORTS_PLAYER_DIESEL_ENGINE_STOPPER: it displays the second frame when the 
player diesel engine is in stopping status, and the second one in all other 
cases. It may be used with the mouse and it can only stop the engine, 
therefore it is useful when starting and stopping the engine is done with 
separate commands (e.g. 2 buttons).

Example::

                TwoState (
                        Type ( ORTS_PLAYER_DIESEL_ENGINE_STOPPER TWO_STATE)
                        Position ( 350 446 27 26 )
                        Graphic ( Bell.ace )
                        NumFrames ( 2 2 1 )
                        Style ( PRESSED )
                        MouseControl ( 1 )
                )



.. _cabs-cabradio:

Cab radio
---------

.. index::
   single: CAB_RADIO

OR supports the cab radio cabview control.
Pressing keys Alt+R switches on and off the cab radio.
Switching on and off the cab radio enables discrete sound triggers 162 and 
163, as explained :ref:`here <sound-discrete>`.
Here is an example of a cab radio control block within the .cvf file::

			TwoState (
            Type ( CAB_RADIO TWO_STATE )
			      Position ( 150 425 30 21 )
			      Graphic ( Horn.ace )
			      NumFrames ( 2 2 1 )
			      Style ( ONOFF )
			      MouseControl ( 1 )
            )


Cab light
---------

.. index::
   single: ORTS_CABLIGHT

OR supports the cab light cabview control.
Pressing key L switches on and off the cab light under the same conditions 
applicable to MSTS.
Switching on and off the cab light enables discrete sound trigger 161, as 
explained :ref:`here <sound-discrete>`.
Here is an example of a cab light control block within the .cvf file::

			TwoState (
            Type ( ORTS_CABLIGHT TWO_STATE )
			      Position ( 120 425 30 21 )
			      Graphic ( Horn.ace )
			      NumFrames ( 2 2 1 )
			      Style ( ONOFF )
			      MouseControl ( 1 )
            )

Dedicated buttons for brake controllers
---------------------------------------

.. index::
    single: ORTS_BAILOFF

In addition to the BailOff keyboard command, a cabview control named
ORTS_BAILOFF is available. It is used to release the brakes of the engine
while keeping the train brakes applied.

.. index::
    single: ORTS_QUICKRELEASE

In some brake controllers, there is a button that provides a full and quick
release of the train brake when pressed. OR supports this via the
ORTS_QUICKRELEASE cabview control.

.. index::
    single: ORTS_OVERCHARGE

Some brake controllers have a dedicated button to overcharge the brake pipe.
The ORTS_OVERCHARGE cabview control can be used for this purpose.

Here is an example of one of this controls within the .cvf file::

			TwoState (
            Type ( ORTS_BAILOFF TWO_STATE )
			      Position ( 120 425 30 21 )
			      Graphic ( BailOff.ace )
			      NumFrames ( 2 2 1 )
			      Style ( PRESSED )
			      MouseControl ( 1 )
            )

Signed Traction Braking control
-------------------------------

.. index::
   single: ORTS_SIGNED_TRACTION_BRAKING
   single: TRACTION_BRAKING

This cabview control shows the signed value of the force (+ve or -ve, that is 
tractive or due to dynamic braking) as displayed 
in many real loco cabs. The control is ORTS_SIGNED_TRACTION_BRAKING. 
For comparison, the MSTS-compatible TRACTION_BRAKING cabview control shows the 
absolute value of the force. 
Here is an example of a cab light control block within the .cvf file::


            Dial (
                Type ( ORTS_SIGNED_TRACTION_BRAKING DIAL )
                Position ( 319 223 3 32 )
                Graphic ( ../../Common.Cab/CabE464/AgoDin.ace )
                Style ( NEEDLE )
                ScaleRange ( -761 1600 )
                ScalePos ( 190 70 )
                Units ( AMPS )
                Pivot ( 36 )
                DirIncrease ( 0 )
                )

Signed Traction Total Braking control
-------------------------------------

.. index::
   single: ORTS_SIGNED_TRACTION_TOTAL_BRAKING

ORTS_SIGNED_TRACTION_TOTAL_BRAKING control behaves and is defined like 
ORTS_SIGNED_TRACTION_BRAKING, with the only difference that the braking 
force does include also the train brake force in addition to the dynamic 
brake force.

Animated 2D Wipers
------------------

.. index::
   single:  ORTS_2DEXTERNALWIPERS

This control animates the wipers as seen from a 2D cab.
Animation is triggered on/off through key V.

Here is an example of a 2D wipers control block within the .cvf file::


        ORTSAnimatedDisplay  (
			Type ( ORTS_2DEXTERNALWIPERS MULTI_STATE_DISPLAY )
			Position ( 155 0 331.875 236.25 )
			Graphic ( ..//..//Common.Cab//CabE464_DMI//e464Tergicristallo9.ace )
			ORTSCycleTime ( 1.35 )
			States ( 9 3 3
				State (
					Style ( 0 )
					SwitchVal ( 0 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.11 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.22 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.33 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.44 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.55 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.66 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.77 )
				)
				State (
					Style ( 0 )
					SwitchVal ( 0.88 )
				)
			)
		)

ORTSCycleTime is expressed in seconds.
The .ace file must contain only the frames related to half cycle, that is 
if e.g. the wiper moves from left to right and back, only the frames related 
to the motion from left to right have to be included. For the reverse 
motion the same frames are used from last to first. SwitchVal can vary from 0 to 1.

Further OR cab controls
-----------------------

OR supports the cabview control to open/close the left doors, the right doors 
and the mirrors.

.. index::
   single: ORTS_LEFTDOOR
   single: ORTS_RIGHTDOOR
   single: ORTS_MIRRORS

The control blocks are like the one shown for the cab light. The Type strings 
are ORTS_LEFTDOOR, ORTS_RIGHTDOOR and ORTS_MIRRORS.


High-resolution Cab Backgrounds and Controls
--------------------------------------------

In MSTS the resolution of the cab background image is limited to 1024x1024; 
this limitation does not apply in OR as a result of OR's better handling of 
large textures.

2D cab backgrounds can reach at least to 3072x3072; however very fine results 
can be obtained with a resolution of 2560x1600. The image does not have to be 
square.

2D cab animations have also been greatly improved; you are reminded here that 
there are two types of animated rotary gauges, i.e. normal gauges and general 
animations using multiple frames. In this second case in MSTS all of the 
frames had to be present in a single texture with a max resolution of 
640x480. In OR these frames can be as large as desired and OR will scale them 
to the correct size. In general it is not necessary to use a resolution 
greater than 200x200 for every frame. 

The syntax to be used in the .cvf file is the standard one as defined by MSTS.

To clarify this, the position parameters of a sample needle block are 
described here.

In the ``Position`` statement, the first 2 numbers are the position of the top 
left-hand side of the needle texture in cabview units with the needle in the 
vertical position. In the ``Dial`` type the last 2 numbers are the size of the 
needle texture. The last number (50 in the example) controls the scaling of 
the needle texture, i.e. changing this changes the size of the needle that OR 
displays.

.. index::
   single: SPEEDOMETER

::

    Dial (
        Type ( SPEEDOMETER DIAL )
        Position ( 549 156 10 50 )
        Graphic ( Speed_recorder_needle_2.01.ace )
        Style ( NEEDLE )
        ScaleRange ( 0 140 )
        ScalePos ( 243 115 )
        Units ( KM_PER_HOUR )
        Pivot ( 38 )
        DirIncrease ( 0 ) 
    )

Next is an example of a control animation, this one is a simple 3 frame 
animation. The examples shown in the following images are the two rotary 
switches to the right of the two lower brake gauges, both being 3 position. 
(The left most switch is for the headlights). For these animations the 
graphic was done at 1600x1600; when each frame was finished it was scaled 
down to 200x200 and placed into the animation texture. Note the extreme 
sharpness of these controls in the inset image.

Adding a slight amount of 2x2 pixel blur helps the animation blend into the 
background better ( this has been done to the gauge needles).

Below is the appropriate part of the CVF. The scaling is controlled by the 
last two digits of the ``Position`` statement::

    TriState (
        Type ( DIRECTION TRI_STATE )
        Position ( 445 397 35 35 )
        Graphic ( Switch_nob_3.0_Transmission.ace )
        NumFrames ( 3 3 1 )
        Style ( NONE )
        MouseControl ( 1 )
        Orientation ( 0 )
        DirIncrease ( 0 )
    )

Note that the "Airbrake On" light (on the panel upper left) has also been 
animated. This is a simple 2 frame animation. 

.. image:: images/cabs-hires-full.png
.. image:: images/cabs-hires-detail.png

Shown above are two pictures of one hi-res 2D cabview, one showing the whole 
cab, and the other one showing the detail of some controls. In this example 
the cab background image used was cut down to 2560x1600. The texture for the 
Speed Recorder needle is 183x39 and for the brake gauge needles is 181x29, 
Note the odd number for the width. This is required as OR (and MSTS) assume 
the needle is in the center of the image. The Reversing and Headlight switch 
animation frames are 116x116.

There are as yet no specific tools to create these cabviews; a standard image 
manipulation program to do all textures is required, and to create any new 
items, e.g. the gauge faces, a standard drawing program can be used. To 
actual set up the cabview and to position the animations the .cvf file is 
modified with a standard text editor, and OR is used as a viewer, using a 
straight section of track on a quick loading route. Through successive 
iterations one arrives quite quickly at a satisfactory result. 

Configurable Fonts
------------------

OR supports a configurable font family, with font size selection, and a 
choice of regular or bold style. More than one font or size can be used in 
the same cabview. This does not affect the display in MSTS.

.. index::
   single: ORTSfont
   
An optional line of the form ``ORTSfont ( fontsize  fontstyle  "fontfamily" )`` 
must be inserted into the .cvf block of the digital control or digital clock, 
where *fontsize* is a float (default value 10), *fontstyle* an integer having 
the value 0 (default) for regular and 1 for bold, and *fontfamily* is a 
string with the font family name (ex. "Times New Roman"). The default is 
"Courier New". A convenient font, if available, is "Quartz MS" or "Quartz", 
which models a 7-segment display.

.. index::
   single: DIGITAL_CLOCK

Here is an example that displays the digital clock with a 12 pt. bold font 
using the Sans Serif font family::

    DigitalClock (
        Type ( CLOCK DIGITAL_CLOCK )
        Position ( 40 350 56 11 )
        Style ( 12HOUR )
        Accuracy ( 1 )
        ControlColour ( 255 255 255 )
        ORTSFont ( 12 1 "Sans Serif" )
    )

It is acceptable if only the first parameter of ORTSFont is present, or only 
the first two, or all three. 
Note that you cannot use the MS Cabview editor on the .cvf file after having 
inserted these optional lines, because the editor will delete these added 
lines when the file is saved.

Rotation of Gauges and Digital controls
---------------------------------------

.. index::
   single: ORTSAngle

One of the drawbacks of rendering a cabview in 2D is that some parts of it 
are not shown with a frontal, precisely vertical or horizontal, view. 
Displaying a vertical gauge or a horizontal digital control on it generates an
unrealistic effect. This is the rationale of following entry, to be added
within a Gauge or Digital cabview control block in the .cvf file::

  ORTSAngle ( 5 )

The number in parenthesis is the angle in degrees with respect to the horizontal 
(or to the vertical for vertical gauges). Positive values produce
counterclockwise rotation.

At the left of the picture an example of a white vertical gauge that has been rotated 
by 12 degrees

.. image:: images/cabs-gauges-rotation.png

Here an example of a red max speed indication that has been rotated by 5 degrees

.. image:: images/cabs-digitals-rotation.png

Gauges may have Style POINTER or SOLID.

Rotation may be applied, with the same syntax, also to DigitalClock cab controls.


3D cabs
=======

If the locomotive has a 3D cab, it will be selected by default by the simulator.
You can press key ``<1>`` to enter the cab. In case locomotive has both 2D and 3D cabs
provided, the key ``<Alt+1>`` can be used in order to switch between 2D and 3D cabs.

Development Rules
-----------------

.. index::
   single: CABVIEW3D
   single: SPEEDOMETER
   single: TRAIN_BRAKE

- The 3D cab is described by an .s file, the associated .ace or .dds files, 
  and a .cvf file having the same name as the .s file. All these files reside 
  in a folder named ``CABVIEW3D`` created within the main folder of the 
  locomotive.
- If the .cvf file cannot be found in the ``CABVIEW3D`` folder, the 3D cab is 
  associated with the .cvf file of the 2D cab.
- Instruments are named with the same conventions as 2D cabs, i.e. 
  ``FRONT_HLIGHT``, ``SPEEDOMETER``, etc.
- A cab can have multiple instances of the same instruments, for example 
  multiple clocks or speedometers.
- Instruments are sorted based on the order of their appearance in the .cvf 
  file, for example ``SPEEDOMETER:0`` corresponds to the first speedometer in 
  the .cvf file, ``SPEEDOMETER:1`` corresponds to the second one.
- An instrument can have multiple subgroups to make the animation realistic, 
  for example, ``TRAIN_BRAKE:0:0`` and ``TRAIN_BRAKE:0:1`` belong to the 
  instrument ``TRAIN_BRAKE:0``. However, if the instrument is a digital 
  device, the second number will be used to indicate the font size used, for 
  example ``SPEEDOMETER:1:14`` means the second speedometer (which is digital 
  as defined in .cvf) will be rendered with 14pt font. This may be changed 
  in future OR releases. The important information for a digital device is 
  its location, thus it can be defined as an object with a small single face 
  in the 3D model.

.. index::
   single: ORTS3DCab
   single: ORTS3DCabFile
   single: ORTS3DCabHeadPos
   single: RotationLimit
   single: StartDirection

- Animation ranges must be in agreement with the .cvf file 
- Within the Wagon section of the .eng file a block like the following one 
  has to be generated::
  
    ORTS3DCab(
        ORTS3DCabFile ( Cab.s )
        ORTS3DCabHeadPos ( -0.9 2.4 5.2 )
        RotationLimit ( 40 60 0 )
        StartDirection ( 12 0 0 )
    )

.. index::
   single: EXTERNALWIPERS
   single: AMMETER
   single: CLOCK
   single: CABVIEW3D
   single: LEFTDOOR
   single: RIGHTDOOR
   single: MIRRORS
   single: NIGHT
   single: ESD_Alternative_Texture

- It is also possible to animate the wipers, by inserting into the .s file an 
  animation named ``EXTERNALWIPERS:0:0``
- Gauges of solid type have to be named ``AMMETER:1:10:100``; where the three 
  numbers indicate that this is the second ammeter, that it has a width 10 mm, 
  and a maximum length of 100 mm. The color and direction/orientation follow 
  those defined in .cvf files.
- Digits for 3D cabs can now use custom ACE files; e.g. name the part as 
  ``CLOCK:1:15:CLOCKS``. This will draw the second clock with 15mm font 
  dimension, with the ``CLOCKS.ACE`` file in ``CABVIEW3D`` containing the 
  font. If no ace is specified, the default will be used.
- Mirrors and doors can be operated from 3D cabs. The names used are 
  ``LEFTDOOR``, ``RIGHTDOOR`` and ``MIRRORS``.
- like the 2D cabs, also 3D cabs can have a night version. Night textures, named like the 
  corresponding day textures, must be located within a ``NIGHT`` subfolder of the 
  ``CABVIEW3D`` folder. To enable night cabs an ``.sd`` file with the same name as the 
  shape file of the 3D cab must be present in the ``CABVIEW3D`` folder. This ``.sd`` file 
  has a standard format and must contain following line::

    ESD_Alternative_Texture ( 256 )

- How to control the view in a 3D cab is described :ref:`here <driving-changing-view>`.

A demo trainset with a 3Dcab, that may be useful for developers, can be 
downloaded from: `http://www.tsimserver.com/Download/Df11G3DCab.zip`_

.. _http://www.tsimserver.com/Download/Df11G3DCab.zip: http://www.tsimserver.com/Download/Df11G3DCab.zip

A Practical Development Example For a Digital Speedometer
---------------------------------------------------------

Let's suppose you wish to create a digital speedometer using a size 14 font.

To explain it in *gmax* language, you must have an object called ``SPEEDOMETER`` 
in the cab view and it must be comprised of at least one face.

As the sample cab has only one digital speedometer, it can be named 
``SPEEDOMETER_0_14``.

The number 0 indicates that this is the first speedometer gauge in the cab 
and the number 14 indicates the size of the font to display. Note that an 
underscore is used to separate the numbers as the LOD export tool does not 
support the use of colons in object names when exporting. More on this later.

The speed does not display where the face for the ``SPEEDOMETER`` object is 
located but where the *pivot point* for the ``SPEEDOMETER`` object is located. 
Normally you would place the ``SPEEDOMETER`` object somewhere in the cab where 
it will not be seen. 

With the ``SPEEDOMETER_0_14`` object selected in gmax, go to the *Hierarchy* 
tab, select *Affect Pivot Only* and click *Align to World* to reset the 
orientation to world coordinates. Then use the *Select and Move* tool to move 
the pivot to where in the cab you wish the numerals to appear. As you have 
aligned the pivot point to World coordinates the numerals will display 
vertically. As most locomotive primary displays are normally angled you may 
have to rotate the pivot point so that it aligns with the angle of the 
*display screen*.

Export the .S file for the cab as usually.

You will then have to uncompress the .s file for the cab using Shape File 
Manager or the .S file decompression tool of your choice.

Then open the .S file with a text editor and search for the letters "speed" 
until you find the first instance of ``SPEEDOMETER_0_14`` and change it to be 
``SPEEDOMETER:0:14``. Search again and find the second instance of 
``SPEEDOMETER_0_14`` and change that also to ``SPEEDOMETER:0:14``. Save the 
.S file in the text editor.

Now just one more thing. Download the ``DF11G3DCab`` demo trainset. In the 
``CABVIEW3D`` folder of that download you will find an ace file called 
``SPEED.ACE``. Copy that file and paste it into the ``CABVIEW3D`` folder 
for your model.

Now, open OR and test your speedometer.


FUEL_GAUGE for steam locomotives
--------------------------------

.. index::
   single: FUEL_GAUGE

The FUEL_GAUGE dial is available also for steam locomotives. It may be used 
both to display a fuel level for oil burning steam locomotives (also in 2D cabs), 
and to animate the coal level in a tank loco. Default unit of measure is Kg; 
alternate unit of measure may be LBS.
Here below is an example of an entry for a 3D cab::

  Dial (
  Type ( FUEL_GAUGE DIAL )
  Style ( POINTER )
  ScaleRange ( 0 5000 )
  Units ( LBS )
  ) 
