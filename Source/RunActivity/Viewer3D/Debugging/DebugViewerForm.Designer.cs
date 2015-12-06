namespace Orts.Viewer3D.Debugging
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
             grayPen.Dispose();
             greenPen.Dispose();
             orangePen.Dispose();
             redPen.Dispose();
             pathPen.Dispose();
             trainPen.Dispose();
             trainBrush.Dispose();
             trainFont.Dispose();
             sidingBrush.Dispose();
             sidingFont.Dispose();
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
          this.resLabel = new System.Windows.Forms.Label();
          this.AvatarView = new System.Windows.Forms.ListView();
          this.rmvButton = new System.Windows.Forms.Button();
          this.chkAllowUserSwitch = new System.Windows.Forms.CheckBox();
          this.chkShowAvatars = new System.Windows.Forms.CheckBox();
          this.MSG = new System.Windows.Forms.TextBox();
          this.msgSelected = new System.Windows.Forms.Button();
          this.msgAll = new System.Windows.Forms.Button();
          this.composeMSG = new System.Windows.Forms.Button();
          this.label1 = new System.Windows.Forms.Label();
          this.reply2Selected = new System.Windows.Forms.Button();
          this.chkDrawPath = new System.Windows.Forms.CheckBox();
          this.boxSetSignal = new System.Windows.Forms.ListBox();
          this.boxSetSwitch = new System.Windows.Forms.ListBox();
          this.chkPickSignals = new System.Windows.Forms.CheckBox();
          this.chkPickSwitches = new System.Windows.Forms.CheckBox();
          this.chkAllowNew = new System.Windows.Forms.CheckBox();
          this.messages = new System.Windows.Forms.ListBox();
          this.btnAssist = new System.Windows.Forms.Button();
          this.btnNormal = new System.Windows.Forms.Button();
          this.btnFollow = new System.Windows.Forms.Button();
          this.chkBoxPenalty = new System.Windows.Forms.CheckBox();
          this.chkPreferGreen = new System.Windows.Forms.CheckBox();
          this.btnSeeInGame = new System.Windows.Forms.Button();
          ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
          ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).BeginInit();
          this.SuspendLayout();
          // 
          // pictureBox1
          // 
          this.pictureBox1.Location = new System.Drawing.Point(4, 161);
          this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
          this.pictureBox1.Name = "pictureBox1";
          this.pictureBox1.Size = new System.Drawing.Size(1005, 770);
          this.pictureBox1.TabIndex = 0;
          this.pictureBox1.TabStop = false;
          this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseMove);
          this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseDown);
          this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseUp);
          // 
          // refreshButton
          // 
          this.refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.refreshButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.refreshButton.Location = new System.Drawing.Point(1091, 129);
          this.refreshButton.Margin = new System.Windows.Forms.Padding(4);
          this.refreshButton.Name = "refreshButton";
          this.refreshButton.Size = new System.Drawing.Size(124, 28);
          this.refreshButton.TabIndex = 1;
          this.refreshButton.Text = "View Train";
          this.refreshButton.UseVisualStyleBackColor = true;
          this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
          // 
          // windowSizeUpDown
          // 
          this.windowSizeUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.windowSizeUpDown.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.windowSizeUpDown.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
          this.windowSizeUpDown.Location = new System.Drawing.Point(1076, 6);
          this.windowSizeUpDown.Margin = new System.Windows.Forms.Padding(4);
          this.windowSizeUpDown.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
          this.windowSizeUpDown.Minimum = new decimal(new int[] {
            80,
            0,
            0,
            0});
          this.windowSizeUpDown.Name = "windowSizeUpDown";
          this.windowSizeUpDown.Size = new System.Drawing.Size(105, 27);
          this.windowSizeUpDown.TabIndex = 6;
          this.windowSizeUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
          this.windowSizeUpDown.ValueChanged += new System.EventHandler(this.windowSizeUpDown_ValueChanged);
          // 
          // resLabel
          // 
          this.resLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.resLabel.AutoSize = true;
          this.resLabel.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.resLabel.Location = new System.Drawing.Point(1189, 11);
          this.resLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
          this.resLabel.Name = "resLabel";
          this.resLabel.Size = new System.Drawing.Size(26, 21);
          this.resLabel.TabIndex = 8;
          this.resLabel.Text = "m";
          // 
          // AvatarView
          // 
          this.AvatarView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.AvatarView.Location = new System.Drawing.Point(1039, 197);
          this.AvatarView.Margin = new System.Windows.Forms.Padding(4);
          this.AvatarView.Name = "AvatarView";
          this.AvatarView.Size = new System.Drawing.Size(160, 734);
          this.AvatarView.TabIndex = 14;
          this.AvatarView.UseCompatibleStateImageBehavior = false;
          this.AvatarView.SelectedIndexChanged += new System.EventHandler(this.AvatarView_SelectedIndexChanged);
          // 
          // rmvButton
          // 
          this.rmvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.rmvButton.Location = new System.Drawing.Point(1021, 162);
          this.rmvButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.rmvButton.Name = "rmvButton";
          this.rmvButton.Size = new System.Drawing.Size(96, 30);
          this.rmvButton.TabIndex = 15;
          this.rmvButton.Text = "Remove";
          this.rmvButton.UseVisualStyleBackColor = true;
          this.rmvButton.Click += new System.EventHandler(this.rmvButton_Click);
          // 
          // chkAllowUserSwitch
          // 
          this.chkAllowUserSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkAllowUserSwitch.AutoSize = true;
          this.chkAllowUserSwitch.Checked = true;
          this.chkAllowUserSwitch.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkAllowUserSwitch.Location = new System.Drawing.Point(944, 60);
          this.chkAllowUserSwitch.Margin = new System.Windows.Forms.Padding(4);
          this.chkAllowUserSwitch.Name = "chkAllowUserSwitch";
          this.chkAllowUserSwitch.Size = new System.Drawing.Size(103, 21);
          this.chkAllowUserSwitch.TabIndex = 16;
          this.chkAllowUserSwitch.Text = "Auto Switch";
          this.chkAllowUserSwitch.UseVisualStyleBackColor = true;
          this.chkAllowUserSwitch.CheckedChanged += new System.EventHandler(this.chkAllowUserSwitch_CheckedChanged);
          // 
          // chkShowAvatars
          // 
          this.chkShowAvatars.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkShowAvatars.AutoSize = true;
          this.chkShowAvatars.Checked = true;
          this.chkShowAvatars.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkShowAvatars.Location = new System.Drawing.Point(944, 38);
          this.chkShowAvatars.Margin = new System.Windows.Forms.Padding(4);
          this.chkShowAvatars.Name = "chkShowAvatars";
          this.chkShowAvatars.Size = new System.Drawing.Size(116, 21);
          this.chkShowAvatars.TabIndex = 17;
          this.chkShowAvatars.Text = "Show Avatars";
          this.chkShowAvatars.UseVisualStyleBackColor = true;
          this.chkShowAvatars.CheckedChanged += new System.EventHandler(this.chkShowAvatars_CheckedChanged);
          // 
          // MSG
          // 
          this.MSG.Enabled = false;
          this.MSG.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.MSG.Location = new System.Drawing.Point(1, 4);
          this.MSG.Margin = new System.Windows.Forms.Padding(4);
          this.MSG.Name = "MSG";
          this.MSG.Size = new System.Drawing.Size(776, 30);
          this.MSG.TabIndex = 18;
          this.MSG.WordWrap = false;
          this.MSG.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.checkKeys);
          this.MSG.Leave += new System.EventHandler(this.MSGLeave);
          this.MSG.Enter += new System.EventHandler(this.MSGEnter);
          // 
          // msgSelected
          // 
          this.msgSelected.Enabled = false;
          this.msgSelected.Location = new System.Drawing.Point(787, 75);
          this.msgSelected.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.msgSelected.Name = "msgSelected";
          this.msgSelected.Size = new System.Drawing.Size(139, 30);
          this.msgSelected.TabIndex = 19;
          this.msgSelected.Text = "MSG to Selected";
          this.msgSelected.UseVisualStyleBackColor = true;
          this.msgSelected.Click += new System.EventHandler(this.msgSelected_Click);
          // 
          // msgAll
          // 
          this.msgAll.Enabled = false;
          this.msgAll.Location = new System.Drawing.Point(787, 39);
          this.msgAll.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.msgAll.Name = "msgAll";
          this.msgAll.Size = new System.Drawing.Size(139, 30);
          this.msgAll.TabIndex = 20;
          this.msgAll.Text = "MSG to All";
          this.msgAll.UseVisualStyleBackColor = true;
          this.msgAll.Click += new System.EventHandler(this.msgAll_Click);
          // 
          // composeMSG
          // 
          this.composeMSG.Location = new System.Drawing.Point(787, 4);
          this.composeMSG.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.composeMSG.Name = "composeMSG";
          this.composeMSG.Size = new System.Drawing.Size(139, 30);
          this.composeMSG.TabIndex = 21;
          this.composeMSG.Text = "Compose MSG";
          this.composeMSG.UseVisualStyleBackColor = true;
          this.composeMSG.Click += new System.EventHandler(this.composeMSG_Click);
          // 
          // label1
          // 
          this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.label1.AutoSize = true;
          this.label1.Location = new System.Drawing.Point(1033, 11);
          this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
          this.label1.Name = "label1";
          this.label1.Size = new System.Drawing.Size(33, 17);
          this.label1.TabIndex = 7;
          this.label1.Text = "Res";
          // 
          // reply2Selected
          // 
          this.reply2Selected.Enabled = false;
          this.reply2Selected.Location = new System.Drawing.Point(787, 111);
          this.reply2Selected.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.reply2Selected.Name = "reply2Selected";
          this.reply2Selected.Size = new System.Drawing.Size(139, 30);
          this.reply2Selected.TabIndex = 23;
          this.reply2Selected.Text = "Reply to Selected";
          this.reply2Selected.UseVisualStyleBackColor = true;
          this.reply2Selected.Click += new System.EventHandler(this.replySelected);
          // 
          // chkDrawPath
          // 
          this.chkDrawPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkDrawPath.AutoSize = true;
          this.chkDrawPath.Checked = true;
          this.chkDrawPath.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkDrawPath.Location = new System.Drawing.Point(1076, 39);
          this.chkDrawPath.Margin = new System.Windows.Forms.Padding(4);
          this.chkDrawPath.Name = "chkDrawPath";
          this.chkDrawPath.Size = new System.Drawing.Size(95, 21);
          this.chkDrawPath.TabIndex = 24;
          this.chkDrawPath.Text = "Draw Path";
          this.chkDrawPath.UseVisualStyleBackColor = true;
          this.chkDrawPath.CheckedChanged += new System.EventHandler(this.chkDrawPathChanged);
          // 
          // boxSetSignal
          // 
          this.boxSetSignal.BorderStyle = System.Windows.Forms.BorderStyle.None;
          this.boxSetSignal.Enabled = false;
          this.boxSetSignal.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.boxSetSignal.FormattingEnabled = true;
          this.boxSetSignal.ItemHeight = 25;
          this.boxSetSignal.Location = new System.Drawing.Point(279, 252);
          this.boxSetSignal.Margin = new System.Windows.Forms.Padding(4);
          this.boxSetSignal.MinimumSize = new System.Drawing.Size(213, 123);
          this.boxSetSignal.Name = "boxSetSignal";
          this.boxSetSignal.Size = new System.Drawing.Size(219, 100);
          this.boxSetSignal.TabIndex = 25;
          this.boxSetSignal.Visible = false;
          this.boxSetSignal.SelectedIndexChanged += new System.EventHandler(this.boxSetSignalChosen);
          // 
          // boxSetSwitch
          // 
          this.boxSetSwitch.BorderStyle = System.Windows.Forms.BorderStyle.None;
          this.boxSetSwitch.Enabled = false;
          this.boxSetSwitch.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.boxSetSwitch.FormattingEnabled = true;
          this.boxSetSwitch.ItemHeight = 25;
          this.boxSetSwitch.Items.AddRange(new object[] {
            "To Main Route",
            "To Side Route"});
          this.boxSetSwitch.Location = new System.Drawing.Point(531, 252);
          this.boxSetSwitch.Margin = new System.Windows.Forms.Padding(4);
          this.boxSetSwitch.MinimumSize = new System.Drawing.Size(160, 62);
          this.boxSetSwitch.Name = "boxSetSwitch";
          this.boxSetSwitch.Size = new System.Drawing.Size(167, 50);
          this.boxSetSwitch.TabIndex = 26;
          this.boxSetSwitch.Visible = false;
          this.boxSetSwitch.SelectedIndexChanged += new System.EventHandler(this.boxSetSwitchChosen);
          // 
          // chkPickSignals
          // 
          this.chkPickSignals.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkPickSignals.AutoSize = true;
          this.chkPickSignals.Checked = true;
          this.chkPickSignals.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkPickSignals.Location = new System.Drawing.Point(1076, 60);
          this.chkPickSignals.Margin = new System.Windows.Forms.Padding(4);
          this.chkPickSignals.Name = "chkPickSignals";
          this.chkPickSignals.Size = new System.Drawing.Size(106, 21);
          this.chkPickSignals.TabIndex = 27;
          this.chkPickSignals.Text = "Pick Signals";
          this.chkPickSignals.UseVisualStyleBackColor = true;
          // 
          // chkPickSwitches
          // 
          this.chkPickSwitches.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkPickSwitches.AutoSize = true;
          this.chkPickSwitches.Checked = true;
          this.chkPickSwitches.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkPickSwitches.Location = new System.Drawing.Point(1076, 81);
          this.chkPickSwitches.Margin = new System.Windows.Forms.Padding(4);
          this.chkPickSwitches.Name = "chkPickSwitches";
          this.chkPickSwitches.Size = new System.Drawing.Size(115, 21);
          this.chkPickSwitches.TabIndex = 28;
          this.chkPickSwitches.Text = "Pick Switches";
          this.chkPickSwitches.UseVisualStyleBackColor = true;
          // 
          // chkAllowNew
          // 
          this.chkAllowNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkAllowNew.AutoSize = true;
          this.chkAllowNew.Checked = true;
          this.chkAllowNew.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkAllowNew.Location = new System.Drawing.Point(944, 15);
          this.chkAllowNew.Margin = new System.Windows.Forms.Padding(4);
          this.chkAllowNew.Name = "chkAllowNew";
          this.chkAllowNew.Size = new System.Drawing.Size(85, 21);
          this.chkAllowNew.TabIndex = 29;
          this.chkAllowNew.Text = "Can Join";
          this.chkAllowNew.UseVisualStyleBackColor = true;
          this.chkAllowNew.CheckedChanged += new System.EventHandler(this.chkAllowNewCheck);
          // 
          // messages
          // 
          this.messages.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.messages.FormattingEnabled = true;
          this.messages.ItemHeight = 24;
          this.messages.Location = new System.Drawing.Point(1, 47);
          this.messages.Margin = new System.Windows.Forms.Padding(4);
          this.messages.Name = "messages";
          this.messages.Size = new System.Drawing.Size(776, 76);
          this.messages.TabIndex = 22;
          this.messages.SelectedIndexChanged += new System.EventHandler(this.msgSelectedChanged);
          // 
          // btnAssist
          // 
          this.btnAssist.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.btnAssist.Location = new System.Drawing.Point(929, 128);
          this.btnAssist.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.btnAssist.Name = "btnAssist";
          this.btnAssist.Size = new System.Drawing.Size(64, 30);
          this.btnAssist.TabIndex = 30;
          this.btnAssist.Text = "Assist";
          this.btnAssist.UseVisualStyleBackColor = true;
          this.btnAssist.Click += new System.EventHandler(this.AssistClick);
          // 
          // btnNormal
          // 
          this.btnNormal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.btnNormal.Location = new System.Drawing.Point(1007, 128);
          this.btnNormal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
          this.btnNormal.Name = "btnNormal";
          this.btnNormal.Size = new System.Drawing.Size(77, 30);
          this.btnNormal.TabIndex = 31;
          this.btnNormal.Text = "Normal";
          this.btnNormal.UseVisualStyleBackColor = true;
          this.btnNormal.Click += new System.EventHandler(this.btnNormalClick);
          // 
          // btnFollow
          // 
          this.btnFollow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.btnFollow.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.btnFollow.Location = new System.Drawing.Point(1127, 164);
          this.btnFollow.Margin = new System.Windows.Forms.Padding(4);
          this.btnFollow.Name = "btnFollow";
          this.btnFollow.Size = new System.Drawing.Size(80, 28);
          this.btnFollow.TabIndex = 32;
          this.btnFollow.Text = "Follow";
          this.btnFollow.UseVisualStyleBackColor = true;
          this.btnFollow.Click += new System.EventHandler(this.btnFollowClick);
          // 
          // chkBoxPenalty
          // 
          this.chkBoxPenalty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkBoxPenalty.AutoSize = true;
          this.chkBoxPenalty.Checked = true;
          this.chkBoxPenalty.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkBoxPenalty.Location = new System.Drawing.Point(944, 81);
          this.chkBoxPenalty.Margin = new System.Windows.Forms.Padding(4);
          this.chkBoxPenalty.Name = "chkBoxPenalty";
          this.chkBoxPenalty.Size = new System.Drawing.Size(77, 21);
          this.chkBoxPenalty.TabIndex = 33;
          this.chkBoxPenalty.Text = "Penalty";
          this.chkBoxPenalty.UseVisualStyleBackColor = true;
          this.chkBoxPenalty.CheckedChanged += new System.EventHandler(this.chkOPenaltyHandle);
          // 
          // chkPreferGreen
          // 
          this.chkPreferGreen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.chkPreferGreen.AutoSize = true;
          this.chkPreferGreen.Checked = true;
          this.chkPreferGreen.CheckState = System.Windows.Forms.CheckState.Checked;
          this.chkPreferGreen.Location = new System.Drawing.Point(944, 102);
          this.chkPreferGreen.Margin = new System.Windows.Forms.Padding(4);
          this.chkPreferGreen.Name = "chkPreferGreen";
          this.chkPreferGreen.Size = new System.Drawing.Size(113, 21);
          this.chkPreferGreen.TabIndex = 34;
          this.chkPreferGreen.Text = "Prefer Green";
          this.chkPreferGreen.UseVisualStyleBackColor = true;
          this.chkPreferGreen.Visible = false;
          this.chkPreferGreen.CheckedChanged += new System.EventHandler(this.chkPreferGreenHandle);
          // 
          // btnSeeInGame
          // 
          this.btnSeeInGame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
          this.btnSeeInGame.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
          this.btnSeeInGame.Location = new System.Drawing.Point(1091, 100);
          this.btnSeeInGame.Margin = new System.Windows.Forms.Padding(4);
          this.btnSeeInGame.Name = "btnSeeInGame";
          this.btnSeeInGame.Size = new System.Drawing.Size(124, 28);
          this.btnSeeInGame.TabIndex = 35;
          this.btnSeeInGame.Text = "See in Game";
          this.btnSeeInGame.UseVisualStyleBackColor = true;
          this.btnSeeInGame.Click += new System.EventHandler(this.btnSeeInGameClick);
          // 
          // DispatchViewer
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.AutoScroll = true;
          this.ClientSize = new System.Drawing.Size(1231, 945);
          this.Controls.Add(this.btnSeeInGame);
          this.Controls.Add(this.chkPreferGreen);
          this.Controls.Add(this.chkBoxPenalty);
          this.Controls.Add(this.btnFollow);
          this.Controls.Add(this.btnNormal);
          this.Controls.Add(this.btnAssist);
          this.Controls.Add(this.chkAllowNew);
          this.Controls.Add(this.chkPickSwitches);
          this.Controls.Add(this.chkPickSignals);
          this.Controls.Add(this.boxSetSwitch);
          this.Controls.Add(this.boxSetSignal);
          this.Controls.Add(this.chkDrawPath);
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
          this.Controls.Add(this.resLabel);
          this.Controls.Add(this.label1);
          this.Controls.Add(this.windowSizeUpDown);
          this.Controls.Add(this.refreshButton);
          this.Controls.Add(this.pictureBox1);
          this.Margin = new System.Windows.Forms.Padding(4);
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
	  private System.Windows.Forms.Label resLabel;
	  private System.Windows.Forms.ListView AvatarView;
	  private System.Windows.Forms.Button rmvButton;
	  private System.Windows.Forms.CheckBox chkAllowUserSwitch;
	  private System.Windows.Forms.CheckBox chkShowAvatars;
	  private System.Windows.Forms.TextBox MSG;
	  private System.Windows.Forms.Button msgSelected;
	  private System.Windows.Forms.Button msgAll;
	  private System.Windows.Forms.Button composeMSG;
	  private System.Windows.Forms.Label label1;
	  private System.Windows.Forms.Button reply2Selected;
	  private System.Windows.Forms.CheckBox chkDrawPath;
	  private System.Windows.Forms.ListBox boxSetSignal;
	  private System.Windows.Forms.ListBox boxSetSwitch;
	  private System.Windows.Forms.CheckBox chkPickSignals;
	  private System.Windows.Forms.CheckBox chkPickSwitches;
	  private System.Windows.Forms.CheckBox chkAllowNew;
	  private System.Windows.Forms.ListBox messages;
	  private System.Windows.Forms.Button btnAssist;
	  private System.Windows.Forms.Button btnNormal;
	  private System.Windows.Forms.Button btnFollow;
	  private System.Windows.Forms.CheckBox chkBoxPenalty;
	  private System.Windows.Forms.CheckBox chkPreferGreen;
	  private System.Windows.Forms.Button btnSeeInGame;
   }
}
