// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System.Windows.Forms;

namespace ORTS
{
    partial class ContentForm : Form
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
            this.dataGridViewAutoInstall = new System.Windows.Forms.DataGridView();
            this.dgvTextBoxColumnAutoInstallRoute = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvTextBoxColumnAutoInstallInstalled = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvTextBoxColumnAutoInstallUrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.labelAutoInstallPath = new System.Windows.Forms.Label();
            this.textBoxAutoInstallPath = new System.Windows.Forms.TextBox();
            this.buttonAutoInstallBrowse = new System.Windows.Forms.Button();
            this.buttonAutoInstallInstall = new System.Windows.Forms.Button();
            this.pictureBoxAutoInstallRoute = new System.Windows.Forms.PictureBox();
            this.textBoxAutoInstallRoute = new System.Windows.Forms.RichTextBox();
            this.buttonAutoInstallInfo = new System.Windows.Forms.Button();
            this.buttonAutoInstallDelete = new System.Windows.Forms.Button();
            this.buttonAutoInstallUpdate = new System.Windows.Forms.Button();
            this.tabControlContent = new System.Windows.Forms.TabControl();
            this.tabPageAutoInstall = new System.Windows.Forms.TabPage();
            this.tabPageManuallyInstall = new System.Windows.Forms.TabPage();
            this.labelManualInstallContent = new System.Windows.Forms.Label();
            this.buttonManualInstallDelete = new System.Windows.Forms.Button();
            this.groupBoxManualInstall = new System.Windows.Forms.GroupBox();
            this.buttonManualInstallBrowse = new System.Windows.Forms.Button();
            this.textBoxManualInstallPath = new System.Windows.Forms.TextBox();
            this.labelManualInstall20 = new System.Windows.Forms.Label();
            this.labelManualInstall22 = new System.Windows.Forms.Label();
            this.textBoxManualInstallRoute = new System.Windows.Forms.TextBox();
            this.buttonManualInstallAdd = new System.Windows.Forms.Button();
            this.dataGridViewManualInstall = new System.Windows.Forms.DataGridView();
            this.dgvTextBoxColumnManualInstallRoute = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvTextBoxColumnManualInstallPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAutoInstall)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAutoInstallRoute)).BeginInit();
            this.tabControlContent.SuspendLayout();
            this.tabPageAutoInstall.SuspendLayout();
            this.tabPageManuallyInstall.SuspendLayout();
            this.groupBoxManualInstall.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewManualInstall)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewAutoInstall
            // 
            this.dataGridViewAutoInstall.AllowUserToAddRows = false;
            this.dataGridViewAutoInstall.AllowUserToDeleteRows = false;
            this.dataGridViewAutoInstall.AllowUserToResizeColumns = false;
            this.dataGridViewAutoInstall.AllowUserToResizeRows = false;
            this.dataGridViewAutoInstall.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dataGridViewAutoInstall.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewAutoInstall.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewAutoInstall.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvTextBoxColumnAutoInstallRoute,
            this.dgvTextBoxColumnAutoInstallInstalled,
            this.dgvTextBoxColumnAutoInstallUrl});
            this.dataGridViewAutoInstall.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridViewAutoInstall.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewAutoInstall.MultiSelect = false;
            this.dataGridViewAutoInstall.Name = "dataGridViewAutoInstall";
            this.dataGridViewAutoInstall.ReadOnly = true;
            this.dataGridViewAutoInstall.RowHeadersVisible = false;
            this.dataGridViewAutoInstall.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewAutoInstall.Size = new System.Drawing.Size(588, 222);
            this.dataGridViewAutoInstall.TabIndex = 0;
            this.dataGridViewAutoInstall.SelectionChanged += new System.EventHandler(this.dataGridViewAutoInstall_SelectionChanged);
            this.dataGridViewAutoInstall.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridViewAutoInstall_KeyDown);
            // 
            // dgvTextBoxColumnAutoInstallRoute
            // 
            this.dgvTextBoxColumnAutoInstallRoute.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dgvTextBoxColumnAutoInstallRoute.HeaderText = "Route";
            this.dgvTextBoxColumnAutoInstallRoute.Name = "dgvTextBoxColumnAutoInstallRoute";
            this.dgvTextBoxColumnAutoInstallRoute.ReadOnly = true;
            this.dgvTextBoxColumnAutoInstallRoute.Width = 61;
            // 
            // dgvTextBoxColumnAutoInstallInstalled
            // 
            this.dgvTextBoxColumnAutoInstallInstalled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dgvTextBoxColumnAutoInstallInstalled.HeaderText = "Installed";
            this.dgvTextBoxColumnAutoInstallInstalled.Name = "dgvTextBoxColumnAutoInstallInstalled";
            this.dgvTextBoxColumnAutoInstallInstalled.ReadOnly = true;
            this.dgvTextBoxColumnAutoInstallInstalled.Width = 71;
            // 
            // dgvTextBoxColumnAutoInstallUrl
            // 
            this.dgvTextBoxColumnAutoInstallUrl.HeaderText = "Url";
            this.dgvTextBoxColumnAutoInstallUrl.Name = "dgvTextBoxColumnAutoInstallUrl";
            this.dgvTextBoxColumnAutoInstallUrl.ReadOnly = true;
            this.dgvTextBoxColumnAutoInstallUrl.Width = 45;
            // 
            // labelAutoInstallPath
            // 
            this.labelAutoInstallPath.AutoSize = true;
            this.labelAutoInstallPath.Location = new System.Drawing.Point(6, 345);
            this.labelAutoInstallPath.Name = "labelAutoInstallPath";
            this.labelAutoInstallPath.Size = new System.Drawing.Size(62, 13);
            this.labelAutoInstallPath.TabIndex = 1;
            this.labelAutoInstallPath.Text = "Install Path:";
            // 
            // textBoxAutoInstallPath
            // 
            this.textBoxAutoInstallPath.Location = new System.Drawing.Point(74, 342);
            this.textBoxAutoInstallPath.Name = "textBoxAutoInstallPath";
            this.textBoxAutoInstallPath.Size = new System.Drawing.Size(445, 20);
            this.textBoxAutoInstallPath.TabIndex = 2;
            this.textBoxAutoInstallPath.TextChanged += new System.EventHandler(this.textBoxAutoInstallPath_TextChanged);
            // 
            // buttonAutoInstallBrowse
            // 
            this.buttonAutoInstallBrowse.Location = new System.Drawing.Point(521, 340);
            this.buttonAutoInstallBrowse.Name = "buttonAutoInstallBrowse";
            this.buttonAutoInstallBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonAutoInstallBrowse.TabIndex = 3;
            this.buttonAutoInstallBrowse.Text = "Browse...";
            this.buttonAutoInstallBrowse.UseVisualStyleBackColor = true;
            this.buttonAutoInstallBrowse.Click += new System.EventHandler(this.buttonAutoInstallBrowse_Click);
            // 
            // buttonAutoInstallInstall
            // 
            this.buttonAutoInstallInstall.Enabled = false;
            this.buttonAutoInstallInstall.Location = new System.Drawing.Point(89, 371);
            this.buttonAutoInstallInstall.Name = "buttonAutoInstallInstall";
            this.buttonAutoInstallInstall.Size = new System.Drawing.Size(75, 23);
            this.buttonAutoInstallInstall.TabIndex = 5;
            this.buttonAutoInstallInstall.Text = "Install";
            this.buttonAutoInstallInstall.UseVisualStyleBackColor = true;
            this.buttonAutoInstallInstall.Click += new System.EventHandler(this.buttonAutoInstallInstall_Click);
            // 
            // pictureBoxAutoInstallRoute
            // 
            this.pictureBoxAutoInstallRoute.Location = new System.Drawing.Point(492, 234);
            this.pictureBoxAutoInstallRoute.Name = "pictureBoxAutoInstallRoute";
            this.pictureBoxAutoInstallRoute.Size = new System.Drawing.Size(102, 98);
            this.pictureBoxAutoInstallRoute.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxAutoInstallRoute.TabIndex = 5;
            this.pictureBoxAutoInstallRoute.TabStop = false;
            // 
            // textBoxAutoInstallRoute
            // 
            this.textBoxAutoInstallRoute.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxAutoInstallRoute.Location = new System.Drawing.Point(6, 234);
            this.textBoxAutoInstallRoute.Name = "textBoxAutoInstallRoute";
            this.textBoxAutoInstallRoute.ReadOnly = true;
            this.textBoxAutoInstallRoute.Size = new System.Drawing.Size(480, 98);
            this.textBoxAutoInstallRoute.TabIndex = 9;
            this.textBoxAutoInstallRoute.TabStop = false;
            this.textBoxAutoInstallRoute.Text = "";
            // 
            // buttonAutoInstallInfo
            // 
            this.buttonAutoInstallInfo.Location = new System.Drawing.Point(8, 371);
            this.buttonAutoInstallInfo.Name = "buttonAutoInstallInfo";
            this.buttonAutoInstallInfo.Size = new System.Drawing.Size(75, 23);
            this.buttonAutoInstallInfo.TabIndex = 4;
            this.buttonAutoInstallInfo.Text = "Info";
            this.buttonAutoInstallInfo.UseVisualStyleBackColor = true;
            this.buttonAutoInstallInfo.Click += new System.EventHandler(this.buttonAutoInstallInfo_Click);
            // 
            // buttonAutoInstallDelete
            // 
            this.buttonAutoInstallDelete.Enabled = false;
            this.buttonAutoInstallDelete.Location = new System.Drawing.Point(251, 371);
            this.buttonAutoInstallDelete.Name = "buttonAutoInstallDelete";
            this.buttonAutoInstallDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonAutoInstallDelete.TabIndex = 7;
            this.buttonAutoInstallDelete.Text = "Delete";
            this.buttonAutoInstallDelete.UseVisualStyleBackColor = true;
            this.buttonAutoInstallDelete.Click += new System.EventHandler(this.buttonAutoInstallDelete_Click);
            // 
            // buttonAutoInstallUpdate
            // 
            this.buttonAutoInstallUpdate.Location = new System.Drawing.Point(170, 371);
            this.buttonAutoInstallUpdate.Name = "buttonAutoInstallUpdate";
            this.buttonAutoInstallUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonAutoInstallUpdate.TabIndex = 6;
            this.buttonAutoInstallUpdate.Text = "Update";
            this.buttonAutoInstallUpdate.UseVisualStyleBackColor = true;
            this.buttonAutoInstallUpdate.Click += new System.EventHandler(this.buttonAutoInstallUpdate_Click);
            // 
            // tabControlContent
            // 
            this.tabControlContent.Controls.Add(this.tabPageAutoInstall);
            this.tabControlContent.Controls.Add(this.tabPageManuallyInstall);
            this.tabControlContent.Location = new System.Drawing.Point(13, 12);
            this.tabControlContent.Name = "tabControlContent";
            this.tabControlContent.SelectedIndex = 0;
            this.tabControlContent.Size = new System.Drawing.Size(610, 428);
            this.tabControlContent.TabIndex = 0;
            this.tabControlContent.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControlContent_Selecting);
            // 
            // tabPageAutoInstall
            // 
            this.tabPageAutoInstall.Controls.Add(this.buttonAutoInstallInfo);
            this.tabPageAutoInstall.Controls.Add(this.buttonAutoInstallInstall);
            this.tabPageAutoInstall.Controls.Add(this.buttonAutoInstallBrowse);
            this.tabPageAutoInstall.Controls.Add(this.pictureBoxAutoInstallRoute);
            this.tabPageAutoInstall.Controls.Add(this.textBoxAutoInstallRoute);
            this.tabPageAutoInstall.Controls.Add(this.buttonAutoInstallDelete);
            this.tabPageAutoInstall.Controls.Add(this.textBoxAutoInstallPath);
            this.tabPageAutoInstall.Controls.Add(this.buttonAutoInstallUpdate);
            this.tabPageAutoInstall.Controls.Add(this.labelAutoInstallPath);
            this.tabPageAutoInstall.Controls.Add(this.dataGridViewAutoInstall);
            this.tabPageAutoInstall.Location = new System.Drawing.Point(4, 22);
            this.tabPageAutoInstall.Name = "tabPageAutoInstall";
            this.tabPageAutoInstall.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAutoInstall.Size = new System.Drawing.Size(602, 402);
            this.tabPageAutoInstall.TabIndex = 0;
            this.tabPageAutoInstall.Text = "Auto Installed";
            this.tabPageAutoInstall.UseVisualStyleBackColor = true;
            // 
            // tabPageManuallyInstall
            // 
            this.tabPageManuallyInstall.Controls.Add(this.labelManualInstallContent);
            this.tabPageManuallyInstall.Controls.Add(this.buttonManualInstallDelete);
            this.tabPageManuallyInstall.Controls.Add(this.groupBoxManualInstall);
            this.tabPageManuallyInstall.Controls.Add(this.buttonManualInstallAdd);
            this.tabPageManuallyInstall.Controls.Add(this.dataGridViewManualInstall);
            this.tabPageManuallyInstall.Location = new System.Drawing.Point(4, 22);
            this.tabPageManuallyInstall.Name = "tabPageManuallyInstall";
            this.tabPageManuallyInstall.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageManuallyInstall.Size = new System.Drawing.Size(602, 402);
            this.tabPageManuallyInstall.TabIndex = 10;
            this.tabPageManuallyInstall.Text = "Manually Installed";
            this.tabPageManuallyInstall.UseVisualStyleBackColor = true;
            // 
            // labelManualInstallContent
            // 
            this.labelManualInstallContent.AutoSize = true;
            this.labelManualInstallContent.ForeColor = System.Drawing.SystemColors.Highlight;
            this.labelManualInstallContent.Location = new System.Drawing.Point(6, 6);
            this.labelManualInstallContent.Margin = new System.Windows.Forms.Padding(3);
            this.labelManualInstallContent.MaximumSize = new System.Drawing.Size(590, 0);
            this.labelManualInstallContent.Name = "labelManualInstallContent";
            this.labelManualInstallContent.Size = new System.Drawing.Size(539, 13);
            this.labelManualInstallContent.TabIndex = 3;
            this.labelManualInstallContent.Text = "Installation profiles tell Open Rails where to look for game content. Add each fu" +
    "ll and mini-route MSTS installation.";
            // 
            // buttonManualInstallDelete
            // 
            this.buttonManualInstallDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonManualInstallDelete.Location = new System.Drawing.Point(6, 364);
            this.buttonManualInstallDelete.Name = "buttonManualInstallDelete";
            this.buttonManualInstallDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonManualInstallDelete.TabIndex = 2;
            this.buttonManualInstallDelete.Text = "Delete";
            this.buttonManualInstallDelete.UseVisualStyleBackColor = true;
            this.buttonManualInstallDelete.Click += new System.EventHandler(this.buttonManualInstallDelete_Click);
            // 
            // groupBoxManualInstall
            // 
            this.groupBoxManualInstall.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxManualInstall.Controls.Add(this.buttonManualInstallBrowse);
            this.groupBoxManualInstall.Controls.Add(this.textBoxManualInstallPath);
            this.groupBoxManualInstall.Controls.Add(this.labelManualInstall20);
            this.groupBoxManualInstall.Controls.Add(this.labelManualInstall22);
            this.groupBoxManualInstall.Controls.Add(this.textBoxManualInstallRoute);
            this.groupBoxManualInstall.Location = new System.Drawing.Point(87, 317);
            this.groupBoxManualInstall.Name = "groupBoxManualInstall";
            this.groupBoxManualInstall.Size = new System.Drawing.Size(509, 79);
            this.groupBoxManualInstall.TabIndex = 3;
            this.groupBoxManualInstall.TabStop = false;
            this.groupBoxManualInstall.Text = "Installation profile";
            // 
            // buttonManualInstallBrowse
            // 
            this.buttonManualInstallBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonManualInstallBrowse.Location = new System.Drawing.Point(428, 47);
            this.buttonManualInstallBrowse.Name = "buttonManualInstallBrowse";
            this.buttonManualInstallBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonManualInstallBrowse.TabIndex = 2;
            this.buttonManualInstallBrowse.Text = "Browse...";
            this.buttonManualInstallBrowse.UseVisualStyleBackColor = true;
            this.buttonManualInstallBrowse.Click += new System.EventHandler(this.buttonManualInstallBrowse_Click);
            // 
            // textBoxManualInstallPath
            // 
            this.textBoxManualInstallPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxManualInstallPath.Location = new System.Drawing.Point(50, 49);
            this.textBoxManualInstallPath.Name = "textBoxManualInstallPath";
            this.textBoxManualInstallPath.Size = new System.Drawing.Size(372, 20);
            this.textBoxManualInstallPath.TabIndex = 1;
            this.textBoxManualInstallPath.TextChanged += new System.EventHandler(this.textBoxManualInstallPath_TextChanged);
            // 
            // labelManualInstall20
            // 
            this.labelManualInstall20.AutoSize = true;
            this.labelManualInstall20.Location = new System.Drawing.Point(6, 24);
            this.labelManualInstall20.Name = "labelManualInstall20";
            this.labelManualInstall20.Size = new System.Drawing.Size(39, 13);
            this.labelManualInstall20.TabIndex = 3;
            this.labelManualInstall20.Text = "Route:";
            // 
            // labelManualInstall22
            // 
            this.labelManualInstall22.AutoSize = true;
            this.labelManualInstall22.Location = new System.Drawing.Point(6, 52);
            this.labelManualInstall22.Name = "labelManualInstall22";
            this.labelManualInstall22.Size = new System.Drawing.Size(32, 13);
            this.labelManualInstall22.TabIndex = 0;
            this.labelManualInstall22.Text = "Path:";
            // 
            // textBoxManualInstallRoute
            // 
            this.textBoxManualInstallRoute.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxManualInstallRoute.Location = new System.Drawing.Point(50, 21);
            this.textBoxManualInstallRoute.Name = "textBoxManualInstallRoute";
            this.textBoxManualInstallRoute.Size = new System.Drawing.Size(453, 20);
            this.textBoxManualInstallRoute.TabIndex = 4;
            this.textBoxManualInstallRoute.TextChanged += new System.EventHandler(this.textBoxManualInstallRoute_TextChanged);
            // 
            // buttonManualInstallAdd
            // 
            this.buttonManualInstallAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonManualInstallAdd.Location = new System.Drawing.Point(6, 336);
            this.buttonManualInstallAdd.Name = "buttonManualInstallAdd";
            this.buttonManualInstallAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonManualInstallAdd.TabIndex = 1;
            this.buttonManualInstallAdd.Text = "Add...";
            this.buttonManualInstallAdd.UseVisualStyleBackColor = true;
            this.buttonManualInstallAdd.Click += new System.EventHandler(this.buttonManualInstallAdd_Click);
            // 
            // dataGridViewManualInstall
            // 
            this.dataGridViewManualInstall.AllowUserToAddRows = false;
            this.dataGridViewManualInstall.AllowUserToDeleteRows = false;
            this.dataGridViewManualInstall.AllowUserToResizeColumns = false;
            this.dataGridViewManualInstall.AllowUserToResizeRows = false;
            this.dataGridViewManualInstall.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dataGridViewManualInstall.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewManualInstall.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewManualInstall.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvTextBoxColumnManualInstallRoute,
            this.dgvTextBoxColumnManualInstallPath});
            this.dataGridViewManualInstall.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridViewManualInstall.Location = new System.Drawing.Point(6, 38);
            this.dataGridViewManualInstall.MultiSelect = false;
            this.dataGridViewManualInstall.Name = "dataGridViewManualInstall";
            this.dataGridViewManualInstall.ReadOnly = true;
            this.dataGridViewManualInstall.RowHeadersVisible = false;
            this.dataGridViewManualInstall.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewManualInstall.Size = new System.Drawing.Size(588, 271);
            this.dataGridViewManualInstall.TabIndex = 0;
            this.dataGridViewManualInstall.SelectionChanged += new System.EventHandler(this.dataGridViewManualInstall_SelectionChanged);
            this.dataGridViewManualInstall.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridViewManualInstall_KeyDown);
            // 
            // dgvTextBoxColumnManualInstallRoute
            // 
            this.dgvTextBoxColumnManualInstallRoute.HeaderText = "Route";
            this.dgvTextBoxColumnManualInstallRoute.Name = "dgvTextBoxColumnManualInstallRoute";
            this.dgvTextBoxColumnManualInstallRoute.ReadOnly = true;
            this.dgvTextBoxColumnManualInstallRoute.Width = 61;
            // 
            // dgvTextBoxColumnManualInstallPath
            // 
            this.dgvTextBoxColumnManualInstallPath.HeaderText = "Path";
            this.dgvTextBoxColumnManualInstallPath.Name = "dgvTextBoxColumnManualInstallPath";
            this.dgvTextBoxColumnManualInstallPath.ReadOnly = true;
            this.dgvTextBoxColumnManualInstallPath.Width = 54;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(544, 442);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 12;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(463, 442);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 11;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // ContentForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 481);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.tabControlContent);
            this.Location = new System.Drawing.Point(13, 12);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ContentForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Content";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DownloadContentForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAutoInstall)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAutoInstallRoute)).EndInit();
            this.tabControlContent.ResumeLayout(false);
            this.tabPageAutoInstall.ResumeLayout(false);
            this.tabPageAutoInstall.PerformLayout();
            this.tabPageManuallyInstall.ResumeLayout(false);
            this.tabPageManuallyInstall.PerformLayout();
            this.groupBoxManualInstall.ResumeLayout(false);
            this.groupBoxManualInstall.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewManualInstall)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DataGridView dataGridViewAutoInstall;
        private Label labelAutoInstallPath;
        private TextBox textBoxAutoInstallPath;
        private Button buttonAutoInstallBrowse;
        private Button buttonAutoInstallInstall;
        private PictureBox pictureBoxAutoInstallRoute;
        private RichTextBox textBoxAutoInstallRoute;
        private Button buttonAutoInstallInfo;
        private Button buttonAutoInstallDelete;
        private Button buttonAutoInstallUpdate;
        private TabControl tabControlContent;
        private TabPage tabPageAutoInstall;
        private TabPage tabPageManuallyInstall;
        private Label labelManualInstallContent;
        private Button buttonManualInstallDelete;
        private GroupBox groupBoxManualInstall;
        private Button buttonManualInstallBrowse;
        private TextBox textBoxManualInstallPath;
        private Label labelManualInstall20;
        private Label labelManualInstall22;
        public TextBox textBoxManualInstallRoute;
        private Button buttonManualInstallAdd;
        private DataGridView dataGridViewManualInstall;
        private DataGridViewTextBoxColumn dgvTextBoxColumnAutoInstallRoute;
        private DataGridViewTextBoxColumn dgvTextBoxColumnAutoInstallInstalled;
        private DataGridViewTextBoxColumn dgvTextBoxColumnAutoInstallUrl;
        private DataGridViewTextBoxColumn dgvTextBoxColumnManualInstallRoute;
        private DataGridViewTextBoxColumn dgvTextBoxColumnManualInstallPath;
        private Button buttonCancel;
        private Button buttonOK;
    }
}
