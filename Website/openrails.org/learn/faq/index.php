<?php include "../../shared/head.php" ?>
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Learn > FAQ</h1>
			</div>
		</div>
		<div class="row">
			<div class="col-md-2">&nbsp;</div>
			<div class="col-md-8">
				<br>
				<ul>
				  <li><a href="#general_questions">General Questions</a></li>
				  <li><a href="#technical_questions">Technical Questions</a></li>
				  <li><a href="#installation_questions">Installation Questions</a></li>
				</ul>
				<br>
				<h2><span id="general_questions">General Questions</span></h2>
				<h3>What is Open Rails?</h3>
				<p>
				Open Rails is a train simulator project compatible with Microsoft's Train Simulator product.
				</p>
				<h3>Is Open Rails freeware? Is there a downloadable version available?</h3>
				<p>
				Yes. A variety of downloads <a href="../../download/program/">are available here</a>.
				</p>
				<h3>Is Open Rails just an improved version of Microsoft Train Simulator?</h3>
				<p>
				Open Rails is not an improvement to Microsoft Train Simulator, but a complete new simulator that can use Microsoft Train Simulator routes, activities, consists and train sets. At
        installation, Open Rails looks for Microsoft Train Simulator entries in the Windows registry to locate any Microsoft Train Simulator files on your computer. It will offer to use them
        in the simulation.
				</p><p>
				The Open Rails simulator operates train services independently of Microsoft Train Simulator and without running any Microsoft Train Simulator code. Currently you must use the Microsoft Train Simulator Route Editor to build
				new routes, but we intend to develop our own Route Editor.
				</p>
				<h3>Can I use my collection of Microsoft Train Simulator locomotives and rolling stock with Open Rails?</h3>
				<p>
				The short answer is 'yes', but with some limitations around cab controls and gauges. Everything else should work very similar to Microsoft Train Simulator.
				</p>
				<h3>Can I use my collection of Microsoft Train Simulator routes with Open Rails?</h3>
				<p>
				The short answer is 'yes'.
				</p>
				<h3>Can I use my collection of Microsoft Train Simulator activities with Open Rails?</h3>
				<p>
				The answer is "Yes", though the signaling in Open Rails and performance of AI (computer-driven) trains is more rigorous and some activities may need adjusting.
				</p>
				<h3>Does Open Rails improve the performance of Microsoft Train Simulator?</h3>
				<p>
				No, Open Rails has no effect on the performance of Microsoft Train Simulator. Open Rails is a completely new simulator. With suitable hardware, most users running Microsoft Train Simulator routes and consists
				in Open Rails see significantly higher frames per second (FPS) because the Open Rails simulator uses modern graphics cards (GPUs) effectively. Loading
        times are much reduced and larger routes can be accommodated.
				</p>
				<h3>Will Open Rails make my Microsoft Train Simulator routes and trains look better?</h3>
				<p>
				Microsoft Train Simulator displays textures as 16-bit color even though most are stored as 24 or 32-bit ACE files. Therefore, the foundation is there to support these 
				higher bit textures. Open Rails may also provide better lighting effects and texture effects which gives a better view of the current Microsoft Train Simulator models.
				</p>
				<h3>Will Open Rails improve my frame rates, decrease stuttering, make my Microsoft Train Simulator content look better or improve the lighting?</h3>
				<p>
				The point of Open Rails is not just better frame rates or display colors, but those are common side effects. Open Rails is more about the future!
				</p>
				<h3>Do I need Microsoft Train Simulator to run Open Rails?</h3>
				<p>
				No. However Open Rails does not yet have a route editor or an activity editor, so you cannot build your own routes and activities.
				</p>
				<h3>Are there any routes available for Open Rails that do not need Microsoft Train Simulator?</h3>
				<p>
				Most free and payware routes are packaged as add-ons to Microsoft Train Simulator and many locos make use of sound and cabview files from Microsoft Train Simulator.
				</p><p>
				Some Australian routes (New South Wales) have been packaged to work just with Open Rails:
        </p>
				<ul>
				  <li><a href="http://www.craven.coalstonewcastle.com.au/">Craven Timber Railway</a></li>
				  <li><a href="http://www.manning-river.coalstonewcastle.com.au/">Manning River Breakwall Railway</a></li>
				  <li><a href="http://www.tweed.coalstonewcastle.com.au/">Tweed Railway</a></li>
				</ul>
        <hr>				
				<h2><span id="technical_questions">Technical Questions</span></h2>
				<h3>What computer specs do I need to run Open Rails?</h3>
				<p>
          In general, Open Rails currently requires a higher hardware specification than Microsoft Train Simulator, especially with regard to video cards (GPUs).
          Community members have Open Rails running on Windows XP, Vista, Windows 7 and 8 operating systems. Some Open Rails users, however, have
          reported difficulties with low frame rates (FPS) especially on laptops with onboard video because of the demands Open Rails places
          on the GPU.
        </p><p> 
          The latest system and software requirements are <a href="#hardware_requirements">shown below</a>.
        </p>
				<h3>Does Open Rails achieve higher FPS by giving the graphics card more of the work in rendering the graphics?</h3>
				<p>
          Yes, Open Rails makes fewer demands on the CPU for processing information and rendering the graphics. 
          The GPU and CPU now share these functions.
        </p>
				<h3>Why does my train operate differently in Open Rails?</h3>
				<p>
          With the current release, Open Rails software has implemented our first phase of independent physics for diesel, diesel electric, 
          electric and steam engines. This more sophisticated physics model incorporates ground-breaking inertia and traction motor loading,
          plus wheel-slipping equations that more realistically model train physics. As a result, you may experience slower acceleration and
          longer stopping distances compared to Microsoft Train Simulator.
        </p>
				<h3>Why don't I see distant mountains?</h3>
				<p>
          Distant mountains, being demanding, were implemented only as an option in v0.9. Please tick the checkbox in <br>
          <span class="tt">Menu > Options > Experiment > Show Distant Mountains</span>.
        </p>
				<h3>How do I change my view?</h3>
				<p>
          You can move about in the route and locomotive in Open Rails with several camera views. The F1 key assignment window details all
          the views available in Open Rails. In current version of the Open Rails software, you can do more than Microsoft Train Simulator with BIN.
        </p><p> 
          The #4 camera (trackside) automatically jumps as the train passes. Use the #8 key to "unlock" the #4 camera to a fixed view, which
          is movable like all the other cameras. Then just press the #4 key to jump to the next automatic viewpoint. 
        </p><p> 
          Use the #8 key to navigate the free camera to any viewpoint. Then use Shift+8 to return to a previous viewpoint.
        </p><p> 
          You can view other (AI) trains using Alt-9.
        </p>
				<h3>Does Open Rails have working signals?</h3>
				<p>
          The Open Rails team has implemented a comprehensive and robust signal system which is detailed in the manual.
        </p>
				<h3>What should I do if I find problems, issues or stuff not working with Open Rails?</h3>
				<p>
          Open Rails is a constantly evolving project that runs on volunteer participation. Please feel free to post questions and comments
          about <a href="../../share/community/">Open Rails on the forums</a>. The Open Rails team monitors these forums daily.
        </p>
				<h3>What's the graphic engine in Open Rails?</h3>
				<p>
          Open Rails currently uses Microsoft XNA technology to display its environment. The XNA technology was developed by Microsoft 
          specifically for computer gaming.
        </p>
				<h3>What about a route editor for Open Rails?</h3>
				<p>
          An Open Rails route editor is a key element of the project and <a href="https://launchpad.net/or/+milestones">identified in our
          project roadmap</a>.
        </p>
        <hr>				
        <h2 id="installation_questions">Installation Questions</h2>
<?php include "install.php" ?>
			</div>
			<div class="col-md-2">&nbsp;</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>