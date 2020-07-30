<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/accordion/accordion.css" >
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-12">
        <h1>Contribute > Developing Code</h1>
      </div>
    </div>
 		<div class="row">
			<div class="col-md-12">

<!-- implement an accordion -->
<ul class="accordion">
  <li>
    <a href=# id="private_changes"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Private Changes</h2></a>
    <div class="accordion_body">
      <p>
Anyone can <a href="../../download/source/">download the source code</a> for Open Rails and make changes to suit themselves and create a personal version.
      </p>
    </div>
  </li><li>
    <a href=# id="public_changes"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Public Changes</h2></a>
    <div class="accordion_body">
      <p>
If your changes might be useful to others, then we encourage you to submit them for inclusion into the product. We will respond to all
submissions and give credit in the project record for submissions that are included.
      </p><p>
Click <a href="https://github.com/twpol/openrails/blob/feature/contributing/CONTRIBUTING.md">here for detailed advice and instructions for contributing to Open Rails</a>.
      </p>
    </div>
  </li><li>
    <a href=# id="source_code_repository"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Version Control System</h2></a>
    <div class="accordion_body">
      <p>
The Open Rails project uses Git as the versioning and revision control system for our software.
Revisions are kept in <a href="https://github.com/openrails/openrails">a public repository at GitHub.com</a> and can be compared, restored and merged, so that versions are safe and several developers can work
 independently without (too much) conflict.
      </p><p>
From your PC, you can simply view the repository from a web browser. 
GitHub.com provides the GitHub Desktop program to interface between GitHub.com and your PC (for 64-bit Windows only).
Other Windows programs are SourceTree and TortoiseGit and also Visual Studio includes a Git tool.
See our <a href="https://onedrive.live.com/view.aspx?resid=7F0F05E28F47C189!295694&ithint=file%2cdocx&authkey=!ABYL6qOsIy85Bdc">step-by-step instructions for starting to work with Git</a>.
      </p><p>
The main folders in the repository are:
    </p>
      <ul>
        <li>Addons - accessory files shipped with the installation package</li>
        <li>Architecture - an incomplete experiment in restructuring the program</li>
        <li>Documentation - operations manual and other documentation</li>
        <li>Program - empty space for executables once they are compiled</li>
        <li>Source - the principal source code files</li>
        <li>Website - source for this website</li>
      </ul>
    </div>
  </li><li>
    <a href=# id="compiling" name="compiling"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Compiling the Open Rails Project</h2></a>
    <div class="accordion_body">
      <p>
To compile and debug the Open Rails source code, ensure you have the following Microsoft products installed:
      </p>
      <ul>
        <li>Visual Studio 2017 or 2019, any edition. The 
          <a href="https://www.visualstudio.com/downloads/">Community Edition</a> 
         is free
		 <br />(Note 1: To save on disk space, all you need is the option Windows > .NET Development)
		 <br />(Note 2: Install this before Microsoft XNA Framework Redistributable 3.1)
        </li>
        <li><a href="https://www.microsoft.com/en-gb/download/details.aspx?id=15163">Microsoft XNA Framework Redistributable 3.1</a></li>
      </ul>
      <p>
After you have downloaded the code:
      </p>
      <ol>
        <li>Open folder <i>Source</i></li>
        <li>Double click on file <i>ORTS.sln</i> to launch Visual Studio with the Open Rails project</li>
        <li>From the Visual Studio menu, select <i>Build > Rebuild Solution</i></li>
        <li>In the status bar (lower left), wait for the message 'Rebuild All succeeded'</li>
      </ol>
      <p>The executable files have now been built and placed in the <i>Program</i> folder.</p>
    </div>
  </li><li>
    <a href=# id="debugging">
      <h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Running the RunActivity.exe Code in Debug Mode</h2>
    </a>
    <div class="accordion_body">
      <p>
Note: When debugging you will bypass the normal start menu and must specify an activity on the command line.
      </p><p>
On the Visual Studio menu,
      </p>
      <ol>
        <li>Select <i>View > Solution Explorer</i></li>
        <li>In the <i>Solution Explorer</i> box (on the right), right click on <i>RunActivity > Properties</i></li>
        <li>Use the Debug tab (on the left) to open the Debug Window.</li>
        <li>Into the "Command line arguments", enter the path of the activity that you want to run: e.g. "c:\personal\msts\routes\lps\activities\ls1.act". 
         Use quotes if the path has spaces in it.
        </li>
        <li>Below this, check the checkbox for "Enable native code debugging"</li>
        <li>Press F5 to run RunActivity.exe using your activity.</li>
      </ol>
    </div>
  </li>
  <li>
    <a href=# id="tests">
      <h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Running the Test Suite</h2>
    </a>
    <div class="accordion_body">
      <p>
        The Open Rails source tree includes a number of unit and integration tests, primarily for portions of the code that deal with data processing. All code changes must pass these tests. Developers are also encouraged to write their own tests for any code that lends itself to testing.
      </p>
      <p>
        After opening the solution in Visual Studio,
      </p>
      <ol>
        <li>Select <em>Test > Test Explorer</em></li>
        <li>Wait for the tests to populate in the <em>Test Explorer</em> pane</li>
        <li>In the top-left corner of this pane, select "Run All Tests," or press Ctrl-R, then A.</li>
      </ol>
    </div>
  </li>
  <li>
    <a href=# id="code_policy"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Policy for Code Changes</h2></a>
    <div class="accordion_body">
      <p>
        To allow many people to contribute directly to Open Rails successfully, we have a policy in place to keep tabs on where changes come from and which changes are allowed. The policy is as follows:
      </p>
      <ol>
        <li>All changes must be one of:
          <ul>
            <li><em>Simple</em> bug fixes, with a link to the bug</li>
            <li><em>Targeted</em> bug fixes, with a link to the bug</li>
            <li><em>Approved</em> blueprints, with a link to the blueprint</li>
            <li>Documentation or localisation updates (kept separate from code changes)</li>
          </ul>
        </li>
        <li><em>Simple</em> means no more than a few lines changes and no uncertainty over the logic change.</li>
        <li><em>Targeted</em> means "Milestone" is set to the next planned stable version.</li>
        <li><em>Approved</em> means "Direction" is "Approved", "Milestone target" is unset or set to the next planned stable version, and at least 7 days of discussion has taken place.</li>
      </ol>
      <br>
      <p>
        For members of the <a href="../../discover/project-team/">Development Team</a>, these policies are defined in more detail in
        <a href="http://www.elvastower.com/forums/index.php?/topic/26392-new-policy-for-code-changes/">Policy for code changes</a>
        and
        <a href="http://www.elvastower.com/forums/index.php?/topic/27109-new-policy-for-blueprints/">Policy for blueprints</a>.
      </p>
    </div>
  </li>
</ul><!-- end of accordion -->

			</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
    <script src="../../shared/accordion/accordion.js"></script>
  </body>
</html>
