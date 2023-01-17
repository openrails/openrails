<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
<?php
  $download_stable = 'OpenRails-1.5.1-Setup.exe';
  $download_testing = 'OpenRails-Testing.zip';
  $file_path = "../../files";
?>
      <div class="row">
        <div class="col-md-4">
          <h1>Download > Program</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-2">&nbsp;</div>
        <div class="col-md-8">
          <h1>Stable Version 1.5.1 <small>(recommended)</small></h1>
          <br>
        </div>
      </div>
      <div class="row">
        <div class="col-md-4">&nbsp;</div>
        <div class="col-md-4">
          <!-- Button to trigger modal -->
          <a href="#modal1" role="button" class='btn download_button btn-lg btn-block' data-toggle="modal">
            <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download the installer</h2>
            <p>
              This installer provides all pre-requisites for Open Rails and an uninstaller.
            </p>
          </a>
          <p style="text-align: center;">
            <!-- Cannot set modification date to correct value so write it literally -->
            <!-- <?php echo date('d F Y', filemtime("$file_path/$download_stable")) . ', ' . round(filesize("$file_path/$download_stable") / 1024 / 1024) . 'MB'; ?> -->
            <?php echo '27 Nov 2022, ' . round(filesize("$file_path/$download_stable") / 1024 / 1024) . 'MB'; ?>
          </p>
          <!--<p class="alert alert-info">
            We're working hard on producing the next stable version. Please check back soon.
          </p>-->
        </div>
      </div>
      <div class="row">
        <div class="col-md-2">&nbsp;</div>
        <div class="col-md-8">
          <h3>Other versions</h3>
          <p><a href="/download/versions/">Two other versions</a> are available.</p>

          <h2><span id="installation_questions">Installation Questions </span><small>from the FAQ</small></h2>
          <?php include "../../learn/faq/install.php" ?>
        </div>
      </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
<?php
$modal = 'modal1';
$title = 'Download Open Rails (stable version)';
$download_file = $download_stable;
include "preamble.php";
?>
  </body>
</html>
