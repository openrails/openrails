<?php 
$cwd = getcwd();

function ends_with($string, $end) { // From http://stackoverflow.com/questions/619610/whats-the-most-efficient-test-of-whether-a-php-string-ends-with-another-string
  if (strlen($end) > strlen($string)) return false;
  return substr_compare($string, $end, -strlen($end)) === 0;
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
<?php if (ends_with($cwd, "dev")) { echo "<li class = 'active'>"; }else{ echo "<li>"; } ?>
						  <a href='/'>Home</a>
            </li>
<?php 
if (ends_with($cwd, "open_rails_project") 
|| ends_with($cwd, "our_mission")
|| ends_with($cwd, "our_plans")
|| ends_with($cwd, "project_team")
|| ends_with($cwd, "or_news")
|| ends_with($cwd, "or_license")) 
  { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; }
?>              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Discover <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/discover/open-rails/">Open Rails</a></li>
                <li><a href="/discover/our-mission/">Our Mission</a></li>
                <li><a href="/discover/our-plans/">Our Plans</a></li>
                <li><a href="/discover/project-team/">Project Team</a></li>
                <li><a href="/discover/news/">News</a></li>
                <li><a href="/discover/license/">License</a></li>
              </ul>
            </li>
<?php 
if (ends_with($cwd, "program") 
|| ends_with($cwd, "source")
|| ends_with($cwd, "changes")
|| ends_with($cwd, "content")) 
  { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; }
?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Download <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/download/program/">Program</a></li>
                <li><a href="/download/source/">Source</a></li>
                <li><a href="/download/changes/">Code Changes</a></li>
                <li><a href="/download/content/">Content</a></li>
              </ul>
            </li>
<?php 
if (ends_with($cwd, "faq") 
|| ends_with($cwd, "manual")) 
  { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; }
?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Learn <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/learn/faq/">FAQ</a></li>
                <li><a href="/learn/manual-and-tutorials/">Manual and Tutorials</a></li>
              </ul>
            </li>
<?php 
if (ends_with($cwd, "community")
|| ends_with($cwd, "gallery")
|| ends_with($cwd, "multiplayer")) 
  { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; }
?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Share <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/share/gallery/">Gallery</a></li>
                <li><a href="/share/community/">Community</a></li>
                <li><a href="/share/multiplayer/">Multi-Player</a></li>
              </ul>
            </li>
<?php 
if (ends_with($cwd, "reporting_bugs") 
|| ends_with($cwd, "building_models") 
|| ends_with($cwd, "developing_code") 
|| ends_with($cwd, "joining_the_team")
|| ends_with($cwd, "credits")) 
  { echo "<li class='active dropdown'>"; }else{ echo "<li class='dropdown'>"; }
?>
              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Contribute <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/contribute/reporting-bugs/">Reporting Bugs</a></li>
                <li><a href="/contribute/building-models/">Building Models</a></li>
                <li><a href="/contribute/developing-code/">Developing Code</a></li>
                <li><a href="/contribute/joining-the-team/">Joining the Team</a></li>
                <li><a href="/contribute/credits/">Credits</a></li>
              </ul>
            </li>
<?php if (ends_with($cwd, "trade")) { echo "<li class = 'active'>"; }else{ echo "<li>"; } ?>
                <a href="/trade/<?php echo '?', htmlspecialchars(SID); ?>">Trade</a></li>
<?php if (ends_with($cwd, "contact")) { echo "<li class = 'active'>"; }else{ echo "<li>"; } ?>
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
    

