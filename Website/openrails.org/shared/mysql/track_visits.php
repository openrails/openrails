<?php
require_once('db_connect.php');

# Find site's root folder, e.g. D:/web/openrails/web
$root = getenv("DOCUMENT_ROOT");
$path = strtr($root, '\\', '/');

// Retrieve id from cookie or create a new one.
$cookie_name = 'or_org3';
if(isset($_COOKIE[$cookie_name])) {
  $id = $_COOKIE[$cookie_name];
  #echo "old id = $id<br>";
}else{	
	$id = md5(date("Y-m-d H:i:s")); // Uses the MySQL DATETIME format
  setcookie($cookie_name, $id, 2147483647); // max time value to expire far into future
  #echo "new id = $id<br>";
  $sql = "INSERT INTO tVisitor (id) VALUES('$id')";
  #echo "$sql <br>";
  if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
	
	$ip = $_SERVER['REMOTE_ADDR']; // Always a real IP address but might be a proxy.
  $sql = "INSERT INTO tVisitor_Attribute (used_by, of_type, of_value) VALUES('$id', 'ip', '$ip')";
  #echo "$sql <br>";
  if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
	
	$referer = $_SERVER['HTTP_REFERER']; // Always a real IP address but might be a proxy.
  $sql = "INSERT INTO tVisitor_Attribute (used_by, of_type, of_value) VALUES('$id', 'referer', '$referer')";
  #echo "$sql <br>";
  if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
	
	$agent = $_SERVER['HTTP_USER_AGENT'];
  $sql = "INSERT INTO tVisitor_Attribute (used_by, of_type, of_value) VALUES('$id', 'agent', '$agent')";
  #echo "$sql <br>";
  if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
}

// Record visit made to this webpage
$folder = strtr(getcwd(), '\\', '/');
$made_to = str_replace($path, '/', $folder); // Remove leading filepath to leave local URL
$made_to = str_replace('//', '/', $made_to); // home page is just '/', but other pages begin with '//' which must become single.

// In a timestamp field, NULL is replaced by current time automatically.
$sql = "INSERT INTO tVisit (made_by, made_to, visited_on) VALUES('$id', '$made_to', NULL)"; 
#echo "$sql <br>";
if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
?>