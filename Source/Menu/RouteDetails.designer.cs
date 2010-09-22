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
			this.txtDescription = new System.Windows.Forms.TextBox();
			this.txtBriefing = new System.Windows.Forms.TextBox();
			this.grpEnvironment = new System.Windows.Forms.GroupBox();
			this.txtDifficulty = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
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
			this.lblName = new System.Windows.Forms.Label();
			this.grpEnvironment.SuspendLayout();
			this.grpDescription.SuspendLayout();
			this.grpBriefing.SuspendLayout();
			this.SuspendLayout();
			// 
			// txtDescription
			// 
			resources.ApplyResources(this.txtDescription, "txtDescription");
			this.txtDescription.Name = "txtDescription";
			this.txtDescription.ReadOnly = true;
			this.txtDescription.TabStop = false;
			// 
			// txtBriefing
			// 
			resources.ApplyResources(this.txtBriefing, "txtBriefing");
			this.txtBriefing.Name = "txtBriefing";
			this.txtBriefing.ReadOnly = true;
			this.txtBriefing.TabStop = false;
			// 
			// grpEnvironment
			// 
			this.grpEnvironment.Controls.Add(this.txtDifficulty);
			this.grpEnvironment.Controls.Add(this.label1);
			this.grpEnvironment.Controls.Add(this.txtWeather);
			this.grpEnvironment.Controls.Add(this.lblWeather);
			this.grpEnvironment.Controls.Add(this.txtSeason);
			this.grpEnvironment.Controls.Add(this.lblSeason);
			this.grpEnvironment.Controls.Add(this.txtDuration);
			this.grpEnvironment.Controls.Add(this.lblDuration);
			this.grpEnvironment.Controls.Add(this.txtStartTime);
			this.grpEnvironment.Controls.Add(this.lblStartTime);
			resources.ApplyResources(this.grpEnvironment, "grpEnvironment");
			this.grpEnvironment.Name = "grpEnvironment";
			this.grpEnvironment.TabStop = false;
			// 
			// txtDifficulty
			// 
			resources.ApplyResources(this.txtDifficulty, "txtDifficulty");
			this.txtDifficulty.Name = "txtDifficulty";
			this.txtDifficulty.ReadOnly = true;
			this.txtDifficulty.TabStop = false;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// txtWeather
			// 
			resources.ApplyResources(this.txtWeather, "txtWeather");
			this.txtWeather.Name = "txtWeather";
			this.txtWeather.ReadOnly = true;
			this.txtWeather.TabStop = false;
			// 
			// lblWeather
			// 
			resources.ApplyResources(this.lblWeather, "lblWeather");
			this.lblWeather.Name = "lblWeather";
			// 
			// txtSeason
			// 
			resources.ApplyResources(this.txtSeason, "txtSeason");
			this.txtSeason.Name = "txtSeason";
			this.txtSeason.ReadOnly = true;
			this.txtSeason.TabStop = false;
			// 
			// lblSeason
			// 
			resources.ApplyResources(this.lblSeason, "lblSeason");
			this.lblSeason.Name = "lblSeason";
			// 
			// txtDuration
			// 
			resources.ApplyResources(this.txtDuration, "txtDuration");
			this.txtDuration.Name = "txtDuration";
			this.txtDuration.ReadOnly = true;
			this.txtDuration.TabStop = false;
			// 
			// lblDuration
			// 
			resources.ApplyResources(this.lblDuration, "lblDuration");
			this.lblDuration.Name = "lblDuration";
			// 
			// txtStartTime
			// 
			resources.ApplyResources(this.txtStartTime, "txtStartTime");
			this.txtStartTime.Name = "txtStartTime";
			this.txtStartTime.ReadOnly = true;
			this.txtStartTime.TabStop = false;
			// 
			// lblStartTime
			// 
			resources.ApplyResources(this.lblStartTime, "lblStartTime");
			this.lblStartTime.Name = "lblStartTime";
			// 
			// buttonClose
			// 
			resources.ApplyResources(this.buttonClose, "buttonClose");
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// grpDescription
			// 
			this.grpDescription.Controls.Add(this.txtDescription);
			resources.ApplyResources(this.grpDescription, "grpDescription");
			this.grpDescription.Name = "grpDescription";
			this.grpDescription.TabStop = false;
			// 
			// grpBriefing
			// 
			this.grpBriefing.Controls.Add(this.txtBriefing);
			resources.ApplyResources(this.grpBriefing, "grpBriefing");
			this.grpBriefing.Name = "grpBriefing";
			this.grpBriefing.TabStop = false;
			// 
			// lblName
			// 
			resources.ApplyResources(this.lblName, "lblName");
			this.lblName.Name = "lblName";
			// 
			// DetailsForm
			// 
			this.AcceptButton = this.buttonClose;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.buttonClose;
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.grpBriefing);
			this.Controls.Add(this.grpDescription);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.grpEnvironment);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DetailsForm";
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtWeather;
        private System.Windows.Forms.TextBox txtDifficulty;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.GroupBox grpDescription;
        private System.Windows.Forms.GroupBox grpBriefing;
		private System.Windows.Forms.Label lblName;
    }
}