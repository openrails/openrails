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
Catania-Messina (SICILIA 1) and for other routes using a1t27mturntable.s.
Route Catania-Messina can be downloaded from 
`here <http://www.trainsimhobby.net/infusions/pro_download_panel/download.php?did=544>`_ . 
A .ws file within the World subdirectory must be replaced with file w-005631+014158.zip
downloadable from the trainsim forum post http://www.trainsim.com/vbts/showthread.php?324957-!!-Working-Turntable-!!&p=1896324#post1896324 
(this has nothing to do with turntables, it's a file that contains incoherent data that 
can cause a crash).

Two test paths, included in file Turntable_PATHS.zip one for each turntable in the route, which can be used either 
in explore mode or within activities can be downloaded from the same forum post.
Within the route's folder an OpenRails subfolder must be created, that must contain 
2 files. The first one is following file turntables.dat, which contains the data needed 
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
integration .trk file that indicates the name of the .sms sound file to be associated to the turntable. For the route SICILIA 1 such file is therefore named SICILIA 1.trk, like 
its parent file. Here is the file contents.

SICILIA 1.trk::


  include ( "../Sicilia 1.trk" )
			ORTSDefaultTurntableSMS ( turntable.sms )

The first line must be blank. 

File a1t27mturntable.s must be modified to add the animation data, as MSTS has provided 
it as a static file. To do this, uncompress it with Route Riter or Shapefilemanager and insert just above the last parenthesis the contents of file a1t27mturntable_animations.zip which can be downloaded from the above trainsim forum 
post.
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

Within the base Sound folder (not the one of the route) a .sms file has to be added to provide sound when the turntable rotates. From the above trainsim forum link file 
turntablesSOUND.zip has to be downloaded. It uses the two default MSTS .wav files for the sound. They have a bit a low volume. It is open to everyone to improve such files. Discrete trigger 1 is triggered when the turntable starts turning empty, discrete trigger 2 is triggered when the turntable starts turning with train on board, and discrete trigger 3 is triggered when rotation stops.

Already many existing turntables have been successfully animated and many new other
have been created. More can be read `here <http://www.elvastower.com/forums/index.php?/topic/28591-operational-turntable/>`_ .
 



