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
        <!-- send this to another host as GitHub doesn't support mail. -->
        <form role="form" 
          action="https://submit-form.com/BjWG1lX2"
          data-botpoison-public-key="pk_016a2dd5-01de-4764-8a29-c4c0107438f0">
          <div class="form-group">
            <label for="email">Email</label>
            <input class="form-control" type="email" id="email" name="email" placeholder="Enter your email address. (We do not share this.)" autofocus required />
          </div>
          <div class="form-group">
            <label for="emailSubject">Subject</label>
            <input class="form-control" type="text" id="name" name="name" placeholder="Enter your subject" required />
          </div>
          <div class="form-group">
            <label for="message">Message</label>
            <textarea 
              class="form-control"
              rows="10"
              id="message"
              name="message"
              placeholder="Please follow the guidance to the left about getting help."
              required 
              title="Please follow the guidance to the left about getting help."
            ></textarea>
          </div>
          <input
            type="hidden"
            name="_redirect"
            value="http://www.openrails.org/contact/success.php"
          />
          <button type="submit">Send</button>
        </form>
			</div>
		</div>
<?php include "../shared/tail.php" ?>
<?php include "../shared/banners/preload_next_banner.php" ?>
    <script src="https://unpkg.com/@botpoison/browser" async></script>
  </body>
</html>