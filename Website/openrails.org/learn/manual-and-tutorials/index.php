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
$file_path = '../../files/Manual.pdf';
echo "For convenience, you can also directly <a href='$file_path'>download the manual</a> - dated ";
echo date('d-M-Y', filemtime($file_path)) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize($file_path) / 1024) . ' KB'; 
?>
				</p>
				<h2>On-Line Manual Replacement</h2>
				<p>
          The conventional manual (above) is being replaced by an <a href="http://www.openrails.coalstonewcastle.com.au/doku.php">on-line "wiki" version</a> which is accessible from any browser
          and includes good search facilities.          
				</p><p>
          Being more convenient for the document editors than the old manual, we hope that the new version will quickly become a
          complete and entirely accurate resource. 
        </p>
			</div>
			<div class="col-md-2">&nbsp;</div>
			<div class="col-md-4">
				<h2>Tutorials</h2>
				<p>
				  We need tutorials for all aspects of Open Rails on:
        </p>
        <ul>
          <li>Driving Trains</li>
          <li>Building Activities and Timetables</li>
          <li>Creating Rolling Stock and Static Objects</li>
          <li>Building Routes</li>
        </ul>
        <p>
          such as the tutorials on <a href="http://msts-roundhouse.nazuka.net" target="_blank">Eric Conrad's blog</a>.
        </p><p>
          If you are interested in helping with tutorials, please <a href='../../contact/'>contact us</a>.
        </p>
        <h3>Some other support materials are available:</h3>
        <ul>
          <li>
<?php 
$file_path = '../../files/Keyboard_Layout_DE_V1.3.1e.pdf';
echo "A <a href='$file_path'>keyboard guide</a> for German keyboards - dated ";
echo date('d-M-Y', filemtime($file_path)) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize($file_path) / 1024) . ' KB'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/signalling_operational.pdf';
echo "<a href='$file_path'>signaling - Operational Changes</a> which describes signaling in OR - dated ";
echo date('d-M-Y', filemtime($file_path)) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize($file_path) / 1024) . ' KB'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/ORTS_Trackviewer_manual.pdf';
echo "<a href='$file_path'>OR Trackviewer</a> which maps the track and roads - dated ";
echo date('d-M-Y', filemtime($file_path)) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize($file_path) / 1024) . ' KB'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/OR_Steam Model_03_02_2014.pdf';
echo "<a href='$file_path'>Steam Model</a> describing the physics OR uses - dated ";
echo date('d-M-Y', filemtime($file_path)) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize($file_path) / 1024) . ' KB'; 
?>
          </li>
          <li>
<?php 
$file_path = '../../files/superelevation_v1.pdf';
echo "<a href='$file_path'>Speed Limits on Curves</a> - dated ";
echo date('d-M-Y', filemtime($file_path)) . '&nbsp; &nbsp; &nbsp; Size: ' . round(filesize($file_path) / 1024) . ' KB'; 
?>
          </li>
        </ul>
			</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
