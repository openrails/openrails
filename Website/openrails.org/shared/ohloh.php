<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
<style>

.ohloh_tall {
  width: 334px;
	height: 190px;
  background-color: white;
}
.ohloh_short {
  width: 334px;
	height: 148px;
  background-color: white;
}

</style>
</head>
<body>
<?php
if (isset($_SESSION['random3'])) {
  $random = $_SESSION['random3'] + 1;
}else{
  $random = time();
}
$random = $random % 3;
$_SESSION['random3'] = $random;
if ($random == 0) {	          
  echo "<div class='ohloh_short'>";
  echo "<script type='text/javascript' src='http://www.ohloh.net/p/642474/widgets/project_factoids.js'></script>";
}else{ 
  if( $random == 1) {
    echo "<div class='ohloh_tall'>";
    echo "<script type='text/javascript' src='http://www.ohloh.net/p/642474/widgets/project_basic_stats.js'></script>";
  }else{ 
    echo "<div class='ohloh_tall'>";
    echo "<script type='text/javascript' src='http://www.ohloh.net/p/642474/widgets/project_cocomo.js'></script>";
  }
}
echo "</div>";
?>
</body>
</html>