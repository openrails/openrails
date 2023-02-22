<?php include "../../shared/head.php" ?>
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Learn > Manual and Tutorials</h1>
			</div>
		</div>
		<div class="row">
			<div class="col-md-1">&nbsp;</div>
			<div class="col-md-4">
				<h2>Manual</h2>
				<p>
				  Each download includes a copy of the Operating Manual. 
<?php 
$file_path = '../../files/OpenRails-Testing-Manual.pdf';
echo "For convenience, you can also directly <a href='$file_path'>download the manual</a>";
echo ' (' . date('d F Y', filemtime($file_path)) . ', ' . round(filesize($file_path) / 1024 / 1024) . 'MB).'; 
?>
				</p>
				<h2>Test Environment for Models</h2>
				<p>
        One of our aims for Open Rails is that train performance should be as realistic as possible. 
        To help achieve this,
        Peter Newell has developed <a href="http://www.coalstonewcastle.com.au/physics/">an environment for testing</a> 
        the performance of locos and rolling stock. Do they perform as we expect?
        </p><p>
        It may be the model that is not configured correctly or, as Open Rails develops, it may be that the simulator is lacking 
        in realism.
        </p><p>
        In either case, a neutral, repeatable test environment helps to pin down the issue and get it fixed. 
        </p>
			</div>
			<div class="col-md-2">&nbsp;</div>
			<div class="col-md-4">
				<h2>Tutorials</h2>
        <p>
          The comprehensive "Build your own route" tutorial <a href="/learn/build-route">is introduced here</a>.
        </p>
        <p>
				  We need tutorials for other aspects of Open Rails on:
        </p>
        <ul>
          <li>Driving Trains</li>
          <li>Building Activities and Timetables</li>
          <li>Creating Rolling Stock and Static Objects</li>
        </ul>
        <p>
          such as the tutorials on <a href="http://msts-roundhouse.vnxglobal.com/" target="_blank">Eric Conrad's blog</a>.
        </p><p>
          If you are interested in helping with tutorials, please <a href='../../contact/'>contact us</a>.
        </p>
        <h3>Some other support materials are available:</h3>
        <ul>
          <li>
<?php 
$file_path = '../../files/Keyboard_Layout_DE_V1.3.1e.pdf';
echo "A <a href='$file_path'>guide for German keyboards</a>";
echo ' (' . date('d F Y', filemtime($file_path)) . ', ' . round(filesize($file_path) / 1024) . ' KB)'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/signalling_operational.pdf';
echo "<a href='$file_path'>Signaling - Operational Changes</a> which describes signaling in Open Rails";
echo ' (' . date('d F Y', filemtime($file_path)) . ', ' . round(filesize($file_path) / 1024) . ' KB)'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/ORTS_Trackviewer_manual.pdf';
echo "<a href='$file_path'>Open Rails Trackviewer</a> which maps the track and roads";
echo ' (' . date('d F Y', filemtime($file_path)) . ', ' . round(filesize($file_path) / 1024) . ' KB)'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/OR_Steam Model_03_02_2014.pdf';
echo "<a href='$file_path'>Steam Model</a> describing the physics in Open Rails";
echo ' (' . date('d F Y', filemtime($file_path)) . ', ' . round(filesize($file_path) / 1024) . ' KB)'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/superelevation_v1.pdf';
echo "<a href='$file_path'>Speed Limits on Curves</a>";
echo ' (' . date('d F Y', filemtime($file_path)) . ', ' . round(filesize($file_path) / 1024) . ' KB)'; 
?>
          </li>
        </ul>
			</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
