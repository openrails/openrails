namespace ORTS
{
    partial class ExploreForm
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
            this.listPaths = new System.Windows.Forms.ListBox();
            this.labelTime = new System.Windows.Forms.Label();
            this.numericHour = new System.Windows.Forms.NumericUpDown();
            this.listSeason = new System.Windows.Forms.ListBox();
            this.listWeather = new System.Windows.Forms.ListBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.groupBoxPaths = new System.Windows.Forms.GroupBox();
            this.groupBoxConsists = new System.Windows.Forms.GroupBox();
            this.ConsistsListView = new System.Windows.Forms.ListView();
            this.Engine = new System.Windows.Forms.ColumnHeader();
            this.ConsistName = new System.Windows.Forms.ColumnHeader();
            this.groupBoxEnvironment = new System.Windows.Forms.GroupBox();
            this.numericMinute = new System.Windows.Forms.NumericUpDown();
            this.labelSeason = new System.Windows.Forms.Label();
            this.labelWeather = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericHour)).BeginInit();
            this.groupBoxPaths.SuspendLayout();
            this.groupBoxConsists.SuspendLayout();
            this.groupBoxEnvironment.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericMinute)).BeginInit();
            this.SuspendLayout();
            // 
            // listPaths
            // 
            this.listPaths.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listPaths.FormattingEnabled = true;
            this.listPaths.IntegralHeight = false;
            this.listPaths.ItemHeight = 16;
            this.listPaths.Location = new System.Drawing.Point(8, 23);
            this.listPaths.Margin = new System.Windows.Forms.Padding(4);
            this.listPaths.Name = "listPaths";
            this.listPaths.Size = new System.Drawing.Size(383, 299);
            this.listPaths.TabIndex = 0;
            // 
            // labelTime
            // 
            this.labelTime.AutoSize = true;
            this.labelTime.Location = new System.Drawing.Point(8, 20);
            this.labelTime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(73, 17);
            this.labelTime.TabIndex = 0;
            this.labelTime.Text = "Start Time";
            // 
            // numericHour
            // 
            this.numericHour.Location = new System.Drawing.Point(8, 39);
            this.numericHour.Margin = new System.Windows.Forms.Padding(4);
            this.numericHour.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
            this.numericHour.Name = "numericHour";
            this.numericHour.Size = new System.Drawing.Size(57, 22);
            this.numericHour.TabIndex = 1;
            // 
            // listSeason
            // 
            this.listSeason.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.listSeason.FormattingEnabled = true;
            this.listSeason.IntegralHeight = false;
            this.listSeason.ItemHeight = 16;
            this.listSeason.Items.AddRange(new object[] {
            "Spring",
            "Summer",
            "Autumn",
            "Winter"});
            this.listSeason.Location = new System.Drawing.Point(139, 39);
            this.listSeason.Margin = new System.Windows.Forms.Padding(4);
            this.listSeason.Name = "listSeason";
            this.listSeason.Size = new System.Drawing.Size(121, 106);
            this.listSeason.TabIndex = 4;
            // 
            // listWeather
            // 
            this.listWeather.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.listWeather.FormattingEnabled = true;
            this.listWeather.IntegralHeight = false;
            this.listWeather.ItemHeight = 16;
            this.listWeather.Items.AddRange(new object[] {
            "Clear",
            "Snow",
            "Rain"});
            this.listWeather.Location = new System.Drawing.Point(269, 39);
            this.listWeather.Margin = new System.Windows.Forms.Padding(4);
            this.listWeather.Name = "listWeather";
            this.listWeather.Size = new System.Drawing.Size(121, 106);
            this.listWeather.TabIndex = 6;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(724, 514);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(4);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 28);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Enabled = false;
            this.buttonOk.Location = new System.Drawing.Point(616, 514);
            this.buttonOk.Margin = new System.Windows.Forms.Padding(4);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(100, 28);
            this.buttonOk.TabIndex = 3;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // groupBoxPaths
            // 
            this.groupBoxPaths.Controls.Add(this.listPaths);
            this.groupBoxPaths.Location = new System.Drawing.Point(16, 15);
            this.groupBoxPaths.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPaths.Name = "groupBoxPaths";
            this.groupBoxPaths.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPaths.Size = new System.Drawing.Size(400, 331);
            this.groupBoxPaths.TabIndex = 0;
            this.groupBoxPaths.TabStop = false;
            this.groupBoxPaths.Text = "Path";
            // 
            // groupBoxConsists
            // 
            this.groupBoxConsists.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxConsists.Controls.Add(this.ConsistsListView);
            this.groupBoxConsists.Location = new System.Drawing.Point(424, 15);
            this.groupBoxConsists.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxConsists.Name = "groupBoxConsists";
            this.groupBoxConsists.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxConsists.Size = new System.Drawing.Size(400, 492);
            this.groupBoxConsists.TabIndex = 1;
            this.groupBoxConsists.TabStop = false;
            this.groupBoxConsists.Text = "Consist";
            // 
            // ConsistsListView
            // 
            this.ConsistsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Engine,
            this.ConsistName});
            this.ConsistsListView.FullRowSelect = true;
            this.ConsistsListView.GridLines = true;
            this.ConsistsListView.HideSelection = false;
            this.ConsistsListView.Location = new System.Drawing.Point(8, 23);
            this.ConsistsListView.MultiSelect = false;
            this.ConsistsListView.Name = "ConsistsListView";
            this.ConsistsListView.Size = new System.Drawing.Size(383, 460);
            this.ConsistsListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.ConsistsListView.TabIndex = 1;
            this.ConsistsListView.UseCompatibleStateImageBehavior = false;
            this.ConsistsListView.View = System.Windows.Forms.View.Details;
            this.ConsistsListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ConsistsViewColumnClick);
            // 
            // Engine
            // 
            this.Engine.Text = "Engine Name";
            this.Engine.Width = 120;
            // 
            // ConsistName
            // 
            this.ConsistName.Text = "Consist Name";
            this.ConsistName.Width = 260;
            // 
            // groupBoxEnvironment
            // 
            this.groupBoxEnvironment.Controls.Add(this.numericHour);
            this.groupBoxEnvironment.Controls.Add(this.numericMinute);
            this.groupBoxEnvironment.Controls.Add(this.labelTime);
            this.groupBoxEnvironment.Controls.Add(this.listSeason);
            this.groupBoxEnvironment.Controls.Add(this.labelSeason);
            this.groupBoxEnvironment.Controls.Add(this.listWeather);
            this.groupBoxEnvironment.Controls.Add(this.labelWeather);
            this.groupBoxEnvironment.Location = new System.Drawing.Point(16, 353);
            this.groupBoxEnvironment.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxEnvironment.Name = "groupBoxEnvironment";
            this.groupBoxEnvironment.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxEnvironment.Size = new System.Drawing.Size(400, 154);
            this.groupBoxEnvironment.TabIndex = 2;
            this.groupBoxEnvironment.TabStop = false;
            this.groupBoxEnvironment.Text = "Environment";
            // 
            // numericMinute
            // 
            this.numericMinute.Location = new System.Drawing.Point(73, 39);
            this.numericMinute.Margin = new System.Windows.Forms.Padding(4);
            this.numericMinute.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.numericMinute.Name = "numericMinute";
            this.numericMinute.Size = new System.Drawing.Size(57, 22);
            this.numericMinute.TabIndex = 2;
            // 
            // labelSeason
            // 
            this.labelSeason.AutoSize = true;
            this.labelSeason.Location = new System.Drawing.Point(135, 20);
            this.labelSeason.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSeason.Name = "labelSeason";
            this.labelSeason.Size = new System.Drawing.Size(56, 17);
            this.labelSeason.TabIndex = 7;
            this.labelSeason.Text = "Season";
            // 
            // labelWeather
            // 
            this.labelWeather.AutoSize = true;
            this.labelWeather.Location = new System.Drawing.Point(265, 20);
            this.labelWeather.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWeather.Name = "labelWeather";
            this.labelWeather.Size = new System.Drawing.Size(62, 17);
            this.labelWeather.TabIndex = 8;
            this.labelWeather.Text = "Weather";
            // 
            // ExploreForm
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(840, 558);
            this.Controls.Add(this.groupBoxPaths);
            this.Controls.Add(this.groupBoxConsists);
            this.Controls.Add(this.groupBoxEnvironment);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Location = new System.Drawing.Point(200, 200);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExploreForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Explore Route Details";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExploreForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.numericHour)).EndInit();
            this.groupBoxPaths.ResumeLayout(false);
            this.groupBoxConsists.ResumeLayout(false);
            this.groupBoxEnvironment.ResumeLayout(false);
            this.groupBoxEnvironment.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericMinute)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listPaths;
        private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.NumericUpDown numericHour;
		private System.Windows.Forms.ListBox listSeason;
        private System.Windows.Forms.ListBox listWeather;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.GroupBox groupBoxPaths;
        private System.Windows.Forms.GroupBox groupBoxConsists;
        private System.Windows.Forms.GroupBox groupBoxEnvironment;
		private System.Windows.Forms.NumericUpDown numericMinute;
		private System.Windows.Forms.Label labelWeather;
		private System.Windows.Forms.Label labelSeason;
        private System.Windows.Forms.ListView ConsistsListView;
        private System.Windows.Forms.ColumnHeader Engine;
        private System.Windows.Forms.ColumnHeader ConsistName;
    }
}
