<?php include "shared/head.php" ?>
    <link rel="stylesheet" href="index.css" type="text/css" />
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
      <div class="row">
        <div class="col-md-9 header">
          <img class="totally_free_software" src="totally_free_software3.png" alt="Totally free software"/>
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
  $download_stable = 'OpenRailsSetup.exe';
  $file_path = "files";
?>
      <div class="row">
        <div class="col-md-6">
          <img class="focus_image" src='shared/banners/banner058.jpg' title="UP7964 leads 'K' Liner on LSRC (Michigan)&#xa;posted by ATW" alt="UP7964 leads 'K' Liner on LSRC (Michigan)&#xa;posted by ATW">
        </div>
        <div class="row preload hidden"></div> <!-- empty div used to hold banner preload -->
        <div class="col-md-6">
          <p>&nbsp;</p>
          <div class="description">
            Open Rails is a train simulator that supports <br />
            the world's largest range of digital content. 
          </div>
          <div class="download">
            <!-- Button to trigger modal -->
            <a href="#modal1" role="button" class='btn download_button' data-toggle="modal">
              <span class='glyphicon glyphicon-download'></span>&nbsp; Download the installer 
              <?php echo(date('d-M-Y', filemtime("$file_path/$download_stable"))); ?>
            </a>
          </div>
        </div>
      </div>
      <div class="row">
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Features</h4>
          </div>
          <p>
            <strong>Accurate behaviour</strong> for steam, diesel and electric traction including trains with multiple locos.
          </p><p>
            <strong>Signals</strong> which correctly protect the train and permit complex timetabled operations.
          </p><p>
            <strong>Multi-user mode</strong> in which any timetabled train can be driven in person or by computer.
					</p>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>News</h4>
          </div>
          <p>
            <strong>Dec 2014</strong>
            <a href="http://www.dekosoft.com">Dekosoft Trains</a> has added locos exclusively for Open Rails to its range.
            These are GP30 diesels taking advantage of our 3D cab feature.
          </p>
          <p>
            <strong>July 2014</strong>
            The legacy graphics-heavy web site has been replaced by one based on Bootstrap which is both easier to maintain and
            suitable for phones and tablets as well as PCs.
          </p><p>
          You can still <a href="/web1/index.html">see an archive of the old site</a>.
          </p><p>
            <strong>April 2014</strong>
            &#8212; An installer is now available, so Open Rails and its pre-requisites such as XNA can be delivered in a single download. 
          </p>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Videos</h4>
          </div>
          <div class="headed_content">
            <h5>Open Rails Version 0.9</h5>
            <p>
              <a href="https://www.youtube.com/watch?v=G73ktiCNKRs" target="_blank">Improvements arriving in v0.9 of Open Rails</a> are demonstrated by one 
              of the project team.
            </p>
            <h5>Video Review <a href="http://www.attherailyard.com" target="_blank">At The Railyard</a></h5>
            <p>
              In his Series 5, Nicholas Ozorak publishes 
              <a href="http://www.attherailyard.com/seasonfiveepisodes.htm#openrails" target="_blank">
                a review of the fictional Full Bucket Line running in Open Rails</a>.
            </p><p>
              See more with this <a href="https://www.youtube.com/results?search_query=open+rails" target="_blank">YouTube search</a>.
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
$ext = 'exe';
include "download/program/preamble.php";
?>
  </body>
</html>