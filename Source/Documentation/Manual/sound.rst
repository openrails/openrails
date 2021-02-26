.. _sound:

***************************
Open Rails Sound Management
***************************

OpenRails vs. MSTS Sound Management
===================================

OR executes .sms files to a very high degree of compatibility with MSTS. 

.sms Instruction Set
====================

OR recognizes and manages the whole MSTS .sms instruction set, in a way 
generally compatible with MSTS. The differences are described below.

The ``Activation ()`` instruction behaves differently from MSTS with regard 
to cameras (``CabCam``, ``ExternalCam`` and ``PassengerCam``): in general OR 
does not consider which cameras are explicitly activated within the .sms 
files. Instead, it uses a sort of implicit activation, that as a general rule 
works as follows:

- when in an inside view (cabview or passenger view) the related inside .sms 
  files are heard, plus all external .sms files (with the exception of those 
  related to the trainset where the camera is in that moment): the volume of 
  those external files is attenuated by a 0.75 factor.
- when in an external view all external .sms files are heard.

For an .sms file to be heard, it must be within the activation distance 
defined in the related instruction.

A hack is available so as to hear only in the cabview some .sms files 
residing outside the cabview trainset. This can be used e.g. to implement 
radio messages. For this to work the related .sms file must be called within 
a .wag file, must contain an ``Activation ( CabCam )`` statement, and the 
related wagon must be within a loose consist, within a not yet started AI 
train or within the consist where the cabview trainset resides. 

The ``ScalabiltyGroup ()`` instruction behaves differently from MSTS for AI 
trains. While MSTS uses ``ScalabiltyGroup ( 0 )`` for AI trains, OR uses for 
AI trains the same ``ScalabiltyGroup`` used for player trains. This way AI 
train sound can profit from the many more triggers active for AI trains in 
ORTS. For instance, Variable2 trigger is not active in MSTS for AI trains, 
while it is in ORTS.

If a ``Stereo()`` line is present within a ``ScalabiltyGroup``, and a mono .wav 
sound is called, MSTS will play the sound at double speed. In order to have it 
play at the correct speed, a frequency curve halving the speed has to be 
inserted. OR behaves the same as MSTS in this case.

Discrete Triggers
-----------------

Unlike MSTS, OR does not restrict the operation of some discrete triggers 
related to locomotives to the cabview related .sms file (usually named 
...cab.sms file). On OR they are all also active in the file related to the 
external view (usually named ...eng.sms file).

OR manages the following MSTS discrete triggers:

=========     ===============================================
Trigger       Function
=========     ===============================================
    2         DynamicBrakeIncrease (currently not managed)
    3         DynamicBrakeOff 
    4         SanderOn
    5         SanderOff
    6         WiperOn
    7         WiperOff
    8         HornOn
    9         HornOff
    10        BellOn
    11        BellOff
    12        CompressorOn
    13        CompressorOff
    14        TrainBrakePressureIncrease
    15        ReverserChange
    16        ThrottleChange
    17        TrainBrakeChange
    18        EngineBrakeChange 
    20        DynamicBrakeChange
    21        EngineBrakePressureIncrease
    22        EngineBrakePressureDecrease
    27        SteamEjector2On 
    28        SteamEjector2Off 
    30        SteamEjector1On 
    31        SteamEjector1Off 
    32        DamperChange
    33        BlowerChange 
    34        CylinderCocksToggle
    36        FireboxDoorChange
    37        LightSwitchToggle
    38        WaterScoopDown
    39        WaterScoopUp
    41        FireboxDoorClose
    42        SteamSafetyValveOn
    43        SteamSafetyValveOff
    44        SteamHeatChange
    45        Pantograph1Up
    46        Pantograph1Down
    47        Pantograph1Toggle
    48        VigilanceAlarmReset
    54        TrainBrakePressureDecrease 
    56        VigilanceAlarmOn
    57        VigilanceAlarmOff 
    58        Couple
    59        CoupleB (currently not managed)
    60        CoupleC (currently not managed)
    61        Uncouple
    62        UncoupleB (currently not managed)
    63        UncoupleC (currently not managed)
