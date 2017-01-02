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

Variable Triggers
-----------------

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
