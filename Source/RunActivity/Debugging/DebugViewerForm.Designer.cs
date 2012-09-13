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
		  this.leftButton = new System.Windows.Forms.Button();
		  this.rightButton = new System.Windows.Forms.Button();
		  this.upButton = new System.Windows.Forms.Button();
		  this.downButton = new System.Windows.Forms.Button();
		  this.windowSizeUpDown = new System.Windows.Forms.NumericUpDown();
		  this.label1 = new System.Windows.Forms.Label();
		  this.label2 = new System.Windows.Forms.Label();
		  this.showSwitches = new System.Windows.Forms.CheckBox();
		  this.showBuffers = new System.Windows.Forms.CheckBox();
		  this.showSignals = new System.Windows.Forms.CheckBox();
		  this.menuStrip = new System.Windows.Forms.MenuStrip();
		  this.interlockingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		  this.viewTracksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		  this.viewSignalsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		  this.viewSwitchesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		  this.label3 = new System.Windows.Forms.Label();
		  this.AvatarView = new System.Windows.Forms.ListView();
		  this.rmvButton = new System.Windows.Forms.Button();
		  this.chkAllowUserSwitch = new System.Windows.Forms.CheckBox();
		  this.chkShowAvatars = new System.Windows.Forms.CheckBox();
		  this.MSG = new System.Windows.Forms.TextBox();
		  this.msgSelected = new System.Windows.Forms.Button();
		  this.msgAll = new System.Windows.Forms.Button();
		  this.composeMSG = new System.Windows.Forms.Button();
		  ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
		  ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).BeginInit();
		  this.menuStrip.SuspendLayout();
		  this.SuspendLayout();
		  // 
		  // pictureBox1
		  // 
		  this.pictureBox1.Location = new System.Drawing.Point(10, 164);
		  this.pictureBox1.Name = "pictureBox1";
		  this.pictureBox1.Size = new System.Drawing.Size(684, 646);
		  this.pictureBox1.TabIndex = 0;
		  this.pictureBox1.TabStop = false;
		  this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseMove);
		  this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseDown);
		  this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseUp);
		  // 
		  // refreshButton
		  // 
		  this.refreshButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.refreshButton.Location = new System.Drawing.Point(4, 44);
		  this.refreshButton.Name = "refreshButton";
		  this.refreshButton.Size = new System.Drawing.Size(91, 23);
		  this.refreshButton.TabIndex = 1;
		  this.refreshButton.Text = "View Self";
		  this.refreshButton.UseVisualStyleBackColor = true;
		  this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
		  // 
		  // leftButton
		  // 
		  this.leftButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.leftButton.Location = new System.Drawing.Point(101, 38);
		  this.leftButton.Name = "leftButton";
		  this.leftButton.Size = new System.Drawing.Size(35, 35);
		  this.leftButton.TabIndex = 2;
		  this.leftButton.Text = "L";
		  this.leftButton.UseVisualStyleBackColor = true;
		  this.leftButton.MouseLeave += new System.EventHandler(this.leftButton_MouseLeave);
		  this.leftButton.Click += new System.EventHandler(this.leftButton_Click);
		  this.leftButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.leftButton_MouseDown);
		  this.leftButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.leftButton_MouseUp);
		  // 
		  // rightButton
		  // 
		  this.rightButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.rightButton.Location = new System.Drawing.Point(184, 38);
		  this.rightButton.Name = "rightButton";
		  this.rightButton.Size = new System.Drawing.Size(35, 35);
		  this.rightButton.TabIndex = 3;
		  this.rightButton.Text = "R";
		  this.rightButton.UseVisualStyleBackColor = true;
		  this.rightButton.MouseLeave += new System.EventHandler(this.rightButton_MouseLeave);
		  this.rightButton.Click += new System.EventHandler(this.rightButton_Click);
		  this.rightButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.rightButton_MouseDown);
		  this.rightButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.rightButton_MouseUp);
		  // 
		  // upButton
		  // 
		  this.upButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.upButton.Location = new System.Drawing.Point(142, 38);
		  this.upButton.Name = "upButton";
		  this.upButton.Size = new System.Drawing.Size(35, 35);
		  this.upButton.TabIndex = 4;
		  this.upButton.Text = "U";
		  this.upButton.UseVisualStyleBackColor = true;
		  this.upButton.MouseLeave += new System.EventHandler(this.upButton_MouseLeave);
		  this.upButton.Click += new System.EventHandler(this.upButton_Click);
		  this.upButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.upButton_MouseDown);
		  this.upButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.upButton_MouseUp);
		  // 
		  // downButton
		  // 
		  this.downButton.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.downButton.Location = new System.Drawing.Point(224, 38);
		  this.downButton.Name = "downButton";
		  this.downButton.Size = new System.Drawing.Size(35, 35);
		  this.downButton.TabIndex = 5;
		  this.downButton.Text = "D";
		  this.downButton.UseVisualStyleBackColor = true;
		  this.downButton.MouseLeave += new System.EventHandler(this.downButton_MouseLeave);
		  this.downButton.Click += new System.EventHandler(this.downButton_Click);
		  this.downButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.downButton_MouseDown);
		  this.downButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.downButton_MouseUp);
		  // 
		  // windowSizeUpDown
		  // 
		  this.windowSizeUpDown.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.windowSizeUpDown.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
		  this.windowSizeUpDown.Location = new System.Drawing.Point(268, 50);
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
		  // label1
		  // 
		  this.label1.AutoSize = true;
		  this.label1.Location = new System.Drawing.Point(266, 34);
		  this.label1.Name = "label1";
		  this.label1.Size = new System.Drawing.Size(57, 13);
		  this.label1.TabIndex = 7;
		  this.label1.Text = "Resolution";
		  // 
		  // label2
		  // 
		  this.label2.AutoSize = true;
		  this.label2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.label2.Location = new System.Drawing.Point(352, 52);
		  this.label2.Name = "label2";
		  this.label2.Size = new System.Drawing.Size(54, 16);
		  this.label2.TabIndex = 8;
		  this.label2.Text = "metres";
		  // 
		  // showSwitches
		  // 
		  this.showSwitches.AutoSize = true;
		  this.showSwitches.Checked = true;
		  this.showSwitches.CheckState = System.Windows.Forms.CheckState.Checked;
		  this.showSwitches.Location = new System.Drawing.Point(407, 56);
		  this.showSwitches.Name = "showSwitches";
		  this.showSwitches.Size = new System.Drawing.Size(160, 17);
		  this.showSwitches.TabIndex = 9;
		  this.showSwitches.Text = "Show Switches (Black Dots)";
		  this.showSwitches.UseVisualStyleBackColor = true;
		  this.showSwitches.CheckedChanged += new System.EventHandler(this.showSwitches_CheckedChanged);
		  // 
		  // showBuffers
		  // 
		  this.showBuffers.AutoSize = true;
		  this.showBuffers.Location = new System.Drawing.Point(407, 76);
		  this.showBuffers.Name = "showBuffers";
		  this.showBuffers.Size = new System.Drawing.Size(167, 17);
		  this.showBuffers.TabIndex = 10;
		  this.showBuffers.Text = "Show Buffers (Black Squares)";
		  this.showBuffers.UseVisualStyleBackColor = true;
		  this.showBuffers.Visible = false;
		  this.showBuffers.CheckedChanged += new System.EventHandler(this.showBuffers_CheckedChanged);
		  // 
		  // showSignals
		  // 
		  this.showSignals.AutoSize = true;
		  this.showSignals.Checked = true;
		  this.showSignals.CheckState = System.Windows.Forms.CheckState.Checked;
		  this.showSignals.Location = new System.Drawing.Point(407, 38);
		  this.showSignals.Name = "showSignals";
		  this.showSignals.Size = new System.Drawing.Size(178, 17);
		  this.showSignals.TabIndex = 11;
		  this.showSignals.Text = "Show Signals (Red/Green Dots)";
		  this.showSignals.UseVisualStyleBackColor = true;
		  this.showSignals.CheckedChanged += new System.EventHandler(this.showSignals_CheckedChanged);
		  // 
		  // menuStrip
		  // 
		  this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.interlockingToolStripMenuItem});
		  this.menuStrip.Location = new System.Drawing.Point(0, 0);
		  this.menuStrip.Name = "menuStrip";
		  this.menuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
		  this.menuStrip.Size = new System.Drawing.Size(865, 24);
		  this.menuStrip.TabIndex = 12;
		  this.menuStrip.Text = "menuStrip1";
		  // 
		  // interlockingToolStripMenuItem
		  // 
		  this.interlockingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewTracksToolStripMenuItem,
            this.viewSignalsToolStripMenuItem,
            this.viewSwitchesToolStripMenuItem});
		  this.interlockingToolStripMenuItem.Name = "interlockingToolStripMenuItem";
		  this.interlockingToolStripMenuItem.Size = new System.Drawing.Size(82, 20);
		  this.interlockingToolStripMenuItem.Text = "Interlocking";
		  // 
		  // viewTracksToolStripMenuItem
		  // 
		  this.viewTracksToolStripMenuItem.Name = "viewTracksToolStripMenuItem";
		  this.viewTracksToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
		  this.viewTracksToolStripMenuItem.Text = "View Tracks";
		  this.viewTracksToolStripMenuItem.Click += new System.EventHandler(this.viewTracksToolStripMenuItem_Click);
		  // 
		  // viewSignalsToolStripMenuItem
		  // 
		  this.viewSignalsToolStripMenuItem.Name = "viewSignalsToolStripMenuItem";
		  this.viewSignalsToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
		  this.viewSignalsToolStripMenuItem.Text = "View Signals";
		  this.viewSignalsToolStripMenuItem.Click += new System.EventHandler(this.viewSignalsToolStripMenuItem_Click);
		  // 
		  // viewSwitchesToolStripMenuItem
		  // 
		  this.viewSwitchesToolStripMenuItem.Name = "viewSwitchesToolStripMenuItem";
		  this.viewSwitchesToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
		  this.viewSwitchesToolStripMenuItem.Text = "View Switches";
		  this.viewSwitchesToolStripMenuItem.Click += new System.EventHandler(this.viewSwitchesToolStripMenuItem_Click);
		  // 
		  // label3
		  // 
		  this.label3.AutoSize = true;
		  this.label3.Location = new System.Drawing.Point(730, 103);
		  this.label3.Name = "label3";
		  this.label3.Size = new System.Drawing.Size(74, 13);
		  this.label3.TabIndex = 13;
		  this.label3.Text = "Online Players";
		  // 
		  // AvatarView
		  // 
		  this.AvatarView.Location = new System.Drawing.Point(728, 165);
		  this.AvatarView.Name = "AvatarView";
		  this.AvatarView.Size = new System.Drawing.Size(121, 601);
		  this.AvatarView.TabIndex = 14;
		  this.AvatarView.UseCompatibleStateImageBehavior = false;
		  // 
		  // rmvButton
		  // 
		  this.rmvButton.Location = new System.Drawing.Point(733, 127);
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
		  this.chkAllowUserSwitch.Location = new System.Drawing.Point(628, 38);
		  this.chkAllowUserSwitch.Name = "chkAllowUserSwitch";
		  this.chkAllowUserSwitch.Size = new System.Drawing.Size(210, 17);
		  this.chkAllowUserSwitch.TabIndex = 16;
		  this.chkAllowUserSwitch.Text = "Server Allows Client to Throw Switches";
		  this.chkAllowUserSwitch.UseVisualStyleBackColor = true;
		  this.chkAllowUserSwitch.CheckedChanged += new System.EventHandler(this.chkAllowUserSwitch_CheckedChanged);
		  // 
		  // chkShowAvatars
		  // 
		  this.chkShowAvatars.AutoSize = true;
		  this.chkShowAvatars.Checked = true;
		  this.chkShowAvatars.CheckState = System.Windows.Forms.CheckState.Checked;
		  this.chkShowAvatars.Location = new System.Drawing.Point(628, 56);
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
		  this.MSG.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		  this.MSG.Location = new System.Drawing.Point(14, 100);
		  this.MSG.Name = "MSG";
		  this.MSG.Size = new System.Drawing.Size(680, 29);
		  this.MSG.TabIndex = 18;
		  this.MSG.WordWrap = false;
		  // 
		  // msgSelected
		  // 
		  this.msgSelected.Location = new System.Drawing.Point(422, 135);
		  this.msgSelected.Margin = new System.Windows.Forms.Padding(2);
		  this.msgSelected.Name = "msgSelected";
		  this.msgSelected.Size = new System.Drawing.Size(105, 24);
		  this.msgSelected.TabIndex = 19;
		  this.msgSelected.Text = "MSG to Selected";
		  this.msgSelected.UseVisualStyleBackColor = true;
		  this.msgSelected.Click += new System.EventHandler(this.msgSelected_Click);
		  // 
		  // msgAll
		  // 
		  this.msgAll.Location = new System.Drawing.Point(280, 135);
		  this.msgAll.Margin = new System.Windows.Forms.Padding(2);
		  this.msgAll.Name = "msgAll";
		  this.msgAll.Size = new System.Drawing.Size(89, 24);
		  this.msgAll.TabIndex = 20;
		  this.msgAll.Text = "MSG to All";
		  this.msgAll.UseVisualStyleBackColor = true;
		  this.msgAll.Click += new System.EventHandler(this.msgAll_Click);
		  // 
		  // composeMSG
		  // 
		  this.composeMSG.Location = new System.Drawing.Point(125, 135);
		  this.composeMSG.Margin = new System.Windows.Forms.Padding(2);
		  this.composeMSG.Name = "composeMSG";
		  this.composeMSG.Size = new System.Drawing.Size(94, 24);
		  this.composeMSG.TabIndex = 21;
		  this.composeMSG.Text = "Compose MSG";
		  this.composeMSG.UseVisualStyleBackColor = true;
		  this.composeMSG.Click += new System.EventHandler(this.composeMSG_Click);
		  // 
		  // DispatchViewer
		  // 
		  this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		  this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		  this.AutoScroll = true;
		  this.ClientSize = new System.Drawing.Size(865, 813);
		  this.Controls.Add(this.composeMSG);
		  this.Controls.Add(this.msgAll);
		  this.Controls.Add(this.msgSelected);
		  this.Controls.Add(this.MSG);
		  this.Controls.Add(this.chkShowAvatars);
		  this.Controls.Add(this.chkAllowUserSwitch);
		  this.Controls.Add(this.rmvButton);
		  this.Controls.Add(this.AvatarView);
		  this.Controls.Add(this.label3);
		  this.Controls.Add(this.showSignals);
		  this.Controls.Add(this.showBuffers);
		  this.Controls.Add(this.showSwitches);
		  this.Controls.Add(this.label2);
		  this.Controls.Add(this.label1);
		  this.Controls.Add(this.windowSizeUpDown);
		  this.Controls.Add(this.downButton);
		  this.Controls.Add(this.upButton);
		  this.Controls.Add(this.rightButton);
		  this.Controls.Add(this.leftButton);
		  this.Controls.Add(this.refreshButton);
		  this.Controls.Add(this.pictureBox1);
		  this.Controls.Add(this.menuStrip);
		  this.MainMenuStrip = this.menuStrip;
		  this.MaximizeBox = false;
		  this.Name = "DispatchViewer";
		  this.Text = "DispatchViewer";
		  this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
		  ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
		  ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).EndInit();
		  this.menuStrip.ResumeLayout(false);
		  this.menuStrip.PerformLayout();
		  this.ResumeLayout(false);
		  this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.PictureBox pictureBox1;
      private System.Windows.Forms.Button refreshButton;
      private System.Windows.Forms.Button leftButton;
      private System.Windows.Forms.Button rightButton;
      private System.Windows.Forms.Button upButton;
      private System.Windows.Forms.Button downButton;
      private System.Windows.Forms.NumericUpDown windowSizeUpDown;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.CheckBox showSwitches;
      private System.Windows.Forms.CheckBox showBuffers;
      private System.Windows.Forms.CheckBox showSignals;
      private System.Windows.Forms.MenuStrip menuStrip;
      private System.Windows.Forms.ToolStripMenuItem interlockingToolStripMenuItem;
	  private System.Windows.Forms.ToolStripMenuItem viewTracksToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem viewSignalsToolStripMenuItem;
	  private System.Windows.Forms.ToolStripMenuItem viewSwitchesToolStripMenuItem;
	  private System.Windows.Forms.Label label3;
	  private System.Windows.Forms.ListView AvatarView;
	  private System.Windows.Forms.Button rmvButton;
	  private System.Windows.Forms.CheckBox chkAllowUserSwitch;
	  private System.Windows.Forms.CheckBox chkShowAvatars;
	  private System.Windows.Forms.TextBox MSG;
	  private System.Windows.Forms.Button msgSelected;
	  private System.Windows.Forms.Button msgAll;
	  private System.Windows.Forms.Button composeMSG;
   }
}