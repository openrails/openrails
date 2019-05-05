<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1"><!-- adds zooming for mobiles -->
    <meta name="description" content="Open Rails is a free train simulator supporting the world's largest range of digital content.">
    <link rel="shortcut icon" href="/shared/logos/or_logo.png"><!-- This icon appears in the page's tab and its bookmark. -->

<title>Open Rails - Free train simulator project</title>    
    <noscript>
      <style>
        /* To support noscript browsing without JavaScript */
        .dropdown-menu {
          display: block;
        }
      </style>
    </noscript>

<!-- To work with Bootstrap, IEv8 requires respond.min.js which must not load from a CDN. -->
<!--[if lte IE 8]>
  <link href="/shared/bootstrap/3.1.0/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<![endif]-->
<!-- Conditional HTML for IE above v8 and any non-IE browsers -->
<!--[if gt IE 8]><!-->
  <link rel="stylesheet" href="http://netdna.bootstrapcdn.com/bootstrap/3.1.0/css/bootstrap.min.css" type="text/css" />
<!--<![endif]--> 
<link rel="stylesheet" href="/shared/template.css" type="text/css" />
<!-- Put these before <body> to avoid the layout changing when they take effect -->
<!-- To work with HTML5, IEv8 requires html5shiv.min.js. -->
<!-- To work with Bootstrap, IEv8 requires respond.min.js. -->
<!--[if lte IE 8]>
  <script src="/shared/html5shiv.min.js"></script>
  <script src="/shared/respond.min.js"></script>
  <link href="/shared/iev8.css" rel="stylesheet" type="text/css" />
<![endif]-->
    <link rel="stylesheet" href="index.css" type="text/css" />
  </head>

  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
      <div class="row">
        <div class="col-md-9 header">
          <img class="totally_free_software" src="shared/totally_free_software3.png" alt="Totally free software"/>
          <!--<img class="totally_free_software" src="shared/now_at_version_1_0b.png" alt="Now at Version 1.0"/>-->
          <!--<img class="totally_free_software" src="shared/now_at_version_1_1.png" alt="Now at Version 1.1"/>-->
          <!--<img class="totally_free_software" src="shared/now_at_version_1_2.png" alt="Now at Version 1.2"/>-->
          <!-- <img class="totally_free_software" src="shared/now_at_version_1_3.png" alt="Now at Version 1.3"/> -->
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
      <div class="navbar navbar-inverse">
        <div class="navbar-header">
          <button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-responsive-collapse">
            <span class="icon-bar"></span>
            <span class="icon-bar"></span>
            <span class="icon-bar"></span>
          </button>
        </div>
        <div class="navbar-collapse collapse navbar-responsive-collapse">
          <ul class="nav navbar-nav">
<li class = 'active'>              <a href='/'>Home</a>
            </li>
<li class='dropdown'>              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Discover <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/discover/open-rails/">Open Rails</a></li>
                <li><a href="/discover/our-mission/">Our Mission</a></li>
                <li><a href="/discover/our-plans/">Our Plans</a></li>
                <!--<li><a href="/discover/version-1-0/">Version 1.0</a></li>-->
                <!--<li><a href="/discover/version-1-1/">Version 1.1</a></li>-->
                <!-- <li><a href="/discover/version-1-2/">Version 1.2</a></li> -->
                <li><a href="/discover/version-1-3/">Version 1.3</a></li>
                <li><a href="/discover/project-team/">Project Team</a></li>
                <li><a href="/discover/news/">News</a></li>
                <li><a href="/discover/license/">License</a></li>
              </ul>
            </li>
<li class='dropdown'>              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Download <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/download/program/">Program</a></li>
                <li><a href="/download/source/">Source</a></li>
                <li><a href="/download/changes/">Changes</a></li>
                <li><a href="/download/content/">Content</a></li>
              </ul>
            </li>
<li class='dropdown'>              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Learn <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/learn/faq/">FAQ</a></li>
                <li><a href="/learn/manual-and-tutorials/">Manual and Tutorials</a></li>
              </ul>
            </li>
<li class='dropdown'>              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Share <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/share/gallery/">Gallery</a></li>
                <li><a href="/share/community/">Community</a></li>
                <li><a href="/share/multiplayer/">Multi-Player</a></li>
              </ul>
            </li>
