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
				The Open Rails simulator operates train services independently of Microsoft Train Simulator and without running any Microsoft Train Simulator code. 
        Currently you must use <a href="http://koniec.org/tsre5/">Goku's modern editor TSRE5</a> or even the original Microsoft Train Simulator Route Editor 
        to build new routes, but we plan to develop our own Route Editor.
				</p>

				<h3>Can I use my collection of Microsoft Train Simulator locomotives and rolling stock with Open Rails?</h3>
				<p>
				The short answer is 'yes'.
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

				<h3>Are there any routes available for Open Rails that do not need Microsoft Train Simulator?</h3>
				<p>
				Older free and payware routes are often packaged as add-ons to Microsoft Train Simulator and many locos make use of sound and cabview files from 
        Microsoft Train Simulator.
				</p><p>
				Newer routes have been developed specifically for Open Rails. See our <a href="/download/content/">Content</a> page.
        </p>
        <hr>				

				<h2><span id="technical_questions">Technical Questions</span></h2>
				<h3>What computer specs do I need to run Open Rails?</h3>
				<p>
          The latest system and software requirements are <a href="#hardware_requirements">shown below</a>.
        </p>

				<h3>Does Open Rails have 3D cabs?</h3>
				<p>
          Yes, these have been supported for a while alongside the older 2D cabs.
        </p>

				<h3>Does Open Rails have working signals?</h3>
				<p>
          The Open Rails team has implemented a comprehensive and robust signal system which is detailed in the manual.
        </p>

				<h3>Does Open Rails have timetables?</h3>
				<p>
          Yes, timetables provide multiple trains running at the same time, with complex operations such as splitting and joining trains as described in the manual.
          You can choose which train to drive and the other trains in the timetable will run automatically.
        </p>

				<h3>Does Open Rails have multi-player operation?</h3>
				<p>
          Yes, you can share your session with as many remote friends as your computer can cope with, all working together driving individual trains to 
          deliver a service.
        </p>

				<h3>What's the graphics engine in Open Rails?</h3>
				<p>
          Open Rails currently uses Monogame technology to display its environment. This makes good use of modern graphics cards and is also
          compatible with the <a href="reshade.me">ReShade graphics post-processor</a>.
        </p>

				<h3>What about a route editor for Open Rails?</h3>
				<p>
          An Open Rails route editor is a key element of the project and <a href="https://launchpad.net/or/+milestones">is identified in our
          project roadmap</a>.
        </p>

				<h3>What should I do if I find problems, issues or stuff not working with Open Rails?</h3>
				<p>
          Open Rails is a constantly evolving project that runs on volunteer participation. Please feel free to post questions and comments
          about <a href="/share/community/">Open Rails on the forums</a>. The Open Rails team monitors these forums daily.
        </p>

        <hr>				

        <h2 id="installation_questions">Installation Questions</h2>
<?php include "new_install.php" ?>
			</div>
			<div class="col-md-2">&nbsp;</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>