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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label4 = new System.Windows.Forms.Label();
            this.listBoxFolder = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // listBoxRoutes
            // 
            this.listBoxRoutes.FormattingEnabled = true;
            this.listBoxRoutes.Location = new System.Drawing.Point(232, 39);
            this.listBoxRoutes.Name = "listBoxRoutes";
            this.listBoxRoutes.Size = new System.Drawing.Size(199, 225);
            this.listBoxRoutes.TabIndex = 0;
            this.listBoxRoutes.SelectedIndexChanged += new System.EventHandler(this.listBoxRoutes_SelectedIndexChanged);
            // 
            // listBoxActivities
            // 
            this.listBoxActivities.FormattingEnabled = true;
            this.listBoxActivities.Location = new System.Drawing.Point(450, 39);
            this.listBoxActivities.Name = "listBoxActivities";
            this.listBoxActivities.Size = new System.Drawing.Size(199, 225);
            this.listBoxActivities.TabIndex = 0;
            // 
            // checkBoxFullScreen
            // 
            this.checkBoxFullScreen.AutoSize = true;
            this.checkBoxFullScreen.Location = new System.Drawing.Point(426, 280);
            this.checkBoxFullScreen.Name = "checkBoxFullScreen";
            this.checkBoxFullScreen.Size = new System.Drawing.Size(79, 17);
            this.checkBoxFullScreen.TabIndex = 1;
            this.checkBoxFullScreen.Text = "Full Screen";
            this.checkBoxFullScreen.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.Location = new System.Drawing.Point(574, 281);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 37);
            this.buttonStart.TabIndex = 2;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = false;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(232, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Routes";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(447, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Activities";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Gray;
            this.label3.Location = new System.Drawing.Point(237, 268);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 29);
            this.label3.TabIndex = 4;
            this.label3.Text = "open";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(174, 265);
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
            this.label4.Location = new System.Drawing.Point(276, 293);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 29);
            this.label4.TabIndex = 4;
            this.label4.Text = "rails";
            // 
            // listBoxFolder
            // 
            this.listBoxFolder.FormattingEnabled = true;
            this.listBoxFolder.Location = new System.Drawing.Point(13, 39);
            this.listBoxFolder.Name = "listBoxFolder";
            this.listBoxFolder.Size = new System.Drawing.Size(199, 225);
            this.listBoxFolder.TabIndex = 0;
            this.listBoxFolder.SelectedIndexChanged += new System.EventHandler(this.listBoxFolder_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(36, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Folder";
            // 
            // buttonAddFolder
            // 
            this.buttonAddFolder.Location = new System.Drawing.Point(12, 271);
            this.buttonAddFolder.Name = "buttonAddFolder";
            this.buttonAddFolder.Size = new System.Drawing.Size(75, 22);
            this.buttonAddFolder.TabIndex = 2;
            this.buttonAddFolder.Text = "Add Folder";
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
            this.buttonRemove.Location = new System.Drawing.Point(93, 271);
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Size = new System.Drawing.Size(75, 22);
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
            this.checkBoxWarnings.Location = new System.Drawing.Point(16, 306);
            this.checkBoxWarnings.Name = "checkBoxWarnings";
            this.checkBoxWarnings.Size = new System.Drawing.Size(87, 17);
            this.checkBoxWarnings.TabIndex = 1;
            this.checkBoxWarnings.Text = "Warning Log";
            this.checkBoxWarnings.UseVisualStyleBackColor = true;
            // 
            // buttonJoin
            // 
            this.buttonJoin.Location = new System.Drawing.Point(347, 306);
            this.buttonJoin.Name = "buttonJoin";
            this.buttonJoin.Size = new System.Drawing.Size(60, 22);
            this.buttonJoin.TabIndex = 2;
            this.buttonJoin.Text = "Join";
            this.buttonJoin.UseVisualStyleBackColor = true;
            // 
            // checkBoxHost
            // 
            this.checkBoxHost.AutoSize = true;
            this.checkBoxHost.Location = new System.Drawing.Point(426, 311);
            this.checkBoxHost.Name = "checkBoxHost";
            this.checkBoxHost.Size = new System.Drawing.Size(88, 17);
            this.checkBoxHost.TabIndex = 1;
            this.checkBoxHost.Text = "Host Remote";
            this.checkBoxHost.UseVisualStyleBackColor = true;
            // 
            // buttonOptions
            // 
            this.buttonOptions.Location = new System.Drawing.Point(347, 276);
            this.buttonOptions.Name = "buttonOptions";
            this.buttonOptions.Size = new System.Drawing.Size(60, 23);
            this.buttonOptions.TabIndex = 6;
            this.buttonOptions.Text = "Options";
            this.buttonOptions.UseVisualStyleBackColor = true;
            this.buttonOptions.Click += new System.EventHandler(this.buttonOptions_Click);
            // 
            // buttonRouteDtls
            // 
            this.buttonRouteDtls.Location = new System.Drawing.Point(374, 5);
            this.buttonRouteDtls.Name = "buttonRouteDtls";
            this.buttonRouteDtls.Size = new System.Drawing.Size(57, 21);
            this.buttonRouteDtls.TabIndex = 7;
            this.buttonRouteDtls.Text = "Details";
            this.buttonRouteDtls.UseVisualStyleBackColor = true;
            this.buttonRouteDtls.Click += new System.EventHandler(this.buttonRouteDtls_Click);
            // 
            // buttonActivityDtls
            // 
            this.buttonActivityDtls.Location = new System.Drawing.Point(592, 5);
            this.buttonActivityDtls.Name = "buttonActivityDtls";
            this.buttonActivityDtls.Size = new System.Drawing.Size(57, 21);
            this.buttonActivityDtls.TabIndex = 8;
            this.buttonActivityDtls.Text = "Details";
            this.buttonActivityDtls.UseVisualStyleBackColor = true;
            this.buttonActivityDtls.Click += new System.EventHandler(this.buttonActivityDtls_Click_1);
            // 
            // buttonResume
            // 
            this.buttonResume.Location = new System.Drawing.Point(508, 288);
            this.buttonResume.Name = "buttonResume";
            this.buttonResume.Size = new System.Drawing.Size(60, 23);
            this.buttonResume.TabIndex = 6;
            this.buttonResume.Text = "Resume";
            this.buttonResume.UseVisualStyleBackColor = true;
            this.buttonResume.Click += new System.EventHandler(this.buttonResume_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 334);
            this.Controls.Add(this.buttonActivityDtls);
            this.Controls.Add(this.buttonRouteDtls);
            this.Controls.Add(this.buttonResume);
            this.Controls.Add(this.buttonOptions);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonJoin);
            this.Controls.Add(this.buttonRemove);
            this.Controls.Add(this.buttonAddFolder);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.checkBoxWarnings);
            this.Controls.Add(this.checkBoxHost);
            this.Controls.Add(this.checkBoxFullScreen);
            this.Controls.Add(this.listBoxActivities);
            this.Controls.Add(this.listBoxFolder);
            this.Controls.Add(this.listBoxRoutes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Open Rails";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxRoutes;
        private System.Windows.Forms.ListBox listBoxActivities;
        private System.Windows.Forms.CheckBox checkBoxFullScreen;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox listBoxFolder;
        private System.Windows.Forms.Label label5;
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
    }
}