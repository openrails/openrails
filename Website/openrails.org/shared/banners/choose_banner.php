<?php
if (!isset($_SESSION['banner_list'])) {
	$path = dirname(__FILE__);  // path to this include file
  $handle = fopen("$path/banner_list.csv", "r");
	if (($handle = fopen("$path/banner_list.csv", "r")) === FALSE) {
		die("File $path/banner_list.csv not found.");
	}
	$banner_list = array();
	$data = fgetcsv($handle, ","); // Pre-read and discard the header row 
	$key = 0;
	setlocale(LC_ALL, 'en_US.UTF-8');  // To read non-ASCII characters
	while (($data = fgetcsv($handle, ",")) !== FALSE) {
  	$data[0] = $key++; 
		array_push($banner_list, $data);
		// Duplicate entries so best images appear more often.
		for( $count = $data[2]; $count > 1; $count--) {
			$data[0] = $key++; 
			array_push($banner_list, $data);
		}
	}
	fclose($handle);
  $_SESSION['banner_list'] = $banner_list;
}

if( !isset($_SESSION['banner_key']) ) { 
  $_SESSION['banner_key'] = 0; 
}

// Pick a banner at random but do not repeat previous one.
function get_banner_key($banner_key) {
	while( true ) {
	  $banner_list = $_SESSION['banner_list'];
	  $key = array_rand($banner_list);
		$id = $banner_list[$key][1];
		$prev_id = $banner_list[$banner_key][1];
    if( $id != $prev_id ) { /* Don't repeat yourself immediately */
	    return($key);
		}
	}
}
?>