=========     ===============================================

MSTS .sms files for crossings (``crossing.sms``), control error and permission 
announcements (``ingame.sms``) together with their triggers, and for fuel tower are managed by OR.

MSTS triggers for derailment are currently not managed by OR.

MSTS .sms files related to weather (``clear_ex.sms``, ``clear_in.sms``, 
``rain_ex.sms``, ``rain_in.sms``, ``snow_ex.sms``, ``snow_in.sms``) are 
managed by OR.

The signal file (``signal.sms``) and its discrete trigger 1 is managed by OR.

Moreover, OR manages the extended set of discrete triggers provided by MSTSbin.

.. _sound-discrete:

OR-Specific Discrete Triggers
-----------------------------

OR manages the following set of new discrete triggers that were not present 
under MSTS. If MSTS (or MSTSbin) executes an .sms where such discrete 
triggers are used, it simply ignores the related statements.

In addition, OpenRails extends triggers 23 and 24 (electric locomotive power 
on/power off), that were introduced by MSTSbin, to diesel engines. Keys 
``<Shift+Y>`` (for diesel player engine) and ``<Ctrl+Y>`` (for diesel 
helpers), apart from physically powering on and off the diesel engines, 
trigger the above triggers.

=========     ==============================================================================================================================================================
Trigger       Function
=========     ==============================================================================================================================================================
101           GearUp : for gear-based engines, triggered by the ``<E>`` key, propagated to all gear-based diesel engines of a train and run also for AI trains
102           GearDown : for gear-based engines, triggered by the ``<Shift+E>`` key, propagated to all gear-based diesel engines of a train and run also for AI trains
103           ReverserToForwardBackward : reverser moved towards the forward or backward position
104           ReverserToNeutral : reverser moved towards the neutral position
105           DoorOpen : triggered by the ``<Q>`` and ``<Shift+Q>`` keys and propagated to the wagons of the consist
106           DoorClose : triggered by the ``<Q>`` and ``<Shift+Q>`` keys and propagated to the wagons of the consist
107           MirrorOpen : triggered by the ``<Shift+Q>`` key
108           MirrorClose : triggered by the ``<Shift+Q>`` key
=========     ==============================================================================================================================================================

Triggers from 109 to 118 are used for TCS scripting, as follows:

=========     ============================
Trigger       Function
=========     ============================
109           TrainControlSystemInfo1
110           TrainControlSystemInfo2
111           TrainControlSystemActivate
112           TrainControlSystemDeactivate
113           TrainControlSystemPenalty1
114           TrainControlSystemPenalty2
115           TrainControlSystemWarning1
116           TrainControlSystemWarning2
117           TrainControlSystemAlert1
118           TrainControlSystemAlert2
=========     ============================

Triggers from 121 to 136 are used to synchronize steam locomotive chuffs with 
wheel rotation. The sixteen triggers are divided into two wheel rotations. 
Therefore every trigger is separated from the preceding one by a rotation 
angle of 45 degrees.

Triggers 137 and 138 are used for the cylinder cocks of steam locomotives:

=========     =============================================================
Trigger       Function
=========     =============================================================
137           CylinderCocksOpen : triggered when cylinder cocks are opened
138           CylinderCocksClose : triggered when cylinder cocks are closed
=========     =============================================================

Triggers from 139 to 143 can be used to make looped brake sounds:

=========     ============================================================================================================================================================================
Trigger       Function
=========     ============================================================================================================================================================================
139           TrainBrakePressureStoppedChanging : for rolling stock equipped with train brakes, to use with triggers 14 and 54, triggered when the automatic brake pressure stops changing
140           EngineBrakePressureStoppedChanging : for locomotives with engine/independent brakes, to use with triggers 21 and 22, triggered when the engine brake pressure stops changing
141           BrakePipePressureIncrease : for rolling stock equipped with train brakes, triggered when brake pipe/brakeline pressure increases
142           BrakePipePressureDecrease : for rolling stock equipped with train brakes, triggered when brake pipe/brakeline pressure decreases
143           BrakePipePressureStoppedChanging : for rolling stock equipped with train brakes, triggered when brake pipe/brakeline pressure stops changing
=========     ============================================================================================================================================================================

