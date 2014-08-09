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
			<div class="col-md-3"></div>
			<div class="col-md-6">

<!-- implement an accordion -->
<ul class="accordion">
  <li>
    <a href=#><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Private Changes</h2></a>
    <div class="accordion_body">
      <p>
Anyone can <a href="../../download/source">download the source code</a> for Open Rails and make changes to suit themselves and create a personal version. <a href="#accessing_the_code">Instructions for this</a> are below.
      </p>
    </div>
  </li><li>
    <a href=#><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Public Changes</h2></a>
    <div class="accordion_body">
      <p>
If your changes might be useful to others, then we encourage you to <a href="#submitting_a_change">submit them for inclusion into the product</a>. We will respond to all
submissions and give credit in the project record for submissions that are included.
      </p>
    </div>
  </li><li>
    <a href=#><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> SVN Version Control System</h2></a>
    <div class="accordion_body">
      <p>
The Open Rails project uses Apache Subversion (often abbreviated to SVN) as the versioning and revision control system for our software.
Revisions are archived in a repository and can be compared, restored and merged, so that versions are safe and several developers can work
 independently without (too much) conflict.
      </p><p>
The Open Rails source code is managed on <a href="http://svn.uktrainsim.com/svn/openrails/trunk">our svn server</a>:<br>
Read-only access is provided from the guest login:
      </p>
      <ul>
        <li>Username = <span class="tt">orpublic</span></li>
        <li>Password = <span class="tt">orpublic</span></li>
      </ul><br>
      <p>
From your PC, you can use an SVN client such as <a href="http://tortoisesvn.net">TortoiseSVN</a> to access the files, or simply view the repository from a web browser.
      </p><p>
P.S. - Our thanks to <a href="http://www.uktrainsim.com">UKTrainSim.com</a> for donating the SVN server space and admin services.
      </p>
    </div>
  </li><li>
    <a href=# id="accessing_the_code"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Accessing The Code With TortoiseSVN</h2></a>
    <div class="accordion_body">
      <p>
If you are not familiar with SVN, please study the TortoiseSVN Help first.
      </p><p>
On your desktop,
      </p>
      <ol>
        <li>From <i>Windows Explorer</i>, choose a folder to hold the project code.</li>
        <li>Right click on the folder, select <i>SVN Checkout...</i></li>
        <li>In the <i>Checkout</i> dialog box, enter the <i>URL of repository</i> : 
          <span class="tt">http://svn.uktrainsim.com/svn/openrails/trunk</span>
        </li>
        <li>Enter <i>Username</i> : <span class="tt">&nbsp;orpublic</span></li>
        <li>Enter <i>Password</i> : <span class="tt">&nbsp;orpublic</span></li>
        <li>Check that the <i>Checkout directory</i> : shows the folder where you want the files extracted to - e.g. 
          <span class="tt">C:\Users\Wayne\Desktop\openrails</span>
        </li>
        <li>Click OK.</li>
      </ol><br>
      <p>
The main folders in the repository are:
      </p>
      <ul>
        <li>Addons - accessory files shipped with the installation package</li>
        <li>Architecture - an incomplete experiment in restructuring the program</li>
        <li>Archive - abandoned code</li>
        <li>Documentation - operations manual and other documentation</li>
        <li>Program - empty space for executables once they are compiled</li>
        <li>Source - the principal source code files</li>
        <li>SVNTesting - sandbox for training new users on SVN</li>
      </ul>
    </div>
  </li><li>
    <a href=#><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Compiling the Open Rails Project</h2></a>
    <div class="accordion_body">
      <p>
To compile and debug the Open Rails source code, ensure you have the following Microsoft products installed:
      </p>
      <ul>
        <li>Visual C# 2010/12/13, any edition. The 
          <a href="http://www.microsoft.com/en-us/download/details.aspx?id=40787">Express Edition</a> 
         is free (Note: Install this first!)
        </li>
        <li><a href="http://www.microsoft.com/en-us/download/details.aspx?id=39">XNA Game Studio 3.1</a></li>
      </ul>
      <p>
After you have downloaded the code:
      </p>
      <ol>
        <li>Open folder <i>Source</i></li>
        <li>Double click on file <i>ORTS.sln</i> to launch Visual Studio with the Open Rails project open</li>
        <li>From the Visual Studio menu bar, select <i>Build > Rebuild Solution</i></li>
        <li>On the status bar (lower left), wait for the message 'Rebuild All succeeded'</li>
      </ol>
      <p>The executable files have now been re-built and placed in your empty Program folder.</p>
    </div>
  </li><li>
    <a href=#>
      <h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Running the RunActivity.exe code in debug mode</h2>
    </a>
    <div class="accordion_body">
      <p>
Note: When debugging you will bypass the normal start menu and must specify an activity on the command line.
      </p><p>
On the Visual Studio menu bar,
      </p>
      <ol>
        <li>Select <i>View > Solution Explorer</i></li>
        <li>In the <i>Solution Explorer</i> box (on the right), right click on <i>RunActivity > Properties</i></li>
        <li>Use the Debug tab (on the left) to open the Debug Window</li>
        <li>Enter the path of the activity that you want to run: e.g. "c:\personal\msts\routes\lps\activities\ls1.act". 
         Use quotes to cope with spaces in the path.
        </li>
        <li>Press F5 to run RunActivity.exe using your activity.</li>
      </ol>
    </div>
  </li><li>
    <a href=# id="submitting_a_change"><h2 class="accordion_head"><span class="glyphicon glyphicon-play btn-xs"></span> Submitting A Change</h2></a>
    <div class="accordion_body">
      <p>
A change is best packaged as a "patch" file - a file which contains change instructions to alter the current version of Open Rails code to
include your changes. Patches are short and readable. Also, thanks to its smart features, SVN can apply your patch successfully 
even if some of the Open Rails code has been changed by someone else in the meantime.
      </p><p>
If you are offering a fix to a Bug Report, then simply attach your patch file to a post on the <a href="http://launchpad.net/or">Bug Tracker</a> explaining what you have done.
      </p><p>
If you are offering an improvement or a new feature, then attach your file to a post on the Elvas Tower forum <a href="http://www.elvastower.com/forums/index.php?/forum/192-discussion/">Open Rails Discussion</a>. It would be helpful to post a message before
 you start work to give us some idea of your intentions.
      </p><p>
We cannot promise that your changes will make it into the code, but show us what you can do and then we can talk about it.
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
