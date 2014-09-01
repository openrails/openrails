<?php
require_once('../../shared/set_session_save_path.php');  // To avoid error 500 on this system
session_start();

$file = $_REQUEST['file'];
$filepath = $_REQUEST['filepath'];
$fileext = $_REQUEST['fileext'];
$downloaded_by = $_REQUEST['id'];

// Download takes place in the background thanks to "Content-Disposition: attachment"
header("Content-type: application/$fileext");
header("Content-Disposition: attachment; filename=\"$file\"");
readfile("$filepath/$file");

// Record successful event
require_once('../../shared/mysql/db_connect.php');
$sql = "INSERT INTO tDownload (downloaded_by, filename, downloaded_on) VALUES ('$downloaded_by', '$file', NULL);";
// Any text output will be prefixed to file content and render the file unusable.
#echo("$sql <br>");
if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
?>