=========     =====================================
Trigger       Function
=========     =====================================
145           WaterScoopRaiseLower
146           WaterScoopBroken
=========     =====================================

=========     ======================================================================
Trigger       Function
=========     ======================================================================
147           SteamGearLeverToggle : Toggles when steam gear lever is moved.
148           AIFiremanSoundOn : AI fireman mode is on.
149           AIFiremanSoundOff : AI fireman mode is off, ie in Manual Firing mode.
=========     ======================================================================

Triggers from 150 to 158 are used for the circuit breaker sounds.

The following triggers are activated when the state of the circuit breaker changes:

=========     =====================================
Trigger       Function
=========     =====================================
150           CircuitBreakerOpen
151           CircuitBreakerClosing
152           CircuitBreakerClosed
=========     =====================================

The following triggers are activated when the driver moves the buttons or switches in the cab:

=========     =====================================
Trigger       Function
=========     =====================================
153           CircuitBreakerClosingOrderOn
154           CircuitBreakerClosingOrderOff
155           CircuitBreakerOpeningOrderOn
156           CircuitBreakerOpeningOrderOff
157           CircuitBreakerClosingAuthorizationOn
158           CircuitBreakerClosingAuthorizationOff
=========     =====================================

Trigger 161 is activated when the cab light is switched on or off.

The following triggers are activated when the state of the cab radio changes 
(see :ref:`here <cabs-cabradio>`):

=========     =====================================
Trigger       Function
=========     =====================================
162           Cab radio switched on
163           Cab radio switched off
=========     =====================================

The following triggers are activated when the state of the engines 
different from the first one change state in a diesel locomotive 
(see :ref:`here <cabs-dieselenginesonoff>`):

=========     =====================================
Trigger       Function
=========     =====================================
167           Second engine power on
168           Second engine power off
=========     =====================================

Following triggers are activated when a 3rd and a 4th Pantograph 
are present on the locomotive:

=========     =====================================
Trigger       Function
=========     =====================================
169           Pantograph3Up
170           Pantograph3Down
171           Pantograph4Up
172           Pantograph4Down
=========     =====================================

Additional triggers:

=========     =====================================
Trigger       Function
=========     =====================================
173           HotBoxBearingOn
174           HotBoxBearingOff
175           BoilerBlowdownOn
176           BoilerBlowdownOff
=========     =====================================



The following triggers are used to activate the gear positions:

=========     =====================================
Trigger       Function
=========     =====================================
200           GearPosition0
201           GearPosition1
202           GearPosition2
203           GearPosition3
204           GearPosition4
205           GearPosition5
206           GearPosition6
207           GearPosition7
208           GearPosition8
=========     =====================================

Additional triggers for vacuum brakes:

=========     =====================================
Trigger       Function
=========     =====================================
210           LargeEjectorOn
211           LargeEjectorOff
212           SmallEjectorOn
213           SmallEjectorOff
=========     =====================================


Variable Triggers
-----------------

ORTS
^^^^

The sound objects attached to a vehicle (wagon or loco) can respond in volume and frequency to changes in the vehicle's properties.
There are 7 properties:

- distance squared from a sound source (m\ :sup:`2`)

- speed (m/s)	

- pressure in the brake cylinder (psi)	

- centrifugal force due to traversing a curve (N)	

- 3 variables in range 0 - 1:

  - Variable1 reflects the throttle

  - Variable2 reflects the engine's RPM (diesel) or Tractive Force (electric) or cylinder pressure (steam)

  - Variable3 reflects the dynamic brake (diesel | electric) or fuel rate (steam)
		
Note: Separately, for a whole route, sounds for all curves below a certain radius can be automatically triggered as vehicles pass - see :ref:`sound-curve` below.		


