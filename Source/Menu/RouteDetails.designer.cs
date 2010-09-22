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
            this.lblName = new System.Windows.Forms.Label();
            this.grpBriefing = new System.Windows.Forms.GroupBox();
            this.grpEnvironment.SuspendLayout();
            this.grpDescription.SuspendLayout();
            this.grpBriefing.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(6, 42);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ReadOnly = true;
            this.txtDescription.Size = new System.Drawing.Size(462, 65);
            this.txtDescription.TabIndex = 0;
            this.txtDescription.TabStop = false;
            // 
            // txtBriefing
            // 
            this.txtBriefing.Location = new System.Drawing.Point(6, 19);
            this.txtBriefing.Multiline = true;
            this.txtBriefing.Name = "txtBriefing";
            this.txtBriefing.ReadOnly = true;
            this.txtBriefing.Size = new System.Drawing.Size(462, 202);
            this.txtBriefing.TabIndex = 0;
            this.txtBriefing.TabStop = false;
            // 
            // grpEnvironment
            // 
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
            this.grpEnvironment.Location = new System.Drawing.Point(12, 364);
            this.grpEnvironment.Name = "grpEnvironment";
            this.grpEnvironment.Size = new System.Drawing.Size(474, 123);
            this.grpEnvironment.TabIndex = 4;
            this.grpEnvironment.TabStop = false;
            // 
            // txtDifficulty
            // 
            this.txtDifficulty.Location = new System.Drawing.Point(139, 31);
            this.txtDifficulty.Name = "txtDifficulty";
            this.txtDifficulty.ReadOnly = true;
            this.txtDifficulty.Size = new System.Drawing.Size(100, 20);
            this.txtDifficulty.TabIndex = 0;
            this.txtDifficulty.TabStop = false;
            // 
            // lblDifficulty
            // 
            this.lblDifficulty.Location = new System.Drawing.Point(45, 31);
            this.lblDifficulty.Name = "lblDifficulty";
            this.lblDifficulty.Size = new System.Drawing.Size(61, 20);
            this.lblDifficulty.TabIndex = 1;
            this.lblDifficulty.Text = "Difficulty";
            // 
            // txtWeather
            // 
            this.txtWeather.Location = new System.Drawing.Point(139, 57);
            this.txtWeather.Name = "txtWeather";
            this.txtWeather.ReadOnly = true;
            this.txtWeather.Size = new System.Drawing.Size(100, 20);
            this.txtWeather.TabIndex = 2;
            this.txtWeather.TabStop = false;
            // 
            // lblWeather
            // 
            this.lblWeather.Location = new System.Drawing.Point(45, 57);
            this.lblWeather.Name = "lblWeather";
            this.lblWeather.Size = new System.Drawing.Size(61, 20);
            this.lblWeather.TabIndex = 3;
            this.lblWeather.Text = "Weather";
            // 
            // txtSeason
            // 
            this.txtSeason.Location = new System.Drawing.Point(139, 83);
            this.txtSeason.Name = "txtSeason";
            this.txtSeason.ReadOnly = true;
            this.txtSeason.Size = new System.Drawing.Size(100, 20);
            this.txtSeason.TabIndex = 4;
            this.txtSeason.TabStop = false;
            // 
            // lblSeason
            // 
            this.lblSeason.Location = new System.Drawing.Point(45, 83);
            this.lblSeason.Name = "lblSeason";
            this.lblSeason.Size = new System.Drawing.Size(64, 20);
            this.lblSeason.TabIndex = 5;
            this.lblSeason.Text = "Season";
            // 
            // txtDuration
            // 
            this.txtDuration.Location = new System.Drawing.Point(341, 57);
            this.txtDuration.Name = "txtDuration";
            this.txtDuration.ReadOnly = true;
            this.txtDuration.Size = new System.Drawing.Size(100, 20);
            this.txtDuration.TabIndex = 6;
            this.txtDuration.TabStop = false;
            // 
            // lblDuration
            // 
            this.lblDuration.Location = new System.Drawing.Point(263, 57);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(75, 20);
            this.lblDuration.TabIndex = 7;
            this.lblDuration.Text = "Duration";
            // 
            // txtStartTime
            // 
            this.txtStartTime.Location = new System.Drawing.Point(341, 30);
            this.txtStartTime.Name = "txtStartTime";
            this.txtStartTime.ReadOnly = true;
            this.txtStartTime.Size = new System.Drawing.Size(100, 20);
            this.txtStartTime.TabIndex = 8;
            this.txtStartTime.TabStop = false;
            // 
            // lblStartTime
            // 
            this.lblStartTime.Location = new System.Drawing.Point(263, 31);
            this.lblStartTime.Name = "lblStartTime";
            this.lblStartTime.Size = new System.Drawing.Size(72, 20);
            this.lblStartTime.TabIndex = 9;
            this.lblStartTime.Text = "Start Time";
            // 
            // buttonClose
            // 
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonClose.Location = new System.Drawing.Point(411, 505);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 23);
            this.buttonClose.TabIndex = 3;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            // 
            // grpDescription
            // 
            this.grpDescription.Controls.Add(this.lblName);
            this.grpDescription.Controls.Add(this.txtDescription);
            this.grpDescription.Location = new System.Drawing.Point(12, 12);
            this.grpDescription.Name = "grpDescription";
            this.grpDescription.Size = new System.Drawing.Size(474, 113);
            this.grpDescription.TabIndex = 2;
            this.grpDescription.TabStop = false;
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(9, 16);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(459, 23);
            this.lblName.TabIndex = 0;
            // 
            // grpBriefing
            // 
            this.grpBriefing.Controls.Add(this.txtBriefing);
            this.grpBriefing.Location = new System.Drawing.Point(12, 131);
            this.grpBriefing.Name = "grpBriefing";
            this.grpBriefing.Size = new System.Drawing.Size(474, 227);
            this.grpBriefing.TabIndex = 1;
            this.grpBriefing.TabStop = false;
            // 
            // DetailsForm
            // 
            this.AcceptButton = this.buttonClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonClose;
            this.ClientSize = new System.Drawing.Size(498, 540);
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
		private System.Windows.Forms.Label lblName;
    }
}