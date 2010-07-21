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
            this.txtName = new System.Windows.Forms.TextBox();
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
            this.grpEnvironment.SuspendLayout();
            this.grpDescription.SuspendLayout();
            this.grpBriefing.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(6, 19);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ReadOnly = true;
            this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDescription.Size = new System.Drawing.Size(304, 124);
            this.txtDescription.TabIndex = 0;
            this.txtDescription.TabStop = false;
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtName.ForeColor = System.Drawing.Color.Blue;
            this.txtName.Location = new System.Drawing.Point(12, 12);
            this.txtName.Name = "txtName";
            this.txtName.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(316, 19);
            this.txtName.TabIndex = 0;
            this.txtName.TabStop = false;
            this.txtName.Text = "After The Storm";
            this.txtName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtBriefing
            // 
            this.txtBriefing.Location = new System.Drawing.Point(6, 19);
            this.txtBriefing.Multiline = true;
            this.txtBriefing.Name = "txtBriefing";
            this.txtBriefing.ReadOnly = true;
            this.txtBriefing.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBriefing.Size = new System.Drawing.Size(304, 124);
            this.txtBriefing.TabIndex = 0;
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
            this.grpEnvironment.Location = new System.Drawing.Point(12, 347);
            this.grpEnvironment.Name = "grpEnvironment";
            this.grpEnvironment.Size = new System.Drawing.Size(316, 97);
            this.grpEnvironment.TabIndex = 3;
            this.grpEnvironment.TabStop = false;
            this.grpEnvironment.Text = "Environment";
            // 
            // txtDifficulty
            // 
            this.txtDifficulty.Location = new System.Drawing.Point(70, 71);
            this.txtDifficulty.Name = "txtDifficulty";
            this.txtDifficulty.ReadOnly = true;
            this.txtDifficulty.Size = new System.Drawing.Size(75, 20);
            this.txtDifficulty.TabIndex = 9;
            this.txtDifficulty.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 74);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Difficulty:";
            // 
            // txtWeather
            // 
            this.txtWeather.Location = new System.Drawing.Point(235, 45);
            this.txtWeather.Name = "txtWeather";
            this.txtWeather.ReadOnly = true;
            this.txtWeather.Size = new System.Drawing.Size(75, 20);
            this.txtWeather.TabIndex = 7;
            this.txtWeather.TabStop = false;
            // 
            // lblWeather
            // 
            this.lblWeather.AutoSize = true;
            this.lblWeather.Location = new System.Drawing.Point(178, 48);
            this.lblWeather.Name = "lblWeather";
            this.lblWeather.Size = new System.Drawing.Size(51, 13);
            this.lblWeather.TabIndex = 6;
            this.lblWeather.Text = "Weather:";
            // 
            // txtSeason
            // 
            this.txtSeason.Location = new System.Drawing.Point(70, 45);
            this.txtSeason.Name = "txtSeason";
            this.txtSeason.ReadOnly = true;
            this.txtSeason.Size = new System.Drawing.Size(75, 20);
            this.txtSeason.TabIndex = 5;
            this.txtSeason.TabStop = false;
            // 
            // lblSeason
            // 
            this.lblSeason.AutoSize = true;
            this.lblSeason.Location = new System.Drawing.Point(6, 48);
            this.lblSeason.Name = "lblSeason";
            this.lblSeason.Size = new System.Drawing.Size(46, 13);
            this.lblSeason.TabIndex = 4;
            this.lblSeason.Text = "Season:";
            // 
            // txtDuration
            // 
            this.txtDuration.Location = new System.Drawing.Point(235, 19);
            this.txtDuration.Name = "txtDuration";
            this.txtDuration.ReadOnly = true;
            this.txtDuration.Size = new System.Drawing.Size(75, 20);
            this.txtDuration.TabIndex = 3;
            this.txtDuration.TabStop = false;
            // 
            // lblDuration
            // 
            this.lblDuration.AutoSize = true;
            this.lblDuration.Location = new System.Drawing.Point(178, 22);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(50, 13);
            this.lblDuration.TabIndex = 2;
            this.lblDuration.Text = "Duration:";
            // 
            // txtStartTime
            // 
            this.txtStartTime.Location = new System.Drawing.Point(70, 19);
            this.txtStartTime.Name = "txtStartTime";
            this.txtStartTime.ReadOnly = true;
            this.txtStartTime.Size = new System.Drawing.Size(75, 20);
            this.txtStartTime.TabIndex = 1;
            this.txtStartTime.TabStop = false;
            // 
            // lblStartTime
            // 
            this.lblStartTime.AutoSize = true;
            this.lblStartTime.Location = new System.Drawing.Point(6, 22);
            this.lblStartTime.Name = "lblStartTime";
            this.lblStartTime.Size = new System.Drawing.Size(58, 13);
            this.lblStartTime.TabIndex = 0;
            this.lblStartTime.Text = "Start Time:";
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.Location = new System.Drawing.Point(253, 450);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 23);
            this.buttonClose.TabIndex = 4;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.cmdClose_Click);
            // 
            // grpDescription
            // 
            this.grpDescription.Controls.Add(this.txtDescription);
            this.grpDescription.Location = new System.Drawing.Point(12, 37);
            this.grpDescription.Name = "grpDescription";
            this.grpDescription.Size = new System.Drawing.Size(316, 149);
            this.grpDescription.TabIndex = 1;
            this.grpDescription.TabStop = false;
            this.grpDescription.Text = "Description";
            // 
            // grpBriefing
            // 
            this.grpBriefing.Controls.Add(this.txtBriefing);
            this.grpBriefing.Location = new System.Drawing.Point(12, 192);
            this.grpBriefing.Name = "grpBriefing";
            this.grpBriefing.Size = new System.Drawing.Size(316, 149);
            this.grpBriefing.TabIndex = 2;
            this.grpBriefing.TabStop = false;
            this.grpBriefing.Text = "Briefing";
            // 
            // DetailsForm
            // 
            this.AcceptButton = this.buttonClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(340, 485);
            this.Controls.Add(this.grpBriefing);
            this.Controls.Add(this.grpDescription);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.grpEnvironment);
            this.Controls.Add(this.txtName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DetailsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RouteDetails";
            this.grpEnvironment.ResumeLayout(false);
            this.grpEnvironment.PerformLayout();
            this.grpDescription.ResumeLayout(false);
            this.grpDescription.PerformLayout();
            this.grpBriefing.ResumeLayout(false);
            this.grpBriefing.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.TextBox txtName;
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
    }
}