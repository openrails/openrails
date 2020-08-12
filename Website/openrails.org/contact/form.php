<?php include "../shared/head.php" ?>
  </head>
  
  <body>
    <div class="container"><!-- Centres content and sets fixed width to suit device -->
<?php include "../shared/banners/choose_banner.php" ?>
<?php include "../shared/banners/show_banner.php" ?>
<?php include "../shared/menu.php" ?>
		<div class="row">
			<div class="col-md-4">
			  <h1>Contact</h1>
			</div>
		</div>
		<div class="row">
			<div class="col-md-3">
        <p>
          For prompt help with a technical problem, please <a href="http://www.elvastower.com/forums/index.php?/forum/190-open-rails-simulator-project/">post on one of the Open Rails forums</a> at Elvas Tower.
        </p><p>
          To report issues with the product or feature requests, please use <a href="../contribute/reporting-bugs/">our Bug Tracker</a>.
        </p><p>
          To contact the Open Rails Management Team, use <a href="form.php">this form to send us a message</a>.
        </p>
      </div>
			<div class="col-md-6">
        <!-- send this to another host as uktrainsim doesn't support mail() -->
        <form role="form" action="http://ccgi.jakeman.plus.com/or/forward_message.php" method="get">
          <div class="form-group">
            <label for="emailAddress">Email address</label>
            <input type="email" class="form-control" id="emailAddress" name="from" placeholder="Enter your email address. (We do not share this.)" autofocus required>
          </div>
          <div class="form-group">
            <label for="emailSubject">Subject</label>
            <input type="text" class="form-control" id="emailSubject" name="subject" placeholder="Enter your subject">
          </div>
    			<input type="hidden" name="send_to_name"   value="or_website">
		    	<input type="hidden" name="send_to_domain" value="jakeman.plus.com">
			    <input type="hidden" name="success_url"    value="http://openrails.org/contact/success.php"> 
		    	<?php $ip = $_SERVER['REMOTE_ADDR']; echo "<input type='hidden' name='ip' value='$ip'>" ?>
		    	<?php $referer = $_SERVER['HTTP_REFERER']; echo "<input type='hidden' name='referer' value='$referer'>" ?>
          <div class="form-group">
            <label for="emailMessage">Message</label>
            <textarea class="form-control" rows="10" id="emailMessage" name="body" placeholder="Please follow the guidance to the left about getting help." required title="Please follow the guidance to the left about getting help."></textarea>
          </div>
          <button type="submit" class="btn btn-default">Send</button>
        </form>

			</div>
		</div>
<?php include "../shared/tail.php" ?>
<?php include "../shared/banners/preload_next_banner.php" ?>
  </body>
</html>