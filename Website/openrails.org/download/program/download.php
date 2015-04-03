<?php
require_once('../../shared/set_session_save_path.php');  // To avoid error 500 on this system
session_start();

$file_path  = $_REQUEST['file'];
$path_parts = pathinfo($file_path);
$file_name  = $path_parts['basename'];
$file_ext   = $path_parts['extension'];
$file_path  = '../../files/' . $file_name;
$file_size  = filesize($file_path);
$downloaded_by = $_REQUEST['id'];

// Download takes place in the background thanks to "Content-Disposition: attachment"
header("Content-Disposition: attachment; filename=\"$file_name\"");
if ($file_ext == 'zip') {
	header("Content-Type: application/zip");
} else {
	header("Content-Type: application/octet-stream");
}
header("Content-Length: $file_size");
readfile("../../files/$file_name");

// Record successful event
require_once('../../shared/mysql/db_connect.php');
$sql = "INSERT INTO tDownload (downloaded_by, filename, downloaded_on) VALUES ('$downloaded_by', '$file', NULL);";
// Any text output will be prefixed to file content and render the file unusable.
mysqli_query($dbc, $sql);
?>