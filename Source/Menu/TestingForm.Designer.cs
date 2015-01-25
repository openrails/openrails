namespace ORTS {
    partial class TestingForm {
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestingForm));
            this.buttonTestAll = new System.Windows.Forms.Button();
            this.buttonTest = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSummary = new System.Windows.Forms.Button();
            this.gridTestActivities = new System.Windows.Forms.DataGridView();
            this.toTestDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.activityFilePathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.defaultSortDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.routeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.activityDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.testedDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.passedDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.errorsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.loadDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fpsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.blankDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.testBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.buttonDetails = new System.Windows.Forms.Button();
            this.checkBoxOverride = new System.Windows.Forms.CheckBox();
            this.buttonNoSort = new System.Windows.Forms.Button();
            this.panelTests = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.gridTestActivities)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.testBindingSource)).BeginInit();
            this.panelTests.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonTestAll
            // 
            this.buttonTestAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonTestAll.Location = new System.Drawing.Point(12, 441);
            this.buttonTestAll.Name = "buttonTestAll";
            this.buttonTestAll.Size = new System.Drawing.Size(75, 23);
            this.buttonTestAll.TabIndex = 1;
            this.buttonTestAll.Text = "Test all";
            this.buttonTestAll.UseVisualStyleBackColor = true;
            this.buttonTestAll.Click += new System.EventHandler(this.buttonTestAll_Click);
            // 
            // buttonTest
            // 
            this.buttonTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonTest.Location = new System.Drawing.Point(93, 441);
            this.buttonTest.Name = "buttonTest";
            this.buttonTest.Size = new System.Drawing.Size(75, 23);
            this.buttonTest.TabIndex = 2;
            this.buttonTest.Text = "Test";
            this.buttonTest.UseVisualStyleBackColor = true;
            this.buttonTest.Click += new System.EventHandler(this.buttonTest_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCancel.Location = new System.Drawing.Point(174, 441);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonSummary
            // 
            this.buttonSummary.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSummary.Location = new System.Drawing.Point(656, 441);
            this.buttonSummary.Name = "buttonSummary";
            this.buttonSummary.Size = new System.Drawing.Size(75, 23);
            this.buttonSummary.TabIndex = 6;
            this.buttonSummary.Text = "Summary";
            this.buttonSummary.UseVisualStyleBackColor = true;
            this.buttonSummary.Click += new System.EventHandler(this.buttonSummary_Click);
            // 
            // gridTestActivities
            // 
            this.gridTestActivities.AllowUserToAddRows = false;
            this.gridTestActivities.AllowUserToDeleteRows = false;
            this.gridTestActivities.AutoGenerateColumns = false;
            this.gridTestActivities.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.gridTestActivities.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.gridTestActivities.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridTestActivities.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridTestActivities.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gridTestActivities.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridTestActivities.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridTestActivities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridTestActivities.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.toTestDataGridViewCheckBoxColumn,
            this.activityFilePathDataGridViewTextBoxColumn,
            this.defaultSortDataGridViewTextBoxColumn,
            this.routeDataGridViewTextBoxColumn,
            this.activityDataGridViewTextBoxColumn,
            this.testedDataGridViewCheckBoxColumn,
            this.passedDataGridViewCheckBoxColumn,
            this.errorsDataGridViewTextBoxColumn,
            this.loadDataGridViewTextBoxColumn,
            this.fpsDataGridViewTextBoxColumn,
            this.blankDataGridViewTextBoxColumn});
            this.gridTestActivities.DataSource = this.testBindingSource;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridTestActivities.DefaultCellStyle = dataGridViewCellStyle4;
            this.gridTestActivities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTestActivities.Location = new System.Drawing.Point(0, 0);
            this.gridTestActivities.Name = "gridTestActivities";
            this.gridTestActivities.ReadOnly = true;
            this.gridTestActivities.RowHeadersVisible = false;
            this.gridTestActivities.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.gridTestActivities.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridTestActivities.Size = new System.Drawing.Size(798, 421);
            this.gridTestActivities.TabIndex = 0;
            // 
            // toTestDataGridViewCheckBoxColumn
            // 
            this.toTestDataGridViewCheckBoxColumn.DataPropertyName = "ToTest";
            this.toTestDataGridViewCheckBoxColumn.HeaderText = "ToTest";
            this.toTestDataGridViewCheckBoxColumn.Name = "toTestDataGridViewCheckBoxColumn";
            this.toTestDataGridViewCheckBoxColumn.ReadOnly = true;
            this.toTestDataGridViewCheckBoxColumn.Visible = false;
            this.toTestDataGridViewCheckBoxColumn.Width = 47;
            // 
            // activityFilePathDataGridViewTextBoxColumn
            // 
            this.activityFilePathDataGridViewTextBoxColumn.DataPropertyName = "ActivityFilePath";
            this.activityFilePathDataGridViewTextBoxColumn.HeaderText = "ActivityFilePath";
            this.activityFilePathDataGridViewTextBoxColumn.Name = "activityFilePathDataGridViewTextBoxColumn";
            this.activityFilePathDataGridViewTextBoxColumn.ReadOnly = true;
            this.activityFilePathDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.activityFilePathDataGridViewTextBoxColumn.Visible = false;
            this.activityFilePathDataGridViewTextBoxColumn.Width = 85;
            // 
            // defaultSortDataGridViewTextBoxColumn
            // 
            this.defaultSortDataGridViewTextBoxColumn.DataPropertyName = "DefaultSort";
            this.defaultSortDataGridViewTextBoxColumn.HeaderText = "DefaultSort";
            this.defaultSortDataGridViewTextBoxColumn.Name = "defaultSortDataGridViewTextBoxColumn";
            this.defaultSortDataGridViewTextBoxColumn.ReadOnly = true;
            this.defaultSortDataGridViewTextBoxColumn.Visible = false;
            this.defaultSortDataGridViewTextBoxColumn.Width = 85;
            // 
            // routeDataGridViewTextBoxColumn
            // 
            this.routeDataGridViewTextBoxColumn.DataPropertyName = "Route";
            this.routeDataGridViewTextBoxColumn.HeaderText = "Route";
            this.routeDataGridViewTextBoxColumn.Name = "routeDataGridViewTextBoxColumn";
            this.routeDataGridViewTextBoxColumn.ReadOnly = true;
            this.routeDataGridViewTextBoxColumn.Width = 59;
            // 
            // activityDataGridViewTextBoxColumn
            // 
            this.activityDataGridViewTextBoxColumn.DataPropertyName = "Activity";
            this.activityDataGridViewTextBoxColumn.HeaderText = "Activity";
            this.activityDataGridViewTextBoxColumn.Name = "activityDataGridViewTextBoxColumn";
            this.activityDataGridViewTextBoxColumn.ReadOnly = true;
            this.activityDataGridViewTextBoxColumn.Width = 64;
            // 
            // testedDataGridViewCheckBoxColumn
            // 
            this.testedDataGridViewCheckBoxColumn.DataPropertyName = "Tested";
            this.testedDataGridViewCheckBoxColumn.HeaderText = "Tested";
            this.testedDataGridViewCheckBoxColumn.Name = "testedDataGridViewCheckBoxColumn";
            this.testedDataGridViewCheckBoxColumn.ReadOnly = true;
            this.testedDataGridViewCheckBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.testedDataGridViewCheckBoxColumn.Width = 63;
            // 
            // passedDataGridViewCheckBoxColumn
            // 
            this.passedDataGridViewCheckBoxColumn.DataPropertyName = "Passed";
            this.passedDataGridViewCheckBoxColumn.HeaderText = "Passed";
            this.passedDataGridViewCheckBoxColumn.Name = "passedDataGridViewCheckBoxColumn";
            this.passedDataGridViewCheckBoxColumn.ReadOnly = true;
            this.passedDataGridViewCheckBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.passedDataGridViewCheckBoxColumn.Width = 65;
            // 
            // errorsDataGridViewTextBoxColumn
            // 
            this.errorsDataGridViewTextBoxColumn.DataPropertyName = "Errors";
            this.errorsDataGridViewTextBoxColumn.HeaderText = "Errors";
            this.errorsDataGridViewTextBoxColumn.Name = "errorsDataGridViewTextBoxColumn";
            this.errorsDataGridViewTextBoxColumn.ReadOnly = true;
            this.errorsDataGridViewTextBoxColumn.Width = 57;
            // 
            // loadDataGridViewTextBoxColumn
            // 
            this.loadDataGridViewTextBoxColumn.DataPropertyName = "Load";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.loadDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.loadDataGridViewTextBoxColumn.HeaderText = "Load";
            this.loadDataGridViewTextBoxColumn.Name = "loadDataGridViewTextBoxColumn";
            this.loadDataGridViewTextBoxColumn.ReadOnly = true;
            this.loadDataGridViewTextBoxColumn.Width = 54;
            // 
            // fpsDataGridViewTextBoxColumn
            // 
            this.fpsDataGridViewTextBoxColumn.DataPropertyName = "FPS";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.fpsDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle3;
            this.fpsDataGridViewTextBoxColumn.HeaderText = "FPS";
            this.fpsDataGridViewTextBoxColumn.Name = "fpsDataGridViewTextBoxColumn";
            this.fpsDataGridViewTextBoxColumn.ReadOnly = true;
            this.fpsDataGridViewTextBoxColumn.Width = 50;
            // 
            // blankDataGridViewTextBoxColumn
            // 
            this.blankDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.blankDataGridViewTextBoxColumn.HeaderText = "";
            this.blankDataGridViewTextBoxColumn.Name = "blankDataGridViewTextBoxColumn";
            this.blankDataGridViewTextBoxColumn.ReadOnly = true;
            this.blankDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // testBindingSource
            // 
            this.testBindingSource.DataSource = typeof(ORTS.TestingForm.TestActivity);
            // 
            // buttonDetails
            // 
            this.buttonDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDetails.Location = new System.Drawing.Point(737, 441);
            this.buttonDetails.Name = "buttonDetails";
            this.buttonDetails.Size = new System.Drawing.Size(75, 23);
            this.buttonDetails.TabIndex = 7;
            this.buttonDetails.Text = "Details";
            this.buttonDetails.UseVisualStyleBackColor = true;
            this.buttonDetails.Click += new System.EventHandler(this.buttonDetails_Click);
            // 
            // checkBoxOverride
            // 
            this.checkBoxOverride.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxOverride.Location = new System.Drawing.Point(255, 441);
            this.checkBoxOverride.Name = "checkBoxOverride";
            this.checkBoxOverride.Size = new System.Drawing.Size(314, 23);
            this.checkBoxOverride.TabIndex = 4;
            this.checkBoxOverride.Text = "Override user settings when running tests";
            this.checkBoxOverride.UseVisualStyleBackColor = true;
            // 
            // buttonNoSort
            // 
            this.buttonNoSort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNoSort.Location = new System.Drawing.Point(575, 441);
            this.buttonNoSort.Name = "buttonNoSort";
            this.buttonNoSort.Size = new System.Drawing.Size(75, 23);
            this.buttonNoSort.TabIndex = 5;
            this.buttonNoSort.Text = "Clear sort";
            this.buttonNoSort.UseVisualStyleBackColor = true;
            this.buttonNoSort.Click += new System.EventHandler(this.buttonNoSort_Click);
            // 
            // panelTests
            // 
            this.panelTests.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelTests.Controls.Add(this.gridTestActivities);
            this.panelTests.Location = new System.Drawing.Point(12, 12);
            this.panelTests.Name = "panelTests";
            this.panelTests.Size = new System.Drawing.Size(800, 423);
            this.panelTests.TabIndex = 13;
            // 
            // TestingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 476);
            this.Controls.Add(this.panelTests);
            this.Controls.Add(this.buttonNoSort);
            this.Controls.Add(this.buttonDetails);
            this.Controls.Add(this.buttonSummary);
            this.Controls.Add(this.checkBoxOverride);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonTest);
            this.Controls.Add(this.buttonTestAll);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.Name = "TestingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Testing";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestingForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.gridTestActivities)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.testBindingSource)).EndInit();
            this.panelTests.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonTestAll;
        private System.Windows.Forms.Button buttonTest;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonSummary;
        private System.Windows.Forms.DataGridView gridTestActivities;
        private System.Windows.Forms.BindingSource testBindingSource;
        private System.Windows.Forms.Button buttonDetails;
        private System.Windows.Forms.CheckBox checkBoxOverride;
        private System.Windows.Forms.Button buttonNoSort;
        private System.Windows.Forms.DataGridViewCheckBoxColumn toTestDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn activityFilePathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn defaultSortDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn routeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn activityDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn testedDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn passedDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn errorsDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn loadDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fpsDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn blankDataGridViewTextBoxColumn;
        private System.Windows.Forms.Panel panelTests;
    }
}
