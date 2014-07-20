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

# Find site's root folder, e.g. "/var/www/html"
$root = getenv("DOCUMENT_ROOT") . "/"; # e.g. D:/XAMPP/htdocs/
$root = $root . "or/";

$urls = array();
build_folder_array($root, "Home", 0);

# Recursive function
function build_folder_array($folder, $menu_path, $level){
  global $urls;
  
  $new_menu_path = $menu_path;
  # for each file and sub-folder in $folder
  $folder_list = new DirectoryIterator($folder);
  foreach($folder_list as $file ){
		if ($file->isDir() and ! $file->isDot() 
		and strpos($file->getPathname(), "dev - Copy") === FALSE) { // Test location has backups, so skip them
  		$subfolder = $file->getPathname();
			if (file_exists("$subfolder/title.php")) {
        $url_index =  count($urls);
				$path = strtr($subfolder, '\\', '/');
				$urls[$url_index]['folder'] = $path;
				echo ($urls[$url_index]['folder'] . "<br>");
				$urls[$url_index]['modify_date'] = filemtime("$subfolder/index.php");
				$leaf = basename($file);
				if ($leaf != "dev") {  // skip folder "dev"
				  $new_menu_path = $menu_path . " > " . $leaf;
				}
				echo ($new_menu_path);
				$urls[$url_index]['menu_path'] = $new_menu_path;
			}
			build_folder_array($subfolder, $new_menu_path, $level+1);
		}
	}
}
require_once('db_connect.php');

while(list($key, $val) = each($urls)){
	$url = $val['folder'];
	$updated_on = $val['modify_date'];
	$menu_path = $val['menu_path'];
	echo "$menu_path, $url, $updated_on <br>";
  $sql = "INSERT INTO tWebpage (url, updated_on, menu_path) VALUES('$url', FROM_UNIXTIME($updated_on), '$menu_path')";
  $sql .= "  ON DUPLICATE KEY UPDATE url=VALUES(url), updated_on=VALUES(updated_on), menu_path=VALUES(menu_path);";
	//echo "$sql <br>";
	if (!mysqli_query($dbc, $sql)) { die('Error: ' . mysqli_error($dbc)); }
}
echo "updated_on field updated";
?>
</body>
</html>
