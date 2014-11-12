          <!-- Modal -->
<?php          
echo("<div id='$modal' class='modal fade' tabindex='-1' role='dialog' aria-labelledby='myModalLabel' aria-hidden='true'>");
?>
            <div class="modal-dialog">
              <div class="modal-content">
                <div class="modal-header">
                  <button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>
<?php
echo("<h2 class='modal-title'>$title</h2>");
?>
                </div>
                <div class="modal-body">
                  <p>
                    Please note: The Open Rails downloads do not currently include any models - routes, rolling stock, activities - just the
                    simulation program. 
                  </p><p>
                    If you have models suitable for Open Rails or MSTS already in place, then you can use the Open Rails program to operate
                    those routes and drive those locos straight away.
                  </p><p>
                    If not, then you will have to install some models <a href='http://openrails.org/trade/'>bought from a vendor</a> or 
                    <a href='http://openrails.org/share/community/'>free from a forum</a> before you can use Open Rails.
                  </p><p class="text-right">
<?php 
echo ("<a href='/download/program/confirm.php?filepath=$file_path&file=$download_file&fileext=$ext' class='btn download_button'>Download</a>");
?>
                    <button type="button" data-dismiss="modal" aria-hidden="true" class="btn btn-default cancel_button">Cancel</button>
                  </p>
                </div><!-- End of Modal body -->
              </div><!-- End of Modal content -->
            </div><!-- End of Modal dialog -->
          </div><!-- End of Modal -->
