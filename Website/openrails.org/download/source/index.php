<?php include "../../shared/head.php" ?>
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
<?php
  $source_stable = 'OpenRails-1.5.1-Source.zip';
  $source_testing = 'OpenRails-Testing-Source.zip';
  $file_path = "../../files";
?>
      <div class="row">
        <div class="col-md-12">
          <h1>Download > Source</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-4">
          <h1>Stable Version 1.5.1</h1>
          <br>
          <a href='<?php echo "$file_path/$source_stable" ?>' class='btn download_button btn-lg btn-block'>
            <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download the source code</h2>
            <p>
              This is the source code for the Stable Version.
            </p>
          </a>
          <p style="text-align: center;">
          <?php echo '27 Nov 2022, ' . round(filesize("$file_path/$source_stable") / 1024 / 1024) . 'MB'; ?>
          </p>
        </div>
        <div class="col-md-2"></div>
        <div class="col-md-4">
          <h1>Testing Version</h1>
          <br>
          <a href='<?php echo "$file_path/$source_testing" ?>' class='btn download_button btn-lg btn-block'>
            <h2><span class='glyphicon glyphicon-download'></span> &nbsp; Download the source code</h2>
            <p>
              This is the source code for the Testing Version.
            </p>
          </a>
          <p style="text-align: center;">
            <?php echo date('d F Y', filemtime("$file_path/$source_testing")) . ', ' . round(filesize("$file_path/$source_testing") / 1024 / 1024) . 'MB'; ?>
          </p>
        </div>
      </div>
      <div class="row">
        <div class="col-md-3"></div>
        <div class="col-md-6">
<h2>Inspecting The Source</h2>
<p>
Open Rails's source code is organised as a Visual Studio solution - see <a href="http://openrails.org/contribute/developing-code/#compiling">Compiling The Open Rails Project</a> for free and commercial Visual Studio products.
</p>
<h2>License Requirements</h2>
<p>
One of the ways that the <a href="../../discover/license/">Free Software License (GPL)</a> protects our rights is by requiring everyone who distributes software under the GPL to make the source available also. That way, everyone who uses the software can examine it and customize it if they wish.
</p>
<h2>Git Software Versioning Control System</h2>
<p>
The entire history of project development is <a href="https://github.com/openrails/openrails">archived in our public Git repository</a>, so visitors are able to extract not just the current version but all the previous ones too back to version 1 in December 2009.
</p><p>
For access details, see <a href="../../contribute/developing-code/">Contribute > Code</a>.
</p>
        </div>
      </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>