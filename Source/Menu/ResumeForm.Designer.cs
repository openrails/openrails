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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ResumeForm));
            this.gridSaves = new System.Windows.Forms.DataGridView();
            this.saveBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.buttonResume = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonUndelete = new System.Windows.Forms.Button();
            this.labelInvalidSaves = new System.Windows.Forms.Label();
            this.buttonDeleteInvalid = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.buttonImportExportSaves = new System.Windows.Forms.Button();
            this.groupBoxInvalid = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.buttonReplayFromPreviousSave = new System.Windows.Forms.Button();
            this.buttonReplayFromStart = new System.Windows.Forms.Button();
            this.checkBoxReplayPauseBeforeEnd = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numericReplayPauseBeforeEnd = new System.Windows.Forms.NumericUpDown();
            this.panelSaves = new System.Windows.Forms.Panel();
            this.panelScreenshot = new System.Windows.Forms.Panel();
            this.pictureBoxScreenshot = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.fileDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.realTimeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pathNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gameTimeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.distanceDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.currentTileDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.validDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.evalDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Blank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.gridSaves)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saveBindingSource)).BeginInit();
            this.groupBoxInvalid.SuspendLayout();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericReplayPauseBeforeEnd)).BeginInit();
            this.panelSaves.SuspendLayout();
            this.panelScreenshot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreenshot)).BeginInit();
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
            this.gridSaves.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridSaves.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gridSaves.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridSaves.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridSaves.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridSaves.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.fileDataGridViewTextBoxColumn,
            this.realTimeDataGridViewTextBoxColumn,
            this.pathNameDataGridViewTextBoxColumn,
            this.gameTimeDataGridViewTextBoxColumn,
            this.distanceDataGridViewTextBoxColumn,
            this.currentTileDataGridViewTextBoxColumn,
            this.validDataGridViewCheckBoxColumn,
            this.evalDataGridViewCheckBoxColumn,
            this.Blank});
            this.gridSaves.DataSource = this.saveBindingSource;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridSaves.DefaultCellStyle = dataGridViewCellStyle3;
            this.gridSaves.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSaves.Location = new System.Drawing.Point(0, 0);
            this.gridSaves.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gridSaves.Name = "gridSaves";
            this.gridSaves.ReadOnly = true;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridSaves.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.gridSaves.RowHeadersVisible = false;
            this.gridSaves.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.gridSaves.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSaves.Size = new System.Drawing.Size(499, 412);
            this.gridSaves.TabIndex = 0;
            this.gridSaves.SelectionChanged += new System.EventHandler(this.gridSaves_SelectionChanged);
            this.gridSaves.DoubleClick += new System.EventHandler(this.gridSaves_DoubleClick);
            // 
            // saveBindingSource
            // 
            this.saveBindingSource.DataSource = typeof(ORTS.ResumeForm.Save);
            // 
            // buttonResume
            // 
            this.buttonResume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResume.Location = new System.Drawing.Point(971, 528);
            this.buttonResume.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonResume.Name = "buttonResume";
            this.buttonResume.Size = new System.Drawing.Size(100, 28);
            this.buttonResume.TabIndex = 1;
            this.buttonResume.Text = "Resume";
            this.buttonResume.UseVisualStyleBackColor = true;
            this.buttonResume.Click += new System.EventHandler(this.buttonResume_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDelete.Location = new System.Drawing.Point(405, 426);
            this.buttonDelete.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(100, 28);
            this.buttonDelete.TabIndex = 7;
            this.buttonDelete.Text = "Delete";
            this.toolTip.SetToolTip(this.buttonDelete, "Deletes the currently selected save or saves.");
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // buttonUndelete
            // 
            this.buttonUndelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonUndelete.Location = new System.Drawing.Point(405, 462);
            this.buttonUndelete.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonUndelete.Name = "buttonUndelete";
            this.buttonUndelete.Size = new System.Drawing.Size(100, 28);
            this.buttonUndelete.TabIndex = 8;
            this.buttonUndelete.Text = "Undelete";
            this.toolTip.SetToolTip(this.buttonUndelete, "Restores all saves deleted in this session.");
            this.buttonUndelete.UseVisualStyleBackColor = true;
            this.buttonUndelete.Click += new System.EventHandler(this.buttonUndelete_Click);
            // 
            // labelInvalidSaves
            // 
            this.labelInvalidSaves.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelInvalidSaves.Location = new System.Drawing.Point(8, 20);
            this.labelInvalidSaves.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelInvalidSaves.Name = "labelInvalidSaves";
            this.labelInvalidSaves.Size = new System.Drawing.Size(377, 75);
            this.labelInvalidSaves.TabIndex = 0;
            // 
            // buttonDeleteInvalid
            // 
            this.buttonDeleteInvalid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDeleteInvalid.Location = new System.Drawing.Point(8, 98);
            this.buttonDeleteInvalid.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonDeleteInvalid.Name = "buttonDeleteInvalid";
            this.buttonDeleteInvalid.Size = new System.Drawing.Size(259, 28);
            this.buttonDeleteInvalid.TabIndex = 1;
            this.buttonDeleteInvalid.Text = "Delete all invalid saves";
            this.buttonDeleteInvalid.UseVisualStyleBackColor = true;
            this.buttonDeleteInvalid.Click += new System.EventHandler(this.buttonDeleteInvalid_Click);
            // 
            // buttonImportExportSaves
            // 
            this.buttonImportExportSaves.Location = new System.Drawing.Point(405, 498);
            this.buttonImportExportSaves.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonImportExportSaves.Name = "buttonImportExportSaves";
            this.tableLayoutPanel.SetRowSpan(this.buttonImportExportSaves, 2);
            this.buttonImportExportSaves.Size = new System.Drawing.Size(100, 60);
            this.buttonImportExportSaves.TabIndex = 9;
            this.buttonImportExportSaves.Text = "Import/ export";
            this.toolTip.SetToolTip(this.buttonImportExportSaves, "Restores all saves deleted in this session.");
            this.buttonImportExportSaves.UseVisualStyleBackColor = true;
            this.buttonImportExportSaves.Click += new System.EventHandler(this.buttonImportExportSaves_Click);
            // 
            // groupBoxInvalid
            // 
            this.groupBoxInvalid.Controls.Add(this.labelInvalidSaves);
            this.groupBoxInvalid.Controls.Add(this.buttonDeleteInvalid);
            this.groupBoxInvalid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxInvalid.Location = new System.Drawing.Point(4, 426);
            this.groupBoxInvalid.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBoxInvalid.Name = "groupBoxInvalid";
            this.groupBoxInvalid.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel.SetRowSpan(this.groupBoxInvalid, 4);
            this.groupBoxInvalid.Size = new System.Drawing.Size(393, 134);
            this.groupBoxInvalid.TabIndex = 10;
            this.groupBoxInvalid.TabStop = false;
            this.groupBoxInvalid.Text = "Invalid saves";
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel.ColumnCount = 5;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel.Controls.Add(this.groupBoxInvalid, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.buttonImportExportSaves, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.buttonReplayFromPreviousSave, 2, 4);
            this.tableLayoutPanel.Controls.Add(this.buttonReplayFromStart, 3, 4);
            this.tableLayoutPanel.Controls.Add(this.buttonResume, 4, 4);
            this.tableLayoutPanel.Controls.Add(this.checkBoxReplayPauseBeforeEnd, 2, 2);
            this.tableLayoutPanel.Controls.Add(this.label1, 2, 3);
            this.tableLayoutPanel.Controls.Add(this.numericReplayPauseBeforeEnd, 3, 3);
            this.tableLayoutPanel.Controls.Add(this.buttonDelete, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.buttonUndelete, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.panelSaves, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.panelScreenshot, 2, 0);
            this.tableLayoutPanel.Location = new System.Drawing.Point(12, 11);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 5;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(1075, 564);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // buttonReplayFromPreviousSave
            // 
            this.buttonReplayFromPreviousSave.Location = new System.Drawing.Point(513, 528);
            this.buttonReplayFromPreviousSave.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonReplayFromPreviousSave.Name = "buttonReplayFromPreviousSave";
            this.buttonReplayFromPreviousSave.Size = new System.Drawing.Size(200, 28);
            this.buttonReplayFromPreviousSave.TabIndex = 2;
            this.buttonReplayFromPreviousSave.Text = "Replay from previous save";
            this.buttonReplayFromPreviousSave.UseVisualStyleBackColor = true;
            this.buttonReplayFromPreviousSave.Click += new System.EventHandler(this.buttonReplayFromPreviousSave_Click);
            // 
            // buttonReplayFromStart
            // 
            this.buttonReplayFromStart.Location = new System.Drawing.Point(721, 528);
            this.buttonReplayFromStart.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonReplayFromStart.Name = "buttonReplayFromStart";
            this.buttonReplayFromStart.Size = new System.Drawing.Size(200, 28);
            this.buttonReplayFromStart.TabIndex = 3;
            this.buttonReplayFromStart.Text = "Replay from start";
            this.buttonReplayFromStart.UseVisualStyleBackColor = true;
            this.buttonReplayFromStart.Click += new System.EventHandler(this.buttonReplayFromStart_Click);
            // 
            // checkBoxReplayPauseBeforeEnd
            // 
            this.checkBoxReplayPauseBeforeEnd.AutoSize = true;
            this.checkBoxReplayPauseBeforeEnd.Checked = true;
            this.checkBoxReplayPauseBeforeEnd.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxReplayPauseBeforeEnd.Location = new System.Drawing.Point(513, 462);
            this.checkBoxReplayPauseBeforeEnd.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxReplayPauseBeforeEnd.Name = "checkBoxReplayPauseBeforeEnd";
            this.checkBoxReplayPauseBeforeEnd.Size = new System.Drawing.Size(157, 21);
            this.checkBoxReplayPauseBeforeEnd.TabIndex = 4;
            this.checkBoxReplayPauseBeforeEnd.Text = "Pause replay at end";
            this.checkBoxReplayPauseBeforeEnd.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(531, 494);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(182, 30);
            this.label1.TabIndex = 6;
            this.label1.Text = "Pause seconds before end:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numericReplayPauseBeforeEnd
            // 
            this.numericReplayPauseBeforeEnd.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.numericReplayPauseBeforeEnd.Location = new System.Drawing.Point(721, 498);
            this.numericReplayPauseBeforeEnd.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.numericReplayPauseBeforeEnd.Maximum = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numericReplayPauseBeforeEnd.Minimum = new decimal(new int[] {
            3600,
            0,
            0,
            -2147483648});
            this.numericReplayPauseBeforeEnd.Name = "numericReplayPauseBeforeEnd";
            this.numericReplayPauseBeforeEnd.Size = new System.Drawing.Size(69, 22);
            this.numericReplayPauseBeforeEnd.TabIndex = 5;
            this.numericReplayPauseBeforeEnd.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // panelSaves
            // 
            this.panelSaves.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel.SetColumnSpan(this.panelSaves, 2);
            this.panelSaves.Controls.Add(this.gridSaves);
            this.panelSaves.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSaves.Location = new System.Drawing.Point(4, 4);
            this.panelSaves.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelSaves.Name = "panelSaves";
            this.panelSaves.Size = new System.Drawing.Size(501, 414);
            this.panelSaves.TabIndex = 11;
            // 
            // panelScreenshot
            // 
            this.panelScreenshot.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel.SetColumnSpan(this.panelScreenshot, 3);
            this.panelScreenshot.Controls.Add(this.pictureBoxScreenshot);
            this.panelScreenshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelScreenshot.Location = new System.Drawing.Point(513, 4);
            this.panelScreenshot.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelScreenshot.Name = "panelScreenshot";
            this.panelScreenshot.Size = new System.Drawing.Size(558, 414);
            this.panelScreenshot.TabIndex = 12;
            // 
            // pictureBoxScreenshot
            // 
            this.pictureBoxScreenshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxScreenshot.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxScreenshot.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxScreenshot.Name = "pictureBoxScreenshot";
            this.pictureBoxScreenshot.Size = new System.Drawing.Size(556, 412);
            this.pictureBoxScreenshot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxScreenshot.TabIndex = 5;
            this.pictureBoxScreenshot.TabStop = false;
            this.pictureBoxScreenshot.Click += new System.EventHandler(this.pictureBoxScreenshot_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // fileDataGridViewTextBoxColumn
            // 
            this.fileDataGridViewTextBoxColumn.DataPropertyName = "File";
            this.fileDataGridViewTextBoxColumn.HeaderText = "File";
            this.fileDataGridViewTextBoxColumn.Name = "fileDataGridViewTextBoxColumn";
            this.fileDataGridViewTextBoxColumn.ReadOnly = true;
            this.fileDataGridViewTextBoxColumn.Visible = false;
            this.fileDataGridViewTextBoxColumn.Width = 59;
            // 
            // realTimeDataGridViewTextBoxColumn
            // 
            this.realTimeDataGridViewTextBoxColumn.DataPropertyName = "RealTime";
            this.realTimeDataGridViewTextBoxColumn.HeaderText = "Saved At";
            this.realTimeDataGridViewTextBoxColumn.Name = "realTimeDataGridViewTextBoxColumn";
            this.realTimeDataGridViewTextBoxColumn.ReadOnly = true;
            this.realTimeDataGridViewTextBoxColumn.Width = 94;
            // 
            // pathNameDataGridViewTextBoxColumn
            // 
            this.pathNameDataGridViewTextBoxColumn.DataPropertyName = "PathName";
            this.pathNameDataGridViewTextBoxColumn.HeaderText = "Path";
            this.pathNameDataGridViewTextBoxColumn.Name = "pathNameDataGridViewTextBoxColumn";
            this.pathNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.pathNameDataGridViewTextBoxColumn.Width = 66;
            // 
            // gameTimeDataGridViewTextBoxColumn
            // 
            this.gameTimeDataGridViewTextBoxColumn.DataPropertyName = "GameTime";
            this.gameTimeDataGridViewTextBoxColumn.HeaderText = "Time";
            this.gameTimeDataGridViewTextBoxColumn.Name = "gameTimeDataGridViewTextBoxColumn";
            this.gameTimeDataGridViewTextBoxColumn.ReadOnly = true;
            this.gameTimeDataGridViewTextBoxColumn.Width = 68;
            // 
            // distanceDataGridViewTextBoxColumn
            // 
            this.distanceDataGridViewTextBoxColumn.DataPropertyName = "Distance";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopRight;
            this.distanceDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.distanceDataGridViewTextBoxColumn.HeaderText = "Distance";
            this.distanceDataGridViewTextBoxColumn.Name = "distanceDataGridViewTextBoxColumn";
            this.distanceDataGridViewTextBoxColumn.ReadOnly = true;
            this.distanceDataGridViewTextBoxColumn.Width = 92;
            // 
            // currentTileDataGridViewTextBoxColumn
            // 
            this.currentTileDataGridViewTextBoxColumn.DataPropertyName = "CurrentTile";
            this.currentTileDataGridViewTextBoxColumn.HeaderText = "Tile";
            this.currentTileDataGridViewTextBoxColumn.Name = "currentTileDataGridViewTextBoxColumn";
            this.currentTileDataGridViewTextBoxColumn.ReadOnly = true;
            this.currentTileDataGridViewTextBoxColumn.Width = 60;
            // 
            // validDataGridViewCheckBoxColumn
            // 
            this.validDataGridViewCheckBoxColumn.DataPropertyName = "Valid";
            this.validDataGridViewCheckBoxColumn.HeaderText = "Valid";
            this.validDataGridViewCheckBoxColumn.Name = "validDataGridViewCheckBoxColumn";
            this.validDataGridViewCheckBoxColumn.ReadOnly = true;
            this.validDataGridViewCheckBoxColumn.ThreeState = true;
            this.validDataGridViewCheckBoxColumn.Width = 45;
            // 
            // evalDataGridViewCheckBoxColumn
            // 
            this.evalDataGridViewCheckBoxColumn.DataPropertyName = "DbfEval";
            this.evalDataGridViewCheckBoxColumn.HeaderText = "Eval";
            this.evalDataGridViewCheckBoxColumn.Name = "evalDataGridViewCheckBoxColumn";
            this.evalDataGridViewCheckBoxColumn.ReadOnly = true;
            this.evalDataGridViewCheckBoxColumn.ThreeState = true;
            this.evalDataGridViewCheckBoxColumn.Width = 45;
            // 
            // Blank
            // 
            this.Blank.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Blank.HeaderText = "";
            this.Blank.Name = "Blank";
            this.Blank.ReadOnly = true;
            this.Blank.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // ResumeForm
            // 
            this.AcceptButton = this.buttonResume;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1099, 586);
            this.Controls.Add(this.tableLayoutPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimizeBox = false;
            this.Name = "ResumeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Saved Games";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ResumeForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.gridSaves)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saveBindingSource)).EndInit();
            this.groupBoxInvalid.ResumeLayout(false);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericReplayPauseBeforeEnd)).EndInit();
            this.panelSaves.ResumeLayout(false);
            this.panelScreenshot.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreenshot)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView gridSaves;
        private System.Windows.Forms.Button buttonResume;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.Button buttonUndelete;
        private System.Windows.Forms.Label labelInvalidSaves;
        private System.Windows.Forms.Button buttonDeleteInvalid;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.BindingSource saveBindingSource;
        private System.Windows.Forms.GroupBox groupBoxInvalid;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Button buttonImportExportSaves;
        private System.Windows.Forms.PictureBox pictureBoxScreenshot;
        private System.Windows.Forms.NumericUpDown numericReplayPauseBeforeEnd;
        private System.Windows.Forms.Button buttonReplayFromPreviousSave;
        private System.Windows.Forms.Button buttonReplayFromStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Panel panelSaves;
        private System.Windows.Forms.Panel panelScreenshot;
        private System.Windows.Forms.CheckBox checkBoxReplayPauseBeforeEnd;
        private System.Windows.Forms.DataGridViewTextBoxColumn fileDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn realTimeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn pathNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn gameTimeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn distanceDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn currentTileDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn validDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn evalDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Blank;
    }
}
