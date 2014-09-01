<?php
// Extra statement needed as session.save_path is null on this system.

if(ini_get('session.save_path') == '') {
  // Cannot use recommended temp directory - leads to error 500
  //ini_set('session.save_path', 'C:\Temp');
  ini_set('session.save_path', 'D:\web\openrails\web\sessions');
}
//echo "'" . ini_get('session.save_path') . "'";
?>