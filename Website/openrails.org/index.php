<?php include "shared/head.php" ?>
    <link rel="stylesheet" href="index.css" type="text/css" />
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
      <div class="row">
        <div class="col-md-9 header">
          <img class="totally_free_software" src="shared/totally_free_software3.png" alt="Totally free software"/>
          <!--<img class="totally_free_software" src="shared/now_at_version_1_0b.png" alt="Now at Version 1.0"/>-->
          <!--<img class="totally_free_software" src="shared/now_at_version_1_1.png" alt="Now at Version 1.1"/>-->
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
  $download_stable = 'OpenRails-1.1.1-Setup.exe';
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
            Open Rails: free train simulator that supports <br />
            the world's largest range of digital content.
          </div>
          <div class="download">
            <!-- Button to trigger modal -->
            <a href="#modal1" role="button" class='btn download_button' data-toggle="modal">
              <span class='glyphicon glyphicon-download'></span>&nbsp; Download the installer
              <?php echo '(' . date('d F Y', filemtime("$file_path/$download_stable")) . ', ' . round(filesize("$file_path/$download_stable") / 1024 / 1024) . 'MB)'; ?>
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
            <strong>Multiple languages</strong> are available for all text and menus.
          </p><p>
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
            <strong>Mar 2016</strong>
            <a href="/discover/version-1-1/">Open Rails 1.1</a> released! <a href="/download/program/">Download it here</a>.
          </p>
          <p>
            <strong>Dec 2015</strong>
            The Elvas Tower forum plays a major role in developing Open Rails but has been closed to non-members following a dispute.
            We can now report that some of the <a href="http://www.elvastower.com/forums/">Open Rails sub-forums</a>
            are open again.
          </p>
          <p>
            <strong>Jun 2015</strong>
            The Australian <a href="http://www.zigzag.coalstonewcastle.com.au/">Great Zig Zag Railway</a> released for Open Rails v1.0
          </p>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Videos</h4>
          </div>
          <div class="headed_content">
            <h5>Demo Model 1</h5>
            <p>
            <a href="https://www.youtube.com/watch?v=aZ5aVEvbOOE&feature=youtu.be" target="_blank">This video</a> records a player driving the
            first <a href="/download/content">Open Rails demonstration route</a>
            and providing a voice-over commentary.
            </p>
            <h5>Video Review <a href="http://www.attherailyard.com" target="_blank">At The Railyard</a></h5>
            <p>
              In his Series 5, Nicholas Ozorak publishes
              <a href="http://www.attherailyard.com/seasonfiveepisodes.htm#openrails" target="_blank">
                a review of the fictional Full Bucket Line running in Open Rails</a>.
            </p><p>
              See more with this <a href="https://www.youtube.com/results?search_query=open+rails" target="_blank">YouTube search</a>.
            </p>
            <h5>Open Rails Version 0.9</h5>
            <p>
              <a href="https://www.youtube.com/watch?v=G73ktiCNKRs" target="_blank">Improvements arriving in v0.9 of Open Rails</a> are demonstrated by one
              of the project team.
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
  </body>
</html>