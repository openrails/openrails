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
              &copy; 2009-<?php // Calculate current year as end of copyright range.
$path = dirname(__FILE__);  // path to this include file
echo date('Y', filemtime("$path/../files/OpenRails-Testing-Source.zip"));
?> &nbsp; Open Rails
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
              Page updated on <?php echo date('d M Y \\a\\t H:i', filemtime('index.php')) ?>
            </p>
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
