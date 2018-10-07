<?php include "../../shared/head_notrack.php" ?>
    <link rel="stylesheet" href="../../shared/iframe/iframe.css" type="text/css" />
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
      <div class="row">
        <div class="col-md-12">
          <h1>Discover > Version 1.2.1</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-1"></div>
        <div class="col-md-10">
          <h2>Changes brought in by Open Rails 1.2.1 (since 1.2)</h2>
          <p>
            This update changes the updater to ask for permission if the new version's authenticity certificate is not 100% identical to the current version, which will be necessary for the update to 1.3. We normally manage to keep these changes hidden, but time constraints this time around means that you will be shown the change and asked to approve it.
          </p>
          <p>
            When updating from Open Rails 1.2.1 to 1.3, you will be given details of two certificates. Please check that the details for the "subject of certificate" are as follows and then select "Yes" to recieve the update.
          </p>
          <ul>
            <li>
              Current version's certificate:<br>
              CN=James Galloway Ross<br>
              O=James Galloway Ross<br>
              L=London<br>
              S=Greater London<br>
              C=GB
            </li>
            <li>
              Update version's certificate:<br>
              CN=James Galloway Ross<br>
              O=James Galloway Ross<br>
              L=London<br>
              C=GB
            </li>
          </ul>
          <p>
            Here is what the message will look like:
          </p>
          <p><img src="updater.png"></p>
        </div>
      </div>
    </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
