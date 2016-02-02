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

namespace Orts.Viewer3D.Debugging
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SoundDebugForm));
            this.inactiveSoundList = new System.Windows.Forms.TreeView();
            this.activeSoundList = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBoxActiveSounds = new System.Windows.Forms.GroupBox();
            this.groupBoxInactiveSounds = new System.Windows.Forms.GroupBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBoxCache = new System.Windows.Forms.GroupBox();
            this.groupBoxSelectedSound = new System.Windows.Forms.GroupBox();
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.alSources = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.waves = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBoxActiveSounds.SuspendLayout();
            this.groupBoxInactiveSounds.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBoxCache.SuspendLayout();
            this.groupBoxSelectedSound.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // inactiveSoundList
            // 
            this.inactiveSoundList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inactiveSoundList.Location = new System.Drawing.Point(3, 16);
            this.inactiveSoundList.Name = "inactiveSoundList";
            this.inactiveSoundList.Size = new System.Drawing.Size(276, 264);
            this.inactiveSoundList.TabIndex = 0;
            this.inactiveSoundList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inactiveSoundList_KeyDown);
            this.inactiveSoundList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.inactiveSoundList_KeyPress);
            // 
            // activeSoundList
            // 
            this.activeSoundList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.activeSoundList.Location = new System.Drawing.Point(3, 16);
            this.activeSoundList.Name = "activeSoundList";
            this.activeSoundList.Size = new System.Drawing.Size(276, 494);
            this.activeSoundList.TabIndex = 0;
            this.activeSoundList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.activeSoundList_KeyDown);
            this.activeSoundList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.activeSoundList_KeyPress);
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
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxActiveSounds);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(3);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBoxInactiveSounds);
            this.splitContainer1.Panel2.Padding = new System.Windows.Forms.Padding(3);
            this.splitContainer1.Size = new System.Drawing.Size(288, 812);
            this.splitContainer1.SplitterDistance = 519;
            this.splitContainer1.TabIndex = 5;
            // 
            // groupBoxActiveSounds
            // 
            this.groupBoxActiveSounds.Controls.Add(this.activeSoundList);
            this.groupBoxActiveSounds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxActiveSounds.Location = new System.Drawing.Point(3, 3);
            this.groupBoxActiveSounds.Name = "groupBoxActiveSounds";
            this.groupBoxActiveSounds.Size = new System.Drawing.Size(282, 513);
            this.groupBoxActiveSounds.TabIndex = 5;
            this.groupBoxActiveSounds.TabStop = false;
            this.groupBoxActiveSounds.Text = "Active sounds";
            // 
            // groupBoxInactiveSounds
            // 
            this.groupBoxInactiveSounds.Controls.Add(this.inactiveSoundList);
            this.groupBoxInactiveSounds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxInactiveSounds.Location = new System.Drawing.Point(3, 3);
            this.groupBoxInactiveSounds.Name = "groupBoxInactiveSounds";
            this.groupBoxInactiveSounds.Size = new System.Drawing.Size(282, 283);
            this.groupBoxInactiveSounds.TabIndex = 4;
            this.groupBoxInactiveSounds.TabStop = false;
            this.groupBoxInactiveSounds.Text = "Inactive sounds";
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
            this.splitContainer2.Panel1.Controls.Add(this.groupBoxCache);
            this.splitContainer2.Panel1.Controls.Add(this.groupBoxSelectedSound);
            this.splitContainer2.Panel1.Padding = new System.Windows.Forms.Padding(3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer2.Size = new System.Drawing.Size(434, 812);
            this.splitContainer2.SplitterDistance = 142;
            this.splitContainer2.TabIndex = 6;
            // 
            // groupBoxCache
            // 
            this.groupBoxCache.Controls.Add(this.tableLayoutPanel1);
            this.groupBoxCache.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxCache.Location = new System.Drawing.Point(3, 196);
            this.groupBoxCache.Name = "groupBoxCache";
            this.groupBoxCache.Size = new System.Drawing.Size(136, 173);
            this.groupBoxCache.TabIndex = 8;
            this.groupBoxCache.TabStop = false;
            this.groupBoxCache.Text = "Sound cache";
            // 
            // groupBoxSelectedSound
            // 
            this.groupBoxSelectedSound.Controls.Add(this.sound3D);
            this.groupBoxSelectedSound.Controls.Add(this.tableLayoutPanel2);
            this.groupBoxSelectedSound.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxSelectedSound.Location = new System.Drawing.Point(3, 3);
            this.groupBoxSelectedSound.Name = "groupBoxSelectedSound";
            this.groupBoxSelectedSound.Size = new System.Drawing.Size(136, 193);
            this.groupBoxSelectedSound.TabIndex = 0;
            this.groupBoxSelectedSound.TabStop = false;
            this.groupBoxSelectedSound.Text = "Selected sound";
            // 
            // sound3D
            // 
            this.sound3D.AutoSize = true;
            this.sound3D.Dock = System.Windows.Forms.DockStyle.Top;
            this.sound3D.Enabled = false;
            this.sound3D.Location = new System.Drawing.Point(3, 167);
            this.sound3D.Name = "sound3D";
            this.sound3D.Padding = new System.Windows.Forms.Padding(3);
            this.sound3D.Size = new System.Drawing.Size(130, 23);
            this.sound3D.TabIndex = 0;
            this.sound3D.Text = "3D";
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
            this.tableLayoutPanel2.Size = new System.Drawing.Size(130, 151);
            this.tableLayoutPanel2.TabIndex = 8;
            // 
            // smsVolume
            // 
            this.smsVolume.Dock = System.Windows.Forms.DockStyle.Fill;
            this.smsVolume.Location = new System.Drawing.Point(73, 128);
            this.smsVolume.Name = "smsVolume";
            this.smsVolume.ReadOnly = true;
            this.smsVolume.Size = new System.Drawing.Size(54, 20);
            this.smsVolume.TabIndex = 5;
            // 
            // distance
            // 
            this.distance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.distance.Location = new System.Drawing.Point(73, 103);
            this.distance.Name = "distance";
            this.distance.ReadOnly = true;
            this.distance.Size = new System.Drawing.Size(54, 20);
            this.distance.TabIndex = 4;
            // 
            // variable3
            // 
            this.variable3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable3.Location = new System.Drawing.Point(73, 78);
            this.variable3.Name = "variable3";
            this.variable3.ReadOnly = true;
            this.variable3.Size = new System.Drawing.Size(54, 20);
            this.variable3.TabIndex = 3;
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
            this.variable2.Location = new System.Drawing.Point(73, 53);
            this.variable2.Name = "variable2";
            this.variable2.ReadOnly = true;
            this.variable2.Size = new System.Drawing.Size(54, 20);
            this.variable2.TabIndex = 2;
            // 
            // variable1
            // 
            this.variable1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable1.Location = new System.Drawing.Point(73, 28);
            this.variable1.Name = "variable1";
            this.variable1.ReadOnly = true;
            this.variable1.Size = new System.Drawing.Size(54, 20);
            this.variable1.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 56);
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
            this.speed.Size = new System.Drawing.Size(54, 20);
            this.speed.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 81);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Variable 3";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Variable 1";
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 106);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Distance";
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 131);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Volume";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.textBox1, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.textBox2, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.textBox3, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBox4, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.alSources, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.waves, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label9, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label10, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label11, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label12, 0, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(130, 151);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(73, 128);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(54, 20);
            this.textBox1.TabIndex = 5;
            // 
            // textBox2
            // 
            this.textBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox2.Location = new System.Drawing.Point(73, 103);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(54, 20);
            this.textBox2.TabIndex = 4;
            // 
            // textBox3
            // 
            this.textBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox3.Location = new System.Drawing.Point(73, 78);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(54, 20);
            this.textBox3.TabIndex = 3;
            // 
            // label7
            // 
            this.label7.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 6);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(61, 13);
            this.label7.TabIndex = 1;
            this.label7.Text = "WavCache";
            // 
            // textBox4
            // 
            this.textBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox4.Location = new System.Drawing.Point(73, 53);
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(54, 20);
            this.textBox4.TabIndex = 2;
            // 
            // alSources
            // 
            this.alSources.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alSources.Location = new System.Drawing.Point(73, 28);
            this.alSources.Name = "alSources";
            this.alSources.ReadOnly = true;
            this.alSources.Size = new System.Drawing.Size(54, 20);
            this.alSources.TabIndex = 1;
            // 
            // label8
            // 
            this.label8.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 56);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(0, 13);
            this.label8.TabIndex = 3;
            // 
            // waves
            // 
            this.waves.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waves.Location = new System.Drawing.Point(73, 3);
            this.waves.Name = "waves";
            this.waves.ReadOnly = true;
            this.waves.Size = new System.Drawing.Size(54, 20);
            this.waves.TabIndex = 0;
            // 
            // label9
            // 
            this.label9.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 81);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(0, 13);
            this.label9.TabIndex = 4;
            // 
            // label10
            // 
            this.label10.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 31);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(62, 13);
            this.label10.TabIndex = 2;
            this.label10.Text = "AL Sources";
            // 
            // label11
            // 
            this.label11.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(3, 106);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(0, 13);
            this.label11.TabIndex = 4;
            // 
            // label12
            // 
            this.label12.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 131);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(0, 13);
            this.label12.TabIndex = 4;
            // 
            // SoundDebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 812);
            this.Controls.Add(this.splitContainer2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SoundDebugForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Sound Debug";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SoundDebugForm_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.groupBoxActiveSounds.ResumeLayout(false);
            this.groupBoxInactiveSounds.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.groupBoxCache.ResumeLayout(false);
            this.groupBoxSelectedSound.ResumeLayout(false);
            this.groupBoxSelectedSound.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView inactiveSoundList;
        private System.Windows.Forms.TreeView activeSoundList;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.GroupBox groupBoxSelectedSound;
        private System.Windows.Forms.GroupBox groupBoxActiveSounds;
        private System.Windows.Forms.GroupBox groupBoxInactiveSounds;
        private System.Windows.Forms.GroupBox groupBoxCache;
        private System.Windows.Forms.CheckBox sound3D;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox smsVolume;
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
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.TextBox alSources;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox waves;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
    }
}