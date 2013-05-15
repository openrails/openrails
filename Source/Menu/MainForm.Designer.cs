namespace ORTS
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.listBoxRoutes = new System.Windows.Forms.ListBox();
            this.listBoxActivities = new System.Windows.Forms.ListBox();
            this.checkBoxWindowed = new System.Windows.Forms.CheckBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.labelLogo1 = new System.Windows.Forms.Label();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.labelLogo2 = new System.Windows.Forms.Label();
            this.listBoxFolders = new System.Windows.Forms.ListBox();
            this.buttonFolderAdd = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.buttonFolderRemove = new System.Windows.Forms.Button();
            this.checkBoxWarnings = new System.Windows.Forms.CheckBox();
            this.buttonOptions = new System.Windows.Forms.Button();
            this.buttonRouteDetails = new System.Windows.Forms.Button();
            this.buttonActivityDetails = new System.Windows.Forms.Button();
            this.buttonResume = new System.Windows.Forms.Button();
            this.groupBoxFolders = new System.Windows.Forms.GroupBox();
            this.groupBoxRoutes = new System.Windows.Forms.GroupBox();
            this.labelRoutes = new System.Windows.Forms.Label();
            this.groupBoxActivities = new System.Windows.Forms.GroupBox();
            this.labelActivities = new System.Windows.Forms.Label();
            this.buttonTesting = new System.Windows.Forms.Button();
            this.checkBoxMultiplayer = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.groupBoxFolders.SuspendLayout();
            this.groupBoxRoutes.SuspendLayout();
            this.groupBoxActivities.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxRoutes
            // 
            this.listBoxRoutes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxRoutes.IntegralHeight = false;
            this.listBoxRoutes.Location = new System.Drawing.Point(6, 19);
            this.listBoxRoutes.Name = "listBoxRoutes";
            this.listBoxRoutes.Size = new System.Drawing.Size(288, 346);
            this.listBoxRoutes.TabIndex = 0;
            this.listBoxRoutes.SelectedIndexChanged += new System.EventHandler(this.listBoxRoutes_SelectedIndexChanged);
            this.listBoxRoutes.DoubleClick += new System.EventHandler(this.listBoxRoutes_DoubleClick);
            // 
            // listBoxActivities
            // 
            this.listBoxActivities.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxActivities.IntegralHeight = false;
            this.listBoxActivities.Location = new System.Drawing.Point(6, 19);
            this.listBoxActivities.Name = "listBoxActivities";
            this.listBoxActivities.Size = new System.Drawing.Size(288, 346);
            this.listBoxActivities.TabIndex = 0;
            this.listBoxActivities.DoubleClick += new System.EventHandler(this.listBoxActivities_DoubleClick);
            // 
            // checkBoxWindowed
            // 
            this.checkBoxWindowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxWindowed.AutoSize = true;
            this.checkBoxWindowed.Checked = true;
            this.checkBoxWindowed.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWindowed.Location = new System.Drawing.Point(739, 451);
            this.checkBoxWindowed.Name = "checkBoxWindowed";
            this.checkBoxWindowed.Size = new System.Drawing.Size(77, 17);
            this.checkBoxWindowed.TabIndex = 9;
            this.checkBoxWindowed.Text = "Windowed";
            this.checkBoxWindowed.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            this.buttonStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStart.Enabled = false;
            this.buttonStart.Location = new System.Drawing.Point(820, 418);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(104, 52);
            this.buttonStart.TabIndex = 3;
            this.buttonStart.Text = "Start";
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // labelLogo1
            // 
            this.labelLogo1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelLogo1.AutoSize = true;
            this.labelLogo1.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLogo1.ForeColor = System.Drawing.Color.Gray;
            this.labelLogo1.Location = new System.Drawing.Point(381, 415);
            this.labelLogo1.Name = "labelLogo1";
            this.labelLogo1.Size = new System.Drawing.Size(71, 29);
            this.labelLogo1.TabIndex = 10;
            this.labelLogo1.Text = "open";
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBoxLogo.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxLogo.Image")));
            this.pictureBoxLogo.Location = new System.Drawing.Point(318, 412);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(67, 68);
            this.pictureBoxLogo.TabIndex = 5;
            this.pictureBoxLogo.TabStop = false;
            // 
            // labelLogo2
            // 
            this.labelLogo2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelLogo2.AutoSize = true;
            this.labelLogo2.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLogo2.ForeColor = System.Drawing.Color.Gray;
            this.labelLogo2.Location = new System.Drawing.Point(420, 440);
            this.labelLogo2.Name = "labelLogo2";
            this.labelLogo2.Size = new System.Drawing.Size(62, 29);
            this.labelLogo2.TabIndex = 11;
            this.labelLogo2.Text = "rails";
            // 
            // listBoxFolders
            // 
            this.listBoxFolders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxFolders.IntegralHeight = false;
            this.listBoxFolders.Location = new System.Drawing.Point(6, 19);
            this.listBoxFolders.Name = "listBoxFolders";
            this.listBoxFolders.Size = new System.Drawing.Size(288, 346);
            this.listBoxFolders.TabIndex = 0;
            this.listBoxFolders.SelectedIndexChanged += new System.EventHandler(this.listBoxFolder_SelectedIndexChanged);
            // 
            // buttonFolderAdd
            // 
            this.buttonFolderAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFolderAdd.Location = new System.Drawing.Point(6, 371);
            this.buttonFolderAdd.Name = "buttonFolderAdd";
            this.buttonFolderAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonFolderAdd.TabIndex = 1;
            this.buttonFolderAdd.Text = "Add...";
            this.buttonFolderAdd.Click += new System.EventHandler(this.buttonFolderAdd_Click);
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Navigate to your alternate MSTS installation folder.";
            this.folderBrowserDialog.ShowNewFolderButton = false;
            // 
            // buttonFolderRemove
            // 
            this.buttonFolderRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFolderRemove.Location = new System.Drawing.Point(87, 371);
            this.buttonFolderRemove.Name = "buttonFolderRemove";
            this.buttonFolderRemove.Size = new System.Drawing.Size(75, 23);
            this.buttonFolderRemove.TabIndex = 2;
            this.buttonFolderRemove.Text = "Remove";
            this.buttonFolderRemove.Click += new System.EventHandler(this.buttonFolderRemove_Click);
            // 
            // checkBoxWarnings
            // 
            this.checkBoxWarnings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxWarnings.AutoSize = true;
            this.checkBoxWarnings.Checked = true;
            this.checkBoxWarnings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWarnings.Location = new System.Drawing.Point(658, 451);
            this.checkBoxWarnings.Name = "checkBoxWarnings";
            this.checkBoxWarnings.Size = new System.Drawing.Size(64, 17);
            this.checkBoxWarnings.TabIndex = 8;
            this.checkBoxWarnings.Text = "Logging";
            this.checkBoxWarnings.UseVisualStyleBackColor = true;
            // 
            // buttonOptions
            // 
            this.buttonOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOptions.Location = new System.Drawing.Point(658, 418);
            this.buttonOptions.Name = "buttonOptions";
            this.buttonOptions.Size = new System.Drawing.Size(75, 23);
            this.buttonOptions.TabIndex = 5;
            this.buttonOptions.Text = "Options";
            this.buttonOptions.Click += new System.EventHandler(this.buttonOptions_Click);
            // 
            // buttonRouteDetails
            // 
            this.buttonRouteDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonRouteDetails.Enabled = false;
            this.buttonRouteDetails.Location = new System.Drawing.Point(6, 371);
            this.buttonRouteDetails.Name = "buttonRouteDetails";
            this.buttonRouteDetails.Size = new System.Drawing.Size(75, 23);
            this.buttonRouteDetails.TabIndex = 1;
            this.buttonRouteDetails.Text = "Details";
            this.buttonRouteDetails.Click += new System.EventHandler(this.buttonRouteDetails_Click);
            // 
            // buttonActivityDetails
            // 
            this.buttonActivityDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonActivityDetails.Enabled = false;
            this.buttonActivityDetails.Location = new System.Drawing.Point(6, 371);
            this.buttonActivityDetails.Name = "buttonActivityDetails";
            this.buttonActivityDetails.Size = new System.Drawing.Size(75, 23);
            this.buttonActivityDetails.TabIndex = 1;
            this.buttonActivityDetails.Text = "Details";
            this.buttonActivityDetails.Click += new System.EventHandler(this.buttonActivityDetails_Click);
            // 
            // buttonResume
            // 
            this.buttonResume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonResume.Enabled = false;
            this.buttonResume.Location = new System.Drawing.Point(739, 418);
            this.buttonResume.Name = "buttonResume";
            this.buttonResume.Size = new System.Drawing.Size(75, 23);
            this.buttonResume.TabIndex = 4;
            this.buttonResume.Text = "Resume";
            this.buttonResume.Click += new System.EventHandler(this.buttonResume_Click);
            // 
            // groupBoxFolders
            // 
            this.groupBoxFolders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxFolders.Controls.Add(this.listBoxFolders);
            this.groupBoxFolders.Controls.Add(this.buttonFolderAdd);
            this.groupBoxFolders.Controls.Add(this.buttonFolderRemove);
            this.groupBoxFolders.Location = new System.Drawing.Point(12, 12);
            this.groupBoxFolders.Name = "groupBoxFolders";
            this.groupBoxFolders.Size = new System.Drawing.Size(300, 400);
            this.groupBoxFolders.TabIndex = 0;
            this.groupBoxFolders.TabStop = false;
            this.groupBoxFolders.Text = "Folders";
            // 
            // groupBoxRoutes
            // 
            this.groupBoxRoutes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxRoutes.Controls.Add(this.labelRoutes);
            this.groupBoxRoutes.Controls.Add(this.listBoxRoutes);
            this.groupBoxRoutes.Controls.Add(this.buttonRouteDetails);
            this.groupBoxRoutes.Location = new System.Drawing.Point(318, 12);
            this.groupBoxRoutes.Name = "groupBoxRoutes";
            this.groupBoxRoutes.Size = new System.Drawing.Size(300, 400);
            this.groupBoxRoutes.TabIndex = 1;
            this.groupBoxRoutes.TabStop = false;
            this.groupBoxRoutes.Text = "Routes";
            // 
            // labelRoutes
            // 
            this.labelRoutes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRoutes.Location = new System.Drawing.Point(6, 19);
            this.labelRoutes.Name = "labelRoutes";
            this.labelRoutes.Size = new System.Drawing.Size(288, 346);
            this.labelRoutes.TabIndex = 0;
            this.labelRoutes.Text = "No routes.";
            this.labelRoutes.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelRoutes.Visible = false;
            // 
            // groupBoxActivities
            // 
            this.groupBoxActivities.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxActivities.Controls.Add(this.labelActivities);
            this.groupBoxActivities.Controls.Add(this.listBoxActivities);
            this.groupBoxActivities.Controls.Add(this.buttonActivityDetails);
            this.groupBoxActivities.Location = new System.Drawing.Point(624, 12);
            this.groupBoxActivities.Name = "groupBoxActivities";
            this.groupBoxActivities.Size = new System.Drawing.Size(300, 400);
            this.groupBoxActivities.TabIndex = 2;
            this.groupBoxActivities.TabStop = false;
            this.groupBoxActivities.Text = "Activities";
            // 
            // labelActivities
            // 
            this.labelActivities.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelActivities.Location = new System.Drawing.Point(6, 19);
            this.labelActivities.Name = "labelActivities";
            this.labelActivities.Size = new System.Drawing.Size(288, 346);
            this.labelActivities.TabIndex = 0;
            this.labelActivities.Text = "No activities.";
            this.labelActivities.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelActivities.Visible = false;
            // 
            // buttonTesting
            // 
            this.buttonTesting.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonTesting.Location = new System.Drawing.Point(577, 418);
            this.buttonTesting.Name = "buttonTesting";
            this.buttonTesting.Size = new System.Drawing.Size(75, 23);
            this.buttonTesting.TabIndex = 6;
            this.buttonTesting.Text = "Testing";
            this.buttonTesting.Click += new System.EventHandler(this.buttonTesting_Click);
            // 
            // checkBoxMultiplayer
            // 
            this.checkBoxMultiplayer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxMultiplayer.AutoSize = true;
            this.checkBoxMultiplayer.Location = new System.Drawing.Point(577, 451);
            this.checkBoxMultiplayer.Name = "checkBoxMultiplayer";
            this.checkBoxMultiplayer.Size = new System.Drawing.Size(76, 17);
            this.checkBoxMultiplayer.TabIndex = 7;
            this.checkBoxMultiplayer.Text = "Multiplayer";
            this.checkBoxMultiplayer.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(936, 482);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.checkBoxWindowed);
            this.Controls.Add(this.checkBoxWarnings);
            this.Controls.Add(this.checkBoxMultiplayer);
            this.Controls.Add(this.buttonOptions);
            this.Controls.Add(this.buttonTesting);
            this.Controls.Add(this.pictureBoxLogo);
            this.Controls.Add(this.labelLogo1);
            this.Controls.Add(this.labelLogo2);
            this.Controls.Add(this.buttonResume);
            this.Controls.Add(this.groupBoxActivities);
            this.Controls.Add(this.groupBoxRoutes);
            this.Controls.Add(this.groupBoxFolders);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Open Rails";
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.groupBoxFolders.ResumeLayout(false);
            this.groupBoxRoutes.ResumeLayout(false);
            this.groupBoxActivities.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxRoutes;
        private System.Windows.Forms.ListBox listBoxActivities;
        private System.Windows.Forms.CheckBox checkBoxWindowed;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label labelLogo1;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelLogo2;
        private System.Windows.Forms.ListBox listBoxFolders;
        private System.Windows.Forms.Button buttonFolderAdd;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.Button buttonFolderRemove;
        private System.Windows.Forms.CheckBox checkBoxWarnings;
        private System.Windows.Forms.Button buttonOptions;
        private System.Windows.Forms.Button buttonRouteDetails;
        private System.Windows.Forms.Button buttonActivityDetails;
        private System.Windows.Forms.Button buttonResume;
        private System.Windows.Forms.GroupBox groupBoxFolders;
        private System.Windows.Forms.GroupBox groupBoxRoutes;
        private System.Windows.Forms.GroupBox groupBoxActivities;
		private System.Windows.Forms.Label labelRoutes;
        private System.Windows.Forms.Label labelActivities;
        private System.Windows.Forms.Button buttonTesting;
        private System.Windows.Forms.CheckBox checkBoxMultiplayer;
    }
}
