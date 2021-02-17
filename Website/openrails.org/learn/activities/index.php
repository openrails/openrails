<?php include "../../shared/head.php" ?>
    <style>
      img#guide {
        padding: 2px;
        margin: 0 10px;
        background-color: darkgray;
        transition: transform .2s; /* Animation */
        width: 200px;
      }
      img#guide:hover { transform: scale(3.0); }
    </style>
  </head>
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../../shared/banners/choose_banner.php" ?>
<?php include "../../shared/banners/show_banner.php" ?>
<?php include "../../shared/menu.php" ?>
      <div class="row">
        <div class="col-md-4">
          <h1>Learn > Activities</h1>
        </div>
      </div>
      <div class="row">
        <div class="col-md-2">&nbsp;</div>
        <div class="col-md-8">
          <h2>Activities in Open Rails</h2>
          <p>
            Activities and timetables populate the tracks with trains and stock which your train must work with.
            Well-designed activities challenge you to operate your train effectively and evaluate your performance.
            Activities are also useful in providing tutorials to help you get the most out of Open Rails.
            See Australia's Zig-Zag Railway for <a href="http://www.zigzag.coalstonewcastle.com.au/">some very effective steam-based tutorials</a>.  
          </p>
          <h3>Developing Activities</h3>
          <p>
            A great place to start would be the free Siskiyou Route from the USA. The creator, Dale Rickert, has assembled <a href="http://www.siskurail.org/news.php">a package of instructions, examples
            and even videos</a> to show how to create both simple and demanding activities.
          </p>
          <p align="center">
            <img id="guide" src="guideexample.jpg"/>
          </p>
          <p>
            The quality of these diagrams reflects the effort that Dale has put into creating this resource.
          </p>
        <div class="col-md-2">&nbsp;</div>
      </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
