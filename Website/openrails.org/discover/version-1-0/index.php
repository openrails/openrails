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
        <h1>Discover > Version 1.0</h1>
      </div>
    </div>
		<div class="row">
			<div class="col-md-1"></div>
			<div class="col-md-10">
              <h2>Changes brought in by Open Rails 1.0 (since 0.9)</h2> 
	          <p>
			    New features are listed below. However, effort has also gone into better sound, environment,
				enhancing performance to cater for larger and more detailed models and, of course, updating the manual. 
				Many models take advantage of quirks in Microsoft Train Simulator, so much effort has also gone into handling these
				tricky cases and in eliminating unexpected behaviour in activities.
			  </p>
			</div>
	    </div>
		<div class="row">
			<div class="col-md-1"></div>
			<div class="col-md-4">
              <h2>New features in Open Rails 1.0</h2> 
			</div>
	    </div>
		<div class="row">
			<div class="col-md-1"></div>
			<div class="col-md-4">
              <h3>System Additions</h3>
	          <p>
		  We now recommend installation through the new installer as it's simpler, but the old method using a zip file continues to be available.
		  The installation now offers to update itself and you can control updating in detail. Also the Saves you make during the game are no
		  longer invalidated once the installation is updated, although re-loading them cannot be guaranteed.
		      </p><p>
		  You can now choose from 9 languages which are used throughout the product. 
		      </p><p>
		  When loading a route, the old console window has been replaced by a loading screen with an image of the route and a progress bar
		  which becomes active after the first time of loading.
		      </p><p>
		  When driving, you can now operate the cab controls using a mouse as well as the keyboard.
		      </p>
              <h3>Environment Additions</h3>
	          <p>
			    Rain and snow can be blocked from falling inside buildings and tunnels and we can hear the rain fall. Fog is more realistic and
                fog and rain intensity can be adjusted in game.
				We have added support for KOSMOS clouds.				
		      </p><p>
			    3D cabs and their controls are now available.
		      </p>
			  <h3>Detailed Changelog</h3>
			  <p>
				A <a href="https://launchpad.net/or/+milestone/1.0">full list of changes</a> is available.
			  </p>
			</div>
			<div class="col-md-2"></div>
			<div class="col-md-4">
              <h3>Operation Additions</h3>
	          <p>
			    Diesels with gearboxes are now supported along with bail-off valves and brake-pipe angle cocks.  
		      </p><p>
			    Activities which start with the player's train in motion are also now supported.
		      </p><p>
			    An odometer has been added so you can check that the rear of your train has passed a location.
		      </p><p>
			    Some major additions have been made. <!-- Withdrawn as not yet stable or documented. There is a Train Control System (TCS) which is scripted to support an unlimited variety
				of regional practices. 
			  </p><p> -->
  				In the new "Autopilot mode" for activities, the player's train can be driven automatically as though it were an AI train
	  			which is very helpful when testing activities.
		      </p><p>
			    There are now facilities for AI trains to carry out coupling, uncoupling and helper operations.
		      </p><p>
			    Perhaps the most significant addition is the new "Timetable Mode", which is an alternative to conventional activities. Timetable Mode 
				makes it easy to schedule a busy day of services and adopt any one of them to be the player's train.
		      </p>
              <h3>Steam Locomotive Additions</h3>
	          <p>
			    Steam and smoke have been re-worked for more realism. They are now blown by the wind and chuff with the piston action.
		      </p><p>
			    All the cab controls can now be animated.
		      </p><p>
			    Support has been added for engines with superheaters, compound expansion and geared locos (e.g. Shay).
		      </p>
            </div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
