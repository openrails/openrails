<?php include "../../shared/head_notrack.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
      <div class="row">
        <div class="col-md-12"> 
          <h1>Discover > Version 1.2</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-10">
          <h2>Changes brought in by Open Rails 1.2 (since 1.1)</h2> 
          <p>
            Selected new features and improvements are listed below, mostly to provide more realism.
          </p>
          <p>
		    A lot of minor bugs (e.g. AI trains, freight loading and refuelling) have also been fixed but our code is reaching the point where these problems are seen only by a few users and not in our regular testing. We need you to <a href="http://openrails.org/contribute/reporting-bugs/">report these events in the usual way</a> as we never see them.
          </p>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-5">
          <h2>New features in Open Rails 1.2</h2> 
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-4">
          <h3>Operation Additions</h3>
          <p>
            The player's loco or a wagon may be turned on a turntable in an activity or in explore mode, with sound and multi-user support too.
	      </p>
          <p>
            UK distant semaphore signals, when on the same post as a home signal, have been enhanced to work prototypically.
	      </p>
          <p>
		    Mileposts and diverging switches are now included in the Track Monitor window.
          </p>
          <p>
            Braking friction is now related to speed and locos and stock will skid if braking is excessive.
          </p>
          <p>
		    Improved modelling of the brake pressure and leakage so that brake controls are more realistic.
		  </p>
		  <p>
            The Head Up Display (HUD) has better information on brake pressures and now shows the load weight of freight or passengers.
          </p>
          <p>
		    Time of day waiting points are easier to use as they no longer require a train to stop if the time has already passed.
          </p>
          <p>
		    The Car ID is now visible when using the Car Operation menu. 
          </p>
        </div>
        <div class="col-md-2"></div>
        <div class="col-md-4">
          <h3>Locomotive Additions</h3>
          <p>
            The switch to night time textures in cabs has been delayed about 45 mins, so that daylight has more time to fade for more realism.
          </p>
          <p>
            For steam locos, wheel-slip has been added to match electric and diesel and the level in the water-glass is now affected by an incline for more realism.
          </p>
          <p>
            The tilting behaviour of tilting trains on super-elevated track was accidentally removed and has now been restored. 
          </p>
          <p>
		    The circuit breaker of an electric locomotive can now be controlled by the driver. The behaviour of the circuit breaker can be modified using scripts. Specific cabview controls and sound triggers are available for content creators. 
          </p>
          <p>
		    Double wires and pantograph operation for electric locos with synchronous triphase motors are now supported.
		  </p>

          <h3>General Improvements</h3>
          <p>
            The tracking cameras (no. 2 and 3) no longer tilt on super-elevated track.
          </p>
          <p>
            Each road can now have different traffic.
          </p>
          <p>
            Each route can now have different track shapes through the use of include files.
          </p>

          <h3>System Additions</h3>
          <p>
            Loading screens can now fill a wide screen and be specific to Open Rails.
          </p>
          <p>
            The multiple warnings when the loading of shape files fail are now disabled by default.
          </p>

          <h3>Detailed Changelog</h3>
          <p>
            A <a href="https://launchpad.net/or/+milestone/1.2">full list of changes</a> is available.
          </p>

          <h3><a href="../version-1-1/">Changes in previous version</a></h3>
		  <p>&nbsp;</p>
        </div>
      </div>
	</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
