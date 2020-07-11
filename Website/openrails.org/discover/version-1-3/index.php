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
          <h1>Discover > Version 1.3</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-10">
          <h2>New in Open Rails 1.3.1 (since 1.3)</h2>
          <p>
            <a href="https://launchpad.net/or/+milestone/1.3.1">10 important bugs</a> have been fixed in this release.
          </p>
          <h2>New in Open Rails 1.3 (since 1.2)</h2>
          <p>
            A summary of the new and improved features can be found below. In addition, over 140 bugs have been fixed in this release. Please keep <a href="http://openrails.org/contribute/reporting-bugs/">reporting bugs and suggesting new features</a> so Open Rails can continue to imporve.
          </p>
          <p>
            This is the last version of Open Rails to support Windows XP; future versions will require Windows 7 or later.
          </p>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-4">
          <h3>Headlines</h3>
          <ul>
            <li>Timetables can join and split trains to form new trains</li>
            <li><a href="https://youtu.be/UO9XrBz3iD0">Mouse control for 3D cabs</a></li>
            <li><a href="https://youtu.be/7-wkgUxQsvI">Working transfer tables</a></li>
            <li>Activity evaluation</li>
            <li>Separate files for extensions to activity files</li>
          </ul>
          <h3>What's been added</h3>
          <ul>
            <li>Steam locomotive vacuum brake ejector and general improvements to vacuum brake operation</li>
            <li>Route-wide sounds when trains pass over switches</li>
            <li>Route-wide sounds when trains pass over low radius curves</li>
            <li>Save and resume in multiplayer</li>
            <li>Signal script functions can be reused by multiple signal types</li>
            <li><a href="https://youtu.be/z61lYgzvlKw">Car spawner option to support walking people</a></li>
            <li>Cab radio sound triggers</li>
            <li>Command-line tool to load all supported file formats</li>
            <li>Activity randomization, including dynamic weather and mechanical problems</li>
            <li>Persistent variables in signal scripts</li>
            <li>Pooling of available trains in sidings for timetables</li>
            <li>Explicit train speed setting in timetable editor</li>
            <li>Visual steam and smoke effects for locomotives and wagons</li>
            <li>Fuel gauge cab control for steam locomotives</li>
          </ul>
          <h3>What's been improved</h3>
          <ul>
            <li>Content-creator option to keep trees off roads and tracks</li>
            <li>Station and siding labels fade in as you approach</li>
            <li>Multiple passenger viewpoints inside train cars</li>
          </ul>
        </div>
        <div class="col-md-2"></div>
        <div class="col-md-4">
          <h3>What's been improved (continued)</h3>
          <ul>
            <li><a href="https://youtu.be/5VCq-8HtmH8">AI trains open and close doors at stations</a></li>
            <li>Steam locomotive simulation and content-creator options</li>
            <li>Content-creator per-model and per-instance level crossing sounds</li>
            <li>Various new signal script functions</li>
            <li>Explore route in activity mode</li>
            <li>Better commands for manually changing weather</li>
            <li>Load animation can be used on wagons and locomotives to vary physics properties</li>
            <li>Option to load only day/night textures as needed, not both</li>
            <li>Can define custom signal function types</li>
            <li>Content-creator options for controlling signal light glow</li>
            <li>Additional animations and mouse control for cab controls</li>
            <li>Improved AI waiting point control in activities</li>
            <li>User and content-creator options for sound attenuation in cab and passenger views</li>
            <li>Content-creator timetable commands for stop positioning</li>
            <li>Content-creator timetable options for random delays on various actions</li>
            <li>Expose to users internal option to force all objects to cast shadows</li>
            <li>Content-creator option for timetables to override default stopping time at stations</li>
            <li>Better checks for when trains stop at stations</li>
            <li>Simulation of wind resistance forces</li>
            <li>Option to automatically discard abnormal values in locomotives</li>
          </ul>
          <h3>Detailed changelog</h3>
          <p>
            A <a href="https://launchpad.net/or/+milestone/1.3">full list of changes</a> is available.
          </p>
          <h3><a href="../version-1-2/">Changes in previous version</a></h3>
          <p>&nbsp;</p>
        </div>
      </div>
	</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
