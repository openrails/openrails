<?php
$cwd = str_replace('\\', '/', getcwd()) . '/';
function in_directory($cwd, $name) {
	return strstr($cwd, "/$name/");
}
?>
      <div class="navbar navbar-inverse">
        <div class="navbar-header">
          <button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-responsive-collapse">
            <span class="icon-bar"></span>
            <span class="icon-bar"></span>
            <span class="icon-bar"></span>
          </button>
        </div>
        <div class="navbar-collapse collapse navbar-responsive-collapse">
          <ul class="nav navbar-nav">
<?php if (!in_directory($cwd, "discover")
 && !in_directory($cwd, "download")
 && !in_directory($cwd, "learn")
 && !in_directory($cwd, "share")
 && !in_directory($cwd, "contribute")
 && !in_directory($cwd, "trade")
 && !in_directory($cwd, "contact")) { echo "<li class = 'active'>"; }else{ echo "<li>"; } ?>
              <a href='/'>Home</a>
            </li>
<?php if (in_directory($cwd, "discover")) { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; } ?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Discover <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/discover/open-rails/">Open Rails</a></li>
                <li><a href="/discover/our-mission/">Mission</a></li>
                <li><a href="/discover/roadmap/">Roadmap</a></li>
                <!-- <li><a href="/discover/our-plans/">Our Plans</a></li> -->
                <!--<li><a href="/discover/version-1-0/">Version 1.0</a></li>-->
                <!--<li><a href="/discover/version-1-1/">Version 1.1</a></li>-->
                <!-- <li><a href="/discover/version-1-2/">Version 1.2</a></li> -->
                <!-- <li><a href="/discover/version-1-3/">Version 1.3</a></li> -->
                <!-- <li><a href="/discover/version-1-4/">Version 1.4</a></li> -->
                <li><a href="/discover/version-1-5/">Version 1.5.1</a></li>
                <li><a href="/discover/project-team/">Project Team</a></li>
                <li><a href="/discover/news/">News</a></li>
                <li><a href="/discover/license/">License</a></li>
              </ul>
            </li>
<?php if (in_directory($cwd, "download")) { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; } ?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Download <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/download/program/">Program</a></li>
                <li><a href="/download/versions/">Versions</a></li>
                <li><a href="/download/source/">Source</a></li>
                <li><a href="/download/changes/">Changes</a></li>
                <li><a href="/download/content/">Content</a></li>
              </ul>
            </li>
<?php if (in_directory($cwd, "learn")) { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; } ?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Learn <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/learn/faq/">FAQ</a></li>
                <li><a href="/learn/manual-and-tutorials/">Manual and Tutorials</a></li>
                <li><a href="/learn/physics/">OR Physics</a></li>
                <li><a href="/learn/activities/">Activities</a></li>
              </ul>
            </li>
<?php if (in_directory($cwd, "share")) { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; } ?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Share <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/share/community/">Community</a></li>
                <li><a href="/share/multiplayer/">Multi-Player</a></li>
              </ul>
            </li>
<?php if (in_directory($cwd, "contribute")) { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; } ?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Contribute <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/contribute/reporting-bugs/">Reporting Bugs</a></li>
                <li><a href="/contribute/building-models/">Building Models</a></li>
                <li><a href="/contribute/developing-code/">Developing Code</a></li>
                <li><a href="/contribute/team-policies/">Team Policies</a></li>
                <li><a href="/contribute/joining-the-team/">Joining the Team</a></li>
                <li><a href="/contribute/credits/">Credits</a></li>
              </ul>
            </li>
<?php if (in_directory($cwd, "trade")) { echo "<li class='active'>"; }else{ echo "<li>"; } ?>
                <a href="/trade/">Trade</a></li>
<?php if (in_directory($cwd, "contact")) { echo "<li class='active'>"; }else{ echo "<li>"; } ?>
                <a href="/contact/">Contact</a></li>
          </ul>
        </div>
      </div>
      <noscript>
        <div class="row">
          <div class="col-md-4"></div>
          <div class="col-md-4">
            <p>&nbsp;</p>
            <p>&nbsp;</p>
            <p>&nbsp;</p>
            <p>&nbsp;</p>
            <p>JavaScript is disabled but this website works best with JavaScript enabled.</p>
          </div>
        </div>
      </noscript>
