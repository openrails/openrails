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
			this.txtDescription = new System.Windows.Forms.TextBox();
			this.txtBriefing = new System.Windows.Forms.TextBox();
			this.grpEnvironment = new System.Windows.Forms.GroupBox();
			this.txtDifficulty = new System.Windows.Forms.TextBox();
			this.lblDifficulty = new System.Windows.Forms.Label();
			this.txtWeather = new System.Windows.Forms.TextBox();
			this.lblWeather = new System.Windows.Forms.Label();
			this.txtSeason = new System.Windows.Forms.TextBox();
			this.lblSeason = new System.Windows.Forms.Label();
			this.txtDuration = new System.Windows.Forms.TextBox();
			this.lblDuration = new System.Windows.Forms.Label();
			this.txtStartTime = new System.Windows.Forms.TextBox();
			this.lblStartTime = new System.Windows.Forms.Label();
			this.buttonClose = new System.Windows.Forms.Button();
			this.grpDescription = new System.Windows.Forms.GroupBox();
			this.grpBriefing = new System.Windows.Forms.GroupBox();
			this.grpEnvironment.SuspendLayout();
			this.grpDescription.SuspendLayout();
			this.grpBriefing.SuspendLayout();
			this.SuspendLayout();
			// 
			// txtDescription
			// 
			this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtDescription.Location = new System.Drawing.Point(6, 19);
			this.txtDescription.Multiline = true;
			this.txtDescription.Name = "txtDescription";
			this.txtDescription.ReadOnly = true;
			this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtDescription.Size = new System.Drawing.Size(424, 75);
			this.txtDescription.TabIndex = 0;
			this.txtDescription.TabStop = false;
			// 
			// txtBriefing
			// 
			this.txtBriefing.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtBriefing.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtBriefing.Location = new System.Drawing.Point(6, 19);
			this.txtBriefing.Multiline = true;
			this.txtBriefing.Name = "txtBriefing";
			this.txtBriefing.ReadOnly = true;
			this.txtBriefing.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtBriefing.Size = new System.Drawing.Size(424, 175);
			this.txtBriefing.TabIndex = 0;
			this.txtBriefing.TabStop = false;
			// 
			// grpEnvironment
			// 
			this.grpEnvironment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grpEnvironment.Controls.Add(this.txtDifficulty);
			this.grpEnvironment.Controls.Add(this.lblDifficulty);
			this.grpEnvironment.Controls.Add(this.txtWeather);
			this.grpEnvironment.Controls.Add(this.lblWeather);
			this.grpEnvironment.Controls.Add(this.txtSeason);
			this.grpEnvironment.Controls.Add(this.lblSeason);
			this.grpEnvironment.Controls.Add(this.txtDuration);
			this.grpEnvironment.Controls.Add(this.lblDuration);
			this.grpEnvironment.Controls.Add(this.txtStartTime);
			this.grpEnvironment.Controls.Add(this.lblStartTime);
			this.grpEnvironment.Location = new System.Drawing.Point(12, 324);
			this.grpEnvironment.Name = "grpEnvironment";
			this.grpEnvironment.Size = new System.Drawing.Size(436, 104);
			this.grpEnvironment.TabIndex = 4;
			this.grpEnvironment.TabStop = false;
			// 
			// txtDifficulty
			// 
			this.txtDifficulty.Location = new System.Drawing.Point(112, 19);
			this.txtDifficulty.Name = "txtDifficulty";
			this.txtDifficulty.ReadOnly = true;
			this.txtDifficulty.Size = new System.Drawing.Size(100, 20);
			this.txtDifficulty.TabIndex = 0;
			this.txtDifficulty.TabStop = false;
			// 
			// lblDifficulty
			// 
			this.lblDifficulty.Location = new System.Drawing.Point(6, 22);
			this.lblDifficulty.Name = "lblDifficulty";
			this.lblDifficulty.Size = new System.Drawing.Size(100, 20);
			this.lblDifficulty.TabIndex = 1;
			this.lblDifficulty.Text = "Difficulty";
			// 
			// txtWeather
			// 
			this.txtWeather.Location = new System.Drawing.Point(112, 45);
			this.txtWeather.Name = "txtWeather";
			this.txtWeather.ReadOnly = true;
			this.txtWeather.Size = new System.Drawing.Size(100, 20);
			this.txtWeather.TabIndex = 2;
			this.txtWeather.TabStop = false;
			// 
			// lblWeather
			// 
			this.lblWeather.Location = new System.Drawing.Point(6, 48);
			this.lblWeather.Name = "lblWeather";
			this.lblWeather.Size = new System.Drawing.Size(100, 20);
			this.lblWeather.TabIndex = 3;
			this.lblWeather.Text = "Weather";
			// 
			// txtSeason
			// 
			this.txtSeason.Location = new System.Drawing.Point(112, 71);
			this.txtSeason.Name = "txtSeason";
			this.txtSeason.ReadOnly = true;
			this.txtSeason.Size = new System.Drawing.Size(100, 20);
			this.txtSeason.TabIndex = 4;
			this.txtSeason.TabStop = false;
			// 
			// lblSeason
			// 
			this.lblSeason.Location = new System.Drawing.Point(6, 74);
			this.lblSeason.Name = "lblSeason";
			this.lblSeason.Size = new System.Drawing.Size(100, 20);
			this.lblSeason.TabIndex = 5;
			this.lblSeason.Text = "Season";
			// 
			// txtDuration
			// 
			this.txtDuration.Location = new System.Drawing.Point(324, 45);
			this.txtDuration.Name = "txtDuration";
			this.txtDuration.ReadOnly = true;
			this.txtDuration.Size = new System.Drawing.Size(100, 20);
			this.txtDuration.TabIndex = 6;
			this.txtDuration.TabStop = false;
			// 
			// lblDuration
			// 
			this.lblDuration.Location = new System.Drawing.Point(218, 48);
			this.lblDuration.Name = "lblDuration";
			this.lblDuration.Size = new System.Drawing.Size(100, 20);
			this.lblDuration.TabIndex = 7;
			this.lblDuration.Text = "Duration";
			// 
			// txtStartTime
			// 
			this.txtStartTime.Location = new System.Drawing.Point(324, 19);
			this.txtStartTime.Name = "txtStartTime";
			this.txtStartTime.ReadOnly = true;
			this.txtStartTime.Size = new System.Drawing.Size(100, 20);
			this.txtStartTime.TabIndex = 8;
			this.txtStartTime.TabStop = false;
			// 
			// lblStartTime
			// 
			this.lblStartTime.Location = new System.Drawing.Point(218, 22);
			this.lblStartTime.Name = "lblStartTime";
			this.lblStartTime.Size = new System.Drawing.Size(100, 20);
			this.lblStartTime.TabIndex = 9;
			this.lblStartTime.Text = "Start Time";
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Location = new System.Drawing.Point(373, 434);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 23);
			this.buttonClose.TabIndex = 3;
			this.buttonClose.Text = "Close";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// grpDescription
			// 
			this.grpDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grpDescription.Controls.Add(this.txtDescription);
			this.grpDescription.Location = new System.Drawing.Point(12, 12);
			this.grpDescription.Name = "grpDescription";
			this.grpDescription.Size = new System.Drawing.Size(436, 100);
			this.grpDescription.TabIndex = 2;
			this.grpDescription.TabStop = false;
			// 
			// grpBriefing
			// 
			this.grpBriefing.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grpBriefing.Controls.Add(this.txtBriefing);
			this.grpBriefing.Location = new System.Drawing.Point(12, 118);
			this.grpBriefing.Name = "grpBriefing";
			this.grpBriefing.Size = new System.Drawing.Size(436, 200);
			this.grpBriefing.TabIndex = 1;
			this.grpBriefing.TabStop = false;
			// 
			// DetailsForm
			// 
			this.AcceptButton = this.buttonClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(460, 469);
			this.Controls.Add(this.grpBriefing);
			this.Controls.Add(this.grpDescription);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.grpEnvironment);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DetailsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.grpEnvironment.ResumeLayout(false);
			this.grpEnvironment.PerformLayout();
			this.grpDescription.ResumeLayout(false);
			this.grpDescription.PerformLayout();
			this.grpBriefing.ResumeLayout(false);
			this.grpBriefing.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

		private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.TextBox txtBriefing;
        private System.Windows.Forms.GroupBox grpEnvironment;
        private System.Windows.Forms.Label lblStartTime;
        private System.Windows.Forms.TextBox txtDuration;
        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.TextBox txtStartTime;
        private System.Windows.Forms.Label lblSeason;
        private System.Windows.Forms.Label lblWeather;
        private System.Windows.Forms.TextBox txtSeason;
        private System.Windows.Forms.Label lblDifficulty;
        private System.Windows.Forms.TextBox txtWeather;
        private System.Windows.Forms.TextBox txtDifficulty;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.GroupBox grpDescription;
		private System.Windows.Forms.GroupBox grpBriefing;
    }
}