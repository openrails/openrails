<?php include "shared/head.php" ?>
    <link rel="stylesheet" href="index.css" type="text/css" />
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
      <div class="row">
        <div class="col-md-9 header">
          <!-- <img class="totally_free_software" src="shared/totally_free_software3.png" alt="Totally free software"/> -->
          <!-- <img class="totally_free_software" src="shared/now_at_version_1_0b.png" alt="Now at Version 1.0"/>-->
          <!-- <img class="totally_free_software" src="shared/now_at_version_1_1.png" alt="Now at Version 1.1"/>-->
          <!-- <img class="totally_free_software" src="shared/now_at_version_1_2.png" alt="Now at Version 1.2"/>-->
          <!-- <img class="totally_free_software" src="shared/now_at_version_1_3.png" alt="Now at Version 1.3"/> -->
          <!-- <img class="totally_free_software" src="shared/now_at_version_1_4.png" alt="Now at Version 1.4"/> -->
          <img class="totally_free_software" src="shared/now_at_version_1_5.png" alt="Now at Version 1.5"/>
          <a href="/">
            <img class="logo" src='shared/logos/or_logo.png' alt='logo for Open Rails'/>
          <div class="logo_text">Open Rails</div>
          </a>
        </div>
        <div class="col-md-3 header">
          <div class="since_last_visit">
            <a class="btn btn-default" href="/changed-pages/index.php">Changes since last visit</a><br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;(uses cookies)
          </div>
        </div>
      </div>
<?php include "shared/menu.php" ?>
<?php
  $download_stable = 'OpenRails-1.5.1-Setup.exe';
  $file_path = "files";
?>
      <div class="row">
        <div class="col-md-6">
          <img class="focus_image" src='banner.jpg' title="UP SD70ACe train consist visiting the CN Ruel Subdivision&#xa;(Open Rails route in development)&#xa;posted by TrainSimulations.net" alt="UP SD70ACe train consist visiting the CN Ruel Subdivision&#xa;(Open Rails route in development)&#xa;posted by TrainSimulations.net">
        </div>
        <div class="row preload hidden"></div> <!-- empty div used to hold banner preload -->
        <div class="col-md-6">
          <p>&nbsp;</p>
          <div class="description">
            Open Rails: free train simulator that supports <br />
            the world's largest range of digital content.
          </div>
          <div class="download">
            <!-- Button to trigger modal -->
            <a href="#modal1" role="button" class='btn download_button' data-toggle="modal">
              <span class='glyphicon glyphicon-download'></span>&nbsp; Download the installer
              <?php echo '27 Nov 2022, ' . round(filesize("$file_path/$download_stable") / 1024 / 1024) . 'MB)'; ?>
			</a>
          </div>
        </div>
      </div>
      <div class="row">
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Key Changes in v1.5.1</h4>
          </div>
          <p>
            Container loading and unloading
          </p><p>
           End Of Train (EOT) devices
          </p><p>
            64-bit compatibility lifting the 3GB RAM limit
          </p><p>
            Many <a href="/discover/version-1-5/">more additions and improvements</a> are listed here.
          </p>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>News</h4>
          </div>
          <div style="background-color: #ffffcc; margin: 0 -5px; padding: 0 5px;">
            <p>
              <strong>Jan 2023</strong>
              <a href="https://github.com/cpvirtual/OR_CPV.git">Portugal '79</a> has been published. 
            </p>
            <p>
              New commercial route <a href="http://www.hsb-simulator.com/">HSB Simulator</a> published. 
            </p>
            </div>
          <div style="background-color: #ffffee; margin: 0 -5px; padding: 0 5px;">
            <p>
              <strong>Nov 2022</strong>
              <a href="http://openrails.org/discover/version-1-5/">Version 1.5.1</a> is now available for immediate download <a href="http://openrails.org/download/program/">here</a>.
            </p>
            <p>
              <strong>Summer 2022</strong>
              Three new UK routes are now available for immediate download <a href="http://openrails.org/download/content/">here</a>.
            </p>
            <p>
              <strong>Oct 2021</strong>
              Support for <a href="https://reshade.me/">ReShade</a>
            </p>          
            <p>
              <strong>Oct 2021</strong>
              Map window for timetabled trains
            </p>
            <p>
              <strong>Nov 2020</strong>
              Wagons can now be braked individually as on the oldest railways. This can be experienced in 2 scenarios - <a href="https://www.coalstonewcastle.com.au/physics/stock/#rainhill">Stephenson's Rocket at Rainhill Trials</a> and <a href="https://www.coalstonewcastle.com.au/physics/stock/#select">Langley Vale Timber Tramway</a>. 
            </p>
          </div>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Videos</h4>
          </div>
          <div class="headed_content">
            <h5><a href="https://youtu.be/ZY2_UYvacXg" target="_blank">New features in Release 1.5.1</a></h5>
          </div>
          <p>&nbsp;</p>
          <div>
            <h5>UK New Forest Route</h5>
            <p>
              To accompany his release of the New Forest route, Rick Loader shows
              <a href="https://youtu.be/mk2YQn271A4" target="_blank">
              timetable mode in action on New Forest Route</a>.
            </p>
          </div>
          <p>&nbsp;</p>
          <div>
            <p>
              See more videos with this <a href="https://www.youtube.com/results?search_query=open+rails" target="_blank">YouTube search</a>.
            </p>
          </div>
        </div>
      </div>
