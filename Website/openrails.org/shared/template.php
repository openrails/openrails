<?php session_start(); ?>
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1"><!-- adds zooming for mobiles -->
    <link rel="shortcut icon" href="http://getbootstrap.com/assets/ico/favicon.ico"><!-- This icon appears in the page's tab and its bookmark. -->

    <title>Open Rails - Free train simulator project</title>
    <!-- Bootstrap core CSS - Latest compiled and minified CSS -->
    <!-- Note: "http:" only needed if not using a webserver. --> 
    <link rel="stylesheet" href="http://netdna.bootstrapcdn.com/bootstrap/3.1.0/css/bootstrap.min.css">
    
    <!-- Optional theme -->
    <link rel="stylesheet" href="http://netdna.bootstrapcdn.com/bootstrap/3.1.0/css/bootstrap-theme.min.css">

    <!-- Custom styles for this template -->
    <link rel="stylesheet" href="/shared/template.css">
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
			<div class="row">
				<div class="col-md-12">
				<img class="banner" src='/shared/union_pacific2.jpg'>
		    <a href="/shared/home/index.php">
          <img class="logo" src='/shared/logos/or_logo.png'/>
		      <div class="logo_text">Open Rails</div>
			</a>
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
			  <li class="active"><a href="localhost/home/index.php">Home</a></li>
			  <li class="dropdown">
				<a href="#" class="dropdown-toggle" data-toggle="dropdown">Discover <b class="caret"></b></a>
				<ul class="dropdown-menu">
				  <li><a href="#">Open Rails Project</a></li>
				  <li><a href="#">Our Mission</a></li>
				  <li><a href="#">OR News</a></li>
				  <li><a href="#">OR Trademark</a></li>
				  <li><a href="#">OR License</a></li>
				</ul>
			  </li>
			  <li class="dropdown">
					<a href="#" class="dropdown-toggle" data-toggle="dropdown">Download <b class="caret"></b></a>
					<ul class="dropdown-menu">
						<li><a href="#">Program</a></li>
						<li><a href="#">Content</a></li>
					</ul>
			  </li>
			  <li class="dropdown">
					<a href="#" class="dropdown-toggle" data-toggle="dropdown">Learn <b class="caret"></b></a>
					<ul class="dropdown-menu">
						<li><a href="/learn/faq/index.php">FAQ</a></li>
						<li><a href="#">Manuals</a></li>
						<li><a href="#">Community</a></li>
					</ul>
			  </li>
			  <li class="dropdown">
				<a href="#" class="dropdown-toggle" data-toggle="dropdown">Contribute <b class="caret"></b></a>
				<ul class="dropdown-menu">
				  <li><a href="#">Reporting Bugs</a></li>
				  <li><a href="#">Building Models</a></li>
					<li><a href="#">Joining the Team</a></li>
			  </ul>
			</li>
		  </ul>
		</div>
	</div>
		<div class="first_heading">
			<div class="row">
				<div class="col-md-4">&nbsp;</div>
				<div class="col-md-4">
			
			<form id="search_form" method="get" action="search.php">
			  <label>Enter</label>
			  <input type="text" id="search_term" placeholder="What are you searching for?" size="30"/>
			  <input type="submit" value="Search" id="search_button" />
			</form>
			
		    <table class="table table-striped table-condensed">
		      <thead>
		        <tr><th>year</th><th>month</th><th>max temperature</th><th>min temperature</th><th>rain mms</th><th>sunshine hours</th></tr>
			  </thead>
			  <tbody id="search_results">
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
			    <tr><td>&nbsp;</td></tr>
				</tbody>
	    	</table>
		  
					</div>
					<div class="col-md-4">&nbsp;</div>
        </div>
      </div>
    </div><!-- /.container -->

    <!-- Bootstrap core JavaScript - Latest compiled and minified JavaScript -->
    <!-- Placed at the end of the document so the pages load faster -->
    <!-- Note: "http:" only needed if not using a webserver. --> 
    <!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js"></script><!-- Must come before Bootstrap.min.js -->
    <script src="http://netdna.bootstrapcdn.com/bootstrap/3.1.0/js/bootstrap.min.js"></script>
  </body>
</html>