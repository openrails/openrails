<?php include "../../shared/head.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
    <style>img { margin: 20px 0 20px 0; border: 2px solid #ccc; }</style>
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
<?php
  $download_stable = 'OpenRails-1.4-Setup.exe';
  $download_testing = 'OpenRails-Testing.zip';
  $file_path = "../../files";
?>
      <div class="row">
        <div class="col-md-12">
          <h1>Download > Versions</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-2">&nbsp;</div>
        <div class="col-md-8">
          <h2>Three Versions of Open Rails</h2>
          <p>
            Three versions of Open Rails are readily available to users, as shown below:
          </p>
          <img src="3_versions.png" width=617/>
          <p>
            For first-time users, we recommend the <a href="../program">Stable Version</a> which comes with an installer.
          </p>

          <h2>Updater</h2>
          <p>	
            Whichever version you choose, Open Rails has a mechanism to notify you of new versions and to update Open Rails for you.
            You will find the settings for this mechanism in <i>Menu > Options > Update</i>
          </p>
          <img src="updater_tab.png" width=617/>
          <p>
            Open Rails will check for updates at most once a day.
            If an update is found, then you can install the update just by clicking on the link in the top, right corner:
          </p>
          <img src="update_link.png" width=617/>

          <h2>Testing Version</h2>
          <p>	
            If you follow the Open Rails project on the forums, then you will hear about bug-fixes and new features.
            These are included in the Unstable Version for developers and testers to try out.
            Once they have been checked and approved, they are published (on Friday) as the latest Testing Version.
            Any user can easily update to the current weekly Testing Version and benefit from these improvements.
          </p>

          <h3>Recent Code Changes</h3>
          <ul>
            <?php include "../../api/update/testing/changelog.html" ?>
          </ul>
          <p><a href='../changes/'>See more code changes</a></p>
	        <p>
            The current Testing Version can also be downloaded as a Zip archive:
          </p>
          <br>
        </div>
      </div>
      <div class="row">
        <div class="col-md-4">&nbsp;</div>
        <div class="col-md-4">
          <!-- Button to trigger modal -->
          <a href="#modal2" role="button" class='btn download_button btn-lg btn-block' data-toggle="modal">
            <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download executables for the Testing Version</h2>
            <p>
              See the installation guides below for the pre-requisites you'll need.
            </p>
          </a>
        </div>
      </div>
      <div class="row">
        <div class="col-md-2">&nbsp;</div>
        <div class="col-md-8">
          <p style="text-align: center;">
            <?php echo date('d F Y', filemtime("$file_path/$download_testing")) . ', ' . round(filesize("$file_path/$download_testing") / 1024 / 1024) . 'MB'; ?>
          </p>
          <ul>
            <li><a href="/files/installation_guide_en.pdf">Installation Guide (English)</a></li>
            <li><a href="/files/installation_guide_es.pdf">Tutorial de Instalación (Spanish)</a></li>
          </ul>
          <p>
            Note: Multiple versions of Open Rails will not interfere if they are saved to different folders.
          </p>

          <h2>Unstable Version</h2>
          <p>
            To support development, the latest unstable version is also available. 
            This is updated whenever developers submit a change for consideration.
            This and previous unstable versions are available as 
            <a href="https://james-ross.co.uk/projects/or/log?utm_campaign=unstable-version&utm_source=openrails.org" target="_blank">a change log</a> 
            and <a href="https://james-ross.co.uk/projects/or/builds" target="_blank">individual downloads</a>. 
            The unstable versions are more <strong>likely to contain serious bugs</strong> 
            and are only recommended for users wishing to help with Open Rails development.
          </p>
          <h3>Improvements</h3>
          <p>
            Improvements to Open Rails are drawn from several public sources as shown here:
          </p>
          <img src="improvements.png" width=617/>
          <p>
            We try to make sure that these changes all work and fit together by reviews as marked in orange in the diagram.
          </p>
          <br/>
          <br/>

        </div>
      </div>
    </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
<?php
$modal = 'modal2';
$title = "Download Open Rails (testing version)";
$download_file = $download_testing;
include "../program/preamble.php";
?>
  </body>
</html>