<?php include "shared/tail.php" ?>
<?php include "shared/banners/choose_banner.php" ?>
<?php include "shared/banners/preload_next_banner.php" ?>
<?php
$modal = 'modal1';
$title = 'Download Open Rails';
$download_file = $download_stable;
include "download/program/preamble.php";
?>
      <!-- Modal -->
      <style>
        .modal-backdrop.in {
          opacity: 0;
        }
        #modal2 .modal-dialog {
          margin-top: 150px;
        }
        @media (min-width: 992px) {
          #modal2 .modal-dialog {
            margin-top: 335px;
            width: 900px;
          }
        }
        #modal2 .modal-content {
          border: none;
          background: none;
          box-shadow: none;
        }
        #modal2 .user-new > .user-content {
          background-color: rgb(88, 139, 45);
        }
        #modal2 .user-existing > .user-content {
          background-color: rgb(45, 94, 139);
        }
        #modal2 .user-existing2 > .user-content {
          background-color: rgb(118, 45, 139);
        }
        #modal2 .user-content {
          padding: 1px 20px 20px;
        }
        #modal2 .user-content * {
          color: white;
        }
        #modal2 .user-content button {
          opacity: 1;
          font-size: 2.5em;
        }
        #modal2 .user-content h1 {
          font-size: 18px;
          font-weight: normal;
        }
        #modal2 .user-content p {
          margin: 1em 0;
        }
        #modal2 .user-content img {
          width: 100%;
        }
      </style>

      <div id='modal2' class='modal fade' tabindex='-1' role='dialog' aria-labelledby='myModalLabel' aria-hidden='true'>
        <div class="modal-dialog">
          <div class="modal-content row">
            <div class="user-new col-md-6">
              <div class="user-content">
                <button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>
                <h1>New to Open Rails?</h1>
                <h1>This <strong><a href="http://www.zigzag.coalstonewcastle.com.au/route/downloads/" title="Tutorials in this kit will help you to learn the rudimentary controls in Open Rails, how to drive a steam locomotive, operate the air brakes to stop the train, to turn the locomotive on a turntable, to fuel locomotive with water and coal, and also how to shunt cars and wagons">Starter Kit</a></strong> is for you</h1>
                <p>Quickest way to get started with Open Rails</p>
                <p>Download installs both Open Rails v1.3.1 and the Zig Zag Railway route with tutorial activities</p>
                <a href="http://www.zigzag.coalstonewcastle.com.au/route/downloads/" title="Tutorials in this kit will help you to learn the rudimentary controls in Open Rails, how to drive a steam locomotive, operate the air brakes to stop the train, to turn the locomotive on a turntable, to fuel locomotive with water and coal, and also how to shunt cars and wagons"><img src="landing_page_ctn.png"></a>
              </div>
            </div>
            <div class="user-existing col-md-6">
              <div class="user-content">
                <button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>
                <h1>Already using Open Rails?</h1>
                <h1>Try the <strong><a href="http://www.trainsimulations.net/ORTS_starter_pack.html">BNSF Scenic Route</a></strong></h1>
                <p>High quality route donated by vendor TrainSimulations</p>
                <p>This route has been updated for Open Rails to optimise physics and sound</p>
                <a href="http://www.trainsimulations.net/ORTS_starter_pack.html"><img src="landing_page_ts.png"></a>
              </div>
            </div>
            <!-- <div class="user-existing2 col-md-6">
              <div class="user-content">
                <button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>
                <h1>Already using Open Rails?</h1>
                <h1>Try the <strong><a href="http://www.siskurail.org" title="The Central Oregon and Pacific Railroad operates between Northern California and Eugene, Oregon, USA.">Siskiyou Route</a></strong></h1>
                <p>This extensive route, created by Dale Rickert, has over 300 miles of mainline track passing through 31 cities and comes with nearly 150 locos and 350 wagons.</p>
                <a href="http://www.siskurail.org" title="The Central Oregon and Pacific Railroad operates between Northern California and Eugene, Oregon, USA."><img src="landing_page_dr.png"></a>
              </div>
            </div>-->
          </div><!-- End of Modal content -->
        </div><!-- End of Modal dialog -->
      </div><!-- End of Modal -->
    </div>
    <!-- pop up modal2 -->
    <script type="text/javascript">
      $(window).on('load', function () {
        $('#modal2').modal('show')
      })
    </script>
  </body>
</html>