<?php
require_once('../../shared/set_session_save_path.php');  // To avoid error 500 on this system
session_start();

$file_path  = $_REQUEST['file'];
$path_parts = pathinfo($file_path);
$file_name  = $path_parts['basename'];
$downloaded_by = $_REQUEST['id'];

// Redirect to download instead of streaming, to reduce problems (e.g. with download accelerators)
header("Location: /files/$file_name?file=$file&id=$downloaded_by");

// Record successful event
require_once('../../shared/mysql/db_connect.php');
$sql = "INSERT INTO tDownload (downloaded_by, filename, downloaded_on) VALUES ('$downloaded_by', '$file', NULL);";
// Any text output will be prefixed to file content and render the file unusable.
mysqli_query($dbc, $sql);
?>