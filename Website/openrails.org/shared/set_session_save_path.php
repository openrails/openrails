<?php
// Extra statement needed as session.save_path is null on this system.

if(ini_get('session.save_path') == '' || ini_get('session.save_path') == 'E:\PHPSave') {
  // Cannot use recommended temp directory - leads to error 500
  //ini_set('session.save_path', 'C:\Temp');
  $root = getenv("DOCUMENT_ROOT");
  ini_set('session.save_path', "$root\sessions");
}
echo "'" . ini_get('session.save_path') . "'";
?>