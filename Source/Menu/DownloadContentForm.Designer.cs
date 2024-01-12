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
    partial class DownloadContentForm : Form
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
            this.dataGridViewDownloadContent = new System.Windows.Forms.DataGridView();
            this.Route = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Installed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Url = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.InstallPathLabel = new System.Windows.Forms.Label();
            this.InstallPathTextBox = new System.Windows.Forms.TextBox();
            this.InstallPathButton = new System.Windows.Forms.Button();
            this.InstallPathDirectoryEntry = new System.DirectoryServices.DirectoryEntry();
            this.DownloadContentButton = new System.Windows.Forms.Button();
            this.pictureBoxRoute = new System.Windows.Forms.PictureBox();
            this.textBoxRoute = new System.Windows.Forms.RichTextBox();
            this.infoButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDownloadContent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRoute)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewDownloadContent
            // 
            this.dataGridViewDownloadContent.AllowUserToAddRows = false;
            this.dataGridViewDownloadContent.AllowUserToDeleteRows = false;
            this.dataGridViewDownloadContent.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewDownloadContent.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDownloadContent.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Route,
            this.Installed,
            this.Url});
            this.dataGridViewDownloadContent.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridViewDownloadContent.Location = new System.Drawing.Point(3, 9);
            this.dataGridViewDownloadContent.Name = "dataGridViewDownloadContent";
            this.dataGridViewDownloadContent.ReadOnly = true;
            this.dataGridViewDownloadContent.RowHeadersVisible = false;
            this.dataGridViewDownloadContent.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewDownloadContent.Size = new System.Drawing.Size(394, 335);
            this.dataGridViewDownloadContent.TabIndex = 0;
            this.dataGridViewDownloadContent.SelectionChanged += new System.EventHandler(this.dataGridViewDownloadContent_SelectionChanged);
            // 
            // Route
            // 
            this.Route.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Route.HeaderText = "Route";
            this.Route.Name = "Route";
            this.Route.ReadOnly = true;
            this.Route.Width = 61;
            // 
            // Installed
            // 
            this.Installed.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Installed.HeaderText = "Installed";
            this.Installed.Name = "Installed";
            this.Installed.ReadOnly = true;
            this.Installed.Width = 71;
            // 
            // Url
            // 
            this.Url.HeaderText = "Url";
            this.Url.Name = "Url";
            this.Url.ReadOnly = true;
            this.Url.Width = 999;
            // 
            // InstallPathLabel
            // 
            this.InstallPathLabel.AutoSize = true;
            this.InstallPathLabel.Location = new System.Drawing.Point(21, 478);
            this.InstallPathLabel.Name = "InstallPathLabel";
            this.InstallPathLabel.Size = new System.Drawing.Size(62, 13);
            this.InstallPathLabel.TabIndex = 1;
            this.InstallPathLabel.Text = "Install Path:";
            // 
            // InstallPathTextBox
            // 
            this.InstallPathTextBox.Location = new System.Drawing.Point(89, 475);
            this.InstallPathTextBox.Name = "InstallPathTextBox";
            this.InstallPathTextBox.Size = new System.Drawing.Size(445, 20);
            this.InstallPathTextBox.TabIndex = 2;
            // 
            // InstallPathButton
            // 
            this.InstallPathButton.Location = new System.Drawing.Point(551, 473);
            this.InstallPathButton.Name = "InstallPathButton";
            this.InstallPathButton.Size = new System.Drawing.Size(75, 23);
            this.InstallPathButton.TabIndex = 3;
            this.InstallPathButton.Text = "Browse...";
            this.InstallPathButton.UseVisualStyleBackColor = true;
            this.InstallPathButton.Click += new System.EventHandler(this.InstallPathButton_Click);
            // 
            // DownloadContentButton
            // 
            this.DownloadContentButton.Enabled = false;
            this.DownloadContentButton.Location = new System.Drawing.Point(3, 507);
            this.DownloadContentButton.Name = "DownloadContentButton";
            this.DownloadContentButton.Size = new System.Drawing.Size(75, 23);
            this.DownloadContentButton.TabIndex = 4;
            this.DownloadContentButton.Text = "Install";
            this.DownloadContentButton.UseVisualStyleBackColor = true;
            this.DownloadContentButton.Click += new System.EventHandler(this.DownloadContentButton_Click);
            // 
            // pictureBoxRoute
            // 
            this.pictureBoxRoute.Location = new System.Drawing.Point(403, 9);
            this.pictureBoxRoute.Name = "pictureBoxRoute";
            this.pictureBoxRoute.Size = new System.Drawing.Size(417, 335);
            this.pictureBoxRoute.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxRoute.TabIndex = 5;
            this.pictureBoxRoute.TabStop = false;
            // 
            // textBoxRoute
            // 
            this.textBoxRoute.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRoute.Location = new System.Drawing.Point(3, 359);
            this.textBoxRoute.Name = "textBoxRoute";
            this.textBoxRoute.Size = new System.Drawing.Size(817, 99);
            this.textBoxRoute.TabIndex = 6;
            this.textBoxRoute.Text = "";
            // 
            // infoButton
            // 
            this.infoButton.Location = new System.Drawing.Point(745, 472);
            this.infoButton.Name = "infoButton";
            this.infoButton.Size = new System.Drawing.Size(75, 23);
            this.infoButton.TabIndex = 7;
            this.infoButton.Text = "Info";
            this.infoButton.UseVisualStyleBackColor = true;
            this.infoButton.Click += new System.EventHandler(this.InfoButton_Click);
            // 
            // startButton
            // 
            this.startButton.Enabled = false;
            this.startButton.Location = new System.Drawing.Point(89, 507);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 8;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // deleteButton
            // 
            this.deleteButton.Enabled = false;
            this.deleteButton.Location = new System.Drawing.Point(175, 507);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(75, 23);
            this.deleteButton.TabIndex = 9;
            this.deleteButton.Text = "Delete";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // DownloadContentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 542);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.infoButton);
            this.Controls.Add(this.textBoxRoute);
            this.Controls.Add(this.pictureBoxRoute);
            this.Controls.Add(this.DownloadContentButton);
            this.Controls.Add(this.InstallPathButton);
            this.Controls.Add(this.InstallPathTextBox);
            this.Controls.Add(this.InstallPathLabel);
            this.Controls.Add(this.dataGridViewDownloadContent);
            this.Name = "DownloadContentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download Content";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DownloadContentForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDownloadContent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRoute)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DataGridView dataGridViewDownloadContent;
        private Label InstallPathLabel;
        private TextBox InstallPathTextBox;
        private Button InstallPathButton;
        private System.DirectoryServices.DirectoryEntry InstallPathDirectoryEntry;
        private Button DownloadContentButton;
        private DataGridViewTextBoxColumn Route;
        private DataGridViewTextBoxColumn Installed;
        private DataGridViewTextBoxColumn Url;
        private PictureBox pictureBoxRoute;
        private RichTextBox textBoxRoute;
        private Button infoButton;
        private Button startButton;
        private Button deleteButton;
    }
}
