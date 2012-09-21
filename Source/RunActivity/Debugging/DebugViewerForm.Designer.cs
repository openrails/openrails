namespace ORTS.Debugging
{
   partial class DispatchViewer
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
		  this.pictureBox1 = new System.Windows.Forms.PictureBox();
		  this.refreshButton = new System.Windows.Forms.Button();
		  this.windowSizeUpDown = new System.Windows.Forms.NumericUpDown();
		  this.label2 = new System.Windows.Forms.Label();
		  this.AvatarView = new System.Windows.Forms.ListView();
		  this.rmvButton = new System.Windows.Forms.Button();
		  this.chkAllowUserSwitch = new System.Windows.Forms.CheckBox();
		  this.chkShowAvatars = new System.Windows.Forms.CheckBox();
		  this.MSG = new System.Windows.Forms.TextBox();
		  this.msgSelected = new System.Windows.Forms.Button();
		  this.msgAll = new System.Windows.Forms.Button();
		  this.composeMSG = new System.Windows.Forms.Button();
		  this.label1 = new System.Windows.Forms.Label();
		  this.messages = new System.Windows.Forms.ListBox();
		  this.reply2Selected = new System.Windows.Forms.Button();
		  ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
		  ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).BeginInit();
		  this.SuspendLayout();
		  // 
		  // pictureBox1
		  // 
		  this.pictureBox1.Location = new System.Drawing.Point(3, 131);
		  this.pictureBox1.Name = "pictureBox1";
		  this.pictureBox1.Size = new System.Drawing.Size(684, 626);
		  this.pictureBox1.TabIndex = 0;
		  this.pictureBox1.TabStop = false;
		  this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseMove);
		  this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseDown);
		  this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseUp);
		  // 
		  // refreshButton
		  // 
		  this.refreshButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.refreshButton.Location = new System.Drawing.Point(711, 9);
		  this.refreshButton.Name = "refreshButton";
		  this.refreshButton.Size = new System.Drawing.Size(141, 23);
		  this.refreshButton.TabIndex = 1;
		  this.refreshButton.Text = "View Selected";
		  this.refreshButton.UseVisualStyleBackColor = true;
		  this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
		  // 
		  // windowSizeUpDown
		  // 
		  this.windowSizeUpDown.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.windowSizeUpDown.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
		  this.windowSizeUpDown.Location = new System.Drawing.Point(755, 38);
		  this.windowSizeUpDown.Maximum = new decimal(new int[] {
            50000,
            0,
            0,
            0});
		  this.windowSizeUpDown.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
		  this.windowSizeUpDown.Name = "windowSizeUpDown";
		  this.windowSizeUpDown.Size = new System.Drawing.Size(79, 23);
		  this.windowSizeUpDown.TabIndex = 6;
		  this.windowSizeUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
		  this.windowSizeUpDown.ValueChanged += new System.EventHandler(this.windowSizeUpDown_ValueChanged);
		  // 
		  // label2
		  // 
		  this.label2.AutoSize = true;
		  this.label2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.label2.Location = new System.Drawing.Point(836, 40);
		  this.label2.Name = "label2";
		  this.label2.Size = new System.Drawing.Size(19, 16);
		  this.label2.TabIndex = 8;
		  this.label2.Text = "m";
		  // 
		  // AvatarView
		  // 
		  this.AvatarView.Location = new System.Drawing.Point(717, 133);
		  this.AvatarView.Name = "AvatarView";
		  this.AvatarView.Size = new System.Drawing.Size(121, 626);
		  this.AvatarView.TabIndex = 14;
		  this.AvatarView.UseCompatibleStateImageBehavior = false;
		  this.AvatarView.SelectedIndexChanged += new System.EventHandler(this.AvatarView_SelectedIndexChanged);
		  // 
		  // rmvButton
		  // 
		  this.rmvButton.Location = new System.Drawing.Point(725, 104);
		  this.rmvButton.Margin = new System.Windows.Forms.Padding(2);
		  this.rmvButton.Name = "rmvButton";
		  this.rmvButton.Size = new System.Drawing.Size(93, 24);
		  this.rmvButton.TabIndex = 15;
		  this.rmvButton.Text = "Remove Player";
		  this.rmvButton.UseVisualStyleBackColor = true;
		  this.rmvButton.Click += new System.EventHandler(this.rmvButton_Click);
		  // 
		  // chkAllowUserSwitch
		  // 
		  this.chkAllowUserSwitch.AutoSize = true;
		  this.chkAllowUserSwitch.Checked = true;
		  this.chkAllowUserSwitch.CheckState = System.Windows.Forms.CheckState.Checked;
		  this.chkAllowUserSwitch.Location = new System.Drawing.Point(710, 66);
		  this.chkAllowUserSwitch.Name = "chkAllowUserSwitch";
		  this.chkAllowUserSwitch.Size = new System.Drawing.Size(142, 17);
		  this.chkAllowUserSwitch.TabIndex = 16;
		  this.chkAllowUserSwitch.Text = " Client Controls Switches";
		  this.chkAllowUserSwitch.UseVisualStyleBackColor = true;
		  this.chkAllowUserSwitch.CheckedChanged += new System.EventHandler(this.chkAllowUserSwitch_CheckedChanged);
		  // 
		  // chkShowAvatars
		  // 
		  this.chkShowAvatars.AutoSize = true;
		  this.chkShowAvatars.Checked = true;
		  this.chkShowAvatars.CheckState = System.Windows.Forms.CheckState.Checked;
		  this.chkShowAvatars.Location = new System.Drawing.Point(710, 85);
		  this.chkShowAvatars.Name = "chkShowAvatars";
		  this.chkShowAvatars.Size = new System.Drawing.Size(92, 17);
		  this.chkShowAvatars.TabIndex = 17;
		  this.chkShowAvatars.Text = "Show Avatars";
		  this.chkShowAvatars.UseVisualStyleBackColor = true;
		  this.chkShowAvatars.CheckedChanged += new System.EventHandler(this.chkShowAvatars_CheckedChanged);
		  // 
		  // MSG
		  // 
		  this.MSG.Enabled = false;
		  this.MSG.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.MSG.Location = new System.Drawing.Point(1, 3);
		  this.MSG.Name = "MSG";
		  this.MSG.Size = new System.Drawing.Size(583, 26);
		  this.MSG.TabIndex = 18;
		  this.MSG.WordWrap = false;
		  this.MSG.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.checkKeys);
		  this.MSG.Leave += new System.EventHandler(this.MSGLeave);
		  this.MSG.Enter += new System.EventHandler(this.MSGEnter);
		  // 
		  // msgSelected
		  // 
		  this.msgSelected.Enabled = false;
		  this.msgSelected.Location = new System.Drawing.Point(590, 61);
		  this.msgSelected.Margin = new System.Windows.Forms.Padding(2);
		  this.msgSelected.Name = "msgSelected";
		  this.msgSelected.Size = new System.Drawing.Size(104, 24);
		  this.msgSelected.TabIndex = 19;
		  this.msgSelected.Text = "MSG to Selected";
		  this.msgSelected.UseVisualStyleBackColor = true;
		  this.msgSelected.Click += new System.EventHandler(this.msgSelected_Click);
		  // 
		  // msgAll
		  // 
		  this.msgAll.Enabled = false;
		  this.msgAll.Location = new System.Drawing.Point(590, 32);
		  this.msgAll.Margin = new System.Windows.Forms.Padding(2);
		  this.msgAll.Name = "msgAll";
		  this.msgAll.Size = new System.Drawing.Size(104, 24);
		  this.msgAll.TabIndex = 20;
		  this.msgAll.Text = "MSG to All";
		  this.msgAll.UseVisualStyleBackColor = true;
		  this.msgAll.Click += new System.EventHandler(this.msgAll_Click);
		  // 
		  // composeMSG
		  // 
		  this.composeMSG.Location = new System.Drawing.Point(590, 3);
		  this.composeMSG.Margin = new System.Windows.Forms.Padding(2);
		  this.composeMSG.Name = "composeMSG";
		  this.composeMSG.Size = new System.Drawing.Size(104, 24);
		  this.composeMSG.TabIndex = 21;
		  this.composeMSG.Text = "Compose MSG";
		  this.composeMSG.UseVisualStyleBackColor = true;
		  this.composeMSG.Click += new System.EventHandler(this.composeMSG_Click);
		  // 
		  // label1
		  // 
		  this.label1.AutoSize = true;
		  this.label1.Location = new System.Drawing.Point(692, 43);
		  this.label1.Name = "label1";
		  this.label1.Size = new System.Drawing.Size(57, 13);
		  this.label1.TabIndex = 7;
		  this.label1.Text = "Resolution";
		  // 
		  // messages
		  // 
		  this.messages.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.messages.FormattingEnabled = true;
		  this.messages.ItemHeight = 18;
		  this.messages.Location = new System.Drawing.Point(1, 38);
		  this.messages.Name = "messages";
		  this.messages.Size = new System.Drawing.Size(583, 76);
		  this.messages.TabIndex = 22;
		  this.messages.SelectedIndexChanged += new System.EventHandler(this.msgSelectedChanged);
		  // 
		  // reply2Selected
		  // 
		  this.reply2Selected.Enabled = false;
		  this.reply2Selected.Location = new System.Drawing.Point(590, 90);
		  this.reply2Selected.Margin = new System.Windows.Forms.Padding(2);
		  this.reply2Selected.Name = "reply2Selected";
		  this.reply2Selected.Size = new System.Drawing.Size(104, 24);
		  this.reply2Selected.TabIndex = 23;
		  this.reply2Selected.Text = "Reply to Selected";
		  this.reply2Selected.UseVisualStyleBackColor = true;
		  this.reply2Selected.Click += new System.EventHandler(this.replySelected);
		  // 
		  // DispatchViewer
		  // 
		  this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		  this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		  this.AutoScroll = true;
		  this.ClientSize = new System.Drawing.Size(868, 768);
		  this.Controls.Add(this.reply2Selected);
		  this.Controls.Add(this.messages);
		  this.Controls.Add(this.composeMSG);
		  this.Controls.Add(this.msgAll);
		  this.Controls.Add(this.msgSelected);
		  this.Controls.Add(this.MSG);
		  this.Controls.Add(this.chkShowAvatars);
		  this.Controls.Add(this.chkAllowUserSwitch);
		  this.Controls.Add(this.rmvButton);
		  this.Controls.Add(this.AvatarView);
		  this.Controls.Add(this.label2);
		  this.Controls.Add(this.label1);
		  this.Controls.Add(this.windowSizeUpDown);
		  this.Controls.Add(this.refreshButton);
		  this.Controls.Add(this.pictureBox1);
		  this.MaximizeBox = false;
		  this.Name = "DispatchViewer";
		  this.Text = "DispatchViewer";
		  this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
		  this.Leave += new System.EventHandler(this.DispatcherLeave);
		  ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
		  ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).EndInit();
		  this.ResumeLayout(false);
		  this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.PictureBox pictureBox1;
	  private System.Windows.Forms.Button refreshButton;
	  private System.Windows.Forms.NumericUpDown windowSizeUpDown;
	  private System.Windows.Forms.Label label2;
	  private System.Windows.Forms.ListView AvatarView;
	  private System.Windows.Forms.Button rmvButton;
	  private System.Windows.Forms.CheckBox chkAllowUserSwitch;
	  private System.Windows.Forms.CheckBox chkShowAvatars;
	  private System.Windows.Forms.TextBox MSG;
	  private System.Windows.Forms.Button msgSelected;
	  private System.Windows.Forms.Button msgAll;
	  private System.Windows.Forms.Button composeMSG;
	  private System.Windows.Forms.Label label1;
	  private System.Windows.Forms.ListBox messages;
	  private System.Windows.Forms.Button reply2Selected;
   }
}