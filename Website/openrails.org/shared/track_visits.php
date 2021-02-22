<?php
require_once('db_connect.php');

# Find site's root folder, e.g. D:/web/openrails/web
$root = getenv("DOCUMENT_ROOT");
$path = strtr($root, '\\', '/');

if (!isset($_SESSION['id']) || strlen($_SESSION['id']) < 10) {
	// Retrieve id from cookie or create a new one.
	$cookie_name = 'or_org3';
	if(isset($_COOKIE[$cookie_name])) {
		$id = $_COOKIE[$cookie_name];
	}else{
		$id = md5(date("Y-m-d H:i:s") . rand()); // Uses the MySQL DATETIME format plus random number
		setcookie($cookie_name, $id, 2147483647); // max time value to expire far into future (19-Jan-2038)
		$sql = "INSERT INTO tVisitor (id) VALUES('$id')";
		if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
	
		$ip = $_SERVER['REMOTE_ADDR']; // Always a real IP address but might be a proxy.
		$sql = "INSERT INTO tVisitor_Attribute (used_by, of_type, of_value) VALUES('$id', 'ip', '$ip')";
		if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
	
		if (isset($_SERVER['HTTP_REFERER'])){
			$referer = $_SERVER['HTTP_REFERER'];
		}else{
			$referer = 'unknown';
		}
		$sql = "INSERT INTO tVisitor_Attribute (used_by, of_type, of_value) VALUES('$id', 'referer', '$referer')";
		if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
		
		if (isset($_SERVER['HTTP_USER_AGENT'])){
			$agent = $_SERVER['HTTP_USER_AGENT'];
		}else{
			$agent = 'unknown';
		}
		$sql = "INSERT INTO tVisitor_Attribute (used_by, of_type, of_value) VALUES('$id', 'agent', '$agent')";
		if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
	}
	// For visitors with cookies turned off, each visit to a webpage results in a new entry and several in Visit and Visitor_Attribute.
	// Could change this so each session will be as a different individual.
	// Not yet implemented.
	// menu.php must be changed to add 	echo '?', htmlspecialchars(SID); as the first parameter in every relative URL.
	// Should be detected automatically by the PHP server, but trials show this isn't the case for v5.2.8 as the setting.
	// ini_get("session.use_only_cookies") returns 1 meaning that the SID parameter is ignored by session_start().

	// Using Session variables can be dangerous.
	// "Please note that if you have register_globals to On, global variables associated to $_SESSION variables are references,
	// so this may lead to some weird situations." Re-using $id and changing its value also changes $_SESSION['id']
	$_SESSION['id'] = $id;
}


// Record visit made to this webpage
$folder = strtr(getcwd(), '\\', '/');
$made_to = str_replace($path, '/', $folder); // Remove leading filepath to leave local URL
$made_to = str_replace('//', '/', $made_to); // home page is just '/', but other pages begin with '//' which must become single.

// In a timestamp field, NULL is replaced by current time automatically.
//$sql = "INSERT INTO tVisit (made_by, made_to, visited_on) VALUES('$id', '$made_to', NULL)";
// But not on this server, so ask PHP for the date and time.
$now = date("Y-m-d H:i:s"); 

/* DISABLED as leading to a 500 Server Error
$sql = "INSERT INTO tVisit (made_by, made_to, visited_on) VALUES('$id', '$made_to', '$now')"; 
echo "<br>$sql<br>";
if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
*/
?>