.. _features-route:

**************************
OR-Specific Route Features
**************************

As a general rule and as already stated, Open Rails provides all route 
functionalities that were already available for MSTS, plus some opportunities 
such as also accepting textures in .dds format.

Repetition of Snow Terrain Textures
===================================

OR provides a simple way to add snow terrain textures: the following default 
snow texture names are recognized: ``ORTSDefaultSnow.ace`` and 
``ORTSDefaultDMSnow.ace``, to be positioned within folder ``TERRTEX\SNOW`` of 
the concerned route. For the snow textures that are missing in the ``SNOW`` 
subfolder, and only for them, ORTS uses such files to display snow, if they 
are present, instead of using file ``blank.bmp``.

To have a minimum working snow texture set, the file ``microtex.ace`` must 
also be present in the ``SNOW`` subfolder.

Operating Turntables
====================

A cool feature available in OR is the one of operating turntables. In MSTS they are 
static, and can't rotate trainsets.
The best way to get a turntable to be operational is to refer to an example.
So here are the instructions and the files to test this function, both for route 
Catania-Messina (SICILIA 1) and for other routes using ``a1t27mturntable.s``.
Route Catania-Messina can be downloaded from 
`here <http://www.trainsimhobby.net/infusions/pro_download_panel/download.php?did=544>`_ . 
A .ws file within the World subdirectory must be replaced with file 
``w-005631+014158.zip``
available in the Open Rails pack in the Documentation\SampleFiles\Manual subfolder. 
(this has nothing to do with turntables, it's a file that contains incoherent data that 
can cause a crash).
Pls. note that also the other sample files cited in this paragraph are available in such subfolder. 

Two test paths, included in file ``Turntable_PATHS.zip``, one for each turntable in the route, which can be used either 
in explore mode or within activities are available in the Open Rails pack.
Within the route's folder an OpenRails subfolder must be created, that must contain 
2 files. The first one is following file ``turntables.dat``, which contains the data needed 
to OR to locate and specify the turntable.

turntables.dat::

  2
  Turntable(
  WFile ( "w-005625+014198.w" )
  UiD ( 1280 )
  XOffset ( 0 )
  YOffset ( -1.92177 )
  ZOffset ( 13.4 )
  TrackShapeIndex ( 253 )
  Animation ( "TRACKPIECE" )
  Diameter ( 27 )
  )
  Turntable(
  WFile ( "w-005631+014158.w" )
  UiD ( 638 )
  XOffset ( 0 )
  YOffset ( -1.92177 )
  ZOffset ( 13.4 )
  TrackShapeIndex ( 253 )
  Animation ( "TRACKPIECE" )
  Diameter ( 27 )
  )
 
To generate this file for other routes following has to be taken into account:

- the first line must be blank
- the number in the second line (2 in the above file) is the number of operating 
  turntables within the route
- WFile is the name of the .w file where the platform is present
- The number in the UiD line is the UiD number of the TrackObj () block within the .w      file related to the turntable
- XOffset, YOffset abd ZOffset are the offsets of the center of rotation of the 
  turntable with respect to the zero of the turntable shape 
- TrackShapeIndex is the index of the TrackShape () block within tsection.dat that
  refers to the turntable; pls. note that if a new TrackShape () block for the 
  turntable is needed, it is not necessary to modify tsection.dat; it is possible to 
  proceed as described here
- The Animation parameter is the name of the Matrix of the rotating part within the .s     file
- the Diameter value is the diameter of the turntable in meters.

The above file refers to turntables using the a1t27mturntable.s shape.

The second file to be inserted within the route's Openrails subfolder is a small 
integration .trk file that indicates the name of the .sms sound file to be associated to the turntable. For the route SICILIA 1 such file is therefore named ``SICILIA 1.trk``, like 
its parent file. Here is the file contents.

SICILIA 1.trk::


  include ( "../Sicilia 1.trk" )
			ORTSDefaultTurntableSMS ( turntable.sms )

The first line must be blank. 

