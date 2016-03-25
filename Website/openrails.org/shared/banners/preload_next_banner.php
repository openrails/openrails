<?php
// Preload the next banner image
$_SESSION['banner_key'] = get_banner_key($_SESSION['banner_key']);
$key = $_SESSION['banner_key'];
$banner_list = $_SESSION['banner_list'];
$id = $banner_list[$key][1];
$file = "/shared/banners/banner" . sprintf("%03d", $id) . ".jpg";
?>
    <script>
      // Append image to div with class=preload after document finishes loading
      $('<img />')
        .attr('src', '<?php echo $file ?>')
        .load(function(){
          $('.preload').append( $(this) );
        });
    </script>
