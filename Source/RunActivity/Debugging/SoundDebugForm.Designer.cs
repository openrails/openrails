// COPYRIGHT 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

namespace ORTS.Debugging
{
    partial class SoundDebugForm
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
            this.inactiveSoundList = new System.Windows.Forms.TreeView();
            this.activeSoundList = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.streamProperties = new System.Windows.Forms.GroupBox();
            this.sound3D = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.smsVolume = new System.Windows.Forms.TextBox();
            this.distance = new System.Windows.Forms.TextBox();
            this.variable3 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.variable2 = new System.Windows.Forms.TextBox();
            this.variable1 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.speed = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cache = new System.Windows.Forms.TextBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.streamProperties.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // inactiveSoundList
            // 
            this.inactiveSoundList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inactiveSoundList.Location = new System.Drawing.Point(0, 0);
            this.inactiveSoundList.Name = "inactiveSoundList";
            this.inactiveSoundList.Size = new System.Drawing.Size(288, 289);
            this.inactiveSoundList.TabIndex = 3;
            this.inactiveSoundList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.inactiveSoundList_KeyPress);
            this.inactiveSoundList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inactiveSoundList_KeyDown);
            // 
            // activeSoundList
            // 
            this.activeSoundList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.activeSoundList.Location = new System.Drawing.Point(0, 0);
            this.activeSoundList.Name = "activeSoundList";
            this.activeSoundList.Size = new System.Drawing.Size(288, 519);
            this.activeSoundList.TabIndex = 4;
            this.activeSoundList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.activeSoundList_KeyPress);
            this.activeSoundList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.activeSoundList_KeyDown);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.activeSoundList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.inactiveSoundList);
            this.splitContainer1.Size = new System.Drawing.Size(288, 812);
            this.splitContainer1.SplitterDistance = 519;
            this.splitContainer1.TabIndex = 5;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer2.Size = new System.Drawing.Size(434, 812);
            this.splitContainer2.SplitterDistance = 142;
            this.splitContainer2.TabIndex = 6;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.streamProperties);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.cache);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(142, 812);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Current SMS";
            // 
            // streamProperties
            // 
            this.streamProperties.Controls.Add(this.sound3D);
            this.streamProperties.Controls.Add(this.tableLayoutPanel2);
            this.streamProperties.Location = new System.Drawing.Point(6, 19);
            this.streamProperties.Name = "streamProperties";
            this.streamProperties.Size = new System.Drawing.Size(133, 200);
            this.streamProperties.TabIndex = 2;
            this.streamProperties.TabStop = false;
            this.streamProperties.Text = "Stream Properties";
            // 
            // sound3D
            // 
            this.sound3D.AutoSize = true;
            this.sound3D.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sound3D.Enabled = false;
            this.sound3D.Location = new System.Drawing.Point(3, 180);
            this.sound3D.Name = "sound3D";
            this.sound3D.Size = new System.Drawing.Size(127, 17);
            this.sound3D.TabIndex = 3;
            this.sound3D.Text = "3D";
            this.sound3D.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.smsVolume, 1, 5);
            this.tableLayoutPanel2.Controls.Add(this.distance, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.variable3, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.variable2, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.variable1, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.speed, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.label6, 0, 5);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 6;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(127, 158);
            this.tableLayoutPanel2.TabIndex = 6;
            // 
            // smsVolume
            // 
            this.smsVolume.Dock = System.Windows.Forms.DockStyle.Fill;
            this.smsVolume.Location = new System.Drawing.Point(73, 133);
            this.smsVolume.Name = "smsVolume";
            this.smsVolume.ReadOnly = true;
            this.smsVolume.Size = new System.Drawing.Size(51, 20);
            this.smsVolume.TabIndex = 0;
            // 
            // distance
            // 
            this.distance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.distance.Location = new System.Drawing.Point(73, 107);
            this.distance.Name = "distance";
            this.distance.ReadOnly = true;
            this.distance.Size = new System.Drawing.Size(51, 20);
            this.distance.TabIndex = 8;
            // 
            // variable3
            // 
            this.variable3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable3.Location = new System.Drawing.Point(73, 81);
            this.variable3.Name = "variable3";
            this.variable3.ReadOnly = true;
            this.variable3.Size = new System.Drawing.Size(51, 20);
            this.variable3.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Speed";
            // 
            // variable2
            // 
            this.variable2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable2.Location = new System.Drawing.Point(73, 55);
            this.variable2.Name = "variable2";
            this.variable2.ReadOnly = true;
            this.variable2.Size = new System.Drawing.Size(51, 20);
            this.variable2.TabIndex = 6;
            // 
            // variable1
            // 
            this.variable1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable1.Location = new System.Drawing.Point(73, 29);
            this.variable1.Name = "variable1";
            this.variable1.ReadOnly = true;
            this.variable1.Size = new System.Drawing.Size(51, 20);
            this.variable1.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 58);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Variable 2";
            // 
            // speed
            // 
            this.speed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speed.Location = new System.Drawing.Point(73, 3);
            this.speed.Name = "speed";
            this.speed.ReadOnly = true;
            this.speed.Size = new System.Drawing.Size(51, 20);
            this.speed.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 84);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Variable 3";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Variable 1";
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 110);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Distance";
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 137);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Volume";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 237);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(116, 26);
            this.label7.TabIndex = 4;
            this.label7.Text = "Total number of sound \r\nfiles in cache:";
            // 
            // cache
            // 
            this.cache.Location = new System.Drawing.Point(9, 266);
            this.cache.Name = "cache";
            this.cache.ReadOnly = true;
            this.cache.Size = new System.Drawing.Size(51, 20);
            this.cache.TabIndex = 0;
            this.cache.Text = "0";
            this.cache.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // SoundDebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 812);
            this.Controls.Add(this.splitContainer2);
            this.Name = "SoundDebugForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "SoundDebugForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SoundDebugForm_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.streamProperties.ResumeLayout(false);
            this.streamProperties.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView inactiveSoundList;
        private System.Windows.Forms.TreeView activeSoundList;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.CheckBox sound3D;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox smsVolume;
        private System.Windows.Forms.GroupBox streamProperties;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox distance;
        private System.Windows.Forms.TextBox variable3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox variable2;
        private System.Windows.Forms.TextBox variable1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox speed;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox cache;
    }
}