Comparison with MSTS
^^^^^^^^^^^^^^^^^^^^

OR manages all of the variable triggers managed by MSTS. There can be some 
difference in the relationship between physical locomotive variables (e.g. 
Force) and the related variable. This applies to Variable2 and Variable3. 

New variables introduced by OR:

- BrakeCyl, which contains the brake cylinder pressure in PSI. Like the 
  traditional MSTS variables, it can be used to control volume or frequency 
  curves (``BrakeCylControlled``) and within variable triggers 
  (``BrakeCyl_Inc_Past`` and ``BrakeCyl_Dec_Past``).
- CurveForce, in Newtons when the rolling stock is in a curve. Can be used for 
  curve flange sounds, with two volume curves: one is ``SpeedControlled``, 
  which makes the sound speed dependent too, and ``CurveForceControlled``. 
  Of course ``CurveForce_Inc_Past``, and ``CurveForce_Dec_Past`` are also 
  available for activating and deactivating the sound.

Sound Loop Management
---------------------

Sound loop management instructions are executed as follows by OR:

- ``StartLoop`` / ``ReleaseLoopRelease``: the .wav file is continuously 
  looped from beginning to end; when the ReleaseLoopRelease instruction is 
  executed, the .wav file is played up to its end and stopped.
- ``StartLoopRelease`` / ``ReleaseLoopRelease``: the .wav file is played from 
  the beginning up to the last CuePoint, and then continuously looped from 
  first to last CuePoint; when the ``ReleaseLoopRelease`` instruction is 
  executed, the .wav file is played up to its end and stopped.
- ``StartLoopRelease`` / ``ReleaseLoopReleaseWithJump``: the .wav file is 
  played from the beginning up to the last CuePoint, and then continuously 
  looped from the first to the last CuePoint. When the 
  ``ReleaseLoopReleaseWithJump`` instruction is executed, the .wav file is 
  played up to the next CuePoint, then jumps to the last CuePoint and 
  stops. It is recommended to use this pair of instructions only where a 
  jump is effectively needed, as e.g. in horns; this because this couple of 
  instructions is more compute intensive and can lead to short sound breaks 
  in the case of high CPU loads.

Testing Sound Files at Runtime
------------------------------

The :ref:`sound debug window <driving-sound-debug>` is a useful tool for 
testing.

.. _sound-curve:

Automatic switch and curve squeal track sound
=============================================

With this feature a specific track sound is played when a train passes over any switch or 
crossover, or over a curve with a low radius, which highly enhances the sound experience.
If this feature is enabled there is no more 
need to lay down specific sound regions around or sound sources above every 
switch or over curves. This is a lengthy task, and in fact most of the routes aren't 
equipped with such sound regions or sound sources.
Three automatic sounds are supported::

-  switch sound
-  curve squeal sound
-  curve + switch sound (when wagon is both on curve and switch).

It is possible to define also only one or two of these automatic sounds. If switch and 
curve squeal sound are defined, and no curve + switch sound is defined, the curve squeal 
sound is played when a wagon is both on curve and switch.
The curve radius threshold below which the curve squeal sound is played is 350 meters for 
freight wagons and 301 meters for all other trainsets.

To enable this feature steps here below must be followed:

1. Suitable external and internal automatic sounds must be available (.sms files); 
   usually you find them in the root's ``SOUND``. It often occurs that switch track 
   and curve squeal sounds are available in modern routes. If not, they must be created 
   or searched on the web. A test sound set may be downloaded from 
   `here <http://www.interazioni-educative.it/Varie/DemoAutoSound.zip>`_.
