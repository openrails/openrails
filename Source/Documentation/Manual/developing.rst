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
- Rolling stock running on superelevated track improves gaming experience.

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
| world tile                   |        w                    |
+------------------------------+-----------------------------+
| 3D shape                     |        s, gltf, glb         |
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

3D shape files
==============

Additionally to the S file format used in MSTS, Openrails is able to read the
glTF format shape files. However there are some conceptual differences between
the two formats that the content developers need to be aware of when creating
such files. One of the important scpecification constraints must be noted, 
+Z is the forward direction in glTF models, as opposed to .s, where the
forward was -Z.

Textures
--------

The texture format can be png, jpg or dds. The 
`MSFT_texture_dds <https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Vendor/MSFT_texture_dds/README.md>`_
extension must be used for referencing a dds texture, it is not just a 
drop-in file replacement as for the .s files. For final game content try to 
avoid using png and jpg formats at least for the base, emissive and specular 
textures, because as sRGB types they can only be loaded in two passes, 
because the dotnet built-in loader is unable to declare sRGB surface format 
at load time, and the whole pixel data must be copied a second time. 
Png and jpg formats also lack the ability to store mipmaps.

The base, emissive and specular color textures rgb channels are in sRGB color 
space. The alpha channels and any other textures are in linear space.

Instead of the night texture set, the authors can use an emissive texture 
for night illumination, they will bloom, glow. The emissive texture display 
can be made to switch on-off automatically at day-night change if specified 
in the material:

.. code-block:: json

  "extras": { "OPENRAILS_material_day_night_switch": true },

(This setting also affects the IBL lights assigned to a material, see below.)

The max value of the emissive strength is 1.0 by the standard, but sometimes 
a bigger glow is needed for being distinctively visible at daytime. 
(LED panels, etc...) There is the 
`KHR_materials_emissive_strength <https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_emissive_strength/README.md>`_.
extension available for achieving any strength.

.. code-block:: json

  "extensions": { "KHR_materials_emissive_strength": { "emissiveStrength": 5.0 },

Seasonal textures (like “Snow”) are managed via the 
`KHR_materials_variants <https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_variants/README.md>`_
extension. A primitive can have multiple materials, each mapped to one or 
more “variants”. The appropriate variant (e.g. “Snow”) will be activated 
at load time.

Animations
----------

The animation driving array is in seconds, unlike in s, where that was the 
frame number. The author may create any number of frames, even with unequal 
time intervals between them, this will not be counted by the program. Rather 
it will precisely interpolate to a required time moment to get the pose, 
even if then is no assigned frame there. So e.g. for a 8-notched throttle 
controller one can skip all the intermediate frames and define only the 
first (at 0 second) and last one (at 8 seconds), it will still work. 
Looped animations can have any time length, they will still be considered one
loop regardless.

Two types of animation are supported by Openrails. Predefined animations are 
designated with the “animations” “name” attributes. The parent-child relations 
of the defined animations or the nodes they target are irrelevant in gltf. 
A node will not be animated just because it is a child of another animated node. 
Instead an animation can have multiple target nodes via its multiple “channels”.

The other supported animation type is the node-animation. Such “nodes” are to 
be marked with the syntax:

.. code-block:: json

  "extras": { "OPENRAILS_animation_name": "WHEELS1" },

This will mark the location for Openrails where to engage its built-in
animation logic when needed. The traditional naming pattern applies here, and
a node should not be both part of a predefined animation and also marked for
node-animation.
(Note, the nodes “name” attributes are not used for anything, unlike in s.)

Level-Of-Detail
---------------

LOD-s can be defined either as internals via the 
`MSFT_lod <https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Vendor/MSFT_lod/README.md>`_
extension, or externals by creating multiple gltf files and adding the 
suffixes of pattern <name>_LOD01.gltf, <name>_LOD02.gltf, etc… The _LOD00
suffix for LOD 0 is optional. The author may still define the displaying 
criteria in the root node of the LOD 0, as described in the extension, 
using a line like:

.. code-block:: json

  "extras": { "MSFT_screencoverage": [ 0.2, 0.05, 0.001 ] },

In the prior case, for internal LOD-s, the author needs to define the root 
nodes of the various LOD-s in the root node of the LOD 0. E.g. if node 
0 is the root of LOD 0, then to declare node 1 for LOD 1 and node 2 for LOD 2
as their root nodes, looks like this:

.. code-block:: json

  "extensions": { "MSFT_lod": { "ids": [ 1, 2 ] } },

(Note for all extensions, the usual extension usage criteria applies, specifically 
the important one is the requirement to register the extension used into 
the "extensionsUsed" array of the gltf.)

Lights
------

Active light sources can be attached to a gltf file as in the 
`KHR_lights_punctual <https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_lights_punctual/README.md>`_
extension. Or even a light-only gltf can be created and used in a W file:

.. code-block:: json

  {
  "asset": { "version": "2.0" },
  "extensionsUsed": [ "KHR_lights_punctual" ],
  "scenes": [ { "nodes": [0] } ],
  "nodes": [ { "translation": [0, 5, 0], "rotation": [-0.7071, 0, 0, 0.7071], "extensions": { "KHR_lights_punctual": { "light": 0 } } } ],
  "extensions": { "KHR_lights_punctual": { "lights": [ { "type": "spot", "range": 500.0, "color": [1.0, 0.9, 0.8], "intensity": 50.0, "spot": { "outerConeAngle": 1.5 } } ] } }
  }

These lights are bound to a node, and the nodes can also be set to follow the 
day-night cycle. By applying the following extra, the whole node will become
invisible during daytime, including the attached light, mesh and all further
subnodes:

.. code-block:: json

  "extras": { "OPENRAILS_node_day_night_switch": true },

It is possible to inject own image based lighting (IBL) to an object via the
`EXT_lights_image_based <https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Vendor/EXT_lights_image_based/README.md>`_
extension. Although it allows only to define a model-wide IBL, Openrails 
allows to limit its effect to a single material instead (requires the IBL 
not to be assigned to the whole "scene"):

.. code-block:: json

  "extras": { "OPENRAILS_material_image_based_light": 0 },

It is useful to use an own IBL for interiors and cabs, because the bilt-in 
night IBL is optimized for outside night illumination, and it may be too 
dark for the inside. A diffuse IBL definition using spherical harmonics
looks like below. AI-s can help in generating such coefficients.

.. code-block:: json

  "extensions": {
    "EXT_lights_image_based": {
      "lights": [
        { "intensity": 1.0, "irradianceCoefficients": [
          [1.250, 1.050, 0.750], [0.000, 0.000, 0.000], [-0.450, -0.380, -0.250],
          [0.000, 0.000, 0.000], [0.030, 0.025, 0.015], [0.015, 0.012, 0.008],
          [0.060, 0.050, 0.030], [0.015, 0.012, 0.008], [0.030, 0.025, 0.015] ]
        }
      ]
    }
  },


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
