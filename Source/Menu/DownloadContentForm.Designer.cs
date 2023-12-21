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
            this.components = new System.ComponentModel.Container();
            this.dataGridViewDownloadContent = new System.Windows.Forms.DataGridView();
            this.DataGridRouteName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataGridDateInstalled = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataGridUrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.InstallPathLabel = new System.Windows.Forms.Label();
            this.InstallPathTextBox = new System.Windows.Forms.TextBox();
            this.InstallPathButton = new System.Windows.Forms.Button();
            this.InstallPathDirectoryEntry = new System.DirectoryServices.DirectoryEntry();
            this.DownloadContentButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDownloadContent)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewDownloadContent
            // 
            this.dataGridViewDownloadContent.AllowUserToAddRows = false;
            this.dataGridViewDownloadContent.AllowUserToDeleteRows = false;
            this.dataGridViewDownloadContent.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewDownloadContent.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDownloadContent.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DataGridRouteName,
            this.DataGridDateInstalled,
            this.DataGridUrl});
            this.dataGridViewDownloadContent.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridViewDownloadContent.Location = new System.Drawing.Point(24, 29);
            this.dataGridViewDownloadContent.Name = "dataGridViewDownloadContent";
            this.dataGridViewDownloadContent.ReadOnly = true;
            this.dataGridViewDownloadContent.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewDownloadContent.Size = new System.Drawing.Size(608, 293);
            this.dataGridViewDownloadContent.TabIndex = 0;
            this.dataGridViewDownloadContent.SelectionChanged += new System.EventHandler(this.dataGridViewDownloadContent_SelectionChanged);
            // 
            // Route
            // 
            this.DataGridRouteName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DataGridRouteName.HeaderText = "Route";
            this.DataGridRouteName.Name = "Route";
            this.DataGridRouteName.ReadOnly = true;
            this.DataGridRouteName.Width = 61;
            // 
            // Installed
            // 
            this.DataGridDateInstalled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DataGridDateInstalled.HeaderText = "Installed";
            this.DataGridDateInstalled.Name = "Installed";
            this.DataGridDateInstalled.ReadOnly = true;
            this.DataGridDateInstalled.Width = 71;
            // 
            // Url
            // 
            this.DataGridUrl.HeaderText = "Url";
            this.DataGridUrl.Name = "Url";
            this.DataGridUrl.ReadOnly = true;
            this.DataGridUrl.Width = 999;
            // 
            // InstallPathLabel
            // 
            this.InstallPathLabel.AutoSize = true;
            this.InstallPathLabel.Location = new System.Drawing.Point(21, 353);
            this.InstallPathLabel.Name = "InstallPathLabel";
            this.InstallPathLabel.Size = new System.Drawing.Size(62, 13);
            this.InstallPathLabel.TabIndex = 1;
            this.InstallPathLabel.Text = "Install Path:";
            // 
            // InstallPathTextBox
            // 
            this.InstallPathTextBox.Location = new System.Drawing.Point(89, 350);
            this.InstallPathTextBox.Name = "InstallPathTextBox";
            this.InstallPathTextBox.Size = new System.Drawing.Size(445, 20);
            this.InstallPathTextBox.TabIndex = 2;
            // 
            // InstallPathButton
            // 
            this.InstallPathButton.Location = new System.Drawing.Point(557, 350);
            this.InstallPathButton.Name = "InstallPathButton";
            this.InstallPathButton.Size = new System.Drawing.Size(75, 23);
            this.InstallPathButton.TabIndex = 3;
            this.InstallPathButton.Text = "Browse...";
            this.InstallPathButton.UseVisualStyleBackColor = true;
            this.InstallPathButton.Click += new System.EventHandler(this.InstallPathButton_Click);
            // 
            // DownloadContentButton
            // 
            this.DownloadContentButton.Location = new System.Drawing.Point(24, 390);
            this.DownloadContentButton.Name = "DownloadContentButton";
            this.DownloadContentButton.Size = new System.Drawing.Size(75, 23);
            this.DownloadContentButton.TabIndex = 4;
            this.DownloadContentButton.Text = "Download";
            this.DownloadContentButton.UseVisualStyleBackColor = true;
            this.DownloadContentButton.Click += new System.EventHandler(this.DownloadContentButton_Click);
            // 
            // DownloadContentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(667, 443);
            this.Controls.Add(this.DownloadContentButton);
            this.Controls.Add(this.InstallPathButton);
            this.Controls.Add(this.InstallPathTextBox);
            this.Controls.Add(this.InstallPathLabel);
            this.Controls.Add(this.dataGridViewDownloadContent);
            this.Name = "DownloadContentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download Content";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDownloadContent)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DataGridView dataGridViewDownloadContent;
        private DataGridViewTextBoxColumn DataGridRouteName;
        private DataGridViewTextBoxColumn DataGridDateInstalled;
        private DataGridViewTextBoxColumn DataGridUrl;
        private Label InstallPathLabel;
        private TextBox InstallPathTextBox;
        private Button InstallPathButton;
        private System.DirectoryServices.DirectoryEntry InstallPathDirectoryEntry;
        private Button DownloadContentButton;
    }
}
