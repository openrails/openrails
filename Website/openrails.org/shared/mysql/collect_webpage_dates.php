<!DOCTYPE HTML>
<html>
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<title>Collect Webapge Dates</title>
</head>

<body>
<?php

# Traverse the folder tree
# Collect URLs and file modification dates

# Find site's root folder, e.g. D:/web/openrails/web
$root = getenv("DOCUMENT_ROOT");
$urls = array(); // Available globally

// Add an entry for the home page
$path = strtr($root, '\\', '/');
$urls[0]['folder'] = $path;
$urls[0]['modify_date'] = filemtime($root . "/index.php");
$urls[0]['menu_path'] = 'Home';
echo ("Home<br>");

// Add more entries recursively for rest of website
build_folder_array($root . "/", "Home", 0);

# Recursive function
function build_folder_array($folder, $menu_path, $level){
  global $urls;
	
  $new_menu_path = $menu_path;
  # for each file and sub-folder in $folder
	#echo("new DirectoryIterator($folder)");
  $folder_list = new DirectoryIterator($folder);
  foreach($folder_list as $file ){ // e.g. $file = "contact"
    #echo("$file<br>");
		if (strpos($file, ".") === FALSE
		&& $file != 'or'
		&& $file != 'web1'
		&& $file != 'files'
		&& $file != 'api'
		&& $file != 'shared'
		&& $file != 'sessions') { // The sessions folder contains files without extensions so test for "." will mislead.
//		if (is_dir($file)) {  // Returns TRUE only for . and .. on this system
//		if ($file->isDir() and ! $file->isDot() // not accepted on this system
//		and strpos($file->getPathname(), "dev - Copy") === FALSE) { // Test location has backups, so skip them
  		$subfolder = $file->getPathname(); // e.g. $folder = "D:\web\openrails\web\contact"
			#echo("Folder = $subfolder, file = $file<br>");
			$leaf = basename($file);
		  $new_menu_path = $menu_path . " > " . $leaf;
			echo ($new_menu_path . "<br>");
			if (file_exists("$subfolder/title.php")) {
        #echo("Folder = $subfolder/title.php<br>");
        $url_index = count($urls);
				$path = strtr($subfolder, '\\', '/');
				$urls[$url_index]['folder'] = $path;
				#echo ($urls[$url_index]['folder'] . "<br>");
				$urls[$url_index]['modify_date'] = filemtime("$subfolder/index.php");
				$urls[$url_index]['menu_path'] = $new_menu_path;
			}
			build_folder_array($subfolder, $new_menu_path, $level+1);
		}
	}
}

require_once('db_connect.php');

while(list($key, $val) = each($urls)){
	$url = str_replace($path, '/', $val['folder']); // Remove leading filepath to leave local URL
	$url = str_replace('//', '/', $url); // home page is just '/', but other pages begin with '//' which must become single.
	$updated_on = $val['modify_date'];
	$menu_path = $val['menu_path'];
	//echo "$menu_path, $url, $updated_on <br>";
  $sql = "INSERT INTO tWebpage (url, updated_on, menu_path) VALUES('$url', FROM_UNIXTIME($updated_on), '$menu_path')";
  $sql .= "  ON DUPLICATE KEY UPDATE url=VALUES(url), updated_on=VALUES(updated_on), menu_path=VALUES(menu_path);";
	echo "$sql <br>";
	if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
}
echo "<br>'updated_on' field has been updated";
?>
</body>
</html>
