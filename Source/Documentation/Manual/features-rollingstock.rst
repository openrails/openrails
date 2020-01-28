.. _features:

*****************************************
Further Open Rails Rolling Stock Features
*****************************************

Train Engine Lights
===================

OR supports the whole set of lights accepted by MSTS.

Tilting trains
==============

OR supports tilting trains. A train tilts when its .con file name contains the 
*tilted* string: e.g. ``ETR460_tilted.con``.

.. image:: images/features-tilting.png

Freight animations and pickups
==============================

OR implementation of MSTS freight animations and pickups
--------------------------------------------------------

OR supports the freight animations as MSTS does (refueling of water, coal and 
diesel); when refueling from a water column the animation of the column arm is 
supported; coal level in the tender of the player loco decreases with 
consumption and increases when refueling.

The following pickup parameters are taken into consideration by OR for the MSTS 
animations:

- Pickup type
- Speed range
- Anim length

The pickup animation frame rate is computed as the ratio between the number of 
frames defined in the .s file, divided by the Anim length.

As in MSTS, Freight Animations are treated differently for tenders than for 
other vehicles.

Tenders:

- First numeric parameter: shape vertical position when full, relative to its 
  origin, in meters
- Second numeric parameter: shape vertical position when empty, relative to its 
  origin, in meters.
- Third numeric parameter: set to any positive value, or omitted, causes the 
  shape to drop - see below.

  - As long as the second parameter is lower than the first and the third parameter is either omitted or has a non-zero value, the shape will drop, based  on fuel consumption.
  - If the second parameter is not lower than the first, no movement will take place irrespective of the 3rd parameter.

Other Vehicles:

- The numeric parameters are not used.

OR specific freight animations and pickups
------------------------------------------

General
'''''''

In addition to the support of the MSTS freight animations, Open Rails provides a 
large extension for freight animations (called *OR freightanims* below) and 
pickups.

Following are the native features Open Rails offers:

- two types of OR freightanims: continuous and static
- continuous OR freightanims are related to commodity loads, like coal, or 
  stones: the load level in the trainset varies accordingly to the amount of load
- static OR freightanims are in fact additional shapes that can be attached to 
  the main trainset shape. Such shapes may also include an animation (independent 
  from train behaviour);
- both types of OR freightanims can be present in the same trainset, and can 
  coexist with original MSTS freight animations
- both types of OR freightanims can be related to locomotives or wagons
- more than one static OR freightanim can be present in a single trainset
- a wagon can be loaded with different commodities in different moments
- commodities can be loaded (in pickup stations) and unloaded (in unloading 
  stations).
- wagons supporting continuous OR freightanims may be provided with a physical 
  animation that is triggered when unloading the wagon (like opening its bottom or 
  fully rotating)
- OR freightanims are defined with an ``ORTSFreightAnims ()`` block within the .wag 
  or within the wagon section of an .eng file. It is suggested that this block 
  be defined within an include file as described :ref:`here <physics-inclusions>`.

Continuous OR Freightanims
''''''''''''''''''''''''''

