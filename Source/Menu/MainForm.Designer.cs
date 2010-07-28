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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.listBoxRoutes = new System.Windows.Forms.ListBox();
			this.listBoxActivities = new System.Windows.Forms.ListBox();
			this.checkBoxFullScreen = new System.Windows.Forms.CheckBox();
			this.buttonStart = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.label4 = new System.Windows.Forms.Label();
			this.listBoxFolders = new System.Windows.Forms.ListBox();
			this.buttonAddFolder = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.checkBoxWarnings = new System.Windows.Forms.CheckBox();
			this.buttonJoin = new System.Windows.Forms.Button();
			this.checkBoxHost = new System.Windows.Forms.CheckBox();
			this.buttonOptions = new System.Windows.Forms.Button();
			this.buttonRouteDtls = new System.Windows.Forms.Button();
			this.buttonActivityDtls = new System.Windows.Forms.Button();
			this.buttonResume = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBoxRoutes
			// 
			this.listBoxRoutes.FormattingEnabled = true;
			this.listBoxRoutes.Location = new System.Drawing.Point(6, 19);
			this.listBoxRoutes.Name = "listBoxRoutes";
			this.listBoxRoutes.Size = new System.Drawing.Size(199, 225);
			this.listBoxRoutes.TabIndex = 0;
			this.listBoxRoutes.SelectedIndexChanged += new System.EventHandler(this.listBoxRoutes_SelectedIndexChanged);
			// 
			// listBoxActivities
			// 
			this.listBoxActivities.FormattingEnabled = true;
			this.listBoxActivities.Location = new System.Drawing.Point(6, 19);
			this.listBoxActivities.Name = "listBoxActivities";
			this.listBoxActivities.Size = new System.Drawing.Size(199, 225);
			this.listBoxActivities.TabIndex = 0;
			// 
			// checkBoxFullScreen
			// 
			this.checkBoxFullScreen.AutoSize = true;
			this.checkBoxFullScreen.Location = new System.Drawing.Point(416, 301);
			this.checkBoxFullScreen.Name = "checkBoxFullScreen";
			this.checkBoxFullScreen.Size = new System.Drawing.Size(79, 17);
			this.checkBoxFullScreen.TabIndex = 8;
			this.checkBoxFullScreen.Text = "Full Screen";
			this.checkBoxFullScreen.UseVisualStyleBackColor = true;
			// 
			// buttonStart
			// 
			this.buttonStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
			this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.buttonStart.Location = new System.Drawing.Point(582, 297);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.Size = new System.Drawing.Size(75, 57);
			this.buttonStart.TabIndex = 11;
			this.buttonStart.Text = "Start";
			this.buttonStart.UseVisualStyleBackColor = false;
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.ForeColor = System.Drawing.Color.Gray;
			this.label3.Location = new System.Drawing.Point(219, 300);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(71, 29);
			this.label3.TabIndex = 4;
			this.label3.Text = "open";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(156, 297);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(67, 68);
			this.pictureBox1.TabIndex = 5;
			this.pictureBox1.TabStop = false;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.Gray;
			this.label4.Location = new System.Drawing.Point(258, 325);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(62, 29);
			this.label4.TabIndex = 5;
			this.label4.Text = "rails";
			// 
			// listBoxFolders
			// 
			this.listBoxFolders.FormattingEnabled = true;
			this.listBoxFolders.Location = new System.Drawing.Point(6, 19);
			this.listBoxFolders.Name = "listBoxFolders";
			this.listBoxFolders.Size = new System.Drawing.Size(199, 225);
			this.listBoxFolders.TabIndex = 0;
			this.listBoxFolders.SelectedIndexChanged += new System.EventHandler(this.listBoxFolder_SelectedIndexChanged);
			// 
			// buttonAddFolder
			// 
			this.buttonAddFolder.Location = new System.Drawing.Point(6, 250);
			this.buttonAddFolder.Name = "buttonAddFolder";
			this.buttonAddFolder.Size = new System.Drawing.Size(75, 23);
			this.buttonAddFolder.TabIndex = 1;
			this.buttonAddFolder.Text = "Add...";
			this.toolTip1.SetToolTip(this.buttonAddFolder, "List an alternate MSTS folder.");
			this.buttonAddFolder.UseVisualStyleBackColor = true;
			this.buttonAddFolder.Click += new System.EventHandler(this.buttonAddFolder_Click);
			// 
			// folderBrowserDialog
			// 
			this.folderBrowserDialog.Description = "Navigate to your alternate MSTS installation folder.";
			this.folderBrowserDialog.ShowNewFolderButton = false;
			// 
			// buttonRemove
			// 
			this.buttonRemove.Location = new System.Drawing.Point(87, 250);
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Size = new System.Drawing.Size(75, 23);
			this.buttonRemove.TabIndex = 2;
			this.buttonRemove.Text = "Remove";
			this.toolTip1.SetToolTip(this.buttonRemove, "Remove this entry from the list.  It doesn\'t actually delete the folder.");
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
			// 
			// checkBoxWarnings
			// 
			this.checkBoxWarnings.AutoSize = true;
			this.checkBoxWarnings.Checked = true;
			this.checkBoxWarnings.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxWarnings.Location = new System.Drawing.Point(12, 297);
			this.checkBoxWarnings.Name = "checkBoxWarnings";
			this.checkBoxWarnings.Size = new System.Drawing.Size(87, 17);
			this.checkBoxWarnings.TabIndex = 3;
			this.checkBoxWarnings.Text = "Warning Log";
			this.checkBoxWarnings.UseVisualStyleBackColor = true;
			// 
			// buttonJoin
			// 
			this.buttonJoin.Location = new System.Drawing.Point(335, 326);
			this.buttonJoin.Name = "buttonJoin";
			this.buttonJoin.Size = new System.Drawing.Size(75, 23);
			this.buttonJoin.TabIndex = 7;
			this.buttonJoin.Text = "Join";
			this.buttonJoin.UseVisualStyleBackColor = true;
			// 
			// checkBoxHost
			// 
			this.checkBoxHost.AutoSize = true;
			this.checkBoxHost.Location = new System.Drawing.Point(416, 330);
			this.checkBoxHost.Name = "checkBoxHost";
			this.checkBoxHost.Size = new System.Drawing.Size(88, 17);
			this.checkBoxHost.TabIndex = 9;
			this.checkBoxHost.Text = "Host Remote";
			this.checkBoxHost.UseVisualStyleBackColor = true;
			// 
			// buttonOptions
			// 
			this.buttonOptions.Location = new System.Drawing.Point(335, 297);
			this.buttonOptions.Name = "buttonOptions";
			this.buttonOptions.Size = new System.Drawing.Size(75, 23);
			this.buttonOptions.TabIndex = 6;
			this.buttonOptions.Text = "Options";
			this.buttonOptions.UseVisualStyleBackColor = true;
			this.buttonOptions.Click += new System.EventHandler(this.buttonOptions_Click);
			// 
			// buttonRouteDtls
			// 
			this.buttonRouteDtls.Location = new System.Drawing.Point(6, 250);
			this.buttonRouteDtls.Name = "buttonRouteDtls";
			this.buttonRouteDtls.Size = new System.Drawing.Size(75, 23);
			this.buttonRouteDtls.TabIndex = 1;
			this.buttonRouteDtls.Text = "Details";
			this.buttonRouteDtls.UseVisualStyleBackColor = true;
			this.buttonRouteDtls.Click += new System.EventHandler(this.buttonRouteDtls_Click);
			// 
			// buttonActivityDtls
			// 
			this.buttonActivityDtls.Location = new System.Drawing.Point(6, 250);
			this.buttonActivityDtls.Name = "buttonActivityDtls";
			this.buttonActivityDtls.Size = new System.Drawing.Size(75, 23);
			this.buttonActivityDtls.TabIndex = 1;
			this.buttonActivityDtls.Text = "Details";
			this.buttonActivityDtls.UseVisualStyleBackColor = true;
			this.buttonActivityDtls.Click += new System.EventHandler(this.buttonActivityDtls_Click_1);
			// 
			// buttonResume
			// 
			this.buttonResume.Location = new System.Drawing.Point(501, 297);
			this.buttonResume.Name = "buttonResume";
			this.buttonResume.Size = new System.Drawing.Size(75, 23);
			this.buttonResume.TabIndex = 10;
			this.buttonResume.Text = "Resume";
			this.buttonResume.UseVisualStyleBackColor = true;
			this.buttonResume.Click += new System.EventHandler(this.buttonResume_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.listBoxFolders);
			this.groupBox1.Controls.Add(this.buttonAddFolder);
			this.groupBox1.Controls.Add(this.buttonRemove);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(211, 279);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Folders";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.listBoxRoutes);
			this.groupBox2.Controls.Add(this.buttonRouteDtls);
			this.groupBox2.Location = new System.Drawing.Point(229, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(211, 279);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Routes";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.listBoxActivities);
			this.groupBox3.Controls.Add(this.buttonActivityDtls);
			this.groupBox3.Location = new System.Drawing.Point(446, 12);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(211, 279);
			this.groupBox3.TabIndex = 2;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Activities";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(669, 366);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.buttonResume);
			this.Controls.Add(this.buttonOptions);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.buttonJoin);
			this.Controls.Add(this.buttonStart);
			this.Controls.Add(this.checkBoxWarnings);
			this.Controls.Add(this.checkBoxHost);
			this.Controls.Add(this.checkBoxFullScreen);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Open Rails";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxRoutes;
        private System.Windows.Forms.ListBox listBoxActivities;
        private System.Windows.Forms.CheckBox checkBoxFullScreen;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox listBoxFolders;
        private System.Windows.Forms.Button buttonAddFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Button buttonRemove;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox checkBoxWarnings;
        private System.Windows.Forms.Button buttonJoin;
        private System.Windows.Forms.CheckBox checkBoxHost;
        private System.Windows.Forms.Button buttonOptions;
        private System.Windows.Forms.Button buttonRouteDtls;
        private System.Windows.Forms.Button buttonActivityDtls;
        private System.Windows.Forms.Button buttonResume;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
    }
}