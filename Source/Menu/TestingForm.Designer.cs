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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.bClose = new System.Windows.Forms.Button();
            this.bTestLoadingOfAllActivities = new System.Windows.Forms.Button();
            this.bTestLoadingOfSelectedActivities = new System.Windows.Forms.Button();
            this.bCancelTest = new System.Windows.Forms.Button();
            this.lTestLoading = new System.Windows.Forms.Label();
            this.bViewSummary = new System.Windows.Forms.Button();
            this.dgvTestLoadActivities = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Tested = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Passed = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.activityDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.routePathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.activityFileNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bsTestLoadActivities = new System.Windows.Forms.BindingSource( this.components );
            this.bViewDetails = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTestLoadActivities)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsTestLoadActivities)).BeginInit();
            this.SuspendLayout();
            // 
            // bClose
            // 
            this.bClose.Location = new System.Drawing.Point( 642, 280 );
            this.bClose.Name = "bClose";
            this.bClose.Size = new System.Drawing.Size( 101, 25 );
            this.bClose.TabIndex = 8;
            this.bClose.Text = "Close";
            this.bClose.UseVisualStyleBackColor = true;
            this.bClose.Click += new System.EventHandler( this.bClose_Click );
            // 
            // bTestLoadingOfAllActivities
            // 
            this.bTestLoadingOfAllActivities.Location = new System.Drawing.Point( 642, 39 );
            this.bTestLoadingOfAllActivities.Name = "bTestLoadingOfAllActivities";
            this.bTestLoadingOfAllActivities.Size = new System.Drawing.Size( 101, 25 );
            this.bTestLoadingOfAllActivities.TabIndex = 9;
            this.bTestLoadingOfAllActivities.Text = "All activities";
            this.bTestLoadingOfAllActivities.UseVisualStyleBackColor = true;
            this.bTestLoadingOfAllActivities.Click += new System.EventHandler( this.bTestLoadingOfAllActivities_Click );
            // 
            // bTestLoadingOfSelectedActivities
            // 
            this.bTestLoadingOfSelectedActivities.Location = new System.Drawing.Point( 642, 70 );
            this.bTestLoadingOfSelectedActivities.Name = "bTestLoadingOfSelectedActivities";
            this.bTestLoadingOfSelectedActivities.Size = new System.Drawing.Size( 101, 25 );
            this.bTestLoadingOfSelectedActivities.TabIndex = 10;
            this.bTestLoadingOfSelectedActivities.Text = "Selected activities";
            this.bTestLoadingOfSelectedActivities.UseVisualStyleBackColor = true;
            this.bTestLoadingOfSelectedActivities.Click += new System.EventHandler( this.bTestLoadingOfSelectedActivities_Click );
            // 
            // bCancelTest
            // 
            this.bCancelTest.Location = new System.Drawing.Point( 642, 101 );
            this.bCancelTest.Name = "bCancelTest";
            this.bCancelTest.Size = new System.Drawing.Size( 101, 25 );
            this.bCancelTest.TabIndex = 11;
            this.bCancelTest.Text = "Cancel test";
            this.bCancelTest.UseVisualStyleBackColor = true;
            this.bCancelTest.Click += new System.EventHandler( this.bCancelTest_Click );
            // 
            // lTestLoading
            // 
            this.lTestLoading.Location = new System.Drawing.Point( 639, 4 );
            this.lTestLoading.Name = "lTestLoading";
            this.lTestLoading.Size = new System.Drawing.Size( 104, 32 );
            this.lTestLoading.TabIndex = 12;
            this.lTestLoading.Text = "Test loading of activities";
            this.lTestLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // bViewSummary
            // 
            this.bViewSummary.Location = new System.Drawing.Point( 642, 132 );
            this.bViewSummary.Name = "bViewSummary";
            this.bViewSummary.Size = new System.Drawing.Size( 101, 25 );
            this.bViewSummary.TabIndex = 13;
            this.bViewSummary.Text = "View summary";
            this.bViewSummary.UseVisualStyleBackColor = true;
            this.bViewSummary.Click += new System.EventHandler( this.bViewSummary_Click );
            // 
            // dgvTestLoadActivities
            // 
            this.dgvTestLoadActivities.AutoGenerateColumns = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvTestLoadActivities.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvTestLoadActivities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTestLoadActivities.Columns.AddRange( new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Tested,
            this.Passed,
            this.activityDataGridViewTextBoxColumn,
            this.routePathDataGridViewTextBoxColumn,
            this.activityFileNameDataGridViewTextBoxColumn} );
            this.dgvTestLoadActivities.DataSource = this.bsTestLoadActivities;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvTestLoadActivities.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvTestLoadActivities.Location = new System.Drawing.Point( 13, 12 );
            this.dgvTestLoadActivities.Name = "dgvTestLoadActivities";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvTestLoadActivities.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvTestLoadActivities.RowHeadersVisible = false;
            this.dgvTestLoadActivities.Size = new System.Drawing.Size( 623, 293 );
            this.dgvTestLoadActivities.TabIndex = 14;
            // 
            // Column1
            // 
            this.Column1.DataPropertyName = "ToTest";
            this.Column1.HeaderText = "To Test";
            this.Column1.Name = "Column1";
            this.Column1.Visible = false;
            // 
            // Tested
            // 
            this.Tested.DataPropertyName = "Tested";
            this.Tested.HeaderText = "Tested";
            this.Tested.Name = "Tested";
            this.Tested.Width = 50;
            // 
            // Passed
            // 
            this.Passed.DataPropertyName = "Passed";
            this.Passed.HeaderText = "Passed";
            this.Passed.Name = "Passed";
            this.Passed.Width = 50;
            // 
            // activityDataGridViewTextBoxColumn
            // 
            this.activityDataGridViewTextBoxColumn.DataPropertyName = "Activity";
            this.activityDataGridViewTextBoxColumn.HeaderText = "Activity";
            this.activityDataGridViewTextBoxColumn.Name = "activityDataGridViewTextBoxColumn";
            this.activityDataGridViewTextBoxColumn.Width = 200;
            // 
            // routePathDataGridViewTextBoxColumn
            // 
            this.routePathDataGridViewTextBoxColumn.DataPropertyName = "RoutePath";
            this.routePathDataGridViewTextBoxColumn.HeaderText = "RoutePath";
            this.routePathDataGridViewTextBoxColumn.Name = "routePathDataGridViewTextBoxColumn";
            this.routePathDataGridViewTextBoxColumn.Width = 200;
            // 
            // activityFileNameDataGridViewTextBoxColumn
            // 
            this.activityFileNameDataGridViewTextBoxColumn.DataPropertyName = "ActivityFileName";
            this.activityFileNameDataGridViewTextBoxColumn.HeaderText = "ActivityFileName";
            this.activityFileNameDataGridViewTextBoxColumn.Name = "activityFileNameDataGridViewTextBoxColumn";
            this.activityFileNameDataGridViewTextBoxColumn.Width = 200;
            // 
            // bsTestLoadActivities
            // 
            this.bsTestLoadActivities.DataSource = typeof( ORTS.TestLoadActivity );
            this.bsTestLoadActivities.CurrentChanged += new System.EventHandler( this.bsTestLoadActivities_CurrentChanged );
            // 
            // bViewDetails
            // 
            this.bViewDetails.Location = new System.Drawing.Point( 642, 163 );
            this.bViewDetails.Name = "bViewDetails";
            this.bViewDetails.Size = new System.Drawing.Size( 101, 25 );
            this.bViewDetails.TabIndex = 15;
            this.bViewDetails.Text = "View details";
            this.bViewDetails.UseVisualStyleBackColor = true;
            this.bViewDetails.Click += new System.EventHandler( this.bViewDetails_Click );
            // 
            // TestingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 755, 317 );
            this.Controls.Add( this.bViewDetails );
            this.Controls.Add( this.dgvTestLoadActivities );
            this.Controls.Add( this.bViewSummary );
            this.Controls.Add( this.lTestLoading );
            this.Controls.Add( this.bCancelTest );
            this.Controls.Add( this.bTestLoadingOfSelectedActivities );
            this.Controls.Add( this.bTestLoadingOfAllActivities );
            this.Controls.Add( this.bClose );
            this.Name = "TestingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TestingForm";
            ((System.ComponentModel.ISupportInitialize)(this.dgvTestLoadActivities)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsTestLoadActivities)).EndInit();
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.Button bClose;
        private System.Windows.Forms.Button bTestLoadingOfAllActivities;
        private System.Windows.Forms.Button bTestLoadingOfSelectedActivities;
        private System.Windows.Forms.Button bCancelTest;
        private System.Windows.Forms.Label lTestLoading;
        private System.Windows.Forms.Button bViewSummary;
        private System.Windows.Forms.DataGridView dgvTestLoadActivities;
        private System.Windows.Forms.BindingSource bsTestLoadActivities;
        private System.Windows.Forms.Button bViewDetails;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Tested;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Passed;
        private System.Windows.Forms.DataGridViewTextBoxColumn activityDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn routePathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn activityFileNameDataGridViewTextBoxColumn;
    }
}