<li class='dropdown'>              <a href="#" class="dropdown-toggle" data-toggle="dropdown">Contribute <b class="caret"></b></a>
              <ul class="dropdown-menu">
                <li><a href="/contribute/reporting-bugs/">Reporting Bugs</a></li>
                <li><a href="/contribute/building-models/">Building Models</a></li>
                <li><a href="/contribute/developing-code/">Developing Code</a></li>
                <li><a href="/contribute/joining-the-team/">Joining the Team</a></li>
                <li><a href="/contribute/credits/">Credits</a></li>
              </ul>
            </li>
<li>                <a href="/trade/">Trade</a></li>
<li>                <a href="/contact/">Contact</a></li>
          </ul>
        </div>
      </div>
      <noscript>
        <div class="row">
          <div class="col-md-4"></div>
          <div class="col-md-4">
            <p>&nbsp;</p>
            <p>&nbsp;</p>
            <p>&nbsp;</p>
            <p>&nbsp;</p>
            <p>JavaScript is disabled but this website works best with JavaScript enabled.</p>
          </div>
        </div>
      </noscript>
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
              (18 December 2018, 44MB)			</a>
          </div>
        </div>
      </div>
      <div class="row">
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Key Changes in v1.3</h4>
          </div>
          <p>
            Timetables can join and split trains to form new trains
          </p><p>
            Mouse control for 3D cabs
          </p><p>
            Working transfer tables
          </p><p>
            Activity evaluation
          </p><p>
            Separate files for extensions to activity files
          </p><p>
            Many <a href="/discover/version-1-3/">more additions and improvements</a> are listed here.
          </p>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>News</h4>
          </div>
          <p>
            <strong>Apr 2019</strong>
            <a href="https://www.trainsim.com/vbts/tslib.php?searchid=13577139">ENG files published</a> with accurate physics for 176 USA diesel locos.
          </p>
          <p>
            <strong>Mar 2018</strong>
            Geoff Rowlands found a way to model 3D controls so they can be grabbed by the handle <a href="https://www.youtube.com/watch?v=UO9XrBz3iD0&feature=youtu.be">as in this video</a>.
          </p>
          <p>
            <strong>Nov 2017</strong>
            Open Rails trialled <a href="http://www.monogame.net/">with Monogame instead of XNA</a> uses less RAM and give higher frame rates.
          </p>
          <p>
            <strong>Nov 2017</strong>
            Open Rails trialled <a href="http://www.monogame.net/">with Monogame instead of XNA</a> uses less RAM and give higher frame rates.
          </p>
        </div>
        <div class="col-md-4 divider">
          <div class="heading">
            <h4>Videos</h4>
          </div>
          <div class="headed_content">
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
      <div class="row">
        <div class="col-md-12 footer">
          <div class="col-md-5 footer_column top_left_footer">
            <a href="#" onclick='location.reload(true); return false;' title="Click for another snippet">
              <iframe src="/shared/ohloh.php" seamless height="245" width="380"
              style="margin:-10px 0 0 -10px; border:none;">
              </iframe>
              <span class="glyphicon glyphicon-chevron-right next_snippet"></span>
            </a>
          </div>
          <div class="col-md-3 footer_column">
            <p>
              &copy; 2009-2019 &nbsp; Open Rails
            </p>
            <a href="http://www.gnu.org/licenses/licenses.html" target="_blank">
              <img src='/shared/logos/gplv3_logo.png' alt='GPLv3 logo' style="padding-bottom: 0.5em;"/>
            </a>
            <p>
              You use Open Rails entirely at your own risk. It is intended for entertainment purposes only and is not suitable for professional applications.
            </p>
          </div>
          <div class="col-md-4 footer_column top_right_footer">
            <p>
              Page updated on 02 May 2019 at 19:58            </p>
            <ul>
              <li>
                <a href="http://www.getbootstrap.com/"  target="_blank"
                  title="The most popular front-end framework for developing responsive, mobile-first projects on the web.">
                  <img src='/shared/logos/bootstrap.png' alt='Bootstrap logo'/> &nbsp; Built with Bootstrap
                </a><br />
              </li><li>
                <a href="http://validator.w3.org/"  target="_blank"
                  title="W3C Markup Validation Service">
                  <img src='/shared/logos/valid-html.png' alt='Valid HTML logo'/> &nbsp; Validated with W3C
                </a>
              </li><li>
                <a href="http://website-link-checker.online-domain-tools.com/"  target="_blank"
                  title="Link checking service">
                  Links checked with Online Domain Tools
                </a>
              </li><li>
                <a href="http://www.internetmarketingninjas.com/online-spell-checker.php"  target="_blank"
                  title="Spell checker">
                  Spelling checked with Internet Marketing Ninjas
                </a>
              </li><li>
                <a href="http://tools.pingdom.com/fpt/"  target="_blank"
                  title="Webpage performance tools">
                  Speed checked with Pingdom Tools
                </a>
              </li><li>
                <a href="http://cssminifier.com/"  target="_blank"
                  title="CSS Minifier">
                  CSS minified with CSS Minifier
                </a>
              </li>
              <li>
                <a href='/shared/mysql/get_statistics.php?previous_days=1' target='_blank'>Website Statistics</a>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div><!-- /.container -->
    <!-- Must come before Bootstrap.min.js -->
    <!-- http: protocol omitted so server can choose http or https -->
    <script src="//ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js"></script>
    <script>
      // Work-around to use local file if CDN is blocked - see http://stackoverflow.com/questions/5257923/how-to-load-local-script-files-as-fallback-in-cases-where-cdn-are-blocked-unavai
      if (typeof jQuery == 'undefined'){
        document.write('<script src="/shared/jquery/1.11.0/jquery.min.js">\x3C/script>')
      }
    </script>
    <script src="//netdna.bootstrapcdn.com/bootstrap/3.1.0/js/bootstrap.min.js"></script>
    <script>
      // Work-around to use local file if CDN is blocked.
      if (typeof $.fn.popover == 'undefined') { // popover is Bootstrap-specific
        document.write('<script src="/shared/bootstrap/3.1.0/js/bootstrap.min.js">\x3C/script>');
        // Assume CSS was unavailable too.
        $('<link href="/shared/bootstrap/3.1.0/css/bootstrap.min.css" rel="stylesheet" type="text/css" />').appendTo('head');
        // Reload template.css and index.css to keep files in the right sequence.
        $('<link rel="stylesheet" href="/shared/template.css" rel="stylesheet" type="text/css" />').appendTo('head');
        $('<link rel="stylesheet" href="index.css" rel="stylesheet" type="text/css" />').appendTo('head');
      }
    </script>
    <script>
      (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){
      (i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
      m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)
      })(window,document,'script','//www.google-analytics.com/analytics.js','ga');

      ga('create', 'UA-53007731-1', 'auto');
      ga('send', 'pageview');
      ga('set', 'transport', 'beacon');
    </script>
    <script>
      $(function () {
        $('a[href]').on('click', function () {
          if (this.href.indexOf('://openrails.org/') === -1 && this.href.indexOf('://www.openrails.org/') === -1) {
            ga('send', 'event', 'outbound', 'click', this.href)
          }
        })
      })
    </script>
    <script>
      // Append image to div with class=preload after document finishes loading
      $('<img />')
        .attr('src', '/shared/banners/banner021.jpg')
        .load(function(){
          $('.preload').append( $(this) );
        });
    </script>
          <!-- Modal -->
