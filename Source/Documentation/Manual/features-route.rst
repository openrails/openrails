.. _features-route:

**************************
OR-Specific Route Features
**************************

As a general rule and as already stated, Open Rails provides all route 
functionalities that were already available for MSTS, plus some opportunities 
such as also accepting textures in .dds format.

OR provides a simple way to add snow terrain textures: the following default 
snow texture names are recognized: ``ORTSDefaultSnow.ace`` and 
``ORTSDefaultDMSnow.ace``, to be positioned within folder ``TERRTEX\SNOW`` of 
the concerned route. For the snow textures that are missing in the ``SNOW`` 
subfolder, and only for them, ORTS uses such files to display snow, if they 
are present, instead of using file ``blank.bmp``.

To have a minimum working snow texture set, the file ``microtex.ace`` must 
also be present in the ``SNOW`` subfolder.
