<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
<?php
  $download_stable = 'OpenRails-1.3.1-Setup.exe';
  $download_testing = 'OpenRails-Testing.zip';
  $file_path = "../../files";
?>
      <div class="row">
        <div class="col-md-4">
          <h1>Download > Program</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-4">
          <h1>Stable Version 1.3.1 <small>(recommended)</small></h1>
          <br>
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
            <?php echo '08 December 2018, ' . round(filesize("$file_path/$download_stable") / 1024 / 1024) . 'MB'; ?>
          </p>
          <!--<p class="alert alert-info">
            We're working hard on producing the next stable version. Please check back soon.
          </p>-->
        </div>
        <div class="col-md-2"></div>
        <div class="col-md-4">
          <h1>Testing Version</h1>
          <br>
          <!-- Button to trigger modal -->
          <a href="#modal2" role="button" class='btn download_button btn-lg btn-block' data-toggle="modal">
            <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download the executables</h2>
            <p>
              See the installation guides below for the pre-requisites you'll need.
            </p>
          </a>
          <p style="text-align: center;">
            <?php echo date('d F Y', filemtime("$file_path/$download_testing")) . ', ' . round(filesize("$file_path/$download_testing") / 1024 / 1024) . 'MB'; ?>
          </p>
        </div>
      </div>
      <div class="row">
        <div class="col-md-6">
          <h2><span id="installation_questions">Installation Questions </span><small>from the FAQ</small></h2>
          <?php include "../../learn/faq/install.php" ?>
        </div>
        <div class="col-md-6">
          <h2>Recent Code Changes</h2>
          <ul>
            <?php include "../../api/update/testing/changelog.html" ?>
          </ul>
          <p><a href='../changes/'>See more code changes</a></p>
          <h2>Installation Guides</h2>
<ul>
<li><a href="/files/installation_guide_en.pdf">Installation Guide (English)</a></li>
<li><a href="/files/installation_guide_es.pdf">Tutorial de Instalación (Spanish)</a></li>
</ul>
<p>
Note: No installation is necessary and multiple versions of Open Rails can co-exist in different folders.
</p>
<h2>Updater</h2>
<p>
The built-in updater checks this website for new updates once per day. The testing version is updated once per week, usually on Friday around 7pm UK time.
</p>
<h2>Unstable Version</h2>
<p>
To support development, the <a href='http://james-ross.co.uk/projects/or?utm_campaign=unstable-version&utm_source=openrails.org&utm_medium=referral'>latest unstable version</a> is also available, which is updated as and when we <a href='http://james-ross.co.uk/projects/or/log?utm_campaign=unstable-version&utm_source=openrails.org&utm_medium=referral'>make changes to it</a>. <a href='http://james-ross.co.uk/projects/or/builds?utm_campaign=unstable-version&utm_source=openrails.org&utm_medium=referral'>Previous unstable versions are available</a>. The unstable versions are more <strong>likely to contain serious bugs</strong> and are only recommended for users wishing to help with Open Rails development.
</p>
        </div>
      </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
<?php
$modal = 'modal1';
$title = 'Download Open Rails (stable version)';
$download_file = $download_stable;
include "preamble.php";

$modal = 'modal2';
$title = "Download Open Rails (testing version)";
$download_file = $download_testing;
include "preamble.php";
?>
  </body>
</html>
