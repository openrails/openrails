namespace Orts.Viewer3D.Debugging
{
    partial class MapViewer
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
            components = new System.ComponentModel.Container();
            var listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] { "Player1 (you)" }, -1, System.Drawing.Color.Empty, System.Drawing.Color.Empty, new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point));
            var listViewItem2 = new System.Windows.Forms.ListViewItem("Player2");
            var listViewItem3 = new System.Windows.Forms.ListViewItem("Player3");
            var listViewItem4 = new System.Windows.Forms.ListViewItem("...");
            playerRolePanel = new System.Windows.Forms.Panel();
            playerRoleLink = new System.Windows.Forms.LinkLabel();
            playerRoleExplanation = new System.Windows.Forms.Label();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            messagesPanel = new System.Windows.Forms.Panel();
            messages = new System.Windows.Forms.ListBox();
            messageAllButton = new System.Windows.Forms.Button();
            moreReplyOptionsButton = new System.Windows.Forms.Button();
            messageInput = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            canvasPanel = new System.Windows.Forms.Panel();
            timeLabel = new System.Windows.Forms.Label();
            mapCustomizationButton = new System.Windows.Forms.Button();
            mapCustomizationPanel = new System.Windows.Forms.Panel();
            showAllTrainsRadio = new System.Windows.Forms.RadioButton();
            showActiveTrainsRadio = new System.Windows.Forms.RadioButton();
            showTrainStateCheckbox = new System.Windows.Forms.CheckBox();
            showTrainLabelsCheckbox = new System.Windows.Forms.CheckBox();
            showSignalStateCheckbox = new System.Windows.Forms.CheckBox();
            showSignalsCheckbox = new System.Windows.Forms.CheckBox();
            showSwitchesCheckbox = new System.Windows.Forms.CheckBox();
            showSidingLabelsCheckbox = new System.Windows.Forms.CheckBox();
            showPlatformLabelsCheckbox = new System.Windows.Forms.CheckBox();
            showPlatformsCheckbox = new System.Windows.Forms.CheckBox();
            label1 = new System.Windows.Forms.Label();
            rotateThemesButton = new System.Windows.Forms.Button();
            showTimeCheckbox = new System.Windows.Forms.CheckBox();
            useAntiAliasingCheckbox = new System.Windows.Forms.CheckBox();
            mapCanvas = new System.Windows.Forms.PictureBox();
            tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            panel1 = new System.Windows.Forms.Panel();
            multiplayerSettingsPanel = new System.Windows.Forms.Panel();
            groupBox2 = new System.Windows.Forms.GroupBox();
            chkAllowUserSwitch = new System.Windows.Forms.CheckBox();
            penaltyCheckbox = new System.Windows.Forms.CheckBox();
            preferGreenCheckbox = new System.Windows.Forms.CheckBox();
            allowJoiningCheckbox = new System.Windows.Forms.CheckBox();
            mapSettingsPanel = new System.Windows.Forms.Panel();
            groupBox1 = new System.Windows.Forms.GroupBox();
            followMyTrainOnMap = new System.Windows.Forms.Button();
            centerOnMyTrainButton = new System.Windows.Forms.Button();
            seeTrainInGameButton = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            mapResolutionUpDown = new System.Windows.Forms.NumericUpDown();
            allowThrowingSwitchesCheckbox = new System.Windows.Forms.CheckBox();
            allowChangingSignalsCheckbox = new System.Windows.Forms.CheckBox();
            drawPathCheckbox = new System.Windows.Forms.CheckBox();
            playersPanel = new System.Windows.Forms.Panel();
            groupBox3 = new System.Windows.Forms.GroupBox();
            playersView = new System.Windows.Forms.ListView();
            messageActionsMenu = new System.Windows.Forms.ContextMenuStrip(components);
            messageSelectedPlayerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            replyToSelectedPlayerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            playerActionsMenu = new System.Windows.Forms.ContextMenuStrip(components);
            playerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            makeThisPlayerAnAssistantToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            jumpToThisPlayerInGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            followToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            kickFromMultiplayerSessionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setSwitchMenu = new System.Windows.Forms.ContextMenuStrip(components);
            setSwitchToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mainRouteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sideRouteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setSignalMenu = new System.Windows.Forms.ContextMenuStrip(components);
            setSignalAspectToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            systemControlledToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            approachToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            proceedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            allowCallOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            trainActionsMenu = new System.Windows.Forms.ContextMenuStrip(components);
            jumpToThisTrainInGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            followThisTrainOnTheMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            playerRolePanel.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            messagesPanel.SuspendLayout();
            canvasPanel.SuspendLayout();
            mapCustomizationPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mapCanvas).BeginInit();
            tableLayoutPanel4.SuspendLayout();
            multiplayerSettingsPanel.SuspendLayout();
            groupBox2.SuspendLayout();
            mapSettingsPanel.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mapResolutionUpDown).BeginInit();
            playersPanel.SuspendLayout();
            groupBox3.SuspendLayout();
            messageActionsMenu.SuspendLayout();
            playerActionsMenu.SuspendLayout();
            setSwitchMenu.SuspendLayout();
            setSignalMenu.SuspendLayout();
            trainActionsMenu.SuspendLayout();
            SuspendLayout();
            // 
            // playerRolePanel
            // 
            playerRolePanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            playerRolePanel.BackColor = System.Drawing.Color.FromArgb(214, 234, 248);
            playerRolePanel.Controls.Add(playerRoleLink);
            playerRolePanel.Controls.Add(playerRoleExplanation);
            playerRolePanel.Location = new System.Drawing.Point(0, 0);
            playerRolePanel.Margin = new System.Windows.Forms.Padding(0);
            playerRolePanel.Name = "playerRolePanel";
            playerRolePanel.Size = new System.Drawing.Size(915, 35);
            playerRolePanel.TabIndex = 0;
            playerRolePanel.Visible = false;
            // 
            // playerRoleLink
            // 
            playerRoleLink.ActiveLinkColor = System.Drawing.Color.FromArgb(27, 79, 114);
            playerRoleLink.Anchor = System.Windows.Forms.AnchorStyles.Right;
            playerRoleLink.AutoSize = true;
            playerRoleLink.LinkColor = System.Drawing.Color.FromArgb(40, 116, 166);
            playerRoleLink.Location = new System.Drawing.Point(833, 10);
            playerRoleLink.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            playerRoleLink.Name = "playerRoleLink";
            playerRoleLink.Size = new System.Drawing.Size(67, 15);
            playerRoleLink.TabIndex = 1;
            playerRoleLink.TabStop = true;
            playerRoleLink.Text = "Learn more";
            playerRoleLink.LinkClicked += playerRoleLink_LinkClicked;
            // 
            // playerRoleExplanation
            // 
            playerRoleExplanation.AutoSize = true;
            playerRoleExplanation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            playerRoleExplanation.ForeColor = System.Drawing.Color.FromArgb(40, 116, 166);
            playerRoleExplanation.Location = new System.Drawing.Point(14, 10);
            playerRoleExplanation.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            playerRoleExplanation.Name = "playerRoleExplanation";
            playerRoleExplanation.Size = new System.Drawing.Size(284, 13);
            playerRoleExplanation.TabIndex = 0;
            playerRoleExplanation.Text = "You are the dispatcher in this multiplayer session";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(playerRolePanel, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 1);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            tableLayoutPanel1.Size = new System.Drawing.Size(915, 647);
            tableLayoutPanel1.TabIndex = 3;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 233F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel4, 1, 0);
            tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(0, 35);
            tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new System.Drawing.Size(915, 613);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(messagesPanel, 0, 0);
            tableLayoutPanel3.Controls.Add(canvasPanel, 0, 1);
            tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new System.Drawing.Size(682, 613);
            tableLayoutPanel3.TabIndex = 0;
            // 
            // messagesPanel
            // 
            messagesPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            messagesPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            messagesPanel.Controls.Add(messages);
            messagesPanel.Controls.Add(messageAllButton);
            messagesPanel.Controls.Add(moreReplyOptionsButton);
            messagesPanel.Controls.Add(messageInput);
            messagesPanel.Controls.Add(label2);
            messagesPanel.Location = new System.Drawing.Point(0, 0);
            messagesPanel.Margin = new System.Windows.Forms.Padding(0);
            messagesPanel.Name = "messagesPanel";
            messagesPanel.Padding = new System.Windows.Forms.Padding(12);
            messagesPanel.Size = new System.Drawing.Size(682, 188);
            messagesPanel.TabIndex = 0;
            messagesPanel.Visible = false;
            // 
            // messages
            // 
            messages.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            messages.Enabled = false;
            messages.FormattingEnabled = true;
            messages.IntegralHeight = false;
            messages.ItemHeight = 15;
            messages.Location = new System.Drawing.Point(19, 30);
            messages.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            messages.Name = "messages";
            messages.Size = new System.Drawing.Size(647, 111);
            messages.TabIndex = 5;
            // 
            // messageAllButton
            // 
            messageAllButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            messageAllButton.AutoEllipsis = true;
            messageAllButton.Location = new System.Drawing.Point(492, 148);
            messageAllButton.Margin = new System.Windows.Forms.Padding(0);
            messageAllButton.Name = "messageAllButton";
            messageAllButton.Size = new System.Drawing.Size(147, 27);
            messageAllButton.TabIndex = 4;
            messageAllButton.Text = "Message all players";
            messageAllButton.UseVisualStyleBackColor = true;
            messageAllButton.Click += messageAllButton_Click;
            // 
            // moreReplyOptionsButton
            // 
            moreReplyOptionsButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            moreReplyOptionsButton.AutoSize = true;
            moreReplyOptionsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            moreReplyOptionsButton.Location = new System.Drawing.Point(641, 148);
            moreReplyOptionsButton.Margin = new System.Windows.Forms.Padding(0);
            moreReplyOptionsButton.Name = "moreReplyOptionsButton";
            moreReplyOptionsButton.Size = new System.Drawing.Size(25, 25);
            moreReplyOptionsButton.TabIndex = 3;
            moreReplyOptionsButton.Text = ">";
            moreReplyOptionsButton.UseVisualStyleBackColor = true;
            moreReplyOptionsButton.Click += moreReplyOptionsButton_Click;
            // 
            // messageInput
            // 
            messageInput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            messageInput.Location = new System.Drawing.Point(19, 150);
            messageInput.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            messageInput.Name = "messageInput";
            messageInput.Size = new System.Drawing.Size(469, 23);
            messageInput.TabIndex = 2;
            messageInput.Enter += messageInput_Enter;
            messageInput.Leave += messageInput_Leave;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(15, 12);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(61, 15);
            label2.TabIndex = 0;
            label2.Text = "Messages:";
            // 
            // canvasPanel
            // 
            canvasPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            canvasPanel.Controls.Add(timeLabel);
            canvasPanel.Controls.Add(mapCustomizationButton);
            canvasPanel.Controls.Add(mapCustomizationPanel);
            canvasPanel.Controls.Add(mapCanvas);
            canvasPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            canvasPanel.Location = new System.Drawing.Point(0, 188);
            canvasPanel.Margin = new System.Windows.Forms.Padding(0);
            canvasPanel.Name = "canvasPanel";
            canvasPanel.Padding = new System.Windows.Forms.Padding(15);
            canvasPanel.Size = new System.Drawing.Size(682, 425);
            canvasPanel.TabIndex = 1;
            // 
            // timeLabel
            // 
            timeLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            timeLabel.AutoSize = true;
            timeLabel.BackColor = System.Drawing.Color.Transparent;
            timeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            timeLabel.Location = new System.Drawing.Point(29, 384);
            timeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            timeLabel.Name = "timeLabel";
            timeLabel.Size = new System.Drawing.Size(92, 13);
            timeLabel.TabIndex = 1;
            timeLabel.Text = "Simulation time";
            // 
            // mapCustomizationButton
            // 
            mapCustomizationButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            mapCustomizationButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mapCustomizationButton.FlatAppearance.BorderSize = 0;
            mapCustomizationButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            mapCustomizationButton.Location = new System.Drawing.Point(518, 374);
            mapCustomizationButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mapCustomizationButton.Name = "mapCustomizationButton";
            mapCustomizationButton.Size = new System.Drawing.Size(140, 27);
            mapCustomizationButton.TabIndex = 0;
            mapCustomizationButton.Tag = "mapCustomization";
            mapCustomizationButton.Text = "Map customization";
            mapCustomizationButton.UseVisualStyleBackColor = true;
            mapCustomizationButton.Click += mapCustomizationButton_Click;
            // 
            // mapCustomizationPanel
            // 
            mapCustomizationPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            mapCustomizationPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            mapCustomizationPanel.Controls.Add(showAllTrainsRadio);
            mapCustomizationPanel.Controls.Add(showActiveTrainsRadio);
            mapCustomizationPanel.Controls.Add(showTrainStateCheckbox);
            mapCustomizationPanel.Controls.Add(showTrainLabelsCheckbox);
            mapCustomizationPanel.Controls.Add(showSignalStateCheckbox);
            mapCustomizationPanel.Controls.Add(showSignalsCheckbox);
            mapCustomizationPanel.Controls.Add(showSwitchesCheckbox);
            mapCustomizationPanel.Controls.Add(showSidingLabelsCheckbox);
            mapCustomizationPanel.Controls.Add(showPlatformLabelsCheckbox);
            mapCustomizationPanel.Controls.Add(showPlatformsCheckbox);
            mapCustomizationPanel.Controls.Add(label1);
            mapCustomizationPanel.Controls.Add(rotateThemesButton);
            mapCustomizationPanel.Controls.Add(showTimeCheckbox);
            mapCustomizationPanel.Controls.Add(useAntiAliasingCheckbox);
            mapCustomizationPanel.Location = new System.Drawing.Point(436, 3);
            mapCustomizationPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mapCustomizationPanel.Name = "mapCustomizationPanel";
            mapCustomizationPanel.Size = new System.Drawing.Size(221, 363);
            mapCustomizationPanel.TabIndex = 0;
            mapCustomizationPanel.Visible = false;
            // 
            // showAllTrainsRadio
            // 
            showAllTrainsRadio.AutoSize = true;
            showAllTrainsRadio.Location = new System.Drawing.Point(12, 322);
            showAllTrainsRadio.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showAllTrainsRadio.Name = "showAllTrainsRadio";
            showAllTrainsRadio.Size = new System.Drawing.Size(71, 19);
            showAllTrainsRadio.TabIndex = 19;
            showAllTrainsRadio.Text = "All trains";
            showAllTrainsRadio.UseVisualStyleBackColor = true;
            // 
            // showActiveTrainsRadio
            // 
            showActiveTrainsRadio.AutoSize = true;
            showActiveTrainsRadio.Checked = true;
            showActiveTrainsRadio.Location = new System.Drawing.Point(12, 300);
            showActiveTrainsRadio.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showActiveTrainsRadio.Name = "showActiveTrainsRadio";
            showActiveTrainsRadio.Size = new System.Drawing.Size(116, 19);
            showActiveTrainsRadio.TabIndex = 18;
            showActiveTrainsRadio.TabStop = true;
            showActiveTrainsRadio.Text = "Only active trains";
            showActiveTrainsRadio.UseVisualStyleBackColor = true;
            // 
            // showTrainStateCheckbox
            // 
            showTrainStateCheckbox.AutoSize = true;
            showTrainStateCheckbox.Location = new System.Drawing.Point(33, 278);
            showTrainStateCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showTrainStateCheckbox.Name = "showTrainStateCheckbox";
            showTrainStateCheckbox.Size = new System.Drawing.Size(80, 19);
            showTrainStateCheckbox.TabIndex = 17;
            showTrainStateCheckbox.Text = "Train state";
            showTrainStateCheckbox.UseVisualStyleBackColor = true;
            // 
            // showTrainLabelsCheckbox
            // 
            showTrainLabelsCheckbox.AutoSize = true;
            showTrainLabelsCheckbox.Location = new System.Drawing.Point(12, 256);
            showTrainLabelsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showTrainLabelsCheckbox.Name = "showTrainLabelsCheckbox";
            showTrainLabelsCheckbox.Size = new System.Drawing.Size(85, 19);
            showTrainLabelsCheckbox.TabIndex = 16;
            showTrainLabelsCheckbox.Text = "Train labels";
            showTrainLabelsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSignalStateCheckbox
            // 
            showSignalStateCheckbox.AutoSize = true;
            showSignalStateCheckbox.Location = new System.Drawing.Point(33, 234);
            showSignalStateCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showSignalStateCheckbox.Name = "showSignalStateCheckbox";
            showSignalStateCheckbox.Size = new System.Drawing.Size(86, 19);
            showSignalStateCheckbox.TabIndex = 15;
            showSignalStateCheckbox.Text = "Signal state";
            showSignalStateCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSignalsCheckbox
            // 
            showSignalsCheckbox.AutoSize = true;
            showSignalsCheckbox.Location = new System.Drawing.Point(12, 212);
            showSignalsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showSignalsCheckbox.Name = "showSignalsCheckbox";
            showSignalsCheckbox.Size = new System.Drawing.Size(63, 19);
            showSignalsCheckbox.TabIndex = 14;
            showSignalsCheckbox.Text = "Signals";
            showSignalsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSwitchesCheckbox
            // 
            showSwitchesCheckbox.AutoSize = true;
            showSwitchesCheckbox.Location = new System.Drawing.Point(12, 190);
            showSwitchesCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showSwitchesCheckbox.Name = "showSwitchesCheckbox";
            showSwitchesCheckbox.Size = new System.Drawing.Size(72, 19);
            showSwitchesCheckbox.TabIndex = 13;
            showSwitchesCheckbox.Text = "Switches";
            showSwitchesCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSidingLabelsCheckbox
            // 
            showSidingLabelsCheckbox.AutoSize = true;
            showSidingLabelsCheckbox.Location = new System.Drawing.Point(12, 168);
            showSidingLabelsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showSidingLabelsCheckbox.Name = "showSidingLabelsCheckbox";
            showSidingLabelsCheckbox.Size = new System.Drawing.Size(92, 19);
            showSidingLabelsCheckbox.TabIndex = 12;
            showSidingLabelsCheckbox.Text = "Siding labels";
            showSidingLabelsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showPlatformLabelsCheckbox
            // 
            showPlatformLabelsCheckbox.AutoSize = true;
            showPlatformLabelsCheckbox.Location = new System.Drawing.Point(12, 147);
            showPlatformLabelsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showPlatformLabelsCheckbox.Name = "showPlatformLabelsCheckbox";
            showPlatformLabelsCheckbox.Size = new System.Drawing.Size(105, 19);
            showPlatformLabelsCheckbox.TabIndex = 11;
            showPlatformLabelsCheckbox.Text = "Platform labels";
            showPlatformLabelsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showPlatformsCheckbox
            // 
            showPlatformsCheckbox.AutoSize = true;
            showPlatformsCheckbox.Location = new System.Drawing.Point(12, 125);
            showPlatformsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showPlatformsCheckbox.Name = "showPlatformsCheckbox";
            showPlatformsCheckbox.Size = new System.Drawing.Size(77, 19);
            showPlatformsCheckbox.TabIndex = 10;
            showPlatformsCheckbox.Text = "Platforms";
            showPlatformsCheckbox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            label1.Location = new System.Drawing.Point(8, 105);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(81, 13);
            label1.TabIndex = 9;
            label1.Text = "Map features";
            // 
            // rotateThemesButton
            // 
            rotateThemesButton.AutoSize = true;
            rotateThemesButton.Location = new System.Drawing.Point(12, 60);
            rotateThemesButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rotateThemesButton.Name = "rotateThemesButton";
            rotateThemesButton.Size = new System.Drawing.Size(195, 29);
            rotateThemesButton.TabIndex = 8;
            rotateThemesButton.Text = "Rotate between themes";
            rotateThemesButton.UseVisualStyleBackColor = true;
            rotateThemesButton.Click += rotateThemesButton_Click;
            // 
            // showTimeCheckbox
            // 
            showTimeCheckbox.AutoSize = true;
            showTimeCheckbox.Checked = true;
            showTimeCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            showTimeCheckbox.Location = new System.Drawing.Point(12, 33);
            showTimeCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            showTimeCheckbox.Name = "showTimeCheckbox";
            showTimeCheckbox.Size = new System.Drawing.Size(141, 19);
            showTimeCheckbox.TabIndex = 1;
            showTimeCheckbox.Text = "Show simulation time";
            showTimeCheckbox.UseVisualStyleBackColor = true;
            // 
            // useAntiAliasingCheckbox
            // 
            useAntiAliasingCheckbox.AutoSize = true;
            useAntiAliasingCheckbox.Location = new System.Drawing.Point(12, 12);
            useAntiAliasingCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            useAntiAliasingCheckbox.Name = "useAntiAliasingCheckbox";
            useAntiAliasingCheckbox.Size = new System.Drawing.Size(113, 19);
            useAntiAliasingCheckbox.TabIndex = 0;
            useAntiAliasingCheckbox.Text = "Use anti-aliasing";
            useAntiAliasingCheckbox.UseVisualStyleBackColor = true;
            // 
            // mapCanvas
            // 
            mapCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            mapCanvas.Location = new System.Drawing.Point(15, 15);
            mapCanvas.Margin = new System.Windows.Forms.Padding(0);
            mapCanvas.Name = "mapCanvas";
            mapCanvas.Size = new System.Drawing.Size(652, 395);
            mapCanvas.TabIndex = 0;
            mapCanvas.TabStop = false;
            mapCanvas.MouseDown += mapCanvas_MouseDown;
            mapCanvas.MouseMove += mapCanvas_MouseMove;
            mapCanvas.MouseUp += mapCanvas_MouseUp;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 1;
            tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel4.Controls.Add(panel1, 0, 0);
            tableLayoutPanel4.Controls.Add(multiplayerSettingsPanel, 0, 1);
            tableLayoutPanel4.Controls.Add(mapSettingsPanel, 0, 2);
            tableLayoutPanel4.Controls.Add(playersPanel, 0, 3);
            tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel4.Location = new System.Drawing.Point(682, 0);
            tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 4;
            tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new System.Drawing.Size(233, 613);
            tableLayoutPanel4.TabIndex = 1;
            // 
            // panel1
            // 
            panel1.Location = new System.Drawing.Point(4, 3);
            panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(225, 16);
            panel1.TabIndex = 3;
            // 
            // multiplayerSettingsPanel
            // 
            multiplayerSettingsPanel.AutoSize = true;
            multiplayerSettingsPanel.Controls.Add(groupBox2);
            multiplayerSettingsPanel.Location = new System.Drawing.Point(0, 23);
            multiplayerSettingsPanel.Margin = new System.Windows.Forms.Padding(0);
            multiplayerSettingsPanel.Name = "multiplayerSettingsPanel";
            multiplayerSettingsPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            multiplayerSettingsPanel.Size = new System.Drawing.Size(233, 112);
            multiplayerSettingsPanel.TabIndex = 1;
            multiplayerSettingsPanel.Visible = false;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(chkAllowUserSwitch);
            groupBox2.Controls.Add(penaltyCheckbox);
            groupBox2.Controls.Add(preferGreenCheckbox);
            groupBox2.Controls.Add(allowJoiningCheckbox);
            groupBox2.Location = new System.Drawing.Point(12, 0);
            groupBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox2.Size = new System.Drawing.Size(210, 109);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "Multiplayer settings";
            // 
            // chkAllowUserSwitch
            // 
            chkAllowUserSwitch.AutoSize = true;
            chkAllowUserSwitch.Location = new System.Drawing.Point(8, 87);
            chkAllowUserSwitch.Name = "chkAllowUserSwitch";
            chkAllowUserSwitch.Size = new System.Drawing.Size(136, 19);
            chkAllowUserSwitch.TabIndex = 3;
            chkAllowUserSwitch.Text = "Allow manual switch";
            chkAllowUserSwitch.UseVisualStyleBackColor = true;
            chkAllowUserSwitch.CheckedChanged += chkAllowUserSwitch_CheckedChanged;
            // 
            // penaltyCheckbox
            // 
            penaltyCheckbox.AutoSize = true;
            penaltyCheckbox.Checked = true;
            penaltyCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            penaltyCheckbox.Location = new System.Drawing.Point(8, 66);
            penaltyCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            penaltyCheckbox.Name = "penaltyCheckbox";
            penaltyCheckbox.Size = new System.Drawing.Size(178, 19);
            penaltyCheckbox.TabIndex = 2;
            penaltyCheckbox.Text = "Penalty for speeding && SPAD";
            penaltyCheckbox.UseVisualStyleBackColor = true;
            penaltyCheckbox.CheckedChanged += penaltyCheckbox_CheckedChanged;
            // 
            // preferGreenCheckbox
            // 
            preferGreenCheckbox.AutoSize = true;
            preferGreenCheckbox.Checked = true;
            preferGreenCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            preferGreenCheckbox.Location = new System.Drawing.Point(8, 44);
            preferGreenCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            preferGreenCheckbox.Name = "preferGreenCheckbox";
            preferGreenCheckbox.Size = new System.Drawing.Size(129, 19);
            preferGreenCheckbox.TabIndex = 1;
            preferGreenCheckbox.Text = "Prefer green signals";
            preferGreenCheckbox.UseVisualStyleBackColor = true;
            preferGreenCheckbox.CheckedChanged += preferGreenCheckbox_CheckedChanged;
            // 
            // allowJoiningCheckbox
            // 
            allowJoiningCheckbox.AutoSize = true;
            allowJoiningCheckbox.Checked = true;
            allowJoiningCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            allowJoiningCheckbox.Location = new System.Drawing.Point(8, 22);
            allowJoiningCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            allowJoiningCheckbox.Name = "allowJoiningCheckbox";
            allowJoiningCheckbox.Size = new System.Drawing.Size(158, 19);
            allowJoiningCheckbox.TabIndex = 0;
            allowJoiningCheckbox.Text = "Allow new players to join";
            allowJoiningCheckbox.UseVisualStyleBackColor = true;
            allowJoiningCheckbox.CheckedChanged += allowJoiningCheckbox_CheckedChanged;
            // 
            // mapSettingsPanel
            // 
            mapSettingsPanel.AutoSize = true;
            mapSettingsPanel.Controls.Add(groupBox1);
            mapSettingsPanel.Location = new System.Drawing.Point(0, 135);
            mapSettingsPanel.Margin = new System.Windows.Forms.Padding(0);
            mapSettingsPanel.Name = "mapSettingsPanel";
            mapSettingsPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            mapSettingsPanel.Size = new System.Drawing.Size(233, 219);
            mapSettingsPanel.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(followMyTrainOnMap);
            groupBox1.Controls.Add(centerOnMyTrainButton);
            groupBox1.Controls.Add(seeTrainInGameButton);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(mapResolutionUpDown);
            groupBox1.Controls.Add(allowThrowingSwitchesCheckbox);
            groupBox1.Controls.Add(allowChangingSignalsCheckbox);
            groupBox1.Controls.Add(drawPathCheckbox);
            groupBox1.Location = new System.Drawing.Point(12, 3);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(210, 213);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Map settings and controls";
            // 
            // followMyTrainOnMap
            // 
            followMyTrainOnMap.AutoSize = true;
            followMyTrainOnMap.Location = new System.Drawing.Point(8, 180);
            followMyTrainOnMap.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            followMyTrainOnMap.Name = "followMyTrainOnMap";
            followMyTrainOnMap.Size = new System.Drawing.Size(195, 29);
            followMyTrainOnMap.TabIndex = 7;
            followMyTrainOnMap.Text = "Follow my train on the map";
            followMyTrainOnMap.UseVisualStyleBackColor = true;
            followMyTrainOnMap.Click += followMyTrainOnMap_Click;
            // 
            // centerOnMyTrainButton
            // 
            centerOnMyTrainButton.AutoSize = true;
            centerOnMyTrainButton.Location = new System.Drawing.Point(8, 147);
            centerOnMyTrainButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            centerOnMyTrainButton.Name = "centerOnMyTrainButton";
            centerOnMyTrainButton.Size = new System.Drawing.Size(200, 29);
            centerOnMyTrainButton.TabIndex = 6;
            centerOnMyTrainButton.Text = "Jump to my train on the map";
            centerOnMyTrainButton.UseVisualStyleBackColor = true;
            centerOnMyTrainButton.Click += centerOnMyTrainButton_Click;
            // 
            // seeTrainInGameButton
            // 
            seeTrainInGameButton.AutoSize = true;
            seeTrainInGameButton.Location = new System.Drawing.Point(8, 113);
            seeTrainInGameButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            seeTrainInGameButton.Name = "seeTrainInGameButton";
            seeTrainInGameButton.Size = new System.Drawing.Size(195, 29);
            seeTrainInGameButton.TabIndex = 5;
            seeTrainInGameButton.Text = "Jump to my train in game";
            seeTrainInGameButton.UseVisualStyleBackColor = true;
            seeTrainInGameButton.Click += seeTrainInGameButton_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(5, 90);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(112, 15);
            label3.TabIndex = 4;
            label3.Text = "Map resolution (m):";
            // 
            // mapResolutionUpDown
            // 
            mapResolutionUpDown.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            mapResolutionUpDown.Location = new System.Drawing.Point(119, 87);
            mapResolutionUpDown.Margin = new System.Windows.Forms.Padding(0);
            mapResolutionUpDown.Maximum = new decimal(new int[] { 200000, 0, 0, 0 });
            mapResolutionUpDown.Minimum = new decimal(new int[] { 80, 0, 0, 0 });
            mapResolutionUpDown.Name = "mapResolutionUpDown";
            mapResolutionUpDown.Size = new System.Drawing.Size(84, 23);
            mapResolutionUpDown.TabIndex = 3;
            mapResolutionUpDown.ThousandsSeparator = true;
            mapResolutionUpDown.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            mapResolutionUpDown.ValueChanged += mapResolutionUpDown_ValueChanged;
            // 
            // allowThrowingSwitchesCheckbox
            // 
            allowThrowingSwitchesCheckbox.AutoSize = true;
            allowThrowingSwitchesCheckbox.Checked = true;
            allowThrowingSwitchesCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            allowThrowingSwitchesCheckbox.Location = new System.Drawing.Point(8, 66);
            allowThrowingSwitchesCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            allowThrowingSwitchesCheckbox.Name = "allowThrowingSwitchesCheckbox";
            allowThrowingSwitchesCheckbox.Size = new System.Drawing.Size(155, 19);
            allowThrowingSwitchesCheckbox.TabIndex = 2;
            allowThrowingSwitchesCheckbox.Text = "Allow throwing switches";
            allowThrowingSwitchesCheckbox.UseVisualStyleBackColor = true;
            // 
            // allowChangingSignalsCheckbox
            // 
            allowChangingSignalsCheckbox.AutoSize = true;
            allowChangingSignalsCheckbox.Checked = true;
            allowChangingSignalsCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            allowChangingSignalsCheckbox.Location = new System.Drawing.Point(8, 44);
            allowChangingSignalsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            allowChangingSignalsCheckbox.Name = "allowChangingSignalsCheckbox";
            allowChangingSignalsCheckbox.Size = new System.Drawing.Size(148, 19);
            allowChangingSignalsCheckbox.TabIndex = 1;
            allowChangingSignalsCheckbox.Text = "Allow changing signals";
            allowChangingSignalsCheckbox.UseVisualStyleBackColor = true;
            // 
            // drawPathCheckbox
            // 
            drawPathCheckbox.AutoSize = true;
            drawPathCheckbox.Checked = true;
            drawPathCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            drawPathCheckbox.Location = new System.Drawing.Point(8, 22);
            drawPathCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            drawPathCheckbox.Name = "drawPathCheckbox";
            drawPathCheckbox.Size = new System.Drawing.Size(146, 19);
            drawPathCheckbox.TabIndex = 0;
            drawPathCheckbox.Text = "Draw next path section";
            drawPathCheckbox.UseVisualStyleBackColor = true;
            drawPathCheckbox.CheckedChanged += drawPathCheckbox_CheckedChanged;
            // 
            // playersPanel
            // 
            playersPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            playersPanel.Controls.Add(groupBox3);
            playersPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            playersPanel.Location = new System.Drawing.Point(0, 354);
            playersPanel.Margin = new System.Windows.Forms.Padding(0);
            playersPanel.Name = "playersPanel";
            playersPanel.Padding = new System.Windows.Forms.Padding(12, 0, 12, 12);
            playersPanel.Size = new System.Drawing.Size(233, 259);
            playersPanel.TabIndex = 2;
            playersPanel.Visible = false;
            // 
            // groupBox3
            // 
            groupBox3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            groupBox3.Controls.Add(playersView);
            groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            groupBox3.Location = new System.Drawing.Point(12, 0);
            groupBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox3.Size = new System.Drawing.Size(209, 247);
            groupBox3.TabIndex = 0;
            groupBox3.TabStop = false;
            groupBox3.Text = "Players";
            // 
            // playersView
            // 
            playersView.Activation = System.Windows.Forms.ItemActivation.OneClick;
            playersView.Alignment = System.Windows.Forms.ListViewAlignment.Left;
            playersView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            playersView.HoverSelection = true;
            playersView.Items.AddRange(new System.Windows.Forms.ListViewItem[] { listViewItem1, listViewItem2, listViewItem3, listViewItem4 });
            playersView.Location = new System.Drawing.Point(8, 22);
            playersView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            playersView.Name = "playersView";
            playersView.Size = new System.Drawing.Size(193, 217);
            playersView.TabIndex = 1;
            playersView.UseCompatibleStateImageBehavior = false;
            playersView.View = System.Windows.Forms.View.List;
            playersView.MouseClick += playersView_MouseClick;
            // 
            // messageActionsMenu
            // 
            messageActionsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { messageSelectedPlayerMenuItem, replyToSelectedPlayerMenuItem });
            messageActionsMenu.Name = "contextMenuStrip2";
            messageActionsMenu.ShowImageMargin = false;
            messageActionsMenu.Size = new System.Drawing.Size(197, 48);
            messageActionsMenu.ItemClicked += messageActionsMenu_ItemClicked;
            // 
            // messageSelectedPlayerMenuItem
            // 
            messageSelectedPlayerMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            messageSelectedPlayerMenuItem.Name = "messageSelectedPlayerMenuItem";
            messageSelectedPlayerMenuItem.Size = new System.Drawing.Size(196, 22);
            messageSelectedPlayerMenuItem.Tag = "message";
            messageSelectedPlayerMenuItem.Text = "Message the selected player";
            // 
            // replyToSelectedPlayerMenuItem
            // 
            replyToSelectedPlayerMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            replyToSelectedPlayerMenuItem.Name = "replyToSelectedPlayerMenuItem";
            replyToSelectedPlayerMenuItem.Size = new System.Drawing.Size(196, 22);
            replyToSelectedPlayerMenuItem.Tag = "reply";
            replyToSelectedPlayerMenuItem.Text = "Reply to the selected player";
            // 
            // playerActionsMenu
            // 
            playerActionsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { playerToolStripMenuItem, makeThisPlayerAnAssistantToolStripMenuItem, jumpToThisPlayerInGameToolStripMenuItem, followToolStripMenuItem, kickFromMultiplayerSessionToolStripMenuItem });
            playerActionsMenu.Name = "contextMenuStrip3";
            playerActionsMenu.Size = new System.Drawing.Size(230, 114);
            playerActionsMenu.ItemClicked += playerActionsMenu_ItemClicked;
            // 
            // playerToolStripMenuItem
            // 
            playerToolStripMenuItem.Enabled = false;
            playerToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            playerToolStripMenuItem.Name = "playerToolStripMenuItem";
            playerToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            playerToolStripMenuItem.Text = "Player";
            // 
            // makeThisPlayerAnAssistantToolStripMenuItem
            // 
            makeThisPlayerAnAssistantToolStripMenuItem.Name = "makeThisPlayerAnAssistantToolStripMenuItem";
            makeThisPlayerAnAssistantToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            makeThisPlayerAnAssistantToolStripMenuItem.Tag = "assistant";
            makeThisPlayerAnAssistantToolStripMenuItem.Text = "Make this player an assistant";
            // 
            // jumpToThisPlayerInGameToolStripMenuItem
            // 
            jumpToThisPlayerInGameToolStripMenuItem.Name = "jumpToThisPlayerInGameToolStripMenuItem";
            jumpToThisPlayerInGameToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            jumpToThisPlayerInGameToolStripMenuItem.Tag = "seeInGame";
            jumpToThisPlayerInGameToolStripMenuItem.Text = "Jump to this player in game";
            // 
            // followToolStripMenuItem
            // 
            followToolStripMenuItem.Name = "followToolStripMenuItem";
            followToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            followToolStripMenuItem.Tag = "followOnMap";
            followToolStripMenuItem.Text = "Follow on the map";
            // 
            // kickFromMultiplayerSessionToolStripMenuItem
            // 
            kickFromMultiplayerSessionToolStripMenuItem.Name = "kickFromMultiplayerSessionToolStripMenuItem";
            kickFromMultiplayerSessionToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            kickFromMultiplayerSessionToolStripMenuItem.Tag = "kick";
            kickFromMultiplayerSessionToolStripMenuItem.Text = "Kick from multiplayer session";
            // 
            // setSwitchMenu
            // 
            setSwitchMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { setSwitchToToolStripMenuItem, mainRouteToolStripMenuItem, sideRouteToolStripMenuItem });
            setSwitchMenu.Name = "contextMenuStrip1";
            setSwitchMenu.Size = new System.Drawing.Size(157, 70);
            setSwitchMenu.ItemClicked += setSwitchMenu_ItemClicked;
            // 
            // setSwitchToToolStripMenuItem
            // 
            setSwitchToToolStripMenuItem.Enabled = false;
            setSwitchToToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            setSwitchToToolStripMenuItem.Name = "setSwitchToToolStripMenuItem";
            setSwitchToToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            setSwitchToToolStripMenuItem.Text = "Set switch to...";
            // 
            // mainRouteToolStripMenuItem
            // 
            mainRouteToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(93, 64, 55);
            mainRouteToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            mainRouteToolStripMenuItem.Name = "mainRouteToolStripMenuItem";
            mainRouteToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            mainRouteToolStripMenuItem.Tag = "mainRoute";
            mainRouteToolStripMenuItem.Text = "Main route";
            // 
            // sideRouteToolStripMenuItem
            // 
            sideRouteToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(161, 136, 127);
            sideRouteToolStripMenuItem.Name = "sideRouteToolStripMenuItem";
            sideRouteToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            sideRouteToolStripMenuItem.Tag = "sideRoute";
            sideRouteToolStripMenuItem.Text = "Side route";
            // 
            // setSignalMenu
            // 
            setSignalMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { setSignalAspectToToolStripMenuItem, systemControlledToolStripMenuItem, stopToolStripMenuItem, approachToolStripMenuItem, proceedToolStripMenuItem, toolStripSeparator1, allowCallOnToolStripMenuItem });
            setSignalMenu.Name = "contextMenuStrip1";
            setSignalMenu.Size = new System.Drawing.Size(191, 142);
            setSignalMenu.ItemClicked += setSignalMenu_ItemClicked;
            // 
            // setSignalAspectToToolStripMenuItem
            // 
            setSignalAspectToToolStripMenuItem.Enabled = false;
            setSignalAspectToToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            setSignalAspectToToolStripMenuItem.Name = "setSignalAspectToToolStripMenuItem";
            setSignalAspectToToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            setSignalAspectToToolStripMenuItem.Text = "Set signal aspect to...";
            // 
            // systemControlledToolStripMenuItem
            // 
            systemControlledToolStripMenuItem.Name = "systemControlledToolStripMenuItem";
            systemControlledToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            systemControlledToolStripMenuItem.Tag = "system";
            systemControlledToolStripMenuItem.Text = "System controlled";
            // 
            // stopToolStripMenuItem
            // 
            stopToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(203, 67, 53);
            stopToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            stopToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            stopToolStripMenuItem.Tag = "stop";
            stopToolStripMenuItem.Text = "Stop";
            // 
            // approachToolStripMenuItem
            // 
            approachToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(241, 196, 15);
            approachToolStripMenuItem.Name = "approachToolStripMenuItem";
            approachToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            approachToolStripMenuItem.Tag = "approach";
            approachToolStripMenuItem.Text = "Approach";
            // 
            // proceedToolStripMenuItem
            // 
            proceedToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(34, 153, 84);
            proceedToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            proceedToolStripMenuItem.Name = "proceedToolStripMenuItem";
            proceedToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            proceedToolStripMenuItem.Tag = "proceed";
            proceedToolStripMenuItem.Text = "Proceed";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(187, 6);
            // 
            // allowCallOnToolStripMenuItem
            // 
            allowCallOnToolStripMenuItem.Enabled = false;
            allowCallOnToolStripMenuItem.Name = "allowCallOnToolStripMenuItem";
            allowCallOnToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            allowCallOnToolStripMenuItem.Tag = "callOn";
            allowCallOnToolStripMenuItem.Text = "Allow call on";
            // 
            // trainActionsMenu
            // 
            trainActionsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { jumpToThisTrainInGameToolStripMenuItem, followThisTrainOnTheMapToolStripMenuItem });
            trainActionsMenu.Name = "trainActionsMenu";
            trainActionsMenu.Size = new System.Drawing.Size(223, 48);
            trainActionsMenu.ItemClicked += trainActionsMenu_ItemClicked;
            // 
            // jumpToThisTrainInGameToolStripMenuItem
            // 
            jumpToThisTrainInGameToolStripMenuItem.Name = "jumpToThisTrainInGameToolStripMenuItem";
            jumpToThisTrainInGameToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            jumpToThisTrainInGameToolStripMenuItem.Tag = "seeInGame";
            jumpToThisTrainInGameToolStripMenuItem.Text = "Jump to this train in game";
            // 
            // followThisTrainOnTheMapToolStripMenuItem
            // 
            followThisTrainOnTheMapToolStripMenuItem.Name = "followThisTrainOnTheMapToolStripMenuItem";
            followThisTrainOnTheMapToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            followThisTrainOnTheMapToolStripMenuItem.Tag = "followOnMap";
            followThisTrainOnTheMapToolStripMenuItem.Text = "Follow this train on the map";
            // 
            // MapViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(915, 647);
            Controls.Add(tableLayoutPanel1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MinimumSize = new System.Drawing.Size(697, 456);
            Name = "MapViewer";
            Text = "Map window";
            FormClosing += MapViewer_FormClosing;
            Resize += MapViewer_Resize;
            playerRolePanel.ResumeLayout(false);
            playerRolePanel.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            messagesPanel.ResumeLayout(false);
            messagesPanel.PerformLayout();
            canvasPanel.ResumeLayout(false);
            canvasPanel.PerformLayout();
            mapCustomizationPanel.ResumeLayout(false);
            mapCustomizationPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mapCanvas).EndInit();
            tableLayoutPanel4.ResumeLayout(false);
            tableLayoutPanel4.PerformLayout();
            multiplayerSettingsPanel.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            mapSettingsPanel.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mapResolutionUpDown).EndInit();
            playersPanel.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            messageActionsMenu.ResumeLayout(false);
            playerActionsMenu.ResumeLayout(false);
            setSwitchMenu.ResumeLayout(false);
            setSignalMenu.ResumeLayout(false);
            trainActionsMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        public System.Windows.Forms.Panel playerRolePanel;
        private System.Windows.Forms.Label playerRoleExplanation;
        private System.Windows.Forms.LinkLabel playerRoleLink;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        public System.Windows.Forms.Panel messagesPanel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button messageAllButton;
        private System.Windows.Forms.Button moreReplyOptionsButton;
        private System.Windows.Forms.TextBox messageInput;
        private System.Windows.Forms.ContextMenuStrip messageActionsMenu;
        private System.Windows.Forms.ToolStripMenuItem messageSelectedPlayerMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replyToSelectedPlayerMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Panel mapSettingsPanel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox allowThrowingSwitchesCheckbox;
        private System.Windows.Forms.CheckBox allowChangingSignalsCheckbox;
        private System.Windows.Forms.CheckBox drawPathCheckbox;
        public System.Windows.Forms.Panel multiplayerSettingsPanel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox penaltyCheckbox;
        private System.Windows.Forms.CheckBox preferGreenCheckbox;
        private System.Windows.Forms.CheckBox allowJoiningCheckbox;
        private System.Windows.Forms.NumericUpDown mapResolutionUpDown;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button followMyTrainOnMap;
        private System.Windows.Forms.Button centerOnMyTrainButton;
        private System.Windows.Forms.Button seeTrainInGameButton;
        public System.Windows.Forms.Panel playersPanel;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListView playersView;
        private System.Windows.Forms.ContextMenuStrip playerActionsMenu;
        private System.Windows.Forms.ToolStripMenuItem makeThisPlayerAnAssistantToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem followToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jumpToThisPlayerInGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kickFromMultiplayerSessionToolStripMenuItem;
        private System.Windows.Forms.Panel canvasPanel;
        public System.Windows.Forms.PictureBox mapCanvas;
        private System.Windows.Forms.Button mapCustomizationButton;
        private System.Windows.Forms.Panel mapCustomizationPanel;
        private System.Windows.Forms.CheckBox useAntiAliasingCheckbox;
        private System.Windows.Forms.ListBox messages;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox showTimeCheckbox;
        private System.Windows.Forms.Button rotateThemesButton;
        public System.Windows.Forms.Label timeLabel;
        private System.Windows.Forms.CheckBox showSignalsCheckbox;
        private System.Windows.Forms.CheckBox showSwitchesCheckbox;
        private System.Windows.Forms.CheckBox showSidingLabelsCheckbox;
        private System.Windows.Forms.CheckBox showPlatformLabelsCheckbox;
        private System.Windows.Forms.CheckBox showPlatformsCheckbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox showSignalStateCheckbox;
        private System.Windows.Forms.RadioButton showActiveTrainsRadio;
        private System.Windows.Forms.CheckBox showTrainStateCheckbox;
        private System.Windows.Forms.CheckBox showTrainLabelsCheckbox;
        private System.Windows.Forms.RadioButton showAllTrainsRadio;
        private System.Windows.Forms.ContextMenuStrip setSwitchMenu;
        private System.Windows.Forms.ToolStripMenuItem setSwitchToToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mainRouteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sideRouteToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip setSignalMenu;
        private System.Windows.Forms.ToolStripMenuItem setSignalAspectToToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem systemControlledToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem approachToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem proceedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem allowCallOnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playerToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip trainActionsMenu;
        private System.Windows.Forms.ToolStripMenuItem jumpToThisTrainInGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem followThisTrainOnTheMapToolStripMenuItem;
        private System.Windows.Forms.CheckBox chkAllowUserSwitch;
    }
}