A description of this feature is best achieved by showing an example of an 
include file, (in this case named ``AECX1636.wag`` and located in an Openrails 
subfolder within the wagon's folder). Note that the first line of the file must 
be blank.:: 

    include ( ../AECX1636.wag )

    Wagon (
        ORTSFreightAnims
        (
            MSTSFreightAnimEnabled (0)
            WagonEmptyWeight(22t)
            IsGondola(1)
            UnloadingStartDelay (7)
            FreightAnimContinuous
            (
                IntakePoint ( 0.0 6.0 FreightCoal )
                Shape(Coal.s)
                MaxHeight(0.3)
                MinHeight(-2.0)
                FreightWeightWhenFull(99t)
                FullAtStart(0)
            )
            FreightAnimContinuous
            (
                IntakePoint ( 0.0 6.0 FuelCoal )
                Shape(Coal.s)
                MaxHeight(0.3)
                MinHeight(-2.0)
                FreightWeightWhenFull(99t)
                FullAtStart(0)
            )
        )
    )

The ``ORTSFreightAnims`` block is composed by a set of general parameters 
followed by the description of the OR freightanims. Here below the general 
parameters are described:

- ``MSTSFreightAnimEnabled`` specifies if eventual MSTS freight animations within 
  the trainset are enabled (1) or not (0). This is useful if one wants to use a 
  wagon where the load is already shown with a (static) MSTS freight animation. In 
  such a case the MSTS freight animation must be disabled, to use the OR 
  freightanim, that allows to modify the vertical position of the freight shape. 
- ``WagonEmptyWeight`` defines the mass of the wagon when empty. If the parameter 
  is missing, the weight of the load is not considered and the weight of the 
  wagon is always the value present in the root .eng file.
- ``IsGondola`` specifies (in case it is set to 1) if the load has to be rotated 
  during unloading, as happens in a gondola wagon. If absent the parameter is set 
  to 0.
- ``UnloadingStartDelay`` specifies, if present, after how many seconds after 
  pressing of the T key the unloading starts. This is due to the fact that some 
  seconds may be needed before the wagon is set in a unloading layout. For 
  example, a gondola must rotate more than a certain number of degrees before the 
  load begins to fall down.

There may be more than one ``FreightAnimContinuous`` subblock, one for each 
possible load type. The parameters of the subblock are described below:

- ``IntakePoint`` has the same format and the same meaning of the IntakePoint 
  line within the standard MSTS freight animations. Following types of loads are 
  accepted: FreightGrain, FreightCoal, FreightGravel, FreightSand, FuelWater, 
  FuelCoal, FuelDiesel, FuelWood, FuelSand, FreightGeneral, FreightLivestock, 
  FreightFuel, FreightMilk, SpecialMail. All these types of loads can be defined. 
  Some of the pickup types (to right of FuelDiesel) need to be coded in W text files. 
- ``Shape`` defines the path of the shape to be displayed for the load
- ``MaxHeight`` defines the height of the shape over its 0 position at full load
- ``MinHeight`` defines the height of the shape over its 0 position at zero load
- ``FreightWeightWhenFull`` defines the mass of the freight when the wagon is full; 
  the mass of the wagon is computed by adding the mass of the empty wagon to the 
  actual mass of the freight 
- ``FullAtStart`` defines wether the wagon is fully loaded (1) or is empty at game 
  start; if there are more continuous OR freightanims that have ``FullAtStart`` 
  set to 1, only the first one is considered.

As already outlined, the wagon may have a physical animation linked with the 
unload operation.

In a gondola this could be used to rotate the whole wagon, while in a hopper it 
could be used to open the bottom of the wagon.

The base matrix within the wagon shape that has to be animated must have a name 
that starts with ``UNLOADINGPARTS``. There may be more than one, like 
``UNLOADINGPARTS1``, ``UNLOADINGPARTS2`` and so on. Its frame rate is fixed, 
and is 1 frame per second as for the other types of OR trainset animations.

To define a pickup point as an unload point, its shape must be inserted in the 
.ref file of the route as a pickup object . Here is an example of the .ref block::

    Pickup (
        FileName ( rotary_dump.s )
        Shadow ( DYNAMIC )
        Class ( "Track Objects" )
        PickupType ( _FUEL_COAL_ )
        Description ( "Rotary dumper" )
    )

When laying it down in the route with the MSTS Route Editor, its fill rate must 
be set to a negative value.

Such a pickup (which in reality is an unloader) may be animated too. The base matrix 
within the wagon shape that has to be animated must have a name 
that starts with ``ANIMATED_PARTS``. There may be more than one, like 
``ANIMATED_PARTS1``, ``ANIMATED_PARTS2`` and so on. As for the 
MSTS standard pickups, the pickup animation frame rate is computed as the ratio 
between the number of frames defined in the .s file, divided by the Anim length.

By combining a physical animation of the wagon with an unloader animation 
effects like that of a wagon within a rotary dumper may be achieved, as seen in 
the picture below.

.. image:: images/features-freightanim.png

Loading and unloading a trainset is triggered by pressing the ``<T>`` key when 
the trainset is at the pickup/unloader location.

Static OR Freightanims
''''''''''''''''''''''

Only the two general parameters shown below are used for static OR freightanims::

    MSTSFreightAnimEnabled (0)
    WagonEmptyWeight(22t)

The subblock (to be inserted within the ``ORTSFreightAnims`` block) has the 
following format::

    FreightAnimStatic
    (
        SubType(Default)
        Shape(xxshape.s)
        Offset(XOffset, YOffset, ZOffset)
        FreightWeight(weight)
        Flip()
        Visibility ( "Outside,Cab2D,Cab3D" )
    )

Where:

- ``SubType`` is not currently used
- ``Shape`` is the path of the shape file.
- ``XOffset``, ``YOffset`` and ``ZOffset`` are the offsets of the shape with 
  respect to its zero position, and are useful to place the shape precisely. 
- ``FreightWeight`` is the weight of the specific load. This weight is added to 
  the ``WagonEmptyWeight`` value (if present) to provide the total weight of the 
  wagon. If more static OR freightanims are present, each of their weights is 
  added to define the total weight of the wagon.
- ``Flip()``, if present, flips the shape around its pivot point.
- ``Visibility``, if present, changes the default visibility of the static 
  freightanim. Default is visible only from outside cameras and from any inside 
  camera of locomotives different from the one hosting the static freightanim. 
  If substring ``Outside`` is present, the static freightanim is visible from outside 
  cameras and from any inside camera of locomotives different from the one 
  hosting the static freightanim; if ``Cab2D`` is present, the static freightanim 
  is visible from the 2D cabview camera of loco hosting the freightanim; 
  if ``Cab3D`` is present, the static freightanim is visible from the 3D cabview camera
  of loco hosting the freightanim. 1, 2 or 3 of such substrings 
  may be inserted in the ``Visibility`` line allowing for any combination of visibility.  

Because more static OR freightanims may be defined for a wagon, in the case of a 
container wagon that is able to carry more than one container, even as a double 
stack, it is possible to use a static OR freightanim for each 
container, defining its position within the wagon. 

Physics Variation with Loads
----------------------------

Variable Loads (Continuous Freight Animation)
'''''''''''''''''''''''''''''''''''''''''''''
Oepn Rails supports the variation fo key physics parameters in the wagon as the 
load varies within the wagon. The parameters which can be changed are:

- Mass
- Brake and handbrake force
- Friction (general and wind)
- Centre of Gravity (impacts on curve performance)
- Drive wheel weight (impacts upon locomotive adhesve weight)

Locomotives and tenders that are also configured will have their loads, and the 
above physics parameters adjusted as coal and water is used. The adhesive weight 
(Drive wheel weight) will also be adjusted as the load changes.

To support the correct operation of this feature a known physics starting and 
finishing point is required, ie the state of these parameters under empty conditions, 
and the state of these parameters when the wagon or locomotive is full.

To configure the stock correctly the following empty and full parameters need to be 
included in the ORTSFreightAnims file. Empty values are included in the first block, 
and full values are included in the second code block. A sample code block is shown 
below.::

    ORTSFreightAnims
    (
      MSTSFreightAnimEnabled (0)
      WagonEmptyWeight(10.0t-uk)
      EmptyMaxBrakeForce ( 29.892kN )
      EmptyMaxHandbrakeForce ( 9.964kN )
      EmptyORTSDavis_A ( 580.71 )
      EmptyORTSDavis_B ( 5.0148 )
      EmptyORTSDavis_C ( 0.694782 )
      EmptyORTSWagonFrontalArea ( 10.0m )
      EmptyORTSDavisDragConstant ( 0.0003 )
      EmptyCentreOfGravity_Y ( 1.41 )
      IsGondola(0)
      UnloadingStartDelay (5)
      
    FreightAnimContinuous
     (
      IntakePoint ( 0.0 6.0 FreightCoal )
      Shape(H_Coal.s) 
      MaxHeight(0.1)
      MinHeight(-0.85)
      FreightWeightWhenFull(26.0t-uk)
      FullAtStart( 0 )
      FullMaxBrakeForce ( 89.676kN )
      FullMaxHandbrakeForce ( 9.964kN )
      FullORTSDavis_A ( 748.61 )
      FullORTSDavis_B ( 18.0157 )
      FullORTSDavis_C ( 0.838530 )
      FullORTSWagonFrontalArea ( 15.0m )
      FullORTSDavisDragConstant ( 0.005 )
      FullCentreOfGravity_Y ( 1.8 ) 
     )
  )


Note for enclosed wagons, such as covered vans, the freight animation shape may not be required, 
and therefore the parameters Shape, MaxHeight, and MinHeight can be left out of the file.
 
The ``IntakePoint`` statement is necessary to ensure satisfactory operation of the feature.

Open Rails supports the following freight or fuel load types:

- FreightGrain = 1,
- FreightCoal = 2,
- FreightGravel = 3,
- FreightSand = 4,
- FuelWater = 5,
- FuelCoal = 6,
- FuelDiesel = 7,
- FuelWood = 8, 
- FuelSand = 9, 
- FreightGeneral = 10, 
- FreightLivestock = 11, 
- FreightFuel = 12,  
- FreightMilk = 13,   
- SpecialMail = 14  

The key word, e.g. ``FreightMilk``, is used to define the freight type in the ``IntakePoint`` statement, 
whilst the number is used to define the pickup point in the route (Replaces the first number 
in the ``PickupType ( 1 0 )`` statement).

For load variation in a locomotive, a similar configuration is used in regard to the full and empty 
parameters, but as the ``IntakePoint`` statement is normally included elsewhere in the ENG file 
or tender (or auxiliary tender) WAG file these statements can be left out of the freight 
animation section.

For example, the following code block would apply to a steam locomotive (note the absence of the 
``Intakepoint`` statement)::

  ORTSFreightAnims
  (
      WagonEmptyWeight(76.35t-uk)
      EmptyMaxBrakeForce ( 29.892kN )
      EmptyMaxHandbrakeForce ( 9.964kN )
      EmptyORTSDavis_A ( 580.71 )
      EmptyORTSDavis_B ( 5.0148 )
      EmptyORTSDavis_C ( 0.694782 )
      EmptyCentreOfGravity_Y ( 1.41 )
      
    FreightAnimContinuous
     (
      FreightWeightWhenFull(10.34t-uk)
      FullMaxBrakeForce ( 89.676kN )
      FullMaxHandbrakeForce ( 9.964kN )
      FullORTSDavis_A ( 748.61 )
      FullORTSDavis_B ( 18.0157 )
      FullORTSDavis_C ( 0.838530 )
      
      FullCentreOfGravity_Y ( 1.8 ) 
     )
  )
  
Notes:

- Intake points should be defined within the root WAG file
- Intake points, freight animations should not be defined within the INCLUDE file
- Empty weight of tender will be the full mass minus coal and water weight
- ``FreightWeightWhenFull`` will be the sum of the coal and water weight.
- Full physics values will be those values for the combined weight of the tender, water and coal.
- The parameters for wind resistance ( ORTSWagonFrontalArea and ORTSDavisDragConstant ) can be left 
  out if the area and drag does not change between the full and empty states.
   

Static wagons (Static Freight Animations)
'''''''''''''''''''''''''''''''''''''''''
Static wagons can be defined with a full and empty state, however only one freight animation should 
have full values assigned to it,as OR cannot then calculate the known full state.

A typical configuration code block will be as follows::

  ORTSFreightAnims
  (
    MSTSFreightAnimEnabled (0)
    WagonEmptyWeight(6.5t-uk)

  FreightAnimStatic
  (
    SubType(Default)
    Shape( 15ft_3p_HumpSheet2.s )
    Offset( 0, 0, 0)
    FreightWeight( 9.0t-uk )
    FullMaxBrakeForce ( 19.43kN ) 
    FullMaxHandbrakeForce ( 6.477kN )
    FullORTSDavis_A ( 358.37 )
    FullORTSDavis_B ( 7.7739 )
    FullORTSDavis_C ( 0.718740 )
    FullORTSWagonFrontalArea ( 15.0m )
    FullORTSDavisDragConstant ( 0.005 )    
    FullCentreOfGravity_Y ( 1.8 ) 
   )
  )
 
The empty values for the wagon will be read from the normal base WAG file paramers.

.. _features-passengerviewpoints:

Multiple passenger viewpoints
=============================

Additional passenger viewpoints may be added within a carriage that 
is provided with passenger viewpoint.

Such additional passenger viewpoints are defined within an include file 
with the format shown in 
following example for the legacy oebarcar.wag (located in the 380 folder) 
MSTS wagon::
     
  include ( ../oebarcar.wag )
  
  Wagon (
        ORTSAlternatePassengerViewPoints (
                ORTSAlternatePassengerViewPoint (
                        PassengerCabinHeadPos ( -0.0 2.85801 -6.091 )
                        RotationLimit ( 50 270 0 )
                        StartDirection ( 0 0 0 )      
                )  
                ORTSAlternatePassengerViewPoint (
                        PassengerCabinHeadPos ( -0.5 2.35801 -1.791 )
                        RotationLimit ( 50 270 0 )
                        StartDirection ( 0 0 0 )      
                ) 	
                ORTSAlternatePassengerViewPoint (
                        PassengerCabinHeadPos ( 0.9 2.35801 -1.791 )
                        RotationLimit ( 50 270 0 )
                        StartDirection ( -5 -90 0 )      
                ) 				
        )
  )

At runtime, when in passenger view, the player may pass from one viewpoint to 
the other by pressing Shift-5.

Bell animation
==============

Open Rails supports bell animation. The bell animation matrix must be named 
ORTSBELL within the engine's .s file. Its default frame rate is 8 frames per 
second. The default frame rate may be modified through the optional parameter 
ESD_ORTSBellAnimationFPS (n), to be inserted within the .sd file related to the 
.s file. n defines the animation FPS.
It is advisable that the related sound stream within the .sms file is synchronized 
with the visible animation. To accomplish this the .wav file should contain two 
bell strokes, which time interval is equal to the time interval of a bell swing 
from an oscillation end point to the opposite end point. As the first bell stroke 
should not start immediately, but when the bell is about at the maximum of the 
swing, the first stroke within the .wav file should be at the time distance equivalent 
to the oscillation from center point to an oscillation end point. The file should have 
one cue point at its beginning and one after the time interval of a complete bell swing 
forward and backward, and should have a final fadeoff for best result. 



  




