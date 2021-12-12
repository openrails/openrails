namespace ActivityEditor.Preference
{
    partial class Options
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Options));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.EditorOptions = new System.Windows.Forms.TabPage();
            this.ShowLabelPlat = new System.Windows.Forms.CheckBox();
            this.PlSiZoomLevel = new System.Windows.Forms.NumericUpDown();
            this.snapCircle = new System.Windows.Forms.NumericUpDown();
            this.PlSiLabel = new System.Windows.Forms.Label();
            this.snapCircleLabel = new System.Windows.Forms.Label();
            this.ShowSnap = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.PathOptions = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.ListRoutePaths = new System.Windows.Forms.ListBox();
            this.AEPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.RemoveRoutePaths = new System.Windows.Forms.Button();
            this.AddRoutePaths = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.browseMSTSPath = new System.Windows.Forms.Button();
            this.MSTSPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.snapTrack = new System.Windows.Forms.CheckBox();
            this.SnapInfo = new System.Windows.Forms.CheckBox();
            this.snapLine = new System.Windows.Forms.CheckBox();
            this.showRuler = new System.Windows.Forms.CheckBox();
            this.trackInfo = new System.Windows.Forms.CheckBox();
            this.showTiles = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.CommentAction = new System.Windows.Forms.TextBox();
            this.RemoveAction = new System.Windows.Forms.Button();
            this.AddAction = new System.Windows.Forms.Button();
            this.ListUsed = new System.Windows.Forms.ListBox();
            this.ListAvailable = new System.Windows.Forms.ListBox();
            this.label7 = new System.Windows.Forms.Label();
            this.MSTSfolderBrowse = new System.Windows.Forms.FolderBrowserDialog();
            this.OptionOK = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.EditorOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PlSiZoomLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.snapCircle)).BeginInit();
            this.PathOptions.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Left;
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.EditorOptions);
            this.tabControl1.Controls.Add(this.PathOptions);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabControl1.ItemSize = new System.Drawing.Size(30, 120);
            this.tabControl1.Location = new System.Drawing.Point(1, 2);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(605, 358);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 0;
            this.tabControl1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.DrawOnTab);
            // 
            // EditorOptions
            // 
            this.EditorOptions.Controls.Add(this.ShowLabelPlat);
            this.EditorOptions.Controls.Add(this.PlSiZoomLevel);
            this.EditorOptions.Controls.Add(this.snapCircle);
            this.EditorOptions.Controls.Add(this.PlSiLabel);
            this.EditorOptions.Controls.Add(this.snapCircleLabel);
            this.EditorOptions.Controls.Add(this.ShowSnap);
            this.EditorOptions.Controls.Add(this.checkBox1);
            this.EditorOptions.Controls.Add(this.label5);
            this.EditorOptions.Location = new System.Drawing.Point(124, 4);
            this.EditorOptions.Name = "EditorOptions";
            this.EditorOptions.Size = new System.Drawing.Size(477, 350);
            this.EditorOptions.TabIndex = 1;
            this.EditorOptions.Text = "Editor Options";
            this.EditorOptions.UseVisualStyleBackColor = true;
            // 
            // ShowLabelPlat
            // 
            this.ShowLabelPlat.AutoSize = true;
            this.ShowLabelPlat.Location = new System.Drawing.Point(15, 83);
            this.ShowLabelPlat.Name = "ShowLabelPlat";
            this.ShowLabelPlat.Size = new System.Drawing.Size(123, 17);
            this.ShowLabelPlat.TabIndex = 18;
            this.ShowLabelPlat.Text = "Show Platform Label";
            this.ShowLabelPlat.UseVisualStyleBackColor = true;
            this.ShowLabelPlat.CheckedChanged += new System.EventHandler(this.PlSiShow);
            // 
            // PlSiZoomLevel
            // 
            this.PlSiZoomLevel.DecimalPlaces = 2;
            this.PlSiZoomLevel.Enabled = false;
            this.PlSiZoomLevel.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.PlSiZoomLevel.Location = new System.Drawing.Point(286, 82);
            this.PlSiZoomLevel.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            65536});
            this.PlSiZoomLevel.Minimum = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.PlSiZoomLevel.Name = "PlSiZoomLevel";
            this.PlSiZoomLevel.Size = new System.Drawing.Size(58, 20);
            this.PlSiZoomLevel.TabIndex = 17;
            this.PlSiZoomLevel.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.PlSiZoomLevel.ValueChanged += new System.EventHandler(this.PlSiValue);
            // 
            // snapCircle
            // 
            this.snapCircle.Enabled = false;
            this.snapCircle.Location = new System.Drawing.Point(286, 59);
            this.snapCircle.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.snapCircle.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.snapCircle.Name = "snapCircle";
            this.snapCircle.Size = new System.Drawing.Size(58, 20);
            this.snapCircle.TabIndex = 17;
            this.snapCircle.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.snapCircle.ValueChanged += new System.EventHandler(this.snapCircle_ValueChanged);
            // 
            // PlSiLabel
            // 
            this.PlSiLabel.AutoSize = true;
            this.PlSiLabel.Enabled = false;
            this.PlSiLabel.Location = new System.Drawing.Point(175, 84);
            this.PlSiLabel.Name = "PlSiLabel";
            this.PlSiLabel.Size = new System.Drawing.Size(65, 13);
            this.PlSiLabel.TabIndex = 16;
            this.PlSiLabel.Text = "Zoom level :";
            // 
            // snapCircleLabel
            // 
            this.snapCircleLabel.AutoSize = true;
            this.snapCircleLabel.Enabled = false;
            this.snapCircleLabel.Location = new System.Drawing.Point(175, 61);
            this.snapCircleLabel.Name = "snapCircleLabel";
            this.snapCircleLabel.Size = new System.Drawing.Size(99, 13);
            this.snapCircleLabel.TabIndex = 16;
            this.snapCircleLabel.Text = "Size of snap circle :";
            // 
            // ShowSnap
            // 
            this.ShowSnap.AutoSize = true;
            this.ShowSnap.Location = new System.Drawing.Point(15, 60);
            this.ShowSnap.Name = "ShowSnap";
            this.ShowSnap.Size = new System.Drawing.Size(109, 17);
            this.ShowSnap.TabIndex = 14;
            this.ShowSnap.Text = "Show Snap circle";
            this.ShowSnap.UseVisualStyleBackColor = true;
            this.ShowSnap.CheckedChanged += new System.EventHandler(this.CheckedChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(15, 37);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(98, 17);
            this.checkBox1.TabIndex = 13;
            this.checkBox1.Text = "Show all Signal";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(3, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(123, 18);
            this.label5.TabIndex = 12;
            this.label5.Text = "Editor Preference";
            // 
            // PathOptions
            // 
            this.PathOptions.Controls.Add(this.label4);
            this.PathOptions.Controls.Add(this.ListRoutePaths);
            this.PathOptions.Controls.Add(this.AEPath);
            this.PathOptions.Controls.Add(this.label3);
            this.PathOptions.Controls.Add(this.RemoveRoutePaths);
            this.PathOptions.Controls.Add(this.AddRoutePaths);
            this.PathOptions.Controls.Add(this.label2);
            this.PathOptions.Controls.Add(this.browseMSTSPath);
            this.PathOptions.Controls.Add(this.MSTSPath);
            this.PathOptions.Controls.Add(this.label1);
            this.PathOptions.Location = new System.Drawing.Point(124, 4);
            this.PathOptions.Name = "PathOptions";
            this.PathOptions.Size = new System.Drawing.Size(477, 350);
            this.PathOptions.TabIndex = 0;
            this.PathOptions.Text = "Path Options";
            this.PathOptions.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(195, 18);
            this.label4.TabIndex = 11;
            this.label4.Text = "Please, configure your paths";
            // 
            // ListRoutePaths
            // 
            this.ListRoutePaths.FormattingEnabled = true;
            this.ListRoutePaths.Location = new System.Drawing.Point(23, 115);
            this.ListRoutePaths.Name = "ListRoutePaths";
            this.ListRoutePaths.ScrollAlwaysVisible = true;
            this.ListRoutePaths.Size = new System.Drawing.Size(279, 121);
            this.ListRoutePaths.TabIndex = 10;
            // 
            // AEPath
            // 
            this.AEPath.Location = new System.Drawing.Point(20, 266);
            this.AEPath.Name = "AEPath";
            this.AEPath.ReadOnly = true;
            this.AEPath.Size = new System.Drawing.Size(282, 20);
            this.AEPath.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 250);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Activity Editor";
            // 
            // RemoveRoutePaths
            // 
            this.RemoveRoutePaths.Location = new System.Drawing.Point(325, 141);
            this.RemoveRoutePaths.Name = "RemoveRoutePaths";
            this.RemoveRoutePaths.Size = new System.Drawing.Size(80, 23);
            this.RemoveRoutePaths.TabIndex = 6;
            this.RemoveRoutePaths.Text = "Remove";
            this.RemoveRoutePaths.UseVisualStyleBackColor = true;
            this.RemoveRoutePaths.Click += new System.EventHandler(this.RemoveRoutePaths_Click);
            // 
            // AddRoutePaths
            // 
            this.AddRoutePaths.Location = new System.Drawing.Point(325, 115);
            this.AddRoutePaths.Name = "AddRoutePaths";
            this.AddRoutePaths.Size = new System.Drawing.Size(80, 19);
            this.AddRoutePaths.TabIndex = 5;
            this.AddRoutePaths.Text = "Add";
            this.AddRoutePaths.UseVisualStyleBackColor = true;
            this.AddRoutePaths.Click += new System.EventHandler(this.AddRoutePaths_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 100);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Route Paths";
            // 
            // browseMSTSPath
            // 
            this.browseMSTSPath.Location = new System.Drawing.Point(325, 68);
            this.browseMSTSPath.Name = "browseMSTSPath";
            this.browseMSTSPath.Size = new System.Drawing.Size(80, 19);
            this.browseMSTSPath.TabIndex = 2;
            this.browseMSTSPath.Text = "Browse";
            this.browseMSTSPath.UseVisualStyleBackColor = true;
            this.browseMSTSPath.Click += new System.EventHandler(this.browseMSTSPath_Click);
            // 
            // MSTSPath
            // 
            this.MSTSPath.Location = new System.Drawing.Point(20, 67);
            this.MSTSPath.Name = "MSTSPath";
            this.MSTSPath.Size = new System.Drawing.Size(282, 20);
            this.MSTSPath.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "MSTS Path :";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.snapTrack);
            this.tabPage1.Controls.Add(this.SnapInfo);
            this.tabPage1.Controls.Add(this.snapLine);
            this.tabPage1.Controls.Add(this.showRuler);
            this.tabPage1.Controls.Add(this.trackInfo);
            this.tabPage1.Controls.Add(this.showTiles);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Location = new System.Drawing.Point(124, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(477, 350);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Debug Options";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // snapTrack
            // 
            this.snapTrack.AutoSize = true;
            this.snapTrack.Location = new System.Drawing.Point(31, 59);
            this.snapTrack.Name = "snapTrack";
            this.snapTrack.Size = new System.Drawing.Size(133, 17);
            this.snapTrack.TabIndex = 15;
            this.snapTrack.Text = "Show Snap Track Info";
            this.snapTrack.UseVisualStyleBackColor = true;
            this.snapTrack.CheckedChanged += new System.EventHandler(this.snapTrack_CheckedChanged);
            // 
            // SnapInfo
            // 
            this.SnapInfo.AutoSize = true;
            this.SnapInfo.Location = new System.Drawing.Point(31, 82);
            this.SnapInfo.Name = "SnapInfo";
            this.SnapInfo.Size = new System.Drawing.Size(102, 17);
            this.SnapInfo.TabIndex = 14;
            this.SnapInfo.Text = "Show Snap Info";
            this.SnapInfo.UseVisualStyleBackColor = true;
            this.SnapInfo.CheckedChanged += new System.EventHandler(this.SnapInfo_CheckedChanged);
            // 
            // snapLine
            // 
            this.snapLine.AutoSize = true;
            this.snapLine.Location = new System.Drawing.Point(31, 128);
            this.snapLine.Name = "snapLine";
            this.snapLine.Size = new System.Drawing.Size(104, 17);
            this.snapLine.TabIndex = 14;
            this.snapLine.Text = "Show Snap Line";
            this.snapLine.UseVisualStyleBackColor = true;
            this.snapLine.CheckedChanged += new System.EventHandler(this.snapLine_CheckedChanged);
            // 
            // showRuler
            // 
            this.showRuler.AutoSize = true;
            this.showRuler.Location = new System.Drawing.Point(31, 105);
            this.showRuler.Name = "showRuler";
            this.showRuler.Size = new System.Drawing.Size(81, 17);
            this.showRuler.TabIndex = 14;
            this.showRuler.Text = "Show Ruler";
            this.showRuler.UseVisualStyleBackColor = true;
            this.showRuler.CheckedChanged += new System.EventHandler(this.showRuler_CheckedChanged);
            // 
            // trackInfo
            // 
            this.trackInfo.AutoSize = true;
            this.trackInfo.Location = new System.Drawing.Point(251, 36);
            this.trackInfo.Name = "trackInfo";
            this.trackInfo.Size = new System.Drawing.Size(105, 17);
            this.trackInfo.TabIndex = 14;
            this.trackInfo.Text = "Show Track Info";
            this.trackInfo.UseVisualStyleBackColor = true;
            this.trackInfo.CheckedChanged += new System.EventHandler(this.trackInfo_changed);
            // 
            // showTiles
            // 
            this.showTiles.AutoSize = true;
            this.showTiles.Location = new System.Drawing.Point(31, 36);
            this.showTiles.Name = "showTiles";
            this.showTiles.Size = new System.Drawing.Size(78, 17);
            this.showTiles.TabIndex = 14;
            this.showTiles.Text = "Show Tiles";
            this.showTiles.UseVisualStyleBackColor = true;
            this.showTiles.CheckedChanged += new System.EventHandler(this.showTiles_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(6, 3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(123, 18);
            this.label6.TabIndex = 13;
            this.label6.Text = "Editor Preference";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.CommentAction);
            this.tabPage2.Controls.Add(this.RemoveAction);
            this.tabPage2.Controls.Add(this.AddAction);
            this.tabPage2.Controls.Add(this.ListUsed);
            this.tabPage2.Controls.Add(this.ListAvailable);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabPage2.Location = new System.Drawing.Point(124, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(477, 350);
            this.tabPage2.TabIndex = 3;
            this.tabPage2.Text = "Actions Configuration";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // CommentAction
            // 
            this.CommentAction.Location = new System.Drawing.Point(9, 266);
            this.CommentAction.Multiline = true;
            this.CommentAction.Name = "CommentAction";
            this.CommentAction.Size = new System.Drawing.Size(462, 78);
            this.CommentAction.TabIndex = 5;
            // 
            // RemoveAction
            // 
            this.RemoveAction.Location = new System.Drawing.Point(197, 181);
            this.RemoveAction.Name = "RemoveAction";
            this.RemoveAction.Size = new System.Drawing.Size(75, 23);
            this.RemoveAction.TabIndex = 4;
            this.RemoveAction.Text = "< remove";
            this.RemoveAction.UseVisualStyleBackColor = true;
            this.RemoveAction.Click += new System.EventHandler(this.RemoveFromUsed);
            // 
            // AddAction
            // 
            this.AddAction.Location = new System.Drawing.Point(197, 70);
            this.AddAction.Name = "AddAction";
            this.AddAction.Size = new System.Drawing.Size(75, 23);
            this.AddAction.TabIndex = 3;
            this.AddAction.Text = "Add >";
            this.AddAction.UseVisualStyleBackColor = true;
            this.AddAction.Click += new System.EventHandler(this.AddToUsed);
            // 
            // ListUsed
            // 
            this.ListUsed.FormattingEnabled = true;
            this.ListUsed.Location = new System.Drawing.Point(278, 46);
            this.ListUsed.Name = "ListUsed";
            this.ListUsed.Size = new System.Drawing.Size(193, 186);
            this.ListUsed.TabIndex = 2;
            this.ListUsed.Click += new System.EventHandler(this.ShowCommentUsed);
            this.ListUsed.DoubleClick += new System.EventHandler(this.EditProperties);
            this.ListUsed.Enter += new System.EventHandler(this.EditProperties);
            this.ListUsed.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MouseDownUsed);
            // 
            // ListAvailable
            // 
            this.ListAvailable.FormattingEnabled = true;
            this.ListAvailable.Location = new System.Drawing.Point(9, 46);
            this.ListAvailable.Name = "ListAvailable";
            this.ListAvailable.Size = new System.Drawing.Size(182, 186);
            this.ListAvailable.TabIndex = 1;
            this.ListAvailable.Click += new System.EventHandler(this.ShowCommentAvailable);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(6, 13);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(246, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Configuration for Generic Auxiliary Actions";
            // 
            // OptionOK
            // 
            this.OptionOK.Location = new System.Drawing.Point(525, 366);
            this.OptionOK.Name = "OptionOK";
            this.OptionOK.Size = new System.Drawing.Size(77, 22);
            this.OptionOK.TabIndex = 1;
            this.OptionOK.Text = "OK";
            this.OptionOK.UseVisualStyleBackColor = true;
            this.OptionOK.Click += new System.EventHandler(this.optionOK_click);
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(618, 395);
            this.Controls.Add(this.OptionOK);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Options";
            this.Text = "Options";
            this.tabControl1.ResumeLayout(false);
            this.EditorOptions.ResumeLayout(false);
            this.EditorOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PlSiZoomLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.snapCircle)).EndInit();
            this.PathOptions.ResumeLayout(false);
            this.PathOptions.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage EditorOptions;
        private System.Windows.Forms.TabPage PathOptions;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FolderBrowserDialog MSTSfolderBrowse;
        private System.Windows.Forms.Button browseMSTSPath;
        private System.Windows.Forms.TextBox MSTSPath;
        private System.Windows.Forms.Button AddRoutePaths;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox AEPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button RemoveRoutePaths;
        private System.Windows.Forms.ListBox ListRoutePaths;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox ShowSnap;
        private System.Windows.Forms.Label snapCircleLabel;
        private System.Windows.Forms.NumericUpDown snapCircle;
        private System.Windows.Forms.CheckBox ShowLabelPlat;
        private System.Windows.Forms.NumericUpDown PlSiZoomLevel;
        private System.Windows.Forms.Label PlSiLabel;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.CheckBox snapTrack;
        private System.Windows.Forms.CheckBox showTiles;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox SnapInfo;
        private System.Windows.Forms.CheckBox showRuler;
        private System.Windows.Forms.Button OptionOK;
        private System.Windows.Forms.CheckBox snapLine;
        private System.Windows.Forms.CheckBox trackInfo;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox CommentAction;
        private System.Windows.Forms.Button RemoveAction;
        private System.Windows.Forms.Button AddAction;
        private System.Windows.Forms.ListBox ListUsed;
        private System.Windows.Forms.ListBox ListAvailable;
        private System.Windows.Forms.Label label7;
    }
}