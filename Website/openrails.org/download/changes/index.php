<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Download > Code Changes</h1>
			</div>
		</div>
 		<div class="row">
			<div class="col-md-12">

				<p>
					This is a list of all the code changes included in the latest Supporters' Download. Those since the previous Supporters' Download are <span style="color: blue;">highlighted</span>.
				</p>

				<ul>
					<?php include "../../scripts/experimental_changelog.html" ?>
				</ul>

			</div>
		</div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
