<?php include "../shared/head.php" ?>
    <link rel="stylesheet" href="index.css" type="text/css" />
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../shared/banners/choose_banner.php" ?>
<?php include "../shared/banners/show_banner.php" ?>
<?php include "../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Changed Pages</h1>
			</div>
		</div>
		<div class="row">
			<div class="col-md-1"></div>
			<div class="col-md-8">
<h2>The webpages which have changed since your last visit</h2>
<p>
This website uses a cookie to recognise that you're returning for another visit. We record the date when you visit each of our webpages
and compare that with the date when the webpages are <a style="color: #333; text-decoration: none !important;" href="../shared/mysql/collect_webpage_dates.php" target="_blank">updated</a>.
</p>
<div id="changed_pages_list">
<?php include "get_changed_pages.php" ?>
</div>
			</div>
		</div>
<?php include "../shared/tail.php" ?>
<?php include "../shared/banners/preload_next_banner.php" ?>
  </body>
</html>