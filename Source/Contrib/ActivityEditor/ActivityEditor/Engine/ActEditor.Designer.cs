using System;

using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace ActivityEditor
{
    partial class ActEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActEditor));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.activityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trafficToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.metadataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadMetada = new System.Windows.Forms.ToolStripMenuItem();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutActivityEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.StatusEditor = new System.Windows.Forms.ToolStripStatusLabel();
            this.OpenActivity = new System.Windows.Forms.OpenFileDialog();
            this.NewActivity = new System.Windows.Forms.FolderBrowserDialog();
            this.UpdateRouteConfig = new System.Windows.Forms.FolderBrowserDialog();
            this.SaveActivity = new System.Windows.Forms.SaveFileDialog();
            this.informationPanel = new System.Windows.Forms.Panel();
            this.activityOverview = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.ActivityAECB = new System.Windows.Forms.TextBox();
            this.routeData = new System.Windows.Forms.GroupBox();
            this.toolStripPanel1 = new System.Windows.Forms.ToolStripPanel();
            this.routeCFG = new System.Windows.Forms.ToolStrip();
            this.AddArea = new System.Windows.Forms.ToolStripButton();
            this.ConfigStation = new System.Windows.Forms.ToolStripButton();
            this.tagRoute = new System.Windows.Forms.ToolStripButton();
            this.stationRoute = new System.Windows.Forms.ToolStripButton();
            this.metaSegmentBtn = new System.Windows.Forms.ToolStripButton();
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton8 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton9 = new System.Windows.Forms.ToolStripButton();
            this.activityCFG = new System.Windows.Forms.ToolStrip();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton4 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton5 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton6 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton7 = new System.Windows.Forms.ToolStripButton();
            this.tagActivity = new System.Windows.Forms.ToolStripButton();
            this.wizardPageSR = new AEWizard.SelectRoute();
            this.wizardPageAD = new AEWizard.ActivityDescr();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.informationPanel.SuspendLayout();
            this.activityOverview.SuspendLayout();
            this.toolStripPanel1.SuspendLayout();
            this.routeCFG.SuspendLayout();
            this.toolStrip3.SuspendLayout();
            this.activityCFG.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(893, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.loadToolStripMenuItem,
            this.loadMetada,
            this.quitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.activityToolStripMenuItem,
            this.trafficToolStripMenuItem});
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newToolStripMenuItem.Text = "New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // activityToolStripMenuItem
            // 
            this.activityToolStripMenuItem.Name = "activityToolStripMenuItem";
            this.activityToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.activityToolStripMenuItem.Text = "Activity";
            this.activityToolStripMenuItem.ToolTipText = "Click to start new Activity Description (not ready)";
            this.activityToolStripMenuItem.Click += new System.EventHandler(this.activityToolStripMenuItem_Click);
            // 
            // trafficToolStripMenuItem
            // 
            this.trafficToolStripMenuItem.Name = "trafficToolStripMenuItem";
            this.trafficToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.trafficToolStripMenuItem.Text = "Traffic";
            this.trafficToolStripMenuItem.ToolTipText = "Click to create new Traffic description (not ready)";
            this.trafficToolStripMenuItem.Click += new System.EventHandler(this.trafficToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.metadataToolStripMenuItem});
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // metadataToolStripMenuItem
            // 
            this.metadataToolStripMenuItem.Name = "metadataToolStripMenuItem";
            this.metadataToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.metadataToolStripMenuItem.Text = "Metadata";
            this.metadataToolStripMenuItem.Click += new System.EventHandler(this.SaveRouteCfg);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveAsToolStripMenuItem.Text = "Save As";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.loadToolStripMenuItem.Text = "Load Activity";
            this.loadToolStripMenuItem.ToolTipText = "Click to load an existing OR Activity (not ready)";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // loadMetada
            // 
            this.loadMetada.Name = "loadMetada";
            this.loadMetada.Size = new System.Drawing.Size(152, 22);
            this.loadMetada.Text = "Load Metada";
            this.loadMetada.ToolTipText = "Click to Edit Metadata for a specified route";
            this.loadMetada.Click += new System.EventHandler(this.UpdateRouteCfg);
            // 
            // quitToolStripMenuItem
            // 
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.quitToolStripMenuItem.Text = "Quit";
            this.quitToolStripMenuItem.Click += new System.EventHandler(this.quitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.preferenceToolStripMenuItem,
            this.importToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // preferenceToolStripMenuItem
            // 
            this.preferenceToolStripMenuItem.Name = "preferenceToolStripMenuItem";
            this.preferenceToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.preferenceToolStripMenuItem.Text = "Options";
            this.preferenceToolStripMenuItem.Click += new System.EventHandler(this.preferenceToolStripMenuItem_Click);
            // 
            // importToolStripMenuItem
            // 
            this.importToolStripMenuItem.Name = "importToolStripMenuItem";
            this.importToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.importToolStripMenuItem.Text = "Import";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutActivityEditorToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutActivityEditorToolStripMenuItem
            // 
            this.aboutActivityEditorToolStripMenuItem.Name = "aboutActivityEditorToolStripMenuItem";
            this.aboutActivityEditorToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.aboutActivityEditorToolStripMenuItem.Text = "About Activity Editor";
            this.aboutActivityEditorToolStripMenuItem.Click += new System.EventHandler(this.aboutActivityEditorToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusEditor});
            this.statusStrip1.Location = new System.Drawing.Point(0, 590);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(893, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // StatusEditor
            // 
            this.StatusEditor.Name = "StatusEditor";
            this.StatusEditor.Size = new System.Drawing.Size(39, 17);
            this.StatusEditor.Text = "Status";
            // 
            // OpenActivity
            // 
            this.OpenActivity.Filter = "\"Activity file|*.act.json|All files|*.*\"";
            // 
            // NewActivity
            // 
            this.NewActivity.RootFolder = System.Environment.SpecialFolder.LocalApplicationData;
            // 
            // UpdateRouteConfig
            // 
            this.UpdateRouteConfig.Description = "Select the Route directory.";
            this.UpdateRouteConfig.ShowNewFolderButton = false;
            // 
            // SaveActivity
            // 
            this.SaveActivity.DefaultExt = "act.json";
            // 
            // informationPanel
            // 
            this.informationPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.informationPanel.Controls.Add(this.activityOverview);
            this.informationPanel.Controls.Add(this.routeData);
            this.informationPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.informationPanel.Location = new System.Drawing.Point(693, 51);
            this.informationPanel.Name = "informationPanel";
            this.informationPanel.Size = new System.Drawing.Size(200, 539);
            this.informationPanel.TabIndex = 9;
            // 
            // activityOverview
            // 
            this.activityOverview.Controls.Add(this.button2);
            this.activityOverview.Controls.Add(this.button1);
            this.activityOverview.Controls.Add(this.ActivityAECB);
            this.activityOverview.Location = new System.Drawing.Point(5, 3);
            this.activityOverview.Name = "activityOverview";
            this.activityOverview.Size = new System.Drawing.Size(182, 85);
            this.activityOverview.TabIndex = 0;
            this.activityOverview.TabStop = false;
            this.activityOverview.Text = "Activity";
            this.activityOverview.Visible = false;
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F);
            this.button2.Location = new System.Drawing.Point(159, 70);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(15, 15);
            this.button2.TabIndex = 9;
            this.button2.Text = "v";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(6, 45);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(168, 19);
            this.button1.TabIndex = 1;
            this.button1.Text = "Edit Description";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ActivityAECB
            // 
            this.ActivityAECB.Location = new System.Drawing.Point(6, 19);
            this.ActivityAECB.Name = "ActivityAECB";
            this.ActivityAECB.Size = new System.Drawing.Size(170, 20);
            this.ActivityAECB.TabIndex = 0;
            this.ActivityAECB.TextChanged += new System.EventHandler(this.ActivityAECB_TextChanged);
            this.ActivityAECB.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ActEditorKeyDown);
            // 
            // routeData
            // 
            this.routeData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.routeData.Location = new System.Drawing.Point(1, 10);
            this.routeData.Name = "routeData";
            this.routeData.Size = new System.Drawing.Size(190, 500);
            this.routeData.TabIndex = 1;
            this.routeData.TabStop = false;
            this.routeData.Enter += new System.EventHandler(this.routeData_Enter);
            this.routeData.Leave += new System.EventHandler(this.routeData_Leave);
            // 
            // toolStripPanel1
            // 
            this.toolStripPanel1.Controls.Add(this.routeCFG);
            this.toolStripPanel1.Controls.Add(this.toolStrip3);
            this.toolStripPanel1.Controls.Add(this.activityCFG);
            this.toolStripPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolStripPanel1.Location = new System.Drawing.Point(0, 24);
            this.toolStripPanel1.Name = "toolStripPanel1";
            this.toolStripPanel1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripPanel1.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.toolStripPanel1.Size = new System.Drawing.Size(893, 27);
            // 
            // routeCFG
            // 
            this.routeCFG.Dock = System.Windows.Forms.DockStyle.None;
            this.routeCFG.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AddArea,
            this.ConfigStation,
            this.tagRoute,
            this.stationRoute,
            this.metaSegmentBtn});
            this.routeCFG.Location = new System.Drawing.Point(3, 0);
            this.routeCFG.Name = "routeCFG";
            this.routeCFG.Size = new System.Drawing.Size(127, 25);
            this.routeCFG.TabIndex = 3;
            // 
            // AddArea
            // 
            this.AddArea.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.AddArea.Image = global::ActivityEditor.Properties.Resources._32;
            this.AddArea.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.AddArea.Name = "AddArea";
            this.AddArea.Size = new System.Drawing.Size(23, 22);
            this.AddArea.Text = "Add Station Area";
            this.AddArea.Click += new System.EventHandler(this.AddArea_Click);
            // 
            // ConfigStation
            // 
            this.ConfigStation.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ConfigStation.Image = global::ActivityEditor.Properties.Resources._31;
            this.ConfigStation.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ConfigStation.Name = "ConfigStation";
            this.ConfigStation.Size = new System.Drawing.Size(23, 22);
            this.ConfigStation.Text = "Station Configuration";
            // 
            // tagRoute
            // 
            this.tagRoute.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tagRoute.Image = global::ActivityEditor.Properties.Resources.tag;
            this.tagRoute.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tagRoute.Name = "tagRoute";
            this.tagRoute.Size = new System.Drawing.Size(23, 22);
            this.tagRoute.Text = "Add Tag";
            this.tagRoute.Click += new System.EventHandler(this.AddTag_Click);
            // 
            // stationRoute
            // 
            this.stationRoute.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.stationRoute.Image = global::ActivityEditor.Properties.Resources.SignalBox;
            this.stationRoute.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.stationRoute.Name = "stationRoute";
            this.stationRoute.Size = new System.Drawing.Size(23, 22);
            this.stationRoute.Text = "Add Station";
            this.stationRoute.Click += new System.EventHandler(this.AddStation_Click);
            // 
            // metaSegmentBtn
            // 
            this.metaSegmentBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.metaSegmentBtn.Image = ((System.Drawing.Image)(resources.GetObject("metaSegmentBtn.Image")));
            this.metaSegmentBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.metaSegmentBtn.Name = "metaSegmentBtn";
            this.metaSegmentBtn.Size = new System.Drawing.Size(23, 22);
            this.metaSegmentBtn.Text = "Edit MetaSegment";
            this.metaSegmentBtn.Click += new System.EventHandler(this.editMetaSegment);
            // 
            // toolStrip3
            // 
            this.toolStrip3.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.toolStrip3.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton8,
            this.toolStripButton9});
            this.toolStrip3.Location = new System.Drawing.Point(254, 0);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Size = new System.Drawing.Size(58, 25);
            this.toolStrip3.TabIndex = 2;
            // 
            // toolStripButton8
            // 
            this.toolStripButton8.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton8.Image = global::ActivityEditor.Properties.Resources.object_move;
            this.toolStripButton8.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton8.Name = "toolStripButton8";
            this.toolStripButton8.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton8.Text = "Move";
            this.toolStripButton8.Click += new System.EventHandler(this.MoveSelected_Click);
            // 
            // toolStripButton9
            // 
            this.toolStripButton9.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton9.Image = global::ActivityEditor.Properties.Resources.object_rotate;
            this.toolStripButton9.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton9.Name = "toolStripButton9";
            this.toolStripButton9.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton9.Text = "Rotate";
            this.toolStripButton9.Click += new System.EventHandler(this.RotateSelected_Click);
            // 
            // activityCFG
            // 
            this.activityCFG.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.activityCFG.Dock = System.Windows.Forms.DockStyle.None;
            this.activityCFG.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.activityCFG.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton3,
            this.toolStripButton4,
            this.toolStripButton5,
            this.toolStripButton6,
            this.toolStripButton7,
            this.tagActivity});
            this.activityCFG.Location = new System.Drawing.Point(312, 0);
            this.activityCFG.Name = "activityCFG";
            this.activityCFG.Size = new System.Drawing.Size(156, 27);
            this.activityCFG.TabIndex = 1;
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton3.Image = global::ActivityEditor.Properties.Resources._98;
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(24, 24);
            this.toolStripButton3.Text = "Activity Start";
            this.toolStripButton3.Click += new System.EventHandler(this.AddActivityStart_Click);
            // 
            // toolStripButton4
            // 
            this.toolStripButton4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton4.Image = global::ActivityEditor.Properties.Resources._118;
            this.toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton4.Name = "toolStripButton4";
            this.toolStripButton4.Size = new System.Drawing.Size(24, 24);
            this.toolStripButton4.Text = "Activity Stop";
            this.toolStripButton4.Click += new System.EventHandler(this.AddActivityStop_Click);
            // 
            // toolStripButton5
            // 
            this.toolStripButton5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton5.Image = global::ActivityEditor.Properties.Resources._12;
            this.toolStripButton5.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton5.Name = "toolStripButton5";
            this.toolStripButton5.Size = new System.Drawing.Size(24, 24);
            this.toolStripButton5.Text = "Waiting Point";
            this.toolStripButton5.Click += new System.EventHandler(this.AddActivityWait_Click);
            // 
            // toolStripButton6
            // 
            this.toolStripButton6.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton6.Image = global::ActivityEditor.Properties.Resources._78;
            this.toolStripButton6.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton6.Name = "toolStripButton6";
            this.toolStripButton6.Size = new System.Drawing.Size(24, 24);
            this.toolStripButton6.Text = "Action Point";
            this.toolStripButton6.Click += new System.EventHandler(this.AddActivityAction_Click);
            // 
            // toolStripButton7
            // 
            this.toolStripButton7.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton7.Image = global::ActivityEditor.Properties.Resources._77;
            this.toolStripButton7.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton7.Name = "toolStripButton7";
            this.toolStripButton7.Size = new System.Drawing.Size(24, 24);
            this.toolStripButton7.Text = "Action Check";
            this.toolStripButton7.Click += new System.EventHandler(this.AddActivityEval_Click);
            // 
            // tagActivity
            // 
            this.tagActivity.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tagActivity.Image = global::ActivityEditor.Properties.Resources.tag;
            this.tagActivity.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tagActivity.Name = "tagActivity";
            this.tagActivity.Size = new System.Drawing.Size(24, 24);
            this.tagActivity.Text = "toolStripButton10";
            this.tagActivity.Click += new System.EventHandler(this.AddTag_Click);
            // 
            // wizardPageSR
            // 
            this.wizardPageSR.Location = new System.Drawing.Point(-19, 57);
            this.wizardPageSR.Name = "wizardPageSR";
            this.wizardPageSR.routeInfo = null;
            this.wizardPageSR.Size = new System.Drawing.Size(497, 350);
            this.wizardPageSR.TabIndex = 8;
            this.wizardPageSR.Visible = false;
            // 
            // wizardPageAD
            // 
            this.wizardPageAD.activityInfo = null;
            this.wizardPageAD.Location = new System.Drawing.Point(247, 54);
            this.wizardPageAD.Name = "wizardPageAD";
            this.wizardPageAD.Size = new System.Drawing.Size(497, 313);
            this.wizardPageAD.TabIndex = 3;
            this.wizardPageAD.Visible = false;
            // 
            // ActEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(893, 612);
            this.Controls.Add(this.wizardPageSR);
            this.Controls.Add(this.informationPanel);
            this.Controls.Add(this.wizardPageAD);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStripPanel1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ActEditor";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Activity Editor";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ActEditorKeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.informationPanel.ResumeLayout(false);
            this.activityOverview.ResumeLayout(false);
            this.activityOverview.PerformLayout();
            this.toolStripPanel1.ResumeLayout(false);
            this.toolStripPanel1.PerformLayout();
            this.routeCFG.ResumeLayout(false);
            this.routeCFG.PerformLayout();
            this.toolStrip3.ResumeLayout(false);
            this.toolStrip3.PerformLayout();
            this.activityCFG.ResumeLayout(false);
            this.activityCFG.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel StatusEditor;
        private System.Windows.Forms.OpenFileDialog OpenActivity;
        private System.Windows.Forms.FolderBrowserDialog NewActivity;
        private System.Windows.Forms.SaveFileDialog SaveActivity;
        private System.Windows.Forms.FolderBrowserDialog UpdateRouteConfig;
        private AEWizard.ActivityDescr wizardPageAD;
        public System.Windows.Forms.Panel informationPanel;
        private System.Windows.Forms.ToolStripMenuItem activityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem trafficToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutActivityEditorToolStripMenuItem;
        public System.Windows.Forms.GroupBox activityOverview;
        public System.Windows.Forms.GroupBox routeData;

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox ActivityAECB;
        public ToolStripPanel toolStripPanel1;
        private ToolStrip activityCFG;
        private ToolStripButton toolStripButton3;
        private ToolStripButton toolStripButton4;
        private ToolStripButton toolStripButton5;
        private ToolStripButton toolStripButton6;
        private ToolStripButton toolStripButton7;
        private ToolStrip toolStrip3;
        private ToolStripButton toolStripButton8;
        private ToolStripButton toolStripButton9;
        private ToolStrip routeCFG;
        private ToolStripButton AddArea;
        private ToolStripButton ConfigStation;
        private AEWizard.SelectRoute wizardPageSR;
        private ToolStripButton tagActivity;
        private ToolStripButton tagRoute;
        private ToolStripButton stationRoute;
        private ToolStripMenuItem loadMetada;
        private ToolStripButton metaSegmentBtn;
        private ToolStripMenuItem metadataToolStripMenuItem;
    }
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
    public class ToolStripNumberControl : ToolStripControlHost
    {
        public ToolStripNumberControl()
            : base(new NumericUpDown())
        {

        }

        protected override void OnSubscribeControlEvents(Control control)
        {
            base.OnSubscribeControlEvents(control);
            ((NumericUpDown)control).ValueChanged += new System.EventHandler(this.OnValueChanged);
        }

        protected override void OnUnsubscribeControlEvents(Control control)
        {
            base.OnUnsubscribeControlEvents(control);
            ((NumericUpDown)control).ValueChanged -= new System.EventHandler(OnValueChanged);
        }

        public event System.EventHandler ValueChanged;

        public Control NumericUpDownControl
        {
            get { return Control as NumericUpDown; }
        }

        public void OnValueChanged(object sender, EventArgs e)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, e);
            }
        }
    }
}