2. For every route it must be checked whether a reference to the three automatic track 
   sounds are present in the route's ``ttype.dat`` file. If they are, you can proceed 
   to next step. 
   Else you must insert three new lines at the end of ``ttype.dat``, adding the reference 
   to the automatic track sounds, and you must add 3 to the number on top of the file.
   Here below an example of a default ``ttype.dat`` can be found,  where three new lines 
   referring to the above test sound have been added in last position::

     SIMISA@@@@@@@@@@JINX0t1t______
     
     13
     TrackType ( "Default" "EuropeSteamTrack0In.sms" "EuropeSteamTrack0Ex.sms" )
     TrackType ( "Concrete Supported"	"EuropeSteamTrack1In.sms" "EuropeSteamTrack1Ex.sms" )
     TrackType ( "Wood Supported"	"EuropeSteamTrack2In.sms" "EuropeSteamTrack2Ex.sms" )
     TrackType ( "In Tunnel" "EuropeSteamTrack3In.sms" "EuropeSteamTrack3Ex.sms" )
     TrackType ( "Steel Bridge" "EuropeSteamTrack4In.sms" "EuropeSteamTrack4Ex.sms" )
     TrackType ( "Girder Bridge" "EuropeSteamTrack5In.sms" "EuropeSteamTrack5Ex.sms" )
     TrackType ( "Under Bridge" "EuropeSteamTrack6In.sms" "EuropeSteamTrack6Ex.sms" )
     TrackType ( "Concrete Bridge" "EuropeSteamTrack7In.sms" "EuropeSteamTrack7Ex.sms" )
     TrackType ( "Crossing Platform" "EuropeSteamTrack8In.sms" "EuropeSteamTrack8Ex.sms" )
     TrackType ( "Wooden Bridge" "EuropeSteamTrack9In.sms" "EuropeSteamTrack9Ex.sms" )
     TrackType ( "Switch" "DemoAutoSound/switchtrackin.sms" "DemoAutoSound/switchtrackex.sms"     )
     TrackType ( "Squeal Curve" "DemoAutoSound/curvesquealtrackin.sms" "DemoAutoSound/curvesquealtrackex.sms"   )
     TrackType ( "Squeal Switch" "DemoAutoSound/curveswitchtrackin.sms" "DemoAutoSound/curveswitchtrackex.sms"   )

.. index::
   single: ORTSSwitchSMSNumber
   single: ORTSCurveSMSNumber
   single: ORTSCurveSwitchSMSNumber
   single: ORTSDefaultTurntableSMS

3. For every route you must tell OR which of the ttype sound files are those related to 
   automatic sounds. This is done by inserting following line in the route's ``.trk`` file::
     
     ORTSSwitchSMSNumber ( 10 )
     ORTSCurveSMSNumber ( 11 )       
     ORTSCurveSwitchSMSNumber ( 12 ) 

   A better solution, because it leaves the ``.trk`` file unaltered, is to create an 
   ``OpenRails`` subfolder within the route's folder, and to put in it an integration 
   ``.trk`` file, named like the base one, and with following sample content (supposing 
   the base .trk file is named ``ITALIA13.trk``::


       -> BLANK LINE HERE <- 
       include ( "../ITALIA13.trk" )
          ORTSDefaultTurntableSMS ( turntable.sms )
          ORTSSwitchSMSNumber ( 10 )
          ORTSCurveSMSNumber ( 11 )       
          ORTSCurveSwitchSMSNumber ( 12 )  

Note that a blank line must be present above the ``include`` line, but that is difficult to reproduce in this manual.

Note also that with the same integration ``.trk`` file also the default turntable sound 
is defined, in case this route has turntables or transfertables.                  
 
As already stated, you can also define in ``ttype.dat`` and in the ``.trk`` file only 
one or only two types of automatic sounds.

.. _sound-external:   

Override % of external sound heard internally for a specific trainset
=====================================================================

External sounds are reproduced at a lower volume when heard within a cab or 
passenger view. The % of external sound heard internally is defined in the 
``Audio Options`` menu window.

.. index::
   single: ORTSExternalSoundPassedThroughPercent

This percentage may be overridden for any trainset inserting in the Wagon 
section of any .eng or .wag file (or in their "include" file as explained 
:ref:`here <physics-inclusions>`) following line::

  ORTSExternalSoundPassedThroughPercent ( 50 ) 

where the number in parenthesis may be anyone from 0 (nothing heard internally) 
to 100 (external sound reproduced at original volume).  
