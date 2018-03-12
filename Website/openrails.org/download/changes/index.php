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
          <h1>Download > Changes</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-12">
          <p>
            This is a list of all the code changes since the last stable version. Those since the previous testing version are <span class="text-primary">highlighted</span>.
          </p>
          <p>
            <strong>For users on X4081 and earlier:</strong> Due to an issue with the updater, you will need to <a href="../program/confirm.php?file=OpenRails-Testing.zip">manually download the latest version</a> once before you can continue updating within the application.
          </p>
          <ul class="revisions">
            <?php include "../../api/update/testing/changelog_stable.html" ?>
          </ul>
        </div>
      </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
