<?php
$file = $_REQUEST['file'];
$filepath = $_REQUEST['filepath'];
$fileext = $_REQUEST['fileext'];

// Download takes place in the background thanks to "Content-Disposition: attachment"
header("Content-type: application/$fileext");
header("Content-Disposition: attachment; filename=\"$file\"");
readfile("$filepath/$file");

// Record successful event
require_once('../../shared/mysql/db_connect.php');
$cookie_name = 'or_org3';
$downloaded_by = $_COOKIE[$cookie_name];
$sql = "INSERT INTO tDownload (downloaded_by, filename, downloaded_on) VALUES ('$downloaded_by', '$file', NULL);";
// Any text output will be prefixed to file content and render the file unusable.
#echo("$sql <br>");
if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
?>