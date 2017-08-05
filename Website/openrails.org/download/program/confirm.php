<?php include "../../shared/head.php" ?>
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Download > Program</h1>
        <p>&nbsp;</p>
			</div>
		</div>
		<div class="row">
			<div class="col-md-3"></div>
			<div class="col-md-6">
			  <h1>Download Started</h1>
<?php 
$file = $_REQUEST['file'];
echo("<p>Your download of $file will start in a few seconds.</p>");
// Invisible <iframe> to start download as a background task
echo("<iframe src='download.php?file=$file&id=" . $_SESSION['id'] . "' seamless height=1 width=1 style='border:none;'></iframe>");
echo("<p>If your download doesn't start in a few seconds, please try <a href='../../files/$file?direct=1'>this direct link</a>.</p>");
?>
			</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
