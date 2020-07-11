      <div class="row">
        <div class="col-md-12">
<?php 
$key = $_SESSION['banner_key'];
$banner_list = $_SESSION['banner_list'];
$bid = $banner_list[$key][1];
$file = "/shared/banners/banner" . sprintf("%03d", $bid) . ".jpg"; 
$supplier = $banner_list[$key][3];
$title = $banner_list[$key][4];
echo("<img class='banner' src='$file' title='$title &#xa; posted by $supplier' alt='$title &#xa;posted by $supplier'>\n"); 
?>
          <a href="/">
            <img class="logo" src='/shared/logos/or_logo.png' alt="Logo for Open Rails"/>
            <div class="logo_text">Open Rails</div>
          </a>
          <a href="#" onclick='location.reload(true); return false;' title=" For a different random picture, &#xa; visit a different webpage.">
            <span class="glyphicon glyphicon-chevron-right next_banner"></span>
          </a>
        </div>
      </div>
      <div class="row preload hidden"></div> <!-- empty div used to hold banner preload -->
