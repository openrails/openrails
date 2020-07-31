.. _developing:

*********************
Developing OR Content
*********************

At this time, developing content for Open Rails in large part means developing
content for Microsoft Train Simulator. As Open Rails continues to define its own
native data formats, it is expected that more unique features and editors will
become available in the future.

Many MSTS files have already been extended with OR-specific parameters, as
symbolized by their special ``ORTS`` prefix. Some native formats have also been
introduced within the MSTS content directory structure.

New Open Rails Content Formats
==============================

DDS Textures
------------

Open Rails introduces DDS texture support in addition to ACE, the sole format
that was supported by Microsoft Train Simulator. DDS is an industry-standard
format and it is more efficient for the GPU to work with.

Timetables
----------

Timetable files are :ref:`located <timetable-file-structure>` within a special
subfolder of the route's activities directory. The Timetable Editor tool offers
an easier way to construct .timetable-or files.

Trains
------

The Open Rails replacement for MSTS' "consist," a "train" is a sequence of
wagons and engines that can be spawned to represent a player or an AI. Unlike
consist files, which contained fixed lists of wagons and engines, Open Rails
train files may also:

- Refer to other train files.
- Select probabilistically from a set of mutually exclusive alternatives.

Train files use the .train-or extension and they are stored in JSON format. Like
consist files, train files are located in the TRAINS\CONSISTS directory; if a
consist file and train file both share the same filename, the train file takes
priority.

Each train file must declare a single type, along with a list of child wagons,
engines, trains, or consists. The following types are available:

- "List" trains combine their children items in linear order.
- "Random" trains randomly select one of their children items, with
  customizable probability weights.

Legacy Content Formats
======================

Thanks to the high degree of compatibility that Open Rails has with Microsoft
Train Simulator, it is of course entirely possible to develop assets, rolling
stock, routes, and activities using the tools used to develop MSTS content.

Open Rails' Track Viewer tool includes an MSTS-compatible path editor.

Separately from the Open Rails project, the TSRE ("Train Sim Route Explorer")
`project <http://koniec.org/tsre5/>`_ has developed a suite of MSTS and
OR-compatible consist, route, and activity editors.

Advantages of Building for Open Rails
=====================================

Rolling Stock
-------------

- OR is able to display shapes with many more polygons than MSTS. Shapes with 
  more than 100.000 polys have been developed and displayed without problems.
- Thanks to the additional physics description parameters, a much more 
  realistic behavior of the rolling stock is achieved.
- 3D cabs add realism.
- OR graphics renders the results of the rolling stock developers at higher 
  resolution.
- Rolling stock running on super-elevated track improves gaming experience.

Routes
------

- Routes are displayed in higher resolution.
- Extended viewing distance yields much more realism.
- :ref:`Double overhead wire<features-route-overhead-wire-double-wire>` increases the realism of electrified routes.
- Built-in :ref:`triphase overhead electric line<features-route-overhead-wire-triphase>`.
- Extended signaling features provide more realistic signal behavior.
- :ref:`Widescreen<features-route-loading-screen>` and hi-res loading screen.

Activities
----------

- :ref:`Timetable mode <timetable>` is a new activity type available only in 
  Open Rails that allows for development of timetable based gaming sessions.
- By using the dispatcher monitor window, the dispatcher HUD, and the ability 
  to switch the camera to any AI train, the player can more closely monitor 
  and control the execution of conventional activities.
- :ref:`Extended AI shunting <operation-ai-shunting>` greatly increases the 
  interactions between trains.
- New :ref:`OR-specific additions <operation-activity>` to activity (.act) 
  files enhance activities.

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
