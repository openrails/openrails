<?php 

// Better to move this section above track_visits.php
// As it is, someone with cookies disallowed gets new visitor attributes for every page visited, not just on every visit.
require_once('set_session_save_path.php');  // To avoid error 500 on this system
ini_set('session.use_only_cookies',0);
session_start();
require_once('mysql/track_visits.php');

// To avoid the workaround for IE browsers, which is not valid HTML, send it only when browser is MSIE. See
// http://www.validatethis.co.uk/news/fix-bad-value-x-ua-compatible-once-and-for-all/
// Workaround placed in <head>
// <meta http-equiv="X-UA-Compatible" content="IE=edge">
if (isset($_SERVER['HTTP_USER_AGENT'])
&& (strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE') !== false))
  header('X-UA-Compatible: IE=edge,chrome=1');
?>
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1"><!-- adds zooming for mobiles -->
    <meta name="description" content="Open Rails is a free train simulator supporting the world's largest range of digital content.">
    <link rel="shortcut icon" href="/shared/logos/or_logo.png"><!-- This icon appears in the page's tab and its bookmark. -->

<?php include "title.php"; ?>
    
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
