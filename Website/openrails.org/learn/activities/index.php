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
          <h3>Developing Activities - 1</h3>
          <p>
            A great place to start would be the free Siskiyou Route from the USA. The creator, Dale Rickert, has assembled <a href="http://www.siskurail.org/news.php">a package of instructions, examples
            and even videos</a> to show how to create both simple and demanding activities.
          </p><p align="center">
            <img id="guide" src="guideexample.jpg"/>
          </p><p>
            The quality of these diagrams reflects the effort that Dale has put into creating this resource.
          </p><p>
            This resource also features building shunting operations for AI trains, which makes for some fascinating activities. 
          </p>
          <h3>Developing Activities - 2</h3>
          <p>
            Peter Murdock and Dan Reid have collaborated over a set of activities with a tutorial to help people "get started" with shunting activities 
            in Open Rails. 
          </p><p>
            The activities are based on the free BNSF Scenic Route currently being offered by TrainSimulations (<a href="/">see our homepage</a>). 
            There's a version for the route prior to 20-Oct-2020 and another for the more recent route. You can find them by searching the 
            "Open Rails - Open Rails Activities" section of the <a href="https://www.trainsim.com/vbts/tslib.php?do=displaysearch">TrainSim.com Library</a>
            or download from these links: 
            <ul>
              <li>Pre 20-Oct-2020 <a href="https://www.trainsim.com/vbts/tslib.php?do=copyright&fid=36048">open_rails_starter_tutorial_1_0x.zip</a></li>
              <li>and <a href="https://www.trainsim.com/vbts/tslib.php?do=copyright&fid=36050">open_rails_starter_tutorial_1_1.zip</a></li>
            </ul>
            <ul>
              <li>Post 20-Oct-2020 <a href="https://www.trainsim.com/vbts/tslib.php?do=copyright&fid=36266">open_rails_starter_tutorial_2.zip</a></li>
              <li>Also <a href="https://www.trainsim.com/vbts/tslib.php?do=copyright&fid=36326">bnsf_scenic_atk8_11.zip</a> - Amtrak train meets 6 freight trains</li>
              <li>and <a href="https://www.trainsim.com/vbts/tslib.php?do=copyright&fid=36293">ortsscenicsubtimetable.zip</a> - 14 drivable trains, 24-hour timetable</li>
            </ul>
          </p>
          <h3>Developing Activities - 3</h3>
          <p>
            Peter Newell has provided <a href="http://www.coalstonewcastle.com.au/physics/demo-activity/">13 simple activities</a> 
            each designed to showcase an advanced feature of Open Rails. 
            Use them as well for testing your stock by replacing his consists with your own.
          </p><p>
            The topics include refilling and refuelling, visual and lighting effects, couplers, bearing temperature and braking.
          </p>
        <div class="col-md-2">&nbsp;</div>
      </div>
<?php include "../../shared/tail.php" ?>
<?php include "../../shared/banners/preload_next_banner.php" ?>
  </body>
</html>
