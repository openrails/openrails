namespace ORTS {
    partial class ActivitySaveForm {
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dGVActivitySave = new System.Windows.Forms.DataGridView();
            this.bSActivitySave = new System.Windows.Forms.BindingSource( this.components );
            this.bResume = new System.Windows.Forms.Button();
            this.bDeleteSave = new System.Windows.Forms.Button();
            this.bUndeleteSave = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lSaveTotals = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.bDeleteInvalidSaves = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tbActivity = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip( this.components );
            this.bClose = new System.Windows.Forms.Button();
            this.SaveFileName = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dateTimeSavedDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PathDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gameTimeElapsedDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MetresTravelled = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TileXZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Valid = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dGVActivitySave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bSActivitySave)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // dGVActivitySave
            // 
            this.dGVActivitySave.AllowUserToAddRows = false;
            this.dGVActivitySave.AllowUserToOrderColumns = true;
            this.dGVActivitySave.AutoGenerateColumns = false;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dGVActivitySave.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dGVActivitySave.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dGVActivitySave.Columns.AddRange( new System.Windows.Forms.DataGridViewColumn[] {
            this.SaveFileName,
            this.dateTimeSavedDataGridViewTextBoxColumn,
            this.PathDescription,
            this.gameTimeElapsedDataGridViewTextBoxColumn,
            this.MetresTravelled,
            this.TileXZ,
            this.Valid} );
            this.dGVActivitySave.DataSource = this.bSActivitySave;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle11.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dGVActivitySave.DefaultCellStyle = dataGridViewCellStyle11;
            this.dGVActivitySave.Location = new System.Drawing.Point( 13, 35 );
            this.dGVActivitySave.Name = "dGVActivitySave";
            this.dGVActivitySave.ReadOnly = true;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle12.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dGVActivitySave.RowHeadersDefaultCellStyle = dataGridViewCellStyle12;
            this.dGVActivitySave.RowHeadersVisible = false;
            this.dGVActivitySave.Size = new System.Drawing.Size( 506, 208 );
            this.dGVActivitySave.TabIndex = 0;
            this.dGVActivitySave.DoubleClick += new System.EventHandler( this.dGVActivitySave_DoubleClick );
            this.dGVActivitySave.SelectionChanged += new System.EventHandler( this.dGVActivitySave_SelectionChanged );
            // 
            // bSActivitySave
            // 
            this.bSActivitySave.DataSource = typeof( ORTS.ActivitySaveForm.ActivitySave );
            // 
            // bResume
            // 
            this.bResume.BackColor = System.Drawing.Color.FromArgb( ((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))) );
            this.bResume.Location = new System.Drawing.Point( 535, 191 );
            this.bResume.Name = "bResume";
            this.bResume.Size = new System.Drawing.Size( 101, 52 );
            this.bResume.TabIndex = 1;
            this.bResume.Text = "Resume from save";
            this.bResume.UseVisualStyleBackColor = false;
            this.bResume.Click += new System.EventHandler( this.bResume_Click );
            // 
            // bDeleteSave
            // 
            this.bDeleteSave.Location = new System.Drawing.Point( 642, 191 );
            this.bDeleteSave.Name = "bDeleteSave";
            this.bDeleteSave.Size = new System.Drawing.Size( 100, 23 );
            this.bDeleteSave.TabIndex = 2;
            this.bDeleteSave.Text = "Delete save";
            this.toolTip1.SetToolTip( this.bDeleteSave, "Deletes the currently selected save or saves." );
            this.bDeleteSave.UseVisualStyleBackColor = true;
            this.bDeleteSave.Click += new System.EventHandler( this.bDeleteSave_Click );
            // 
            // bUndeleteSave
            // 
            this.bUndeleteSave.Location = new System.Drawing.Point( 642, 220 );
            this.bUndeleteSave.Name = "bUndeleteSave";
            this.bUndeleteSave.Size = new System.Drawing.Size( 100, 23 );
            this.bUndeleteSave.TabIndex = 3;
            this.bUndeleteSave.Text = "Undelete saves";
            this.toolTip1.SetToolTip( this.bUndeleteSave, "Restores all saves deleted in this session." );
            this.bUndeleteSave.UseVisualStyleBackColor = true;
            this.bUndeleteSave.Click += new System.EventHandler( this.bUndeleteSave_Click );
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add( this.lSaveTotals );
            this.groupBox1.Controls.Add( this.label1 );
            this.groupBox1.Controls.Add( this.bDeleteInvalidSaves );
            this.groupBox1.Location = new System.Drawing.Point( 12, 249 );
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size( 507, 57 );
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Invalid Saves";
            // 
            // lSaveTotals
            // 
            this.lSaveTotals.AutoSize = true;
            this.lSaveTotals.Location = new System.Drawing.Point( 9, 31 );
            this.lSaveTotals.Name = "lSaveTotals";
            this.lSaveTotals.Size = new System.Drawing.Size( 30, 13 );
            this.lSaveTotals.TabIndex = 7;
            this.lSaveTotals.Text = "? / ?";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point( 6, 18 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 361, 26 );
            this.label1.TabIndex = 0;
            this.label1.Text = "To avoid crashes, new versions of Open Rails will invalidate existing saves.\r\n   " +
                "           saves are no longer valid.";
            // 
            // bDeleteInvalidSaves
            // 
            this.bDeleteInvalidSaves.Location = new System.Drawing.Point( 386, 12 );
            this.bDeleteInvalidSaves.Name = "bDeleteInvalidSaves";
            this.bDeleteInvalidSaves.Size = new System.Drawing.Size( 105, 39 );
            this.bDeleteInvalidSaves.TabIndex = 5;
            this.bDeleteInvalidSaves.Text = "Delete invalid saves";
            this.bDeleteInvalidSaves.UseVisualStyleBackColor = true;
            this.bDeleteInvalidSaves.Click += new System.EventHandler( this.bDeleteInvalidSaves_Click );
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point( 535, 9 );
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size( 206, 175 );
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler( this.pictureBox1_Click );
            // 
            // tbActivity
            // 
            this.tbActivity.Location = new System.Drawing.Point( 13, 9 );
            this.tbActivity.Name = "tbActivity";
            this.tbActivity.ReadOnly = true;
            this.tbActivity.Size = new System.Drawing.Size( 506, 20 );
            this.tbActivity.TabIndex = 6;
            this.tbActivity.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // bClose
            // 
            this.bClose.Location = new System.Drawing.Point( 535, 250 );
            this.bClose.Name = "bClose";
            this.bClose.Size = new System.Drawing.Size( 101, 25 );
            this.bClose.TabIndex = 7;
            this.bClose.Text = "Close";
            this.bClose.UseVisualStyleBackColor = true;
            this.bClose.Click += new System.EventHandler( this.bClose_Click );
            // 
            // SaveFileName
            // 
            this.SaveFileName.DataPropertyName = "SaveFileName";
            this.SaveFileName.HeaderText = "SaveFileName";
            this.SaveFileName.Name = "SaveFileName";
            this.SaveFileName.ReadOnly = true;
            this.SaveFileName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.SaveFileName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.SaveFileName.Visible = false;
            // 
            // dateTimeSavedDataGridViewTextBoxColumn
            // 
            this.dateTimeSavedDataGridViewTextBoxColumn.DataPropertyName = "DateTimeSaved";
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dateTimeSavedDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle8;
            this.dateTimeSavedDataGridViewTextBoxColumn.FillWeight = 150F;
            this.dateTimeSavedDataGridViewTextBoxColumn.HeaderText = "Saved On";
            this.dateTimeSavedDataGridViewTextBoxColumn.Name = "dateTimeSavedDataGridViewTextBoxColumn";
            this.dateTimeSavedDataGridViewTextBoxColumn.ReadOnly = true;
            this.dateTimeSavedDataGridViewTextBoxColumn.ToolTipText = "Date and time when state was saved";
            this.dateTimeSavedDataGridViewTextBoxColumn.Width = 115;
            // 
            // PathDescription
            // 
            this.PathDescription.DataPropertyName = "PathDescription";
            this.PathDescription.HeaderText = "Path Description";
            this.PathDescription.Name = "PathDescription";
            this.PathDescription.ReadOnly = true;
            this.PathDescription.Width = 185;
            // 
            // gameTimeElapsedDataGridViewTextBoxColumn
            // 
            this.gameTimeElapsedDataGridViewTextBoxColumn.DataPropertyName = "GameTimeElapsed";
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.gameTimeElapsedDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle9;
            this.gameTimeElapsedDataGridViewTextBoxColumn.HeaderText = "Time Elapsed";
            this.gameTimeElapsedDataGridViewTextBoxColumn.Name = "gameTimeElapsedDataGridViewTextBoxColumn";
            this.gameTimeElapsedDataGridViewTextBoxColumn.ReadOnly = true;
            this.gameTimeElapsedDataGridViewTextBoxColumn.ToolTipText = "Time elapsed since game started";
            this.gameTimeElapsedDataGridViewTextBoxColumn.Width = 55;
            // 
            // MetresTravelled
            // 
            this.MetresTravelled.DataPropertyName = "MetresTravelled";
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.MetresTravelled.DefaultCellStyle = dataGridViewCellStyle10;
            this.MetresTravelled.HeaderText = "Metres From Start";
            this.MetresTravelled.Name = "MetresTravelled";
            this.MetresTravelled.ReadOnly = true;
            this.MetresTravelled.ToolTipText = "Distance \"as the crow flies\"";
            this.MetresTravelled.Width = 85;
            // 
            // TileXZ
            // 
            this.TileXZ.DataPropertyName = "TileXZ";
            this.TileXZ.HeaderText = "Tile(X,Z)";
            this.TileXZ.Name = "TileXZ";
            this.TileXZ.ReadOnly = true;
            // 
            // Valid
            // 
            this.Valid.DataPropertyName = "Valid";
            this.Valid.HeaderText = "Valid";
            this.Valid.Name = "Valid";
            this.Valid.ReadOnly = true;
            this.Valid.ToolTipText = "Saves cease to be valid after upgrading to a new version";
            this.Valid.Width = 40;
            // 
            // ActivitySaveForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 756, 318 );
            this.Controls.Add( this.bClose );
            this.Controls.Add( this.tbActivity );
            this.Controls.Add( this.pictureBox1 );
            this.Controls.Add( this.groupBox1 );
            this.Controls.Add( this.bUndeleteSave );
            this.Controls.Add( this.bDeleteSave );
            this.Controls.Add( this.bResume );
            this.Controls.Add( this.dGVActivitySave );
            this.Name = "ActivitySaveForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Activity Saves";
            ((System.ComponentModel.ISupportInitialize)(this.dGVActivitySave)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bSActivitySave)).EndInit();
            this.groupBox1.ResumeLayout( false );
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dGVActivitySave;
        private System.Windows.Forms.BindingSource bSActivitySave;
        private System.Windows.Forms.Button bResume;
        private System.Windows.Forms.Button bDeleteSave;
        private System.Windows.Forms.Button bUndeleteSave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bDeleteInvalidSaves;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox tbActivity;
        private System.Windows.Forms.DataGridViewTextBoxColumn tileXDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn tileZDataGridViewTextBoxColumn;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lSaveTotals;
        private System.Windows.Forms.Button bClose;
        private System.Windows.Forms.DataGridViewCheckBoxColumn SaveFileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dateTimeSavedDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn PathDescription;
        private System.Windows.Forms.DataGridViewTextBoxColumn gameTimeElapsedDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MetresTravelled;
        private System.Windows.Forms.DataGridViewTextBoxColumn TileXZ;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Valid;
    }
}