File ``a1t27mturntable.s`` must be modified to add the animation data, as MSTS has provided 
it as a static file. To do this, uncompress it with Route Riter or Shapefilemanager and insert just above the last parenthesis the contents of file ``a1t27mturntable_animations.zip``.
If other .s files have to be used for turntables, or new ones have to be developed, it must be considered that the rotation animation should be as follows::

		animation ( 3599 30
			anim_nodes ( ..
				..
				..
				..
				anim_node TRACKPIECE (
					controllers ( ..
						tcb_rot ( 3
							tcb_key ( 0 0 0 0 1 0 0 0 0 0 )
							tcb_key ( 1800 0 1 0 0.0 0 0 0 0 0 )
							tcb_key ( 3600 0 0 0 -1 0 0 0 0 0 )
						)

or as follows::

		animation ( 3599 30
			anim_nodes ( ..
				..
				..
				..
                anim_node WHEEL1 (
                    controllers ( 1
                       tcb_rot ( 5
                          slerp_rot ( 0 0 0 0 1 )
                          slerp_rot ( 900 0 0.7071068 0 0.7071067 )
                          slerp_rot ( 1800 0 1 0 -1.629207E-07 )
                          slerp_rot ( 2700 0 -0.7071066 0 0.7071069 )
                          slerp_rot ( 3600 0 0 0 1 )
                        )
                     )
                 )

The above names of the anim_nodes are of course free choice.
The animation rotation direction as defined above must be counterclockwise.

Within the base Sound folder (not the one of the route) the .sms file 
``turntablesSOUND.zip`` has to be added to provide sound when the turntable rotates. It uses the two default MSTS .wav files for the sound. They have a bit a low volume. It is open to everyone to improve such files. Discrete trigger 1 is triggered when the turntable starts turning empty, discrete trigger 2 is triggered when the turntable starts turning with train on board, and discrete trigger 3 is triggered when rotation stops.

Already many existing turntables have been successfully animated and many new other
have been created. More can be read `in this forum thread <http://www.elvastower.com/forums/index.php?/topic/28591-operational-turntable/>`_ .

.. _features-route-turntable-operation:

Path laying and operation considerations
----------------------------------------

By building up a path that enters the turntable, exits it from the opposite side and has 
a reversal point few meters after the end of the turntable, it is possible to use the 
turntable in activity mode. The player will drive the consist into the turntable and 
stop it. At that point the reversal point will have effect and will logically lay the 
consist in the return subpath. The player will put the consist in manual mode, rotate 
the platform by 180 degrees and return to auto mode. At this point the consist will be 
again on the activity path.
If instead the player wants the consist to exit to other tracks, he must drive the 
consist in manual mode out of the platform. If he later wants to drive back the consist 
into the turntable and rotate the train so that it exits the turntable on the track 
where it initially entered the platform, he can pass back the train to auto mode after 
rotation, provided the path is built as defined above.
By using the feature to change :ref:`player train <driving-trainlist>` it is possible 
also to move in and out any locomotive on any track of e.g. a roundhouse. 
 
.. _features-route-modify-wfiles:

.w File modifiers
=================

An ``Openrails`` subfolder can be created within the route's ``World`` folder.
Within this subfolder .w file chunks can be positioned. ORTS will first read the base 
.w files, and then will correct such files with the file chunks of the ``Openrails`` 
subfolder.
This can be used both to modify parameters or to add OR-specific parameters.
Here an example of a w. file chunk for USA1 .w file w-011008+014318.w::

  SIMISA@@@@@@@@@@JINX0w0t______

  Tr_Worldfile (
		CarSpawner (
			UiD ( 532 )
			ORTSListName ( "List2" )
		)
		CarSpawner (
			UiD ( 533 )
			ORTSListName ( "List3" )
		)
		Static (
			UiD ( 296 )
			FileName ( hut3.s )
	  )
  )

With the two CarSpawner block chunks OR interprets the CarSpawners with same UiD 
present in the base .w file as extended ones 
(see :ref:`here <features-route-extended-carspawners>`). With the Static block OR 
replaces the shape defined in the Static block with same UiD within the base .w file 
with the one defined in the file chunk.
WAny Pickup, Transfer, Forest, Signal, Speedpost, LevelCrossing, Hazard, CarSpawner, 
Static, Gantry may have parameters modified or added by the "modifying" .w file. 

.. caution:: If the route is edited with a route editor, UiDs could change and so the .w file chunks could be out of date and should be modified.

.. caution:: Entering wrong data in the .w file chunks may lead to program malfunctions.

.. _features-route-extended-carspawners:

Multiple car spawner lists
==========================

With this OR-specific feature it is possible to associate any car spawner to one of 
additional car lists, therefore allowing e.g. to have different vehicles appearing in 
a highway and in a small country road.

The additional car lists have to be defined within a file named carspawn.dat to be inserted in an ``Openrails`` subfolder within the Route's root folder.
Such file must have the structure as in following example::

  SIMISA@@@@@@@@@@JINX0v1t______

  3
  CarSpawnerList(
  ListName ( "List1" )
  2
  CarSpawnerItem( "car1.s" 4 )
  CarSpawnerItem( "postbus.s" 4 )
  )
  CarSpawnerList(
  ListName ( "List2" )
  3
  CarSpawnerItem( "policePHIL.S" 6 )
  CarSpawnerItem( "truck1.s" 13 )
  CarSpawnerItem( "postbus.s" 6 )
  )
  CarSpawnerList(
  ListName ( "List3" )
  2
  CarSpawnerItem( "US2Pickup.s" 6 )
  CarSpawnerItem( "postbus.s" 13 )
  )

The first ``3`` defines the number of the additional car spawner lists.
To associate a CarSpawner block to one of these lists, a line like this one::

			ORTSListName ( "List2" )

has to be inserted in the CarSpawn block, in any position after the UiD line.

If the CarSpawner block does not contain such additional line, it will be associated 
with the base carspawn.dat file present in the route's root directory.

.. caution:: If the route is edited with the MSTS route editor modifying the .w files referring to the additional car spawners, the above line will be deleted.

To avoid this problem, two other possibilities are available to insert the additional 
line. One is described :ref:`here <features-route-modify-wfiles>`.
The other one is to use the OR specific TSRE route editor, that natively manages this 
feature. Also in the latter case, however, if the route is later edited with the MSTS 
route editor, the above line will be deleted.

.. _features-route-tracksections:

Route specific TrackSections and TrackShapes
============================================
It quite often occurs that for special routes also special TrackSections and TrackShapes 
are needed. Being file tsection.dat unique for every installation, for such routes a 
so-called mini-route installation was needed.
The present feature overcomes this problem. The route still uses the common tsection.dat,but it can add to it route-specific TrackSections and TrackShapes, and can modify common ones. This occurs by putting in an ``OpenRails`` subfolder within the route's root 
folder a route-specific chunk of tsection.dat, which includes the TrackSections and 
TrackShapes to be added or modified. Here a fictitious example for route USA1 (first 
line must be blank)::


  include ( "../../../Global/tsection.dat" )
  _INFO ( Track sections and shapes specific for USA1   )
  _Skip (
  Further comments here
  )
  TrackSections ( 40000
  _Skip (
  Comment here
  )
  _SKIP ( Bernina )
    TrackSection ( 33080
	    SectionSize ( 0.9 1.5825815 )
    )
    TrackSection ( 19950
	    SectionSize ( 0.9 12 )
    )
  )
  TrackShapes ( 40000
  _Skip (
  Comment here
  )
  -INFO(Bernina Pass narrow gauge sections / wood tie texture)
  _INFO(by Massimo Calvi)
  _INFO(straight sections)
    TrackShape ( 30000
	    FileName ( track1_6m_wt.s )
	    NumPaths ( 1 )
	    SectionIdx ( 1 0 0 0 0 33080 )
    )
    TrackShape ( 19858
	    FileName ( track12m_wt.s )
	    NumPaths ( 1 )
	    SectionIdx ( 1 0 0 0 0 19950 )
    )
  )

In this fictitious example the first TrackSection and TrackShape is present also in the 
Global tsection.dat, so the effect is that the original TrackSection and TrackShape are 
modified; the second ones are not present, and so they are added to the lists.   

.. note::  to be able to use these modified items with the actual MSTS RE or with Or's TSRE5 route editor it is necessary that these modified items are present also in the original tsection.dat file. However, when the work with the RE is terminated and route is distributed, it is sufficient to distribute the above route's specific tsection.dat.

.. _features-route-overhead-wire-extensions:

Overhead wire extensions
===================================

.. _features-route-overhead-wire-double-wire:

Double wire
-----------

OR provides an :ref:`experimental function that enables the upper wire <options-double-overhead-wires>` for 
electrified routes. The optional parameter ``ortsdoublewireenabled`` in the ``.trk`` file of the route can
force the activation or disactivation of the option overriding the user setting in the options panel.

In this example the upper wire is enabled overriding the user setting::

  OrtsDoubleWireEnabled ( On )

while in this one the upper wire is forced to be disabled::

  OrtsDoubleWireEnabled ( Off )

Another parameter (``ortsdoublewireheight``) specifies the height of the upper wire relative to the contact wire,
if not specified the default is 1 meter.
In this example the upper wire is 130cm over the main wire (as in most italian routes)::


  include ( "../tures.trk" )
    OrtsTriphaseEnabled ( Off )
    OrtsDoubleWireEnabled ( On )
    OrtsDoubleWireHeight ( 130cm )

Of course you can use any :ref:`distance unit of measure <appendices-units-of-measure>` supported by OR.

.. _features-route-overhead-wire-triphase:

Triphase lines
--------------

The modern electric locos are powered by DC or monophase AC, but some years ago there were triphase AC powered locos.
A triphase circuit needs tre wires (one for each phase, no wire is needed for neutral); in rails systems two wires 
are overhead and the third is made by the rails.

OR can enable the second overhead wire with the parameter ``ortstriphaseenabled`` this way::

  OrtsTriphaseEnabled ( On )

If the parameter is missing or its value is ``Off`` the usual single wire is displayed.

Another parameter (``ortstriphasewidth``) specifies the space between the two wires with a default (if the parameter 
is not declared) of 1 meter.

.. _features-route-loading-screen:

Loading screen
==============

In the ``.trk`` file of the route can be used the parameter ``loadingscreen`` as in this example::

	LoadingScreen ( Load.ace )

If in the main directory of the route there is a file with the same name but with extension ``.dds`` 
and the :ref:`DDS texture support<options-dds-textures>` is enabled
the last one is displayed instead of that with ``.ace`` extension.
If the parameter is omitted then is loaded the file ``load.ace`` (like in MSTS) or ``load.dds`` 
(if present and, again, the dds support is enabled).

The loading screen image can have any resolution and aspect-ratio, it will be displayed letter-boxed
in the screen keeping the aspect-ratio.

Another optional parameter ``ortsloadingscreenwide``, can specify the image to show when the user
loads the route on a wide (16:9) screen. This parameter is ignored when a traditional 4:3 display is used.






  







