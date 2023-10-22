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
            this.pbCanvas = new System.Windows.Forms.PictureBox();
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
            this.lblSimulationTimeText = new System.Windows.Forms.Label();
            this.lblSimulationTime = new System.Windows.Forms.Label();
            this.lblShow = new System.Windows.Forms.Label();
            this.cbShowPlatformLabels = new System.Windows.Forms.CheckBox();
            this.cbShowSidings = new System.Windows.Forms.CheckBox();
            this.cbShowSignals = new System.Windows.Forms.CheckBox();
            this.cbShowSignalState = new System.Windows.Forms.CheckBox();
            this.gbTrainLabels = new System.Windows.Forms.GroupBox();
            this.bTrainKey = new System.Windows.Forms.Button();
            this.rbShowActiveTrainLabels = new System.Windows.Forms.RadioButton();
            this.rbShowAllTrainLabels = new System.Windows.Forms.RadioButton();
            this.nudDaylightOffsetHrs = new System.Windows.Forms.NumericUpDown();
            this.lblDayLightOffsetHrs = new System.Windows.Forms.Label();
            this.cdBackground = new System.Windows.Forms.ColorDialog();
            this.bBackgroundColor = new System.Windows.Forms.Button();
            this.cbShowSwitches = new System.Windows.Forms.CheckBox();
            this.lblInstruction1 = new System.Windows.Forms.Label();
            this.cbShowTrainLabels = new System.Windows.Forms.CheckBox();
            this.tWindow = new System.Windows.Forms.TabControl();
            this.tDispatch = new System.Windows.Forms.TabPage();
            this.tTimetable = new System.Windows.Forms.TabPage();
            this.cbShowTrainState = new System.Windows.Forms.CheckBox();
            this.lblInstruction2 = new System.Windows.Forms.Label();
            this.lblInstruction3 = new System.Windows.Forms.Label();
            this.lblInstruction4 = new System.Windows.Forms.Label();
            this.cbShowPlatforms = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).BeginInit();
            this.gbTrainLabels.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDaylightOffsetHrs)).BeginInit();
            this.tWindow.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbCanvas
            // 
            this.pbCanvas.Location = new System.Drawing.Point(5, 131);
            this.pbCanvas.Name = "pbCanvas";
            this.pbCanvas.Size = new System.Drawing.Size(754, 626);
            this.pbCanvas.TabIndex = 0;
            this.pbCanvas.TabStop = false;
            this.pbCanvas.SizeChanged += new System.EventHandler(this.pbCanvas_SizeChanged);
            this.pbCanvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseDown);
            this.pbCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseMove);
            this.pbCanvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseUp);
            // 
            // refreshButton
            // 
            this.refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refreshButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refreshButton.Location = new System.Drawing.Point(820, 137);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(93, 23);
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
            this.windowSizeUpDown.Location = new System.Drawing.Point(802, 31);
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
            this.windowSizeUpDown.Size = new System.Drawing.Size(79, 23);
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
            this.resLabel.Location = new System.Drawing.Point(887, 35);
            this.resLabel.Name = "resLabel";
            this.resLabel.Size = new System.Drawing.Size(18, 16);
            this.resLabel.TabIndex = 8;
            this.resLabel.Text = "m";
            // 
            // AvatarView
            // 
            this.AvatarView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AvatarView.HideSelection = false;
            this.AvatarView.Location = new System.Drawing.Point(779, 200);
            this.AvatarView.Name = "AvatarView";
            this.AvatarView.Size = new System.Drawing.Size(121, 556);
            this.AvatarView.TabIndex = 14;
            this.AvatarView.UseCompatibleStateImageBehavior = false;
            this.AvatarView.SelectedIndexChanged += new System.EventHandler(this.AvatarView_SelectedIndexChanged);
            // 
            // rmvButton
            // 
            this.rmvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rmvButton.Location = new System.Drawing.Point(773, 164);
            this.rmvButton.Margin = new System.Windows.Forms.Padding(2);
            this.rmvButton.Name = "rmvButton";
            this.rmvButton.Size = new System.Drawing.Size(72, 24);
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
            this.chkAllowUserSwitch.Location = new System.Drawing.Point(681, 71);
            this.chkAllowUserSwitch.Name = "chkAllowUserSwitch";
            this.chkAllowUserSwitch.Size = new System.Drawing.Size(83, 17);
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
            this.chkShowAvatars.Location = new System.Drawing.Point(681, 53);
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
            this.MSG.Location = new System.Drawing.Point(1, 38);
            this.MSG.Name = "MSG";
            this.MSG.Size = new System.Drawing.Size(560, 26);
            this.MSG.TabIndex = 18;
            this.MSG.WordWrap = false;
            this.MSG.Enter += new System.EventHandler(this.MSGEnter);
            this.MSG.Leave += new System.EventHandler(this.MSGLeave);
            this.MSG.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.checkKeys);
            // 
            // msgSelected
            // 
            this.msgSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.msgSelected.Enabled = false;
            this.msgSelected.Location = new System.Drawing.Point(569, 78);
            this.msgSelected.Margin = new System.Windows.Forms.Padding(2);
            this.msgSelected.MaximumSize = new System.Drawing.Size(200, 24);
            this.msgSelected.MinimumSize = new System.Drawing.Size(104, 24);
            this.msgSelected.Name = "msgSelected";
            this.msgSelected.Size = new System.Drawing.Size(104, 24);
            this.msgSelected.TabIndex = 19;
            this.msgSelected.Text = "MSG to Selected";
            this.msgSelected.UseVisualStyleBackColor = true;
            this.msgSelected.Click += new System.EventHandler(this.msgSelected_Click);
            // 
            // msgAll
            // 
            this.msgAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.msgAll.Enabled = false;
            this.msgAll.Location = new System.Drawing.Point(569, 53);
            this.msgAll.Margin = new System.Windows.Forms.Padding(2);
            this.msgAll.MaximumSize = new System.Drawing.Size(200, 24);
            this.msgAll.MinimumSize = new System.Drawing.Size(104, 24);
            this.msgAll.Name = "msgAll";
            this.msgAll.Size = new System.Drawing.Size(104, 24);
            this.msgAll.TabIndex = 20;
            this.msgAll.Text = "MSG to All";
            this.msgAll.UseVisualStyleBackColor = true;
            this.msgAll.Click += new System.EventHandler(this.msgAll_Click);
            // 
            // composeMSG
            // 
            this.composeMSG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.composeMSG.Location = new System.Drawing.Point(569, 28);
            this.composeMSG.Margin = new System.Windows.Forms.Padding(2);
            this.composeMSG.MaximumSize = new System.Drawing.Size(200, 24);
            this.composeMSG.MinimumSize = new System.Drawing.Size(104, 24);
            this.composeMSG.Name = "composeMSG";
            this.composeMSG.Size = new System.Drawing.Size(104, 24);
            this.composeMSG.TabIndex = 21;
            this.composeMSG.Text = "Compose MSG";
            this.composeMSG.UseVisualStyleBackColor = true;
            this.composeMSG.Click += new System.EventHandler(this.composeMSG_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(770, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Res";
            // 
            // reply2Selected
            // 
            this.reply2Selected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.reply2Selected.Enabled = false;
            this.reply2Selected.Location = new System.Drawing.Point(569, 103);
            this.reply2Selected.Margin = new System.Windows.Forms.Padding(2);
            this.reply2Selected.MaximumSize = new System.Drawing.Size(200, 24);
            this.reply2Selected.MinimumSize = new System.Drawing.Size(104, 24);
            this.reply2Selected.Name = "reply2Selected";
            this.reply2Selected.Size = new System.Drawing.Size(104, 24);
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
            this.chkDrawPath.Location = new System.Drawing.Point(803, 58);
            this.chkDrawPath.Name = "chkDrawPath";
            this.chkDrawPath.Size = new System.Drawing.Size(76, 17);
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
            this.boxSetSignal.ItemHeight = 20;
            this.boxSetSignal.Location = new System.Drawing.Point(209, 205);
            this.boxSetSignal.MinimumSize = new System.Drawing.Size(160, 100);
            this.boxSetSignal.Name = "boxSetSignal";
            this.boxSetSignal.Size = new System.Drawing.Size(164, 100);
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
            this.boxSetSwitch.ItemHeight = 20;
            this.boxSetSwitch.Items.AddRange(new object[] {
            "To Main Route",
            "To Side Route"});
            this.boxSetSwitch.Location = new System.Drawing.Point(398, 205);
            this.boxSetSwitch.MinimumSize = new System.Drawing.Size(120, 50);
            this.boxSetSwitch.Name = "boxSetSwitch";
            this.boxSetSwitch.Size = new System.Drawing.Size(125, 40);
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
            this.chkPickSignals.Location = new System.Drawing.Point(803, 75);
            this.chkPickSignals.Name = "chkPickSignals";
            this.chkPickSignals.Size = new System.Drawing.Size(84, 17);
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
            this.chkPickSwitches.Location = new System.Drawing.Point(803, 92);
            this.chkPickSwitches.Name = "chkPickSwitches";
            this.chkPickSwitches.Size = new System.Drawing.Size(93, 17);
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
            this.chkAllowNew.Location = new System.Drawing.Point(681, 34);
            this.chkAllowNew.Name = "chkAllowNew";
            this.chkAllowNew.Size = new System.Drawing.Size(67, 17);
            this.chkAllowNew.TabIndex = 29;
            this.chkAllowNew.Text = "Can Join";
            this.chkAllowNew.UseVisualStyleBackColor = true;
            this.chkAllowNew.CheckedChanged += new System.EventHandler(this.chkAllowNewCheck);
            // 
            // messages
            // 
            this.messages.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messages.FormattingEnabled = true;
            this.messages.ItemHeight = 18;
            this.messages.Location = new System.Drawing.Point(1, 68);
            this.messages.Name = "messages";
            this.messages.Size = new System.Drawing.Size(560, 58);
            this.messages.TabIndex = 22;
            this.messages.SelectedIndexChanged += new System.EventHandler(this.msgSelectedChanged);
            // 
            // btnAssist
            // 
            this.btnAssist.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAssist.Location = new System.Drawing.Point(762, 111);
            this.btnAssist.Margin = new System.Windows.Forms.Padding(2);
            this.btnAssist.Name = "btnAssist";
            this.btnAssist.Size = new System.Drawing.Size(48, 24);
            this.btnAssist.TabIndex = 30;
            this.btnAssist.Text = "Assist";
            this.btnAssist.UseVisualStyleBackColor = true;
            this.btnAssist.Click += new System.EventHandler(this.AssistClick);
            // 
            // btnNormal
            // 
            this.btnNormal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNormal.Location = new System.Drawing.Point(762, 136);
            this.btnNormal.Margin = new System.Windows.Forms.Padding(2);
            this.btnNormal.Name = "btnNormal";
            this.btnNormal.Size = new System.Drawing.Size(58, 24);
            this.btnNormal.TabIndex = 31;
            this.btnNormal.Text = "Normal";
            this.btnNormal.UseVisualStyleBackColor = true;
            this.btnNormal.Click += new System.EventHandler(this.btnNormalClick);
            // 
            // btnFollow
            // 
            this.btnFollow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFollow.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFollow.Location = new System.Drawing.Point(847, 165);
            this.btnFollow.Name = "btnFollow";
            this.btnFollow.Size = new System.Drawing.Size(60, 23);
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
            this.chkBoxPenalty.Location = new System.Drawing.Point(681, 107);
            this.chkBoxPenalty.Name = "chkBoxPenalty";
            this.chkBoxPenalty.Size = new System.Drawing.Size(61, 17);
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
            this.chkPreferGreen.Location = new System.Drawing.Point(681, 89);
            this.chkPreferGreen.Name = "chkPreferGreen";
            this.chkPreferGreen.Size = new System.Drawing.Size(86, 17);
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
            this.btnSeeInGame.Location = new System.Drawing.Point(820, 113);
            this.btnSeeInGame.Name = "btnSeeInGame";
            this.btnSeeInGame.Size = new System.Drawing.Size(93, 23);
            this.btnSeeInGame.TabIndex = 35;
            this.btnSeeInGame.Text = "See in Game";
            this.btnSeeInGame.UseVisualStyleBackColor = true;
            this.btnSeeInGame.Click += new System.EventHandler(this.btnSeeInGameClick);
            // 
            // lblSimulationTimeText
            // 
            this.lblSimulationTimeText.AutoSize = true;
            this.lblSimulationTimeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSimulationTimeText.Location = new System.Drawing.Point(5, 34);
            this.lblSimulationTimeText.Name = "lblSimulationTimeText";
            this.lblSimulationTimeText.Size = new System.Drawing.Size(103, 16);
            this.lblSimulationTimeText.TabIndex = 36;
            this.lblSimulationTimeText.Text = "Simulation Time";
            this.lblSimulationTimeText.Visible = false;
            // 
            // lblSimulationTime
            // 
            this.lblSimulationTime.AutoSize = true;
            this.lblSimulationTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSimulationTime.Location = new System.Drawing.Point(115, 34);
            this.lblSimulationTime.Name = "lblSimulationTime";
            this.lblSimulationTime.Size = new System.Drawing.Size(100, 16);
            this.lblSimulationTime.TabIndex = 37;
            this.lblSimulationTime.Text = "SimulationTime";
            this.lblSimulationTime.Visible = false;
            // 
            // lblShow
            // 
            this.lblShow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblShow.AutoSize = true;
            this.lblShow.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblShow.Location = new System.Drawing.Point(780, 180);
            this.lblShow.Name = "lblShow";
            this.lblShow.Size = new System.Drawing.Size(41, 15);
            this.lblShow.TabIndex = 38;
            this.lblShow.Text = "Show:";
            this.lblShow.Visible = false;
            // 
            // cbShowPlatformLabels
            // 
            this.cbShowPlatformLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowPlatformLabels.AutoSize = true;
            this.cbShowPlatformLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowPlatformLabels.Location = new System.Drawing.Point(780, 220);
            this.cbShowPlatformLabels.Name = "cbShowPlatformLabels";
            this.cbShowPlatformLabels.Size = new System.Drawing.Size(108, 19);
            this.cbShowPlatformLabels.TabIndex = 39;
            this.cbShowPlatformLabels.Text = "Platform labels";
            this.cbShowPlatformLabels.UseVisualStyleBackColor = true;
            this.cbShowPlatformLabels.Visible = false;
            // 
            // cbShowSidings
            // 
            this.cbShowSidings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowSidings.AutoSize = true;
            this.cbShowSidings.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowSidings.Location = new System.Drawing.Point(780, 240);
            this.cbShowSidings.Name = "cbShowSidings";
            this.cbShowSidings.Size = new System.Drawing.Size(97, 19);
            this.cbShowSidings.TabIndex = 40;
            this.cbShowSidings.Text = "Siding labels";
            this.cbShowSidings.UseVisualStyleBackColor = true;
            this.cbShowSidings.Visible = false;
            // 
            // cbShowSignals
            // 
            this.cbShowSignals.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowSignals.AutoSize = true;
            this.cbShowSignals.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowSignals.Location = new System.Drawing.Point(780, 280);
            this.cbShowSignals.Name = "cbShowSignals";
            this.cbShowSignals.Size = new System.Drawing.Size(67, 19);
            this.cbShowSignals.TabIndex = 41;
            this.cbShowSignals.Text = "Signals";
            this.cbShowSignals.UseVisualStyleBackColor = true;
            this.cbShowSignals.Visible = false;
            // 
            // cbShowSignalState
            // 
            this.cbShowSignalState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowSignalState.AutoSize = true;
            this.cbShowSignalState.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowSignalState.Location = new System.Drawing.Point(802, 300);
            this.cbShowSignalState.Name = "cbShowSignalState";
            this.cbShowSignalState.Size = new System.Drawing.Size(90, 19);
            this.cbShowSignalState.TabIndex = 42;
            this.cbShowSignalState.Text = "Signal state";
            this.cbShowSignalState.UseVisualStyleBackColor = true;
            this.cbShowSignalState.Visible = false;
            // 
            // gbTrainLabels
            // 
            this.gbTrainLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbTrainLabels.Controls.Add(this.bTrainKey);
            this.gbTrainLabels.Controls.Add(this.rbShowActiveTrainLabels);
            this.gbTrainLabels.Controls.Add(this.rbShowAllTrainLabels);
            this.gbTrainLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbTrainLabels.Location = new System.Drawing.Point(779, 323);
            this.gbTrainLabels.Name = "gbTrainLabels";
            this.gbTrainLabels.Size = new System.Drawing.Size(120, 129);
            this.gbTrainLabels.TabIndex = 43;
            this.gbTrainLabels.TabStop = false;
            this.gbTrainLabels.Text = "Train labels";
            this.gbTrainLabels.Visible = false;
            // 
            // bTrainKey
            // 
            this.bTrainKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bTrainKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bTrainKey.Location = new System.Drawing.Point(77, 89);
            this.bTrainKey.Name = "bTrainKey";
            this.bTrainKey.Size = new System.Drawing.Size(40, 23);
            this.bTrainKey.TabIndex = 57;
            this.bTrainKey.Text = "Key";
            this.bTrainKey.UseVisualStyleBackColor = true;
            this.bTrainKey.Visible = false;
            this.bTrainKey.Click += new System.EventHandler(this.bTrainKey_Click);
            // 
            // rbShowActiveTrainLabels
            // 
            this.rbShowActiveTrainLabels.AutoSize = true;
            this.rbShowActiveTrainLabels.Checked = true;
            this.rbShowActiveTrainLabels.Location = new System.Drawing.Point(13, 22);
            this.rbShowActiveTrainLabels.Name = "rbShowActiveTrainLabels";
            this.rbShowActiveTrainLabels.Size = new System.Drawing.Size(81, 19);
            this.rbShowActiveTrainLabels.TabIndex = 1;
            this.rbShowActiveTrainLabels.TabStop = true;
            this.rbShowActiveTrainLabels.Text = "Active only";
            this.rbShowActiveTrainLabels.UseVisualStyleBackColor = true;
            this.rbShowActiveTrainLabels.Visible = false;
            // 
            // rbShowAllTrainLabels
            // 
            this.rbShowAllTrainLabels.AutoSize = true;
            this.rbShowAllTrainLabels.Location = new System.Drawing.Point(13, 44);
            this.rbShowAllTrainLabels.Name = "rbShowAllTrainLabels";
            this.rbShowAllTrainLabels.Size = new System.Drawing.Size(38, 19);
            this.rbShowAllTrainLabels.TabIndex = 0;
            this.rbShowAllTrainLabels.Text = "All";
            this.rbShowAllTrainLabels.UseVisualStyleBackColor = true;
            this.rbShowAllTrainLabels.Visible = false;
            // 
            // nudDaylightOffsetHrs
            // 
            this.nudDaylightOffsetHrs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudDaylightOffsetHrs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudDaylightOffsetHrs.Location = new System.Drawing.Point(817, 556);
            this.nudDaylightOffsetHrs.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.nudDaylightOffsetHrs.Minimum = new decimal(new int[] {
            12,
            0,
            0,
            -2147483648});
            this.nudDaylightOffsetHrs.Name = "nudDaylightOffsetHrs";
            this.nudDaylightOffsetHrs.Size = new System.Drawing.Size(40, 21);
            this.nudDaylightOffsetHrs.TabIndex = 44;
            this.nudDaylightOffsetHrs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudDaylightOffsetHrs.Visible = false;
            this.nudDaylightOffsetHrs.ValueChanged += new System.EventHandler(this.nudDaylightOffsetHrs_ValueChanged);
            // 
            // lblDayLightOffsetHrs
            // 
            this.lblDayLightOffsetHrs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDayLightOffsetHrs.AutoSize = true;
            this.lblDayLightOffsetHrs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDayLightOffsetHrs.Location = new System.Drawing.Point(779, 534);
            this.lblDayLightOffsetHrs.Name = "lblDayLightOffsetHrs";
            this.lblDayLightOffsetHrs.Size = new System.Drawing.Size(111, 15);
            this.lblDayLightOffsetHrs.TabIndex = 45;
            this.lblDayLightOffsetHrs.Text = "Daylight offset (hrs)";
            this.lblDayLightOffsetHrs.Visible = false;
            // 
            // cdBackground
            // 
            this.cdBackground.AnyColor = true;
            this.cdBackground.ShowHelp = true;
            // 
            // bBackgroundColor
            // 
            this.bBackgroundColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bBackgroundColor.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bBackgroundColor.Location = new System.Drawing.Point(779, 590);
            this.bBackgroundColor.Name = "bBackgroundColor";
            this.bBackgroundColor.Size = new System.Drawing.Size(131, 23);
            this.bBackgroundColor.TabIndex = 46;
            this.bBackgroundColor.Text = "Background color";
            this.bBackgroundColor.UseVisualStyleBackColor = true;
            this.bBackgroundColor.Visible = false;
            this.bBackgroundColor.Click += new System.EventHandler(this.bBackgroundColor_Click);
            // 
            // cbShowSwitches
            // 
            this.cbShowSwitches.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowSwitches.AutoSize = true;
            this.cbShowSwitches.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowSwitches.Location = new System.Drawing.Point(780, 260);
            this.cbShowSwitches.Name = "cbShowSwitches";
            this.cbShowSwitches.Size = new System.Drawing.Size(75, 19);
            this.cbShowSwitches.TabIndex = 47;
            this.cbShowSwitches.Text = "Switches";
            this.cbShowSwitches.UseVisualStyleBackColor = true;
            this.cbShowSwitches.Visible = false;
            // 
            // lblInstruction1
            // 
            this.lblInstruction1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction1.Location = new System.Drawing.Point(8, 717);
            this.lblInstruction1.Name = "lblInstruction1";
            this.lblInstruction1.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction1.Size = new System.Drawing.Size(327, 22);
            this.lblInstruction1.TabIndex = 48;
            this.lblInstruction1.Text = "To pan, drag with left mouse.";
            this.lblInstruction1.Visible = false;
            // 
            // cbShowTrainLabels
            // 
            this.cbShowTrainLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowTrainLabels.AutoSize = true;
            this.cbShowTrainLabels.Checked = true;
            this.cbShowTrainLabels.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbShowTrainLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowTrainLabels.Location = new System.Drawing.Point(790, 395);
            this.cbShowTrainLabels.Name = "cbShowTrainLabels";
            this.cbShowTrainLabels.Size = new System.Drawing.Size(60, 19);
            this.cbShowTrainLabels.TabIndex = 50;
            this.cbShowTrainLabels.Text = "Name";
            this.cbShowTrainLabels.UseVisualStyleBackColor = true;
            this.cbShowTrainLabels.Visible = false;
            // 
            // tWindow
            // 
            this.tWindow.Controls.Add(this.tDispatch);
            this.tWindow.Controls.Add(this.tTimetable);
            this.tWindow.Location = new System.Drawing.Point(0, 0);
            this.tWindow.Name = "tWindow";
            this.tWindow.SelectedIndex = 0;
            this.tWindow.Size = new System.Drawing.Size(923, 32);
            this.tWindow.TabIndex = 51;
            this.tWindow.SelectedIndexChanged += new System.EventHandler(this.tWindow_SelectedIndexChanged);
            // 
            // tDispatch
            // 
            this.tDispatch.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tDispatch.Location = new System.Drawing.Point(4, 22);
            this.tDispatch.Name = "tDispatch";
            this.tDispatch.Padding = new System.Windows.Forms.Padding(3);
            this.tDispatch.Size = new System.Drawing.Size(915, 6);
            this.tDispatch.TabIndex = 0;
            this.tDispatch.Text = "Dispatch";
            this.tDispatch.UseVisualStyleBackColor = true;
            // 
            // tTimetable
            // 
            this.tTimetable.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tTimetable.Location = new System.Drawing.Point(4, 22);
            this.tTimetable.Name = "tTimetable";
            this.tTimetable.Padding = new System.Windows.Forms.Padding(3);
            this.tTimetable.Size = new System.Drawing.Size(915, 6);
            this.tTimetable.TabIndex = 1;
            this.tTimetable.Text = "Timetable";
            this.tTimetable.UseVisualStyleBackColor = true;
            // 
            // cbShowTrainState
            // 
            this.cbShowTrainState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowTrainState.AutoSize = true;
            this.cbShowTrainState.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowTrainState.Location = new System.Drawing.Point(802, 415);
            this.cbShowTrainState.Name = "cbShowTrainState";
            this.cbShowTrainState.Size = new System.Drawing.Size(54, 19);
            this.cbShowTrainState.TabIndex = 52;
            this.cbShowTrainState.Text = "State";
            this.cbShowTrainState.UseVisualStyleBackColor = true;
            this.cbShowTrainState.Visible = false;
            // 
            // lblInstruction2
            // 
            this.lblInstruction2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction2.Location = new System.Drawing.Point(8, 738);
            this.lblInstruction2.Name = "lblInstruction2";
            this.lblInstruction2.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction2.Size = new System.Drawing.Size(327, 21);
            this.lblInstruction2.TabIndex = 53;
            this.lblInstruction2.Text = "To zoom, drag with left and right mouse or scroll mouse wheel.";
            this.lblInstruction2.Visible = false;
            // 
            // lblInstruction3
            // 
            this.lblInstruction3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction3.Location = new System.Drawing.Point(8, 759);
            this.lblInstruction3.Name = "lblInstruction3";
            this.lblInstruction3.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction3.Size = new System.Drawing.Size(327, 21);
            this.lblInstruction3.TabIndex = 54;
            this.lblInstruction3.Text = "To zoom in to a location, press Shift and click the left mouse.";
            this.lblInstruction3.Visible = false;
            // 
            // lblInstruction4
            // 
            this.lblInstruction4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction4.Location = new System.Drawing.Point(8, 780);
            this.lblInstruction4.Name = "lblInstruction4";
            this.lblInstruction4.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction4.Size = new System.Drawing.Size(327, 21);
            this.lblInstruction4.TabIndex = 55;
            this.lblInstruction4.Text = "To zoom out of a location, press Alt and click the left mouse.";
            this.lblInstruction4.Visible = false;
            // 
            // cbShowPlatforms
            // 
            this.cbShowPlatforms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShowPlatforms.AutoSize = true;
            this.cbShowPlatforms.Checked = true;
            this.cbShowPlatforms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbShowPlatforms.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowPlatforms.Location = new System.Drawing.Point(780, 200);
            this.cbShowPlatforms.Name = "cbShowPlatforms";
            this.cbShowPlatforms.Size = new System.Drawing.Size(78, 19);
            this.cbShowPlatforms.TabIndex = 56;
            this.cbShowPlatforms.Text = "Platforms";
            this.cbShowPlatforms.UseVisualStyleBackColor = true;
            this.cbShowPlatforms.Visible = false;
            // 
            // DispatchViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(923, 749);
            this.Controls.Add(this.cbShowPlatforms);
            this.Controls.Add(this.lblInstruction4);
            this.Controls.Add(this.lblInstruction3);
            this.Controls.Add(this.lblInstruction2);
            this.Controls.Add(this.cbShowTrainState);
            this.Controls.Add(this.cbShowTrainLabels);
            this.Controls.Add(this.lblInstruction1);
            this.Controls.Add(this.cbShowSwitches);
            this.Controls.Add(this.bBackgroundColor);
            this.Controls.Add(this.lblDayLightOffsetHrs);
            this.Controls.Add(this.nudDaylightOffsetHrs);
            this.Controls.Add(this.gbTrainLabels);
            this.Controls.Add(this.cbShowSignalState);
            this.Controls.Add(this.cbShowSignals);
            this.Controls.Add(this.cbShowSidings);
            this.Controls.Add(this.cbShowPlatformLabels);
            this.Controls.Add(this.lblShow);
            this.Controls.Add(this.lblSimulationTime);
            this.Controls.Add(this.lblSimulationTimeText);
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
            this.Controls.Add(this.pbCanvas);
            this.Controls.Add(this.tWindow);
            this.Name = "DispatchViewer";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Map Window";
            this.Leave += new System.EventHandler(this.DispatcherLeave);
            ((System.ComponentModel.ISupportInitialize)(this.pbCanvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).EndInit();
            this.gbTrainLabels.ResumeLayout(false);
            this.gbTrainLabels.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDaylightOffsetHrs)).EndInit();
            this.tWindow.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

      }

      #endregion
	  private System.Windows.Forms.ListBox boxSetSignal;
	  private System.Windows.Forms.ListBox boxSetSwitch;
        private System.Windows.Forms.ColorDialog cdBackground;
        private System.Windows.Forms.Label lblInstruction1;
        private System.Windows.Forms.TabPage tDispatch;
        private System.Windows.Forms.TabPage tTimetable;
        public System.Windows.Forms.Button refreshButton;
        public System.Windows.Forms.NumericUpDown windowSizeUpDown;
        public System.Windows.Forms.Label resLabel;
        public System.Windows.Forms.ListView AvatarView;
        public System.Windows.Forms.Button rmvButton;
        public System.Windows.Forms.CheckBox chkAllowUserSwitch;
        public System.Windows.Forms.CheckBox chkShowAvatars;
        public System.Windows.Forms.TextBox MSG;
        public System.Windows.Forms.Button msgSelected;
        public System.Windows.Forms.Button msgAll;
        public System.Windows.Forms.Button composeMSG;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Button reply2Selected;
        public System.Windows.Forms.CheckBox chkDrawPath;
        public System.Windows.Forms.CheckBox chkPickSignals;
        public System.Windows.Forms.CheckBox chkPickSwitches;
        public System.Windows.Forms.CheckBox chkAllowNew;
        public System.Windows.Forms.ListBox messages;
        public System.Windows.Forms.Button btnAssist;
        public System.Windows.Forms.Button btnNormal;
        public System.Windows.Forms.Button btnFollow;
        public System.Windows.Forms.CheckBox chkBoxPenalty;
        public System.Windows.Forms.CheckBox chkPreferGreen;
        public System.Windows.Forms.Button btnSeeInGame;
        public System.Windows.Forms.Label lblSimulationTimeText;
        public System.Windows.Forms.Label lblSimulationTime;
        public System.Windows.Forms.Label lblShow;
        public System.Windows.Forms.CheckBox cbShowPlatformLabels;
        public System.Windows.Forms.CheckBox cbShowSidings;
        public System.Windows.Forms.CheckBox cbShowSignals;
        public System.Windows.Forms.CheckBox cbShowSignalState;
        public System.Windows.Forms.GroupBox gbTrainLabels;
        public System.Windows.Forms.RadioButton rbShowActiveTrainLabels;
        public System.Windows.Forms.RadioButton rbShowAllTrainLabels;
        public System.Windows.Forms.NumericUpDown nudDaylightOffsetHrs;
        public System.Windows.Forms.Label lblDayLightOffsetHrs;
        public System.Windows.Forms.Button bBackgroundColor;
        public System.Windows.Forms.CheckBox cbShowSwitches;
        public System.Windows.Forms.CheckBox cbShowTrainLabels;
        public System.Windows.Forms.PictureBox pbCanvas;
        public System.Windows.Forms.TabControl tWindow;
        public System.Windows.Forms.CheckBox cbShowTrainState;
        private System.Windows.Forms.Label lblInstruction2;
        private System.Windows.Forms.Label lblInstruction3;
        private System.Windows.Forms.Label lblInstruction4;
        public System.Windows.Forms.CheckBox cbShowPlatforms;
        public System.Windows.Forms.Button bTrainKey;
    }
}
