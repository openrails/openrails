<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-12">
        <h1>Contribute > Building Models</h1>
      </div>
    </div>
 		<div class="row">
			<div class="col-md-3"></div>
			<div class="col-md-6">
<h2>The Building Process</h2>
<p>
Currently models for Open Rails are created in the same way as for Microsoft Train Simulator. This process is quite involved and the Open Rails team will be developing tools and formats to streamline it.
</p>
<h2>3D Modelling</h2>
<p>
There are several products which support modelling in 3D and produce the formats used by Open Rails and Microsoft Train Simulator. 
</p>
<ul>
  <li><a href="http://www.amabilis.com">Amabilis 3DCrafter</a></li>
  <li><a href="https://www.blender.org/">Blender</a> with 
    <a href="http://www.elvastower.com/forums/index.php?/files/file/2251-blender-to-msts-exporter/">an exporter to MSTS/OR format</a></li>
  <li><a href="http://www.sketchup.com/">Google Sketch Up</a> (good for static models)</li>
  <li><a href="https://www.google.com/?q=gmax#q=gmax">Autodesk GMax</a></li>
  <li><a href="http://www.sketchup.com/">Autodesk 3ds Max</a></li>
  <li>Train Sim Modeler (TSM) - no longer sold by Abacus <br><a href="https://www.digital-rails.com/">but downloadable free</a> from Digital Rails</li>
</ul>
<p>&nbsp;</p>
<h2>Tutorials</h2>
<p>
<i>scottb613</i> has posted a <a href="http://www.trainsim.com/vbts/showthread.php?298900-Big-Dog-Birth-of-a-Steam-Locomotive/page7">tutorial for building locos</a> using 3DCrafter as a series of forum posts.
</p><p>
<img src="les-ross.jpg" title="Built by bruce16 using 3DCrafter" alt="Built by bruce16 using 3DCrafter">
</p><p>&nbsp;
</p><p>
<a href="http://YouTube.com">YouTube</a> hosts many tutorials for SketchUp. Google <a href="http://www.google.co.uk/earth/learn/3dbuildings.html">
provides a tutorial</a> especially for 3D buildings.
</p><p>&nbsp;
</p><p>
There is <a href="http://www.elvastower.com/forums/index.php?/topic/21949-gmax-tutorials/">a list of tutorials</a> for GMax on Elvas Tower.
</p><p>&nbsp;
</p>
<h2>3D Modelling For Open Rails</h2>
<p>
The key advantage that Open Rails currently offers over Microsoft Train Simulator is that good frame rates can be maintained with a much higher number of polygons,
so curves can be smoother and more detail can be modelled. Open Rails also displays 32-bit color (whereas Microsoft Train Simulator is limited to 16-bit). 
Another advantage is the longer viewing distances, adjustable from 2km out to 10km. 
</p><p>
There will be other advantages as we move beyond our current status of Microsoft Train Simulator-compatible.    
</p>
<h2>3D Cabs</h2>
<p>
One of these advantages is 3D cabs. Open Rails now supports 3D cabs interiors as well as the Microsoft Train Simulator 2D interiors.
</p>
<img src="3d_cab.jpg" title="Cab from Hungarian MAV 424 class loco &#xa; posted by zaza" alt="Cab from Hungarian MAV 424 class loco &#xa; posted by zaza" />
<p>&nbsp;
</p>
<h2>Operational Modelling For Open Rails</h2>
<p>
Models of rolling stock and signals can take advantage of a few features which are only available in Open Rails. The manual contains a list
of about 40 parameters which improve the operation of locos. 
</p><p>
There is also a Train Control System (TCS) under development which reports the state of the train to the driver. This allows any continuous
monitoring system such as the German LZB and the European ERTMS to be modelled. The modelling is carried out by programming in C#.
</p>
		</div>
	</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
