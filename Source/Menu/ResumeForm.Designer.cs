namespace ORTS {
    partial class ResumeForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ResumeForm));
            this.gridSaves = new System.Windows.Forms.DataGridView();
            this.fileDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.realTimeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pathNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gameTimeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.distanceDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.currentTileDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.validDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Blank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.saveBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.buttonResume = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonUndelete = new System.Windows.Forms.Button();
            this.labelInvalidSaves = new System.Windows.Forms.Label();
            this.buttonDeleteInvalid = new System.Windows.Forms.Button();
            this.pictureBoxScreenshot = new System.Windows.Forms.PictureBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupBoxInvalid = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.gridSaves)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saveBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreenshot)).BeginInit();
            this.groupBoxInvalid.SuspendLayout();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // gridSaves
            // 
            this.gridSaves.AllowUserToAddRows = false;
            this.gridSaves.AllowUserToDeleteRows = false;
            this.gridSaves.AutoGenerateColumns = false;
            this.gridSaves.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.gridSaves.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.gridSaves.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridSaves.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gridSaves.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.gridSaves.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridSaves.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.fileDataGridViewTextBoxColumn,
            this.realTimeDataGridViewTextBoxColumn,
            this.pathNameDataGridViewTextBoxColumn,
            this.gameTimeDataGridViewTextBoxColumn,
            this.distanceDataGridViewTextBoxColumn,
            this.currentTileDataGridViewTextBoxColumn,
            this.validDataGridViewCheckBoxColumn,
            this.Blank});
            this.gridSaves.DataSource = this.saveBindingSource;
            this.gridSaves.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSaves.Location = new System.Drawing.Point(3, 3);
            this.gridSaves.Name = "gridSaves";
            this.gridSaves.ReadOnly = true;
            this.gridSaves.RowHeadersVisible = false;
            this.gridSaves.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.gridSaves.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSaves.Size = new System.Drawing.Size(507, 350);
            this.gridSaves.TabIndex = 1;
            this.gridSaves.DoubleClick += new System.EventHandler(this.gridSaves_DoubleClick);
            this.gridSaves.SelectionChanged += new System.EventHandler(this.gridSaves_SelectionChanged);
            // 
            // fileDataGridViewTextBoxColumn
            // 
            this.fileDataGridViewTextBoxColumn.DataPropertyName = "File";
            this.fileDataGridViewTextBoxColumn.HeaderText = "File";
            this.fileDataGridViewTextBoxColumn.Name = "fileDataGridViewTextBoxColumn";
            this.fileDataGridViewTextBoxColumn.ReadOnly = true;
            this.fileDataGridViewTextBoxColumn.Visible = false;
            this.fileDataGridViewTextBoxColumn.Width = 48;
            // 
            // realTimeDataGridViewTextBoxColumn
            // 
            this.realTimeDataGridViewTextBoxColumn.DataPropertyName = "RealTime";
            this.realTimeDataGridViewTextBoxColumn.HeaderText = "Saved At";
            this.realTimeDataGridViewTextBoxColumn.Name = "realTimeDataGridViewTextBoxColumn";
            this.realTimeDataGridViewTextBoxColumn.ReadOnly = true;
            this.realTimeDataGridViewTextBoxColumn.Width = 76;
            // 
            // pathNameDataGridViewTextBoxColumn
            // 
            this.pathNameDataGridViewTextBoxColumn.DataPropertyName = "PathName";
            this.pathNameDataGridViewTextBoxColumn.HeaderText = "Path";
            this.pathNameDataGridViewTextBoxColumn.Name = "pathNameDataGridViewTextBoxColumn";
            this.pathNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.pathNameDataGridViewTextBoxColumn.Width = 54;
            // 
            // gameTimeDataGridViewTextBoxColumn
            // 
            this.gameTimeDataGridViewTextBoxColumn.DataPropertyName = "GameTime";
            this.gameTimeDataGridViewTextBoxColumn.HeaderText = "Time";
            this.gameTimeDataGridViewTextBoxColumn.Name = "gameTimeDataGridViewTextBoxColumn";
            this.gameTimeDataGridViewTextBoxColumn.ReadOnly = true;
            this.gameTimeDataGridViewTextBoxColumn.Width = 55;
            // 
            // distanceDataGridViewTextBoxColumn
            // 
            this.distanceDataGridViewTextBoxColumn.DataPropertyName = "Distance";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopRight;
            this.distanceDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle1;
            this.distanceDataGridViewTextBoxColumn.HeaderText = "Distance";
            this.distanceDataGridViewTextBoxColumn.Name = "distanceDataGridViewTextBoxColumn";
            this.distanceDataGridViewTextBoxColumn.ReadOnly = true;
            this.distanceDataGridViewTextBoxColumn.Width = 74;
            // 
            // currentTileDataGridViewTextBoxColumn
            // 
            this.currentTileDataGridViewTextBoxColumn.DataPropertyName = "CurrentTile";
            this.currentTileDataGridViewTextBoxColumn.HeaderText = "Tile";
            this.currentTileDataGridViewTextBoxColumn.Name = "currentTileDataGridViewTextBoxColumn";
            this.currentTileDataGridViewTextBoxColumn.ReadOnly = true;
            this.currentTileDataGridViewTextBoxColumn.Width = 49;
            // 
            // validDataGridViewCheckBoxColumn
            // 
            this.validDataGridViewCheckBoxColumn.DataPropertyName = "Valid";
            this.validDataGridViewCheckBoxColumn.HeaderText = "Valid";
            this.validDataGridViewCheckBoxColumn.Name = "validDataGridViewCheckBoxColumn";
            this.validDataGridViewCheckBoxColumn.ReadOnly = true;
            this.validDataGridViewCheckBoxColumn.Width = 36;
            // 
            // Blank
            // 
            this.Blank.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Blank.HeaderText = "";
            this.Blank.Name = "Blank";
            this.Blank.ReadOnly = true;
            this.Blank.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // saveBindingSource
            // 
            this.saveBindingSource.DataSource = typeof(ORTS.ResumeForm.Save);
            // 
            // buttonResume
            // 
            this.buttonResume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResume.Location = new System.Drawing.Point(749, 359);
            this.buttonResume.Name = "buttonResume";
            this.tableLayoutPanel.SetRowSpan(this.buttonResume, 2);
            this.buttonResume.Size = new System.Drawing.Size(104, 52);
            this.buttonResume.TabIndex = 2;
            this.buttonResume.Text = "Resume";
            this.buttonResume.UseVisualStyleBackColor = true;
            this.buttonResume.Click += new System.EventHandler(this.buttonResume_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Location = new System.Drawing.Point(516, 359);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(104, 23);
            this.buttonDelete.TabIndex = 3;
            this.buttonDelete.Text = "Delete save";
            this.toolTip.SetToolTip(this.buttonDelete, "Deletes the currently selected save or saves.");
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // buttonUndelete
            // 
            this.buttonUndelete.Location = new System.Drawing.Point(516, 388);
            this.buttonUndelete.Name = "buttonUndelete";
            this.buttonUndelete.Size = new System.Drawing.Size(104, 23);
            this.buttonUndelete.TabIndex = 4;
            this.buttonUndelete.Text = "Undelete saves";
            this.toolTip.SetToolTip(this.buttonUndelete, "Restores all saves deleted in this session.");
            this.buttonUndelete.UseVisualStyleBackColor = true;
            this.buttonUndelete.Click += new System.EventHandler(this.buttonUndelete_Click);
            // 
            // labelInvalidSaves
            // 
            this.labelInvalidSaves.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelInvalidSaves.Location = new System.Drawing.Point(6, 16);
            this.labelInvalidSaves.Name = "labelInvalidSaves";
            this.labelInvalidSaves.Size = new System.Drawing.Size(385, 33);
            this.labelInvalidSaves.TabIndex = 0;
            // 
            // buttonDeleteInvalid
            // 
            this.buttonDeleteInvalid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDeleteInvalid.Location = new System.Drawing.Point(397, 23);
            this.buttonDeleteInvalid.Name = "buttonDeleteInvalid";
            this.buttonDeleteInvalid.Size = new System.Drawing.Size(104, 23);
            this.buttonDeleteInvalid.TabIndex = 1;
            this.buttonDeleteInvalid.Text = "Delete invalid";
            this.buttonDeleteInvalid.UseVisualStyleBackColor = true;
            this.buttonDeleteInvalid.Click += new System.EventHandler(this.buttonDeleteInvalid_Click);
            // 
            // pictureBoxScreenshot
            // 
            this.pictureBoxScreenshot.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel.SetColumnSpan(this.pictureBoxScreenshot, 2);
            this.pictureBoxScreenshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxScreenshot.Location = new System.Drawing.Point(516, 3);
            this.pictureBoxScreenshot.Name = "pictureBoxScreenshot";
            this.pictureBoxScreenshot.Size = new System.Drawing.Size(337, 350);
            this.pictureBoxScreenshot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxScreenshot.TabIndex = 5;
            this.pictureBoxScreenshot.TabStop = false;
            this.pictureBoxScreenshot.Click += new System.EventHandler(this.pictureBoxScreenshot_Click);
            // 
            // groupBoxInvalid
            // 
            this.groupBoxInvalid.Controls.Add(this.labelInvalidSaves);
            this.groupBoxInvalid.Controls.Add(this.buttonDeleteInvalid);
            this.groupBoxInvalid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxInvalid.Location = new System.Drawing.Point(3, 359);
            this.groupBoxInvalid.Name = "groupBoxInvalid";
            this.tableLayoutPanel.SetRowSpan(this.groupBoxInvalid, 2);
            this.groupBoxInvalid.Size = new System.Drawing.Size(507, 52);
            this.groupBoxInvalid.TabIndex = 6;
            this.groupBoxInvalid.TabStop = false;
            this.groupBoxInvalid.Text = "Invalid saves";
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel.ColumnCount = 3;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel.Controls.Add(this.groupBoxInvalid, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.pictureBoxScreenshot, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.buttonDelete, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.gridSaves, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.buttonResume, 2, 1);
            this.tableLayoutPanel.Controls.Add(this.buttonUndelete, 1, 2);
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 3;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(856, 414);
            this.tableLayoutPanel.TabIndex = 7;
            // 
            // ResumeForm
            // 
            this.AcceptButton = this.buttonResume;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(874, 432);
            this.Controls.Add(this.tableLayoutPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.Name = "ResumeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Saved Games";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ResumeForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.gridSaves)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saveBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreenshot)).EndInit();
            this.groupBoxInvalid.ResumeLayout(false);
            this.tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView gridSaves;
        private System.Windows.Forms.Button buttonResume;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.Button buttonUndelete;
        private System.Windows.Forms.Label labelInvalidSaves;
        private System.Windows.Forms.Button buttonDeleteInvalid;
        private System.Windows.Forms.PictureBox pictureBoxScreenshot;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.BindingSource saveBindingSource;
        private System.Windows.Forms.GroupBox groupBoxInvalid;
        private System.Windows.Forms.DataGridViewTextBoxColumn fileDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn realTimeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn pathNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn gameTimeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn distanceDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn currentTileDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn validDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Blank;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
    }
}