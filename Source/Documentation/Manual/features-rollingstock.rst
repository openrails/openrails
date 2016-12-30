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
  the main trainset shape
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
  FuelCoal, FuelDiesel. All these types of loads can be defined also for a pickup 
  with the MSTS Route editor.
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
that starts with ``ANIMATED_PARTS``. There may be more than one, like 
``ANIMATED_PARTS1``, ``ANIMATED_PARTS2`` and so on. Its frame rate is fixed, 
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

Such a pickup (which in reality is an unloader) may be animated too. As for the 
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

Because more static OR freightanims may be defined for a wagon, in the case of a 
container wagon that is able to carry more than one container, even as a double 
stack, it is therefore possible to use a static OR freightanim for each 
container, defining its position within the wagon. 

