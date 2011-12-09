namespace ORTS
{
    partial class DetailsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DetailsForm));
			this.textDescription = new System.Windows.Forms.TextBox();
			this.textBriefing = new System.Windows.Forms.TextBox();
			this.groupEnvironment = new System.Windows.Forms.GroupBox();
			this.textWeather = new System.Windows.Forms.TextBox();
			this.labelWeather = new System.Windows.Forms.Label();
			this.textSeason = new System.Windows.Forms.TextBox();
			this.labelSeason = new System.Windows.Forms.Label();
			this.textStartTime = new System.Windows.Forms.TextBox();
			this.labelStartTime = new System.Windows.Forms.Label();
			this.buttonClose = new System.Windows.Forms.Button();
			this.groupBoxDescription = new System.Windows.Forms.GroupBox();
			this.groupBoxBriefing = new System.Windows.Forms.GroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBoxActivity = new System.Windows.Forms.GroupBox();
			this.labelDifficulty = new System.Windows.Forms.Label();
			this.textDifficulty = new System.Windows.Forms.TextBox();
			this.labelDuration = new System.Windows.Forms.Label();
			this.textDuration = new System.Windows.Forms.TextBox();
			this.groupEnvironment.SuspendLayout();
			this.groupBoxDescription.SuspendLayout();
			this.groupBoxBriefing.SuspendLayout();
			this.groupBoxActivity.SuspendLayout();
			this.SuspendLayout();
			// 
			// textDescription
			// 
			this.textDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textDescription.Location = new System.Drawing.Point(6, 19);
			this.textDescription.Multiline = true;
			this.textDescription.Name = "textDescription";
			this.textDescription.ReadOnly = true;
			this.textDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textDescription.Size = new System.Drawing.Size(588, 75);
			this.textDescription.TabIndex = 0;
			this.textDescription.TabStop = false;
			// 
			// textBriefing
			// 
			this.textBriefing.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBriefing.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBriefing.Location = new System.Drawing.Point(6, 19);
			this.textBriefing.Multiline = true;
			this.textBriefing.Name = "textBriefing";
			this.textBriefing.ReadOnly = true;
			this.textBriefing.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBriefing.Size = new System.Drawing.Size(588, 175);
			this.textBriefing.TabIndex = 0;
			this.textBriefing.TabStop = false;
			// 
			// groupEnvironment
			// 
			this.groupEnvironment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupEnvironment.Controls.Add(this.textStartTime);
			this.groupEnvironment.Controls.Add(this.labelStartTime);
			this.groupEnvironment.Controls.Add(this.textSeason);
			this.groupEnvironment.Controls.Add(this.labelSeason);
			this.groupEnvironment.Controls.Add(this.textWeather);
			this.groupEnvironment.Controls.Add(this.labelWeather);
			this.groupEnvironment.Location = new System.Drawing.Point(12, 324);
			this.groupEnvironment.Name = "groupEnvironment";
			this.groupEnvironment.Size = new System.Drawing.Size(300, 58);
			this.groupEnvironment.TabIndex = 2;
			this.groupEnvironment.TabStop = false;
			this.groupEnvironment.Text = "Environment";
			// 
			// textWeather
			// 
			this.textWeather.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textWeather.Location = new System.Drawing.Point(202, 32);
			this.textWeather.Name = "textWeather";
			this.textWeather.ReadOnly = true;
			this.textWeather.Size = new System.Drawing.Size(92, 13);
			this.textWeather.TabIndex = 3;
			this.textWeather.TabStop = false;
			// 
			// labelWeather
			// 
			this.labelWeather.AutoSize = true;
			this.labelWeather.Location = new System.Drawing.Point(199, 16);
			this.labelWeather.Name = "labelWeather";
			this.labelWeather.Size = new System.Drawing.Size(48, 13);
			this.labelWeather.TabIndex = 2;
			this.labelWeather.Text = "Weather";
			// 
			// textSeason
			// 
			this.textSeason.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textSeason.Location = new System.Drawing.Point(104, 32);
			this.textSeason.Name = "textSeason";
			this.textSeason.ReadOnly = true;
			this.textSeason.Size = new System.Drawing.Size(92, 13);
			this.textSeason.TabIndex = 5;
			this.textSeason.TabStop = false;
			// 
			// labelSeason
			// 
			this.labelSeason.AutoSize = true;
			this.labelSeason.Location = new System.Drawing.Point(101, 16);
			this.labelSeason.Name = "labelSeason";
			this.labelSeason.Size = new System.Drawing.Size(43, 13);
			this.labelSeason.TabIndex = 4;
			this.labelSeason.Text = "Season";
			// 
			// textStartTime
			// 
			this.textStartTime.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textStartTime.Location = new System.Drawing.Point(6, 32);
			this.textStartTime.Name = "textStartTime";
			this.textStartTime.ReadOnly = true;
			this.textStartTime.Size = new System.Drawing.Size(92, 13);
			this.textStartTime.TabIndex = 7;
			this.textStartTime.TabStop = false;
			// 
			// labelStartTime
			// 
			this.labelStartTime.AutoSize = true;
			this.labelStartTime.Location = new System.Drawing.Point(3, 16);
			this.labelStartTime.Name = "labelStartTime";
			this.labelStartTime.Size = new System.Drawing.Size(55, 13);
			this.labelStartTime.TabIndex = 6;
			this.labelStartTime.Text = "Start Time";
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Location = new System.Drawing.Point(537, 388);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 23);
			this.buttonClose.TabIndex = 3;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// groupBoxDescription
			// 
			this.groupBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxDescription.Controls.Add(this.textDescription);
			this.groupBoxDescription.Location = new System.Drawing.Point(12, 12);
			this.groupBoxDescription.Name = "groupBoxDescription";
			this.groupBoxDescription.Size = new System.Drawing.Size(600, 100);
			this.groupBoxDescription.TabIndex = 0;
			this.groupBoxDescription.TabStop = false;
			// 
			// groupBoxBriefing
			// 
			this.groupBoxBriefing.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxBriefing.Controls.Add(this.groupBox1);
			this.groupBoxBriefing.Controls.Add(this.textBriefing);
			this.groupBoxBriefing.Location = new System.Drawing.Point(12, 118);
			this.groupBoxBriefing.Name = "groupBoxBriefing";
			this.groupBoxBriefing.Size = new System.Drawing.Size(600, 200);
			this.groupBoxBriefing.TabIndex = 1;
			this.groupBoxBriefing.TabStop = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Location = new System.Drawing.Point(307, 206);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(293, 58);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "groupBox1";
			// 
			// groupBoxActivity
			// 
			this.groupBoxActivity.Controls.Add(this.textDuration);
			this.groupBoxActivity.Controls.Add(this.labelDuration);
			this.groupBoxActivity.Controls.Add(this.textDifficulty);
			this.groupBoxActivity.Controls.Add(this.labelDifficulty);
			this.groupBoxActivity.Location = new System.Drawing.Point(318, 324);
			this.groupBoxActivity.Name = "groupBoxActivity";
			this.groupBoxActivity.Size = new System.Drawing.Size(294, 58);
			this.groupBoxActivity.TabIndex = 4;
			this.groupBoxActivity.TabStop = false;
			this.groupBoxActivity.Text = "Activity";
			// 
			// labelDifficulty
			// 
			this.labelDifficulty.AutoSize = true;
			this.labelDifficulty.Location = new System.Drawing.Point(104, 16);
			this.labelDifficulty.Name = "labelDifficulty";
			this.labelDifficulty.Size = new System.Drawing.Size(47, 13);
			this.labelDifficulty.TabIndex = 10;
			this.labelDifficulty.Text = "Difficulty";
			// 
			// textDifficulty
			// 
			this.textDifficulty.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textDifficulty.Location = new System.Drawing.Point(107, 32);
			this.textDifficulty.Name = "textDifficulty";
			this.textDifficulty.ReadOnly = true;
			this.textDifficulty.Size = new System.Drawing.Size(92, 13);
			this.textDifficulty.TabIndex = 11;
			this.textDifficulty.TabStop = false;
			// 
			// labelDuration
			// 
			this.labelDuration.AutoSize = true;
			this.labelDuration.Location = new System.Drawing.Point(6, 16);
			this.labelDuration.Name = "labelDuration";
			this.labelDuration.Size = new System.Drawing.Size(47, 13);
			this.labelDuration.TabIndex = 12;
			this.labelDuration.Text = "Duration";
			// 
			// textDuration
			// 
			this.textDuration.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textDuration.Location = new System.Drawing.Point(9, 32);
			this.textDuration.Name = "textDuration";
			this.textDuration.ReadOnly = true;
			this.textDuration.Size = new System.Drawing.Size(92, 13);
			this.textDuration.TabIndex = 13;
			this.textDuration.TabStop = false;
			// 
			// DetailsForm
			// 
			this.AcceptButton = this.buttonClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(624, 423);
			this.Controls.Add(this.groupBoxDescription);
			this.Controls.Add(this.groupBoxBriefing);
			this.Controls.Add(this.groupEnvironment);
			this.Controls.Add(this.groupBoxActivity);
			this.Controls.Add(this.buttonClose);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DetailsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.groupEnvironment.ResumeLayout(false);
			this.groupEnvironment.PerformLayout();
			this.groupBoxDescription.ResumeLayout(false);
			this.groupBoxDescription.PerformLayout();
			this.groupBoxBriefing.ResumeLayout(false);
			this.groupBoxBriefing.PerformLayout();
			this.groupBoxActivity.ResumeLayout(false);
			this.groupBoxActivity.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

		private System.Windows.Forms.TextBox textDescription;
        private System.Windows.Forms.TextBox textBriefing;
        private System.Windows.Forms.GroupBox groupEnvironment;
		private System.Windows.Forms.Label labelStartTime;
        private System.Windows.Forms.TextBox textStartTime;
        private System.Windows.Forms.Label labelSeason;
        private System.Windows.Forms.Label labelWeather;
		private System.Windows.Forms.TextBox textSeason;
		private System.Windows.Forms.TextBox textWeather;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.GroupBox groupBoxDescription;
		private System.Windows.Forms.GroupBox groupBoxBriefing;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBoxActivity;
		private System.Windows.Forms.Label labelDifficulty;
		private System.Windows.Forms.TextBox textDifficulty;
		private System.Windows.Forms.Label labelDuration;
		private System.Windows.Forms.TextBox textDuration;
    }
}