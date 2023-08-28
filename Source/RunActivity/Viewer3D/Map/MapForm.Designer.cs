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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Player1 (you)"}, -1, System.Drawing.Color.Empty, System.Drawing.Color.Empty, new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Player2");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Player3");
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("...");
            this.playerRolePanel = new System.Windows.Forms.Panel();
            this.playerRoleLink = new System.Windows.Forms.LinkLabel();
            this.playerRoleExplanation = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.messagesPanel = new System.Windows.Forms.Panel();
            this.messages = new System.Windows.Forms.ListBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.canvasPanel = new System.Windows.Forms.Panel();
            this.timeLabel = new System.Windows.Forms.Label();
            this.mapCustomizationButton = new System.Windows.Forms.Button();
            this.mapCustomizationPanel = new System.Windows.Forms.Panel();
            this.showAllTrainsRadio = new System.Windows.Forms.RadioButton();
            this.showActiveTrainsRadio = new System.Windows.Forms.RadioButton();
            this.showTrainStateCheckbox = new System.Windows.Forms.CheckBox();
            this.showTrainLabelsCheckbox = new System.Windows.Forms.CheckBox();
            this.showSignalStateCheckbox = new System.Windows.Forms.CheckBox();
            this.showSignalsCheckbox = new System.Windows.Forms.CheckBox();
            this.showSwitchesCheckbox = new System.Windows.Forms.CheckBox();
            this.showSidingLabelsCheckbox = new System.Windows.Forms.CheckBox();
            this.showPlatformLabelsCheckbox = new System.Windows.Forms.CheckBox();
            this.showPlatformsCheckbox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.rotateThemesButton = new System.Windows.Forms.Button();
            this.showTimeCheckbox = new System.Windows.Forms.CheckBox();
            this.useAntiAliasingCheckbox = new System.Windows.Forms.CheckBox();
            this.mapCanvas = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.multiplayerSettingsPanel = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.penaltyCheckbox = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.allowJoiningCheckbox = new System.Windows.Forms.CheckBox();
            this.mapSettingsPanel = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button5 = new System.Windows.Forms.Button();
            this.centerOnMyTrainButton = new System.Windows.Forms.Button();
            this.seeTrainInGameButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.mapResolutionUpDown = new System.Windows.Forms.NumericUpDown();
            this.allowThrowingSwitchesCheckbox = new System.Windows.Forms.CheckBox();
            this.allowChangingSignalsCheckbox = new System.Windows.Forms.CheckBox();
            this.drawPathCheckbox = new System.Windows.Forms.CheckBox();
            this.playersPanel = new System.Windows.Forms.Panel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.playersView = new System.Windows.Forms.ListView();
            this.messageActionsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.messageSelectedPlayerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replyToSelectedPlayerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playerActionsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.makeThisPlayerAnAssistantToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.jumpToThisPlayerInGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.followToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kickFromMultiplayerSessionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setSwitchMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setSwitchToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainRouteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sideRouteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setSignalMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.approachToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.proceedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.allowCallOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playerRolePanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.messagesPanel.SuspendLayout();
            this.canvasPanel.SuspendLayout();
            this.mapCustomizationPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            this.multiplayerSettingsPanel.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.mapSettingsPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapResolutionUpDown)).BeginInit();
            this.playersPanel.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.messageActionsMenu.SuspendLayout();
            this.playerActionsMenu.SuspendLayout();
            this.setSwitchMenu.SuspendLayout();
            this.setSignalMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // playerRolePanel
            // 
            this.playerRolePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playerRolePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(234)))), ((int)(((byte)(248)))));
            this.playerRolePanel.Controls.Add(this.playerRoleLink);
            this.playerRolePanel.Controls.Add(this.playerRoleExplanation);
            this.playerRolePanel.Location = new System.Drawing.Point(0, 0);
            this.playerRolePanel.Margin = new System.Windows.Forms.Padding(0);
            this.playerRolePanel.Name = "playerRolePanel";
            this.playerRolePanel.Size = new System.Drawing.Size(784, 30);
            this.playerRolePanel.TabIndex = 0;
            this.playerRolePanel.Visible = false;
            // 
            // playerRoleLink
            // 
            this.playerRoleLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(79)))), ((int)(((byte)(114)))));
            this.playerRoleLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.playerRoleLink.AutoSize = true;
            this.playerRoleLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(116)))), ((int)(((byte)(166)))));
            this.playerRoleLink.Location = new System.Drawing.Point(717, 9);
            this.playerRoleLink.Name = "playerRoleLink";
            this.playerRoleLink.Size = new System.Drawing.Size(60, 13);
            this.playerRoleLink.TabIndex = 1;
            this.playerRoleLink.TabStop = true;
            this.playerRoleLink.Text = "Learn more";
            this.playerRoleLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.playerRoleLink_LinkClicked);
            // 
            // playerRoleExplanation
            // 
            this.playerRoleExplanation.AutoSize = true;
            this.playerRoleExplanation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.playerRoleExplanation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(116)))), ((int)(((byte)(166)))));
            this.playerRoleExplanation.Location = new System.Drawing.Point(12, 9);
            this.playerRoleExplanation.Name = "playerRoleExplanation";
            this.playerRoleExplanation.Size = new System.Drawing.Size(284, 13);
            this.playerRoleExplanation.TabIndex = 0;
            this.playerRoleExplanation.Text = "You are the dispatcher in this multiplayer session";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.playerRolePanel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(784, 561);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel4, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 30);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(784, 531);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.messagesPanel, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.canvasPanel, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(584, 531);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // messagesPanel
            // 
            this.messagesPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messagesPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.messagesPanel.Controls.Add(this.messages);
            this.messagesPanel.Controls.Add(this.button2);
            this.messagesPanel.Controls.Add(this.button1);
            this.messagesPanel.Controls.Add(this.textBox1);
            this.messagesPanel.Controls.Add(this.label2);
            this.messagesPanel.Location = new System.Drawing.Point(0, 0);
            this.messagesPanel.Margin = new System.Windows.Forms.Padding(0);
            this.messagesPanel.Name = "messagesPanel";
            this.messagesPanel.Padding = new System.Windows.Forms.Padding(10);
            this.messagesPanel.Size = new System.Drawing.Size(584, 163);
            this.messagesPanel.TabIndex = 0;
            this.messagesPanel.Visible = false;
            // 
            // messages
            // 
            this.messages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messages.Enabled = false;
            this.messages.FormattingEnabled = true;
            this.messages.IntegralHeight = false;
            this.messages.Location = new System.Drawing.Point(16, 26);
            this.messages.Name = "messages";
            this.messages.Size = new System.Drawing.Size(554, 97);
            this.messages.TabIndex = 5;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(421, 128);
            this.button2.Margin = new System.Windows.Forms.Padding(0);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(126, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Message all players";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.AutoSize = true;
            this.button1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(547, 128);
            this.button1.Margin = new System.Windows.Forms.Padding(0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = ">";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(16, 130);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(402, 20);
            this.textBox1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Messages:";
            // 
            // canvasPanel
            // 
            this.canvasPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.canvasPanel.Controls.Add(this.timeLabel);
            this.canvasPanel.Controls.Add(this.mapCustomizationButton);
            this.canvasPanel.Controls.Add(this.mapCustomizationPanel);
            this.canvasPanel.Controls.Add(this.mapCanvas);
            this.canvasPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvasPanel.Location = new System.Drawing.Point(0, 163);
            this.canvasPanel.Margin = new System.Windows.Forms.Padding(0);
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Padding = new System.Windows.Forms.Padding(13);
            this.canvasPanel.Size = new System.Drawing.Size(584, 368);
            this.canvasPanel.TabIndex = 1;
            // 
            // timeLabel
            // 
            this.timeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.timeLabel.AutoSize = true;
            this.timeLabel.BackColor = System.Drawing.Color.Transparent;
            this.timeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeLabel.Location = new System.Drawing.Point(25, 333);
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Size = new System.Drawing.Size(92, 13);
            this.timeLabel.TabIndex = 1;
            this.timeLabel.Text = "Simulation time";
            // 
            // mapCustomizationButton
            // 
            this.mapCustomizationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.mapCustomizationButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mapCustomizationButton.FlatAppearance.BorderSize = 0;
            this.mapCustomizationButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.mapCustomizationButton.Location = new System.Drawing.Point(443, 324);
            this.mapCustomizationButton.Name = "mapCustomizationButton";
            this.mapCustomizationButton.Size = new System.Drawing.Size(120, 23);
            this.mapCustomizationButton.TabIndex = 0;
            this.mapCustomizationButton.Tag = "mapCustomization";
            this.mapCustomizationButton.Text = "Map customization";
            this.mapCustomizationButton.UseVisualStyleBackColor = true;
            this.mapCustomizationButton.Click += new System.EventHandler(this.mapCustomizationButton_Click);
            // 
            // mapCustomizationPanel
            // 
            this.mapCustomizationPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.mapCustomizationPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapCustomizationPanel.Controls.Add(this.showAllTrainsRadio);
            this.mapCustomizationPanel.Controls.Add(this.showActiveTrainsRadio);
            this.mapCustomizationPanel.Controls.Add(this.showTrainStateCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showTrainLabelsCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showSignalStateCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showSignalsCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showSwitchesCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showSidingLabelsCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showPlatformLabelsCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.showPlatformsCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.label1);
            this.mapCustomizationPanel.Controls.Add(this.rotateThemesButton);
            this.mapCustomizationPanel.Controls.Add(this.showTimeCheckbox);
            this.mapCustomizationPanel.Controls.Add(this.useAntiAliasingCheckbox);
            this.mapCustomizationPanel.Location = new System.Drawing.Point(373, 3);
            this.mapCustomizationPanel.Name = "mapCustomizationPanel";
            this.mapCustomizationPanel.Size = new System.Drawing.Size(190, 315);
            this.mapCustomizationPanel.TabIndex = 0;
            this.mapCustomizationPanel.Visible = false;
            // 
            // showAllTrainsRadio
            // 
            this.showAllTrainsRadio.AutoSize = true;
            this.showAllTrainsRadio.Location = new System.Drawing.Point(10, 279);
            this.showAllTrainsRadio.Name = "showAllTrainsRadio";
            this.showAllTrainsRadio.Size = new System.Drawing.Size(64, 17);
            this.showAllTrainsRadio.TabIndex = 19;
            this.showAllTrainsRadio.Text = "All trains";
            this.showAllTrainsRadio.UseVisualStyleBackColor = true;
            // 
            // showActiveTrainsRadio
            // 
            this.showActiveTrainsRadio.AutoSize = true;
            this.showActiveTrainsRadio.Checked = true;
            this.showActiveTrainsRadio.Location = new System.Drawing.Point(10, 260);
            this.showActiveTrainsRadio.Name = "showActiveTrainsRadio";
            this.showActiveTrainsRadio.Size = new System.Drawing.Size(106, 17);
            this.showActiveTrainsRadio.TabIndex = 18;
            this.showActiveTrainsRadio.TabStop = true;
            this.showActiveTrainsRadio.Text = "Only active trains";
            this.showActiveTrainsRadio.UseVisualStyleBackColor = true;
            // 
            // showTrainStateCheckbox
            // 
            this.showTrainStateCheckbox.AutoSize = true;
            this.showTrainStateCheckbox.Location = new System.Drawing.Point(28, 241);
            this.showTrainStateCheckbox.Name = "showTrainStateCheckbox";
            this.showTrainStateCheckbox.Size = new System.Drawing.Size(76, 17);
            this.showTrainStateCheckbox.TabIndex = 17;
            this.showTrainStateCheckbox.Text = "Train state";
            this.showTrainStateCheckbox.UseVisualStyleBackColor = true;
            // 
            // showTrainLabelsCheckbox
            // 
            this.showTrainLabelsCheckbox.AutoSize = true;
            this.showTrainLabelsCheckbox.Location = new System.Drawing.Point(10, 222);
            this.showTrainLabelsCheckbox.Name = "showTrainLabelsCheckbox";
            this.showTrainLabelsCheckbox.Size = new System.Drawing.Size(80, 17);
            this.showTrainLabelsCheckbox.TabIndex = 16;
            this.showTrainLabelsCheckbox.Text = "Train labels";
            this.showTrainLabelsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSignalStateCheckbox
            // 
            this.showSignalStateCheckbox.AutoSize = true;
            this.showSignalStateCheckbox.Location = new System.Drawing.Point(28, 203);
            this.showSignalStateCheckbox.Name = "showSignalStateCheckbox";
            this.showSignalStateCheckbox.Size = new System.Drawing.Size(81, 17);
            this.showSignalStateCheckbox.TabIndex = 15;
            this.showSignalStateCheckbox.Text = "Signal state";
            this.showSignalStateCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSignalsCheckbox
            // 
            this.showSignalsCheckbox.AutoSize = true;
            this.showSignalsCheckbox.Location = new System.Drawing.Point(10, 184);
            this.showSignalsCheckbox.Name = "showSignalsCheckbox";
            this.showSignalsCheckbox.Size = new System.Drawing.Size(60, 17);
            this.showSignalsCheckbox.TabIndex = 14;
            this.showSignalsCheckbox.Text = "Signals";
            this.showSignalsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSwitchesCheckbox
            // 
            this.showSwitchesCheckbox.AutoSize = true;
            this.showSwitchesCheckbox.Location = new System.Drawing.Point(10, 165);
            this.showSwitchesCheckbox.Name = "showSwitchesCheckbox";
            this.showSwitchesCheckbox.Size = new System.Drawing.Size(69, 17);
            this.showSwitchesCheckbox.TabIndex = 13;
            this.showSwitchesCheckbox.Text = "Switches";
            this.showSwitchesCheckbox.UseVisualStyleBackColor = true;
            // 
            // showSidingLabelsCheckbox
            // 
            this.showSidingLabelsCheckbox.AutoSize = true;
            this.showSidingLabelsCheckbox.Location = new System.Drawing.Point(10, 146);
            this.showSidingLabelsCheckbox.Name = "showSidingLabelsCheckbox";
            this.showSidingLabelsCheckbox.Size = new System.Drawing.Size(85, 17);
            this.showSidingLabelsCheckbox.TabIndex = 12;
            this.showSidingLabelsCheckbox.Text = "Siding labels";
            this.showSidingLabelsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showPlatformLabelsCheckbox
            // 
            this.showPlatformLabelsCheckbox.AutoSize = true;
            this.showPlatformLabelsCheckbox.Location = new System.Drawing.Point(10, 127);
            this.showPlatformLabelsCheckbox.Name = "showPlatformLabelsCheckbox";
            this.showPlatformLabelsCheckbox.Size = new System.Drawing.Size(94, 17);
            this.showPlatformLabelsCheckbox.TabIndex = 11;
            this.showPlatformLabelsCheckbox.Text = "Platform labels";
            this.showPlatformLabelsCheckbox.UseVisualStyleBackColor = true;
            // 
            // showPlatformsCheckbox
            // 
            this.showPlatformsCheckbox.AutoSize = true;
            this.showPlatformsCheckbox.Location = new System.Drawing.Point(10, 108);
            this.showPlatformsCheckbox.Name = "showPlatformsCheckbox";
            this.showPlatformsCheckbox.Size = new System.Drawing.Size(69, 17);
            this.showPlatformsCheckbox.TabIndex = 10;
            this.showPlatformsCheckbox.Text = "Platforms";
            this.showPlatformsCheckbox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(7, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Map features";
            // 
            // rotateThemesButton
            // 
            this.rotateThemesButton.AutoSize = true;
            this.rotateThemesButton.Location = new System.Drawing.Point(10, 52);
            this.rotateThemesButton.Name = "rotateThemesButton";
            this.rotateThemesButton.Size = new System.Drawing.Size(167, 23);
            this.rotateThemesButton.TabIndex = 8;
            this.rotateThemesButton.Text = "Rotate between themes";
            this.rotateThemesButton.UseVisualStyleBackColor = true;
            this.rotateThemesButton.Click += new System.EventHandler(this.rotateThemesButton_Click);
            // 
            // showTimeCheckbox
            // 
            this.showTimeCheckbox.AutoSize = true;
            this.showTimeCheckbox.Checked = true;
            this.showTimeCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showTimeCheckbox.Location = new System.Drawing.Point(10, 29);
            this.showTimeCheckbox.Name = "showTimeCheckbox";
            this.showTimeCheckbox.Size = new System.Drawing.Size(124, 17);
            this.showTimeCheckbox.TabIndex = 1;
            this.showTimeCheckbox.Text = "Show simulation time";
            this.showTimeCheckbox.UseVisualStyleBackColor = true;
            // 
            // useAntiAliasingCheckbox
            // 
            this.useAntiAliasingCheckbox.AutoSize = true;
            this.useAntiAliasingCheckbox.Location = new System.Drawing.Point(10, 10);
            this.useAntiAliasingCheckbox.Name = "useAntiAliasingCheckbox";
            this.useAntiAliasingCheckbox.Size = new System.Drawing.Size(103, 17);
            this.useAntiAliasingCheckbox.TabIndex = 0;
            this.useAntiAliasingCheckbox.Text = "Use anti-aliasing";
            this.useAntiAliasingCheckbox.UseVisualStyleBackColor = true;
            // 
            // mapCanvas
            // 
            this.mapCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapCanvas.Location = new System.Drawing.Point(13, 13);
            this.mapCanvas.Margin = new System.Windows.Forms.Padding(0);
            this.mapCanvas.Name = "mapCanvas";
            this.mapCanvas.Size = new System.Drawing.Size(558, 342);
            this.mapCanvas.TabIndex = 0;
            this.mapCanvas.TabStop = false;
            this.mapCanvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mapCanvas_MouseDown);
            this.mapCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mapCanvas_MouseMove);
            this.mapCanvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mapCanvas_MouseUp);
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.multiplayerSettingsPanel, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.mapSettingsPanel, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.playersPanel, 0, 3);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(584, 0);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 4;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(200, 531);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(194, 14);
            this.panel1.TabIndex = 3;
            // 
            // multiplayerSettingsPanel
            // 
            this.multiplayerSettingsPanel.AutoSize = true;
            this.multiplayerSettingsPanel.Controls.Add(this.groupBox2);
            this.multiplayerSettingsPanel.Location = new System.Drawing.Point(0, 20);
            this.multiplayerSettingsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.multiplayerSettingsPanel.Name = "multiplayerSettingsPanel";
            this.multiplayerSettingsPanel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.multiplayerSettingsPanel.Size = new System.Drawing.Size(200, 83);
            this.multiplayerSettingsPanel.TabIndex = 1;
            this.multiplayerSettingsPanel.Visible = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.penaltyCheckbox);
            this.groupBox2.Controls.Add(this.checkBox5);
            this.groupBox2.Controls.Add(this.allowJoiningCheckbox);
            this.groupBox2.Location = new System.Drawing.Point(10, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(180, 80);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Multiplayer settings";
            // 
            // penaltyCheckbox
            // 
            this.penaltyCheckbox.AutoSize = true;
            this.penaltyCheckbox.Checked = true;
            this.penaltyCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.penaltyCheckbox.Location = new System.Drawing.Point(7, 57);
            this.penaltyCheckbox.Name = "penaltyCheckbox";
            this.penaltyCheckbox.Size = new System.Drawing.Size(163, 17);
            this.penaltyCheckbox.TabIndex = 2;
            this.penaltyCheckbox.Text = "Penalty for speeding && SPAD";
            this.penaltyCheckbox.UseVisualStyleBackColor = true;
            this.penaltyCheckbox.CheckedChanged += new System.EventHandler(this.penaltyCheckbox_CheckedChanged);
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Enabled = false;
            this.checkBox5.Location = new System.Drawing.Point(7, 38);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(99, 17);
            this.checkBox5.TabIndex = 1;
            this.checkBox5.Text = "Prefer green (?)";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // allowJoiningCheckbox
            // 
            this.allowJoiningCheckbox.AutoSize = true;
            this.allowJoiningCheckbox.Checked = true;
            this.allowJoiningCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.allowJoiningCheckbox.Location = new System.Drawing.Point(7, 19);
            this.allowJoiningCheckbox.Name = "allowJoiningCheckbox";
            this.allowJoiningCheckbox.Size = new System.Drawing.Size(141, 17);
            this.allowJoiningCheckbox.TabIndex = 0;
            this.allowJoiningCheckbox.Text = "Allow new players to join";
            this.allowJoiningCheckbox.UseVisualStyleBackColor = true;
            this.allowJoiningCheckbox.CheckedChanged += new System.EventHandler(this.allowJoiningCheckbox_CheckedChanged);
            // 
            // mapSettingsPanel
            // 
            this.mapSettingsPanel.AutoSize = true;
            this.mapSettingsPanel.Controls.Add(this.groupBox1);
            this.mapSettingsPanel.Location = new System.Drawing.Point(0, 103);
            this.mapSettingsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.mapSettingsPanel.Name = "mapSettingsPanel";
            this.mapSettingsPanel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.mapSettingsPanel.Size = new System.Drawing.Size(200, 191);
            this.mapSettingsPanel.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button5);
            this.groupBox1.Controls.Add(this.centerOnMyTrainButton);
            this.groupBox1.Controls.Add(this.seeTrainInGameButton);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.mapResolutionUpDown);
            this.groupBox1.Controls.Add(this.allowThrowingSwitchesCheckbox);
            this.groupBox1.Controls.Add(this.allowChangingSignalsCheckbox);
            this.groupBox1.Controls.Add(this.drawPathCheckbox);
            this.groupBox1.Location = new System.Drawing.Point(10, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(180, 185);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Map settings and controls";
            // 
            // button5
            // 
            this.button5.AutoSize = true;
            this.button5.Enabled = false;
            this.button5.Location = new System.Drawing.Point(7, 156);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(167, 23);
            this.button5.TabIndex = 7;
            this.button5.Text = "Follow my train on the map";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // centerOnMyTrainButton
            // 
            this.centerOnMyTrainButton.AutoSize = true;
            this.centerOnMyTrainButton.Location = new System.Drawing.Point(7, 127);
            this.centerOnMyTrainButton.Name = "centerOnMyTrainButton";
            this.centerOnMyTrainButton.Size = new System.Drawing.Size(167, 23);
            this.centerOnMyTrainButton.TabIndex = 6;
            this.centerOnMyTrainButton.Text = "Jump to my train on the map";
            this.centerOnMyTrainButton.UseVisualStyleBackColor = true;
            this.centerOnMyTrainButton.Click += new System.EventHandler(this.centerOnMyTrainButton_Click);
            // 
            // seeTrainInGameButton
            // 
            this.seeTrainInGameButton.AutoSize = true;
            this.seeTrainInGameButton.Enabled = false;
            this.seeTrainInGameButton.Location = new System.Drawing.Point(7, 98);
            this.seeTrainInGameButton.Name = "seeTrainInGameButton";
            this.seeTrainInGameButton.Size = new System.Drawing.Size(167, 23);
            this.seeTrainInGameButton.TabIndex = 5;
            this.seeTrainInGameButton.Text = "Jump to my train in game";
            this.seeTrainInGameButton.UseVisualStyleBackColor = true;
            this.seeTrainInGameButton.Click += new System.EventHandler(this.seeTrainInGameButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 78);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Map resolution (m):";
            // 
            // mapResolutionUpDown
            // 
            this.mapResolutionUpDown.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.mapResolutionUpDown.Location = new System.Drawing.Point(102, 75);
            this.mapResolutionUpDown.Margin = new System.Windows.Forms.Padding(0);
            this.mapResolutionUpDown.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.mapResolutionUpDown.Minimum = new decimal(new int[] {
            80,
            0,
            0,
            0});
            this.mapResolutionUpDown.Name = "mapResolutionUpDown";
            this.mapResolutionUpDown.Size = new System.Drawing.Size(72, 20);
            this.mapResolutionUpDown.TabIndex = 3;
            this.mapResolutionUpDown.ThousandsSeparator = true;
            this.mapResolutionUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.mapResolutionUpDown.ValueChanged += new System.EventHandler(this.mapResolutionUpDown_ValueChanged);
            // 
            // allowThrowingSwitchesCheckbox
            // 
            this.allowThrowingSwitchesCheckbox.AutoSize = true;
            this.allowThrowingSwitchesCheckbox.Checked = true;
            this.allowThrowingSwitchesCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.allowThrowingSwitchesCheckbox.Location = new System.Drawing.Point(7, 57);
            this.allowThrowingSwitchesCheckbox.Name = "allowThrowingSwitchesCheckbox";
            this.allowThrowingSwitchesCheckbox.Size = new System.Drawing.Size(138, 17);
            this.allowThrowingSwitchesCheckbox.TabIndex = 2;
            this.allowThrowingSwitchesCheckbox.Text = "Allow throwing switches";
            this.allowThrowingSwitchesCheckbox.UseVisualStyleBackColor = true;
            // 
            // allowChangingSignalsCheckbox
            // 
            this.allowChangingSignalsCheckbox.AutoSize = true;
            this.allowChangingSignalsCheckbox.Checked = true;
            this.allowChangingSignalsCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.allowChangingSignalsCheckbox.Location = new System.Drawing.Point(7, 38);
            this.allowChangingSignalsCheckbox.Name = "allowChangingSignalsCheckbox";
            this.allowChangingSignalsCheckbox.Size = new System.Drawing.Size(133, 17);
            this.allowChangingSignalsCheckbox.TabIndex = 1;
            this.allowChangingSignalsCheckbox.Text = "Allow changing signals";
            this.allowChangingSignalsCheckbox.UseVisualStyleBackColor = true;
            // 
            // drawPathCheckbox
            // 
            this.drawPathCheckbox.AutoSize = true;
            this.drawPathCheckbox.Checked = true;
            this.drawPathCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.drawPathCheckbox.Location = new System.Drawing.Point(7, 19);
            this.drawPathCheckbox.Name = "drawPathCheckbox";
            this.drawPathCheckbox.Size = new System.Drawing.Size(135, 17);
            this.drawPathCheckbox.TabIndex = 0;
            this.drawPathCheckbox.Text = "Draw next path section";
            this.drawPathCheckbox.UseVisualStyleBackColor = true;
            this.drawPathCheckbox.CheckedChanged += new System.EventHandler(this.drawPathCheckbox_CheckedChanged);
            // 
            // playersPanel
            // 
            this.playersPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.playersPanel.Controls.Add(this.groupBox3);
            this.playersPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playersPanel.Location = new System.Drawing.Point(0, 294);
            this.playersPanel.Margin = new System.Windows.Forms.Padding(0);
            this.playersPanel.Name = "playersPanel";
            this.playersPanel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 10);
            this.playersPanel.Size = new System.Drawing.Size(200, 237);
            this.playersPanel.TabIndex = 2;
            this.playersPanel.Visible = false;
            // 
            // groupBox3
            // 
            this.groupBox3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox3.Controls.Add(this.playersView);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(10, 0);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(180, 227);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Players";
            // 
            // playersView
            // 
            this.playersView.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.playersView.Alignment = System.Windows.Forms.ListViewAlignment.Left;
            this.playersView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playersView.HideSelection = false;
            this.playersView.HoverSelection = true;
            this.playersView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4});
            this.playersView.Location = new System.Drawing.Point(7, 19);
            this.playersView.MultiSelect = false;
            this.playersView.Name = "playersView";
            this.playersView.Size = new System.Drawing.Size(167, 202);
            this.playersView.TabIndex = 1;
            this.playersView.UseCompatibleStateImageBehavior = false;
            this.playersView.View = System.Windows.Forms.View.List;
            this.playersView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.playersView_MouseClick);
            // 
            // messageActionsMenu
            // 
            this.messageActionsMenu.Enabled = false;
            this.messageActionsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.messageSelectedPlayerMenuItem,
            this.replyToSelectedPlayerMenuItem});
            this.messageActionsMenu.Name = "contextMenuStrip2";
            this.messageActionsMenu.ShowImageMargin = false;
            this.messageActionsMenu.Size = new System.Drawing.Size(197, 48);
            // 
            // messageSelectedPlayerMenuItem
            // 
            this.messageSelectedPlayerMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.messageSelectedPlayerMenuItem.Enabled = false;
            this.messageSelectedPlayerMenuItem.Name = "messageSelectedPlayerMenuItem";
            this.messageSelectedPlayerMenuItem.Size = new System.Drawing.Size(196, 22);
            this.messageSelectedPlayerMenuItem.Text = "Message the selected player";
            // 
            // replyToSelectedPlayerMenuItem
            // 
            this.replyToSelectedPlayerMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.replyToSelectedPlayerMenuItem.Enabled = false;
            this.replyToSelectedPlayerMenuItem.Name = "replyToSelectedPlayerMenuItem";
            this.replyToSelectedPlayerMenuItem.Size = new System.Drawing.Size(196, 22);
            this.replyToSelectedPlayerMenuItem.Text = "Reply to the selected player";
            // 
            // playerActionsMenu
            // 
            this.playerActionsMenu.Enabled = false;
            this.playerActionsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.makeThisPlayerAnAssistantToolStripMenuItem,
            this.jumpToThisPlayerInGameToolStripMenuItem,
            this.followToolStripMenuItem,
            this.kickFromMultiplayerSessionToolStripMenuItem});
            this.playerActionsMenu.Name = "contextMenuStrip3";
            this.playerActionsMenu.Size = new System.Drawing.Size(230, 92);
            // 
            // makeThisPlayerAnAssistantToolStripMenuItem
            // 
            this.makeThisPlayerAnAssistantToolStripMenuItem.Enabled = false;
            this.makeThisPlayerAnAssistantToolStripMenuItem.Name = "makeThisPlayerAnAssistantToolStripMenuItem";
            this.makeThisPlayerAnAssistantToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this.makeThisPlayerAnAssistantToolStripMenuItem.Text = "Make this player an assistant";
            // 
            // jumpToThisPlayerInGameToolStripMenuItem
            // 
            this.jumpToThisPlayerInGameToolStripMenuItem.Enabled = false;
            this.jumpToThisPlayerInGameToolStripMenuItem.Name = "jumpToThisPlayerInGameToolStripMenuItem";
            this.jumpToThisPlayerInGameToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this.jumpToThisPlayerInGameToolStripMenuItem.Text = "Jump to this player in game";
            // 
            // followToolStripMenuItem
            // 
            this.followToolStripMenuItem.Enabled = false;
            this.followToolStripMenuItem.Name = "followToolStripMenuItem";
            this.followToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this.followToolStripMenuItem.Text = "Follow on the map";
            // 
            // kickFromMultiplayerSessionToolStripMenuItem
            // 
            this.kickFromMultiplayerSessionToolStripMenuItem.Enabled = false;
            this.kickFromMultiplayerSessionToolStripMenuItem.Name = "kickFromMultiplayerSessionToolStripMenuItem";
            this.kickFromMultiplayerSessionToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this.kickFromMultiplayerSessionToolStripMenuItem.Text = "Kick from multiplayer session";
            // 
            // setSwitchMenu
            // 
            this.setSwitchMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setSwitchToToolStripMenuItem,
            this.mainRouteToolStripMenuItem,
            this.sideRouteToolStripMenuItem});
            this.setSwitchMenu.Name = "contextMenuStrip1";
            this.setSwitchMenu.Size = new System.Drawing.Size(157, 70);
            this.setSwitchMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.setSwitchMenu_ItemClicked);
            // 
            // setSwitchToToolStripMenuItem
            // 
            this.setSwitchToToolStripMenuItem.Enabled = false;
            this.setSwitchToToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.setSwitchToToolStripMenuItem.Name = "setSwitchToToolStripMenuItem";
            this.setSwitchToToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.setSwitchToToolStripMenuItem.Text = "Set switch to...";
            // 
            // mainRouteToolStripMenuItem
            // 
            this.mainRouteToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(93)))), ((int)(((byte)(64)))), ((int)(((byte)(55)))));
            this.mainRouteToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.mainRouteToolStripMenuItem.Name = "mainRouteToolStripMenuItem";
            this.mainRouteToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.mainRouteToolStripMenuItem.Tag = "mainRoute";
            this.mainRouteToolStripMenuItem.Text = "Main route";
            // 
            // sideRouteToolStripMenuItem
            // 
            this.sideRouteToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(161)))), ((int)(((byte)(136)))), ((int)(((byte)(127)))));
            this.sideRouteToolStripMenuItem.Name = "sideRouteToolStripMenuItem";
            this.sideRouteToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.sideRouteToolStripMenuItem.Tag = "sideRoute";
            this.sideRouteToolStripMenuItem.Text = "Side route";
            // 
            // setSignalMenu
            // 
            this.setSignalMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.approachToolStripMenuItem,
            this.proceedToolStripMenuItem,
            this.toolStripSeparator1,
            this.allowCallOnToolStripMenuItem});
            this.setSignalMenu.Name = "contextMenuStrip1";
            this.setSignalMenu.Size = new System.Drawing.Size(191, 142);
            this.setSignalMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.setSignalMenu_ItemClicked);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Enabled = false;
            this.toolStripMenuItem1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(190, 22);
            this.toolStripMenuItem1.Text = "Set signal aspect to...";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(190, 22);
            this.toolStripMenuItem2.Tag = "system";
            this.toolStripMenuItem2.Text = "System controlled";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(203)))), ((int)(((byte)(67)))), ((int)(((byte)(53)))));
            this.toolStripMenuItem3.ForeColor = System.Drawing.Color.White;
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(190, 22);
            this.toolStripMenuItem3.Tag = "stop";
            this.toolStripMenuItem3.Text = "Stop";
            // 
            // approachToolStripMenuItem
            // 
            this.approachToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(196)))), ((int)(((byte)(15)))));
            this.approachToolStripMenuItem.Name = "approachToolStripMenuItem";
            this.approachToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.approachToolStripMenuItem.Tag = "approach";
            this.approachToolStripMenuItem.Text = "Approach";
            // 
            // proceedToolStripMenuItem
            // 
            this.proceedToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(153)))), ((int)(((byte)(84)))));
            this.proceedToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.proceedToolStripMenuItem.Name = "proceedToolStripMenuItem";
            this.proceedToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.proceedToolStripMenuItem.Tag = "proceed";
            this.proceedToolStripMenuItem.Text = "Proceed";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(187, 6);
            // 
            // allowCallOnToolStripMenuItem
            // 
            this.allowCallOnToolStripMenuItem.Enabled = false;
            this.allowCallOnToolStripMenuItem.Name = "allowCallOnToolStripMenuItem";
            this.allowCallOnToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.allowCallOnToolStripMenuItem.Tag = "callOn";
            this.allowCallOnToolStripMenuItem.Text = "Allow call on";
            // 
            // DispatchViewerBeta
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "DispatchViewerBeta";
            this.Text = "Map window";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DispatchViewerBeta_FormClosing);
            this.Resize += new System.EventHandler(this.DispatchViewerBeta_Resize);
            this.playerRolePanel.ResumeLayout(false);
            this.playerRolePanel.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.messagesPanel.ResumeLayout(false);
            this.messagesPanel.PerformLayout();
            this.canvasPanel.ResumeLayout(false);
            this.canvasPanel.PerformLayout();
            this.mapCustomizationPanel.ResumeLayout(false);
            this.mapCustomizationPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.multiplayerSettingsPanel.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.mapSettingsPanel.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapResolutionUpDown)).EndInit();
            this.playersPanel.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.messageActionsMenu.ResumeLayout(false);
            this.playerActionsMenu.ResumeLayout(false);
            this.setSwitchMenu.ResumeLayout(false);
            this.setSignalMenu.ResumeLayout(false);
            this.ResumeLayout(false);

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
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
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
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox allowJoiningCheckbox;
        private System.Windows.Forms.NumericUpDown mapResolutionUpDown;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button5;
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
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem approachToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem proceedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem allowCallOnToolStripMenuItem;
    }
}

