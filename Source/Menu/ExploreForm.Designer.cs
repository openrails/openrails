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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExploreForm));
			this.listPaths = new System.Windows.Forms.ListBox();
			this.listConsists = new System.Windows.Forms.ListBox();
			this.labelTime = new System.Windows.Forms.Label();
			this.numericHour = new System.Windows.Forms.NumericUpDown();
			this.listSeason = new System.Windows.Forms.ListBox();
			this.listWeather = new System.Windows.Forms.ListBox();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOk = new System.Windows.Forms.Button();
			this.groupBoxPaths = new System.Windows.Forms.GroupBox();
			this.groupBoxConsists = new System.Windows.Forms.GroupBox();
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
			this.listPaths.Location = new System.Drawing.Point(6, 19);
			this.listPaths.Name = "listPaths";
			this.listPaths.Size = new System.Drawing.Size(288, 244);
			this.listPaths.TabIndex = 0;
			// 
			// listConsists
			// 
			this.listConsists.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.listConsists.IntegralHeight = false;
			this.listConsists.Location = new System.Drawing.Point(6, 19);
			this.listConsists.Name = "listConsists";
			this.listConsists.Size = new System.Drawing.Size(288, 375);
			this.listConsists.TabIndex = 0;
			// 
			// labelTime
			// 
			this.labelTime.AutoSize = true;
			this.labelTime.Location = new System.Drawing.Point(6, 16);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(55, 13);
			this.labelTime.TabIndex = 0;
			this.labelTime.Text = "Start Time";
			// 
			// numericHour
			// 
			this.numericHour.Location = new System.Drawing.Point(6, 32);
			this.numericHour.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
			this.numericHour.Name = "numericHour";
			this.numericHour.Size = new System.Drawing.Size(43, 20);
			this.numericHour.TabIndex = 1;
			// 
			// listSeason
			// 
			this.listSeason.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.listSeason.FormattingEnabled = true;
			this.listSeason.IntegralHeight = false;
			this.listSeason.Items.AddRange(new object[] {
            "Spring",
            "Summer",
            "Autumn",
            "Winter"});
			this.listSeason.Location = new System.Drawing.Point(104, 32);
			this.listSeason.Name = "listSeason";
			this.listSeason.Size = new System.Drawing.Size(92, 87);
			this.listSeason.TabIndex = 4;
			// 
			// listWeather
			// 
			this.listWeather.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.listWeather.FormattingEnabled = true;
			this.listWeather.IntegralHeight = false;
			this.listWeather.Items.AddRange(new object[] {
            "Clear",
            "Snow",
            "Rain"});
			this.listWeather.Location = new System.Drawing.Point(202, 32);
			this.listWeather.Name = "listWeather";
			this.listWeather.Size = new System.Drawing.Size(92, 87);
			this.listWeather.TabIndex = 6;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(543, 418);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOk
			// 
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(462, 418);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 3;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			// 
			// groupBoxPaths
			// 
			this.groupBoxPaths.Controls.Add(this.listPaths);
			this.groupBoxPaths.Location = new System.Drawing.Point(12, 12);
			this.groupBoxPaths.Name = "groupBoxPaths";
			this.groupBoxPaths.Size = new System.Drawing.Size(300, 269);
			this.groupBoxPaths.TabIndex = 0;
			this.groupBoxPaths.TabStop = false;
			this.groupBoxPaths.Text = "Path";
			// 
			// groupBoxConsists
			// 
			this.groupBoxConsists.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxConsists.Controls.Add(this.listConsists);
			this.groupBoxConsists.Location = new System.Drawing.Point(318, 12);
			this.groupBoxConsists.Name = "groupBoxConsists";
			this.groupBoxConsists.Size = new System.Drawing.Size(300, 400);
			this.groupBoxConsists.TabIndex = 1;
			this.groupBoxConsists.TabStop = false;
			this.groupBoxConsists.Text = "Consist";
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
			this.groupBoxEnvironment.Location = new System.Drawing.Point(12, 287);
			this.groupBoxEnvironment.Name = "groupBoxEnvironment";
			this.groupBoxEnvironment.Size = new System.Drawing.Size(300, 125);
			this.groupBoxEnvironment.TabIndex = 2;
			this.groupBoxEnvironment.TabStop = false;
			this.groupBoxEnvironment.Text = "Environment";
			// 
			// numericMinute
			// 
			this.numericMinute.Location = new System.Drawing.Point(55, 32);
			this.numericMinute.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
			this.numericMinute.Name = "numericMinute";
			this.numericMinute.Size = new System.Drawing.Size(43, 20);
			this.numericMinute.TabIndex = 2;
			// 
			// labelSeason
			// 
			this.labelSeason.AutoSize = true;
			this.labelSeason.Location = new System.Drawing.Point(101, 16);
			this.labelSeason.Name = "labelSeason";
			this.labelSeason.Size = new System.Drawing.Size(43, 13);
			this.labelSeason.TabIndex = 7;
			this.labelSeason.Text = "Season";
			// 
			// labelWeather
			// 
			this.labelWeather.AutoSize = true;
			this.labelWeather.Location = new System.Drawing.Point(199, 16);
			this.labelWeather.Name = "labelWeather";
			this.labelWeather.Size = new System.Drawing.Size(48, 13);
			this.labelWeather.TabIndex = 8;
			this.labelWeather.Text = "Weather";
			// 
			// ExploreForm
			// 
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(630, 453);
			this.Controls.Add(this.groupBoxPaths);
			this.Controls.Add(this.groupBoxConsists);
			this.Controls.Add(this.groupBoxEnvironment);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Location = new System.Drawing.Point(200, 200);
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
        private System.Windows.Forms.ListBox listConsists;
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
    }
}