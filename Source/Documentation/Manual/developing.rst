.. _developing:

*********************
Developing OR Content
*********************

Open Rails already has some own development tools and is defining and developing other ones. A path editor is available within TrackViewer under the *Tools* button in the main menu window. An editor for timetable mode is also available under the *Tools* button. Route editor and consist editor are in an advanced stage of development and may already be tested. You can read about and download the consist editor `here <http://www.elvastower.com/forums/index.php?/topic/28623-new-consist-editor-for-open-rails/>`_ .
You can read about and download the TSRE5 route editor `at this link <http://www.elvastower.com/forums/index.php?/topic/26669-new-route-editor-for-open-rails/>`_

IT is of course already possible to develop OR content (rolling stock, routes, 3D 
objects, activities) using the tools used to develop MSTS content, thanks to 
the high compatibility that OR has with MSTS. Below, some of the advantages of 
OR-specific content are described.

Rolling Stock
=============

- OR is able to display shapes with many more polygons than MSTS. Shapes with 
  more than 100.000 polys have been developed and displayed without problems.
- Thanks to the additional physics description parameters, a much more 
  realistic behavior of the rolling stock is achieved.
- 3D cabs add realism.
- OR graphics renders the results of the rolling stock developers at higher 
  resolution.
- Rolling stock running on super-elevated track improves gaming experience.

Routes
======

- Routes are displayed in higher resolution.
- Extended viewing distance yields much more realism.
- :ref:`Double overhead wire<features-route-overhead-wire-double-wire>` increases the realism of electrified routes.
- Built-in :ref:`triphase overhead electric line<features-route-overhead-wire-triphase>`.
- Extended signaling features provide more realistic signal behavior.
- :ref:`Widescreen<features-route-loading-screen>` and hi-res loading screen.

Activities
==========

- :ref:`Timetable mode <timetable>` is a new activity type available only in 
  Open Rails that allows for development of timetable based gaming sessions.
- By using the dispatcher monitor window, the dispatcher HUD, and the ability 
  to switch the camera to any AI train, the player can more closely monitor 
  and control the execution of conventional activities.
- :ref:`Extended AI shunting <operation-ai-shunting>` greatly increases the 
  interactions between trains.
- New :ref:`OR-specific additions <operation-activity>` to activity (.act) 
  files enhance activities.

.. _parameters_and_tokens:

Parameters and Tokens
=====================
The parameters used in content files have been mentioned throughout this manual for:

+------------------------------+-----------------------------+
| Content Type                 |        File Extension       |
+==============================+=============================+
| locomotive                   |        eng                  |
+------------------------------+-----------------------------+
| wagon or non-powered vehicle |        wag                  |
+------------------------------+-----------------------------+
| activity                     |        act                  |
+------------------------------+-----------------------------+
| cab view                     |        cvf                  |
+------------------------------+-----------------------------+
| consist                      |        con                  |
+------------------------------+-----------------------------+
| train service                |        srv                  |
+------------------------------+-----------------------------+
| train traffic                |        trf                  |
+------------------------------+-----------------------------+
| signal configuration         |        sigcfg.dat           |
+------------------------------+-----------------------------+
| signal scripts               |        sigscr.dat           |
+------------------------------+-----------------------------+
| sound management             |        sms                  |
+------------------------------+-----------------------------+
| train timetable              |        timetable-or         |
+------------------------------+-----------------------------+

The complete list is very extensive and is documented in an online spreadsheet at `tinyurl.com/or-parameters-excel
<https://tinyurl.com/or-parameters-excel>`_.

Since this is a spreadsheet with many rows, you can restrict your view to relevant rows using the filters at the top of each column.

Testing and Debugging Tools
===========================

As listed :ref:`here <driving-analysis>`, a rich and powerful set of analysis 
tools eases the testing and debugging of content under development.

Open Rails Best Practices
=========================

Polys vs. Draw Calls -- What's Important
----------------------------------------

Poly counts are still important in Open Rails software, but with newer video 
cards they're much less important than in the early days of MSTS. What does 
remain important to both environments are Draw Calls.

A Draw Call occurs when the CPU sends a block of data to the Video Card. Each 
model in view, plus terrain, will evoke one or more Draw Calls per frame 
(i.e., a frame rate of 60/second means all of the draw calls needed to 
display a scene are repeated 60 times a second). Given the large number of 
models displayed in any scene and a reasonable frame rate, the total number 
of Draw Calls per second creates a very large demand on the CPU. Open Rails 
software will adjust the frame rate according to the number of required Draw 
Calls. For example, if your CPU can handle 60,000 Draw Calls per second and 
the scene in view requires 1000 Draw Calls, your frame rate per second will 
be 60. For the same CPU, if the scene requires 2000 Draw Calls, your frame 
rate per second will be 30. Newer design / faster CPU's can do more Draw 
Calls per second than older design / slower CPU's.

Generally speaking, each Draw Call sends one or more polygon meshes for each 
occurrence of a texture file for a model (and usually more when there are 
multiple material types). What this means in practice is if you have a model 
that uses two texture files and there are three instances of that model in 
view there will be six draw calls -- once for each of the models (3 in view) 
times once for each texture file (2 files used), results in six Draw Calls. 
As an aid to performance Open Rails will examine a scene and will issue Draw 
Calls for only the models that are visible. As you rotate the camera, other 
models will come into view and some that were in view will leave the scene, 
resulting in a variable number of Draw Calls, all of which will affect the 
frame rate.

Model builders are advised that the best performance will result by not 
mixing different material types in a texture file as well as using the fewest 
number of texture files as is practical.

Support
=======

Support can be requested on the OR forum on `<http://www.elvastower.com/forums>`_.

The OR development team, within the limits of its possibilities, is willing 
to support contents developers.