<div id='modal1' class='modal fade' tabindex='-1' role='dialog' aria-labelledby='myModalLabel' aria-hidden='true'>            <div class="modal-dialog">
              <div class="modal-content">
                <div class="modal-header">
                  <button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>
<h2 class='modal-title'>Download Open Rails</h2>                </div>
                <div class="modal-body">
                  <p>
                    Please note: This Open Rails download does not include any content - no routes, trains, activities - just the
                    simulation program. 
                  </p><p>
                    If you have content suitable for Open Rails or Microsoft Train Simulator already in place, then you can use the
                    Open Rails program to operate those routes and drive those trains straight away.
                  </p><p>
                    If not, then you will need to install some models <a href='http://openrails.org/trade/'>bought from a vendor</a> or 
                    <a href='http://openrails.org/share/community/'>free from the community</a> before you can use Open Rails.
                  </p><p>
                    Or you can try out the 2 self-installing models on <a href="/">our home page</a> - both free of charge.
                  </p><p class="text-right">
<a href='/download/program/confirm.php?file=OpenRails-1.3.1-Setup.exe' class='btn download_button'>Download</a>                    <button type="button" data-dismiss="modal" aria-hidden="true" class="btn btn-default cancel_button">Cancel</button>
                  </p>
                </div><!-- End of Modal body -->
              </div><!-- End of Modal content -->
            </div><!-- End of Modal dialog -->
          </div><!-- End of Modal -->
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