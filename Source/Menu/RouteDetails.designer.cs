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
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtBriefing = new System.Windows.Forms.TextBox();
            this.lblBriefing = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
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
            this.cmdOK = new System.Windows.Forms.Button();
            this.lblOpen = new System.Windows.Forms.Label();
            this.lblRails = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(18, 61);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ReadOnly = true;
            this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDescription.Size = new System.Drawing.Size(304, 124);
            this.txtDescription.TabIndex = 1;
            this.txtDescription.TabStop = false;
            // 
            // txtName
            // 
            this.txtName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtName.ForeColor = System.Drawing.Color.Blue;
            this.txtName.Location = new System.Drawing.Point(1, 12);
            this.txtName.Name = "txtName";
            this.txtName.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(338, 19);
            this.txtName.TabIndex = 0;
            this.txtName.TabStop = false;
            this.txtName.Text = "After The Storm";
            this.txtName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(18, 45);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(63, 13);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "Description:";
            // 
            // txtBriefing
            // 
            this.txtBriefing.Location = new System.Drawing.Point(18, 204);
            this.txtBriefing.Multiline = true;
            this.txtBriefing.Name = "txtBriefing";
            this.txtBriefing.ReadOnly = true;
            this.txtBriefing.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBriefing.Size = new System.Drawing.Size(305, 116);
            this.txtBriefing.TabIndex = 3;
            this.txtBriefing.TabStop = false;
            // 
            // lblBriefing
            // 
            this.lblBriefing.AutoSize = true;
            this.lblBriefing.Location = new System.Drawing.Point(18, 188);
            this.lblBriefing.Name = "lblBriefing";
            this.lblBriefing.Size = new System.Drawing.Size(42, 13);
            this.lblBriefing.TabIndex = 4;
            this.lblBriefing.Text = "Briefing";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtDifficulty);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtWeather);
            this.groupBox1.Controls.Add(this.lblWeather);
            this.groupBox1.Controls.Add(this.txtSeason);
            this.groupBox1.Controls.Add(this.lblSeason);
            this.groupBox1.Controls.Add(this.txtDuration);
            this.groupBox1.Controls.Add(this.lblDuration);
            this.groupBox1.Controls.Add(this.txtStartTime);
            this.groupBox1.Controls.Add(this.lblStartTime);
            this.groupBox1.Location = new System.Drawing.Point(18, 326);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(304, 97);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            // 
            // txtDifficulty
            // 
            this.txtDifficulty.Location = new System.Drawing.Point(70, 65);
            this.txtDifficulty.Name = "txtDifficulty";
            this.txtDifficulty.ReadOnly = true;
            this.txtDifficulty.Size = new System.Drawing.Size(75, 20);
            this.txtDifficulty.TabIndex = 9;
            this.txtDifficulty.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Difficulty:";
            // 
            // txtWeather
            // 
            this.txtWeather.Location = new System.Drawing.Point(217, 39);
            this.txtWeather.Name = "txtWeather";
            this.txtWeather.ReadOnly = true;
            this.txtWeather.Size = new System.Drawing.Size(75, 20);
            this.txtWeather.TabIndex = 7;
            this.txtWeather.TabStop = false;
            // 
            // lblWeather
            // 
            this.lblWeather.AutoSize = true;
            this.lblWeather.Location = new System.Drawing.Point(161, 42);
            this.lblWeather.Name = "lblWeather";
            this.lblWeather.Size = new System.Drawing.Size(51, 13);
            this.lblWeather.TabIndex = 6;
            this.lblWeather.Text = "Weather:";
            // 
            // txtSeason
            // 
            this.txtSeason.Location = new System.Drawing.Point(70, 39);
            this.txtSeason.Name = "txtSeason";
            this.txtSeason.ReadOnly = true;
            this.txtSeason.Size = new System.Drawing.Size(75, 20);
            this.txtSeason.TabIndex = 5;
            this.txtSeason.TabStop = false;
            // 
            // lblSeason
            // 
            this.lblSeason.AutoSize = true;
            this.lblSeason.Location = new System.Drawing.Point(6, 42);
            this.lblSeason.Name = "lblSeason";
            this.lblSeason.Size = new System.Drawing.Size(46, 13);
            this.lblSeason.TabIndex = 4;
            this.lblSeason.Text = "Season:";
            // 
            // txtDuration
            // 
            this.txtDuration.Location = new System.Drawing.Point(217, 13);
            this.txtDuration.Name = "txtDuration";
            this.txtDuration.ReadOnly = true;
            this.txtDuration.Size = new System.Drawing.Size(75, 20);
            this.txtDuration.TabIndex = 3;
            this.txtDuration.TabStop = false;
            // 
            // lblDuration
            // 
            this.lblDuration.AutoSize = true;
            this.lblDuration.Location = new System.Drawing.Point(161, 16);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(50, 13);
            this.lblDuration.TabIndex = 2;
            this.lblDuration.Text = "Duration:";
            // 
            // txtStartTime
            // 
            this.txtStartTime.Location = new System.Drawing.Point(70, 13);
            this.txtStartTime.Name = "txtStartTime";
            this.txtStartTime.ReadOnly = true;
            this.txtStartTime.Size = new System.Drawing.Size(75, 20);
            this.txtStartTime.TabIndex = 1;
            this.txtStartTime.TabStop = false;
            // 
            // lblStartTime
            // 
            this.lblStartTime.AutoSize = true;
            this.lblStartTime.Location = new System.Drawing.Point(6, 16);
            this.lblStartTime.Name = "lblStartTime";
            this.lblStartTime.Size = new System.Drawing.Size(58, 13);
            this.lblStartTime.TabIndex = 0;
            this.lblStartTime.Text = "Start Time:";
            // 
            // cmdOK
            // 
            this.cmdOK.Location = new System.Drawing.Point(262, 469);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(61, 25);
            this.cmdOK.TabIndex = 6;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // lblOpen
            // 
            this.lblOpen.AutoSize = true;
            this.lblOpen.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOpen.ForeColor = System.Drawing.Color.Gray;
            this.lblOpen.Location = new System.Drawing.Point(101, 429);
            this.lblOpen.Name = "lblOpen";
            this.lblOpen.Size = new System.Drawing.Size(71, 29);
            this.lblOpen.TabIndex = 8;
            this.lblOpen.Text = "open";
            // 
            // lblRails
            // 
            this.lblRails.AutoSize = true;
            this.lblRails.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRails.ForeColor = System.Drawing.Color.Gray;
            this.lblRails.Location = new System.Drawing.Point(127, 463);
            this.lblRails.Name = "lblRails";
            this.lblRails.Size = new System.Drawing.Size(62, 29);
            this.lblRails.TabIndex = 9;
            this.lblRails.Text = "rails";
            // 
            // picLogo
            // 
            this.picLogo.Image = ((System.Drawing.Image)(resources.GetObject("picLogo.Image")));
            this.picLogo.Location = new System.Drawing.Point(21, 432);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(67, 68);
            this.picLogo.TabIndex = 10;
            this.picLogo.TabStop = false;
            // 
            // DetailsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(341, 512);
            this.Controls.Add(this.picLogo);
            this.Controls.Add(this.lblRails);
            this.Controls.Add(this.lblOpen);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblBriefing);
            this.Controls.Add(this.txtBriefing);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.txtName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DetailsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RouteDetails";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.TextBox txtBriefing;
        private System.Windows.Forms.Label lblBriefing;
        private System.Windows.Forms.GroupBox groupBox1;
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
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Label lblOpen;
        private System.Windows.Forms.Label lblRails;
        private System.Windows.Forms.PictureBox picLogo;
    }
}