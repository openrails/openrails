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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.speedLabel = new System.Windows.Forms.Label();
            this.speed = new System.Windows.Forms.TextBox();
            this.wheelRPMLabel = new System.Windows.Forms.Label();
            this.wheelRPM = new System.Windows.Forms.TextBox();
            this.tractiveEffortLabel = new System.Windows.Forms.Label();
            this.tractiveEffort = new System.Windows.Forms.TextBox();
            this.tractivePowerLabel = new System.Windows.Forms.Label();
            this.tractivePower = new System.Windows.Forms.TextBox();
            this.variable1Label = new System.Windows.Forms.Label();
            this.variable1 = new System.Windows.Forms.TextBox();
            this.variable2Label = new System.Windows.Forms.Label();
            this.variable2 = new System.Windows.Forms.TextBox();
            this.variable2BoosterLabel = new System.Windows.Forms.Label();
            this.variable2Booster = new System.Windows.Forms.TextBox();
            this.variable3Label = new System.Windows.Forms.Label();
            this.variable3 = new System.Windows.Forms.TextBox();
            this.engineRPMLabel = new System.Windows.Forms.Label();
            this.engineRPM = new System.Windows.Forms.TextBox();
            this.enginePowerLabel = new System.Windows.Forms.Label();
            this.enginePower = new System.Windows.Forms.TextBox();
            this.engineTorqueLabel = new System.Windows.Forms.Label();
            this.engineTorque = new System.Windows.Forms.TextBox();
            this.backPressureLabel = new System.Windows.Forms.Label();
            this.backPressure = new System.Windows.Forms.TextBox();
            this.brakeCylLabel = new System.Windows.Forms.Label();
            this.brakeCyl = new System.Windows.Forms.TextBox();
            this.curveForceLabel = new System.Windows.Forms.Label();
            this.curveForce = new System.Windows.Forms.TextBox();
            this.angleOfAttackLabel = new System.Windows.Forms.Label();
            this.angleOfAttack = new System.Windows.Forms.TextBox();
            this.carFrictionLabel = new System.Windows.Forms.Label();
            this.carFriction = new System.Windows.Forms.TextBox();
            this.carTunnelDistanceLabel = new System.Windows.Forms.Label();
            this.carTunnelDistance = new System.Windows.Forms.TextBox();
            this.distanceLabel = new System.Windows.Forms.Label();
            this.distance = new System.Windows.Forms.TextBox();
            this.distanceSquaredLabel = new System.Windows.Forms.Label();
            this.distanceSquared = new System.Windows.Forms.TextBox();
            this.smsVolumeLabel = new System.Windows.Forms.Label();
            this.smsVolume = new System.Windows.Forms.TextBox();
            this.smsFrequencyLabel = new System.Windows.Forms.Label();
            this.smsFrequency = new System.Windows.Forms.TextBox();
            this.sound3D = new System.Windows.Forms.CheckBox();
            this.concreteSleepers = new System.Windows.Forms.CheckBox();
            this.carInTunnel = new System.Windows.Forms.CheckBox();
            this.waveLabel = new System.Windows.Forms.Label();
            this.waves = new System.Windows.Forms.TextBox();
            this.alSourcesLabel = new System.Windows.Forms.Label();
            this.alSources = new System.Windows.Forms.TextBox();
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
            this.groupBoxActiveSounds.Text = "Active Sound Sources";
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
            this.groupBoxInactiveSounds.Text = "Inactive Sound Sources";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = false;
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
            this.splitContainer2.Size = new System.Drawing.Size(612, 812);
            this.splitContainer2.SplitterDistance = 320;
            this.splitContainer2.TabIndex = 6;
            // 
            // groupBoxCache
            // 
            this.groupBoxCache.Controls.Add(this.tableLayoutPanel1);
            this.groupBoxCache.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxCache.Location = new System.Drawing.Point(3, 196);
            this.groupBoxCache.Name = "groupBoxCache";
            this.groupBoxCache.Size = new System.Drawing.Size(176, 73);
            this.groupBoxCache.TabIndex = 8;
            this.groupBoxCache.TabStop = false;
            this.groupBoxCache.Text = "Sound Cache";
            // 
            // groupBoxSelectedSound
            // 
            this.groupBoxSelectedSound.Controls.Add(this.concreteSleepers);
            this.groupBoxSelectedSound.Controls.Add(this.carInTunnel);
            this.groupBoxSelectedSound.Controls.Add(this.sound3D);
            this.groupBoxSelectedSound.Controls.Add(this.tableLayoutPanel2);
            this.groupBoxSelectedSound.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxSelectedSound.Location = new System.Drawing.Point(3, 3);
            this.groupBoxSelectedSound.Name = "groupBoxSelectedSound";
            this.groupBoxSelectedSound.Size = new System.Drawing.Size(310, 618);
            this.groupBoxSelectedSound.TabIndex = 0;
            this.groupBoxSelectedSound.TabStop = false;
            this.groupBoxSelectedSound.Text = "Selected Sound Source Variables";
            // 
            // sound3D
            // 
            this.sound3D.AutoSize = true;
            this.sound3D.Dock = System.Windows.Forms.DockStyle.Top;
            this.sound3D.Enabled = false;
            this.sound3D.Location = new System.Drawing.Point(3, 442);
            this.sound3D.Name = "sound3D";
            this.sound3D.Padding = new System.Windows.Forms.Padding(3);
            this.sound3D.Size = new System.Drawing.Size(130, 23);
            this.sound3D.TabIndex = 0;
            this.sound3D.Text = "3D";
            // 
            // concreteSleepers
            // 
            this.concreteSleepers.AutoSize = true;
            this.concreteSleepers.Dock = System.Windows.Forms.DockStyle.Top;
            this.concreteSleepers.Enabled = false;
            this.concreteSleepers.Location = new System.Drawing.Point(3, 467);
            this.concreteSleepers.Name = "concreteSleepers";
            this.concreteSleepers.Padding = new System.Windows.Forms.Padding(3);
            this.concreteSleepers.Size = new System.Drawing.Size(130, 23);
            this.concreteSleepers.TabIndex = 0;
            this.concreteSleepers.Text = "Concrete Sleepers";
            // 
            // carInTunnel
            // 
            this.carInTunnel.AutoSize = true;
            this.carInTunnel.Dock = System.Windows.Forms.DockStyle.Top;
            this.carInTunnel.Enabled = false;
            this.carInTunnel.Location = new System.Drawing.Point(3, 492);
            this.carInTunnel.Name = "carInTunnel";
            this.carInTunnel.Padding = new System.Windows.Forms.Padding(3);
            this.carInTunnel.Size = new System.Drawing.Size(130, 23);
            this.carInTunnel.TabIndex = 0;
            this.carInTunnel.Text = "Car In Tunnel";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.speedLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.speed, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.wheelRPMLabel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.wheelRPM, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.tractiveEffortLabel, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.tractiveEffort, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.tractivePowerLabel, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.tractivePower, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.variable1Label, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.variable1, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.variable2Label, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.variable2, 1, 5);
            this.tableLayoutPanel2.Controls.Add(this.variable2BoosterLabel, 0, 6);
            this.tableLayoutPanel2.Controls.Add(this.variable2Booster, 1, 6);
            this.tableLayoutPanel2.Controls.Add(this.variable3Label, 0, 7);
            this.tableLayoutPanel2.Controls.Add(this.variable3, 1, 7);
            this.tableLayoutPanel2.Controls.Add(this.engineRPMLabel, 0, 8);
            this.tableLayoutPanel2.Controls.Add(this.engineRPM, 1, 8);
            this.tableLayoutPanel2.Controls.Add(this.enginePowerLabel, 0, 9);
            this.tableLayoutPanel2.Controls.Add(this.enginePower, 1, 9);
            this.tableLayoutPanel2.Controls.Add(this.engineTorqueLabel, 0, 10);
            this.tableLayoutPanel2.Controls.Add(this.engineTorque, 1, 10);
            this.tableLayoutPanel2.Controls.Add(this.backPressureLabel, 0, 11);
            this.tableLayoutPanel2.Controls.Add(this.backPressure, 1, 11);
            this.tableLayoutPanel2.Controls.Add(this.brakeCylLabel, 0, 12);
            this.tableLayoutPanel2.Controls.Add(this.brakeCyl, 1, 12);
            this.tableLayoutPanel2.Controls.Add(this.curveForceLabel, 0, 13);
            this.tableLayoutPanel2.Controls.Add(this.curveForce, 1, 13);
            this.tableLayoutPanel2.Controls.Add(this.angleOfAttackLabel, 0, 14);
            this.tableLayoutPanel2.Controls.Add(this.angleOfAttack, 1, 14);
            this.tableLayoutPanel2.Controls.Add(this.carFrictionLabel, 0, 15);
            this.tableLayoutPanel2.Controls.Add(this.carFriction, 1, 15);
            this.tableLayoutPanel2.Controls.Add(this.carTunnelDistanceLabel, 0, 16);
            this.tableLayoutPanel2.Controls.Add(this.carTunnelDistance, 1, 16);
            this.tableLayoutPanel2.Controls.Add(this.distanceLabel, 0, 17);
            this.tableLayoutPanel2.Controls.Add(this.distance, 1, 17);
            this.tableLayoutPanel2.Controls.Add(this.distanceSquaredLabel, 0, 18);
            this.tableLayoutPanel2.Controls.Add(this.distanceSquared, 1, 18);
            this.tableLayoutPanel2.Controls.Add(this.smsVolumeLabel, 0, 19);
            this.tableLayoutPanel2.Controls.Add(this.smsVolume, 1, 19);
            this.tableLayoutPanel2.Controls.Add(this.smsFrequencyLabel, 0, 20);
            this.tableLayoutPanel2.Controls.Add(this.smsFrequency, 1, 20);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 21;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(190, 526);
            this.tableLayoutPanel2.TabIndex = 8;
            // 
            // speedLabel
            // 
            this.speedLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.speedLabel.AutoSize = true;
            this.speedLabel.Location = new System.Drawing.Point(3, 31);
            this.speedLabel.Name = "speedLabel";
            this.speedLabel.Size = new System.Drawing.Size(130, 13);
            this.speedLabel.TabIndex = 0;
            this.speedLabel.Text = "Speed (m/s)";
            // 
            // speed
            // 
            this.speed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speed.Location = new System.Drawing.Point(73, 28);
            this.speed.Name = "speed";
            this.speed.ReadOnly = true;
            this.speed.Size = new System.Drawing.Size(54, 20);
            this.speed.TabIndex = 0;
            // 
            // wheelRPMLabel
            // 
            this.wheelRPMLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.wheelRPMLabel.AutoSize = true;
            this.wheelRPMLabel.Location = new System.Drawing.Point(3, 56);
            this.wheelRPMLabel.Name = "wheelRPMLabel";
            this.wheelRPMLabel.Size = new System.Drawing.Size(130, 13);
            this.wheelRPMLabel.TabIndex = 1;
            this.wheelRPMLabel.Text = "Wheel RPM";
            // 
            // wheelRPM
            // 
            this.wheelRPM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wheelRPM.Location = new System.Drawing.Point(73, 53);
            this.wheelRPM.Name = "wheelRPM";
            this.wheelRPM.ReadOnly = true;
            this.wheelRPM.Size = new System.Drawing.Size(54, 20);
            this.wheelRPM.TabIndex = 1;
            // 
            // tractiveEffortLabel
            // 
            this.tractiveEffortLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tractiveEffortLabel.AutoSize = true;
            this.tractiveEffortLabel.Location = new System.Drawing.Point(3, 81);
            this.tractiveEffortLabel.Name = "tractiveEffortLabel";
            this.tractiveEffortLabel.Size = new System.Drawing.Size(130, 13);
            this.tractiveEffortLabel.TabIndex = 2;
            this.tractiveEffortLabel.Text = "Tractive Effort (kN)";
            // 
            // tractiveEffort
            // 
            this.tractiveEffort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tractiveEffort.Location = new System.Drawing.Point(73, 78);
            this.tractiveEffort.Name = "tractiveEffort";
            this.tractiveEffort.ReadOnly = true;
            this.tractiveEffort.Size = new System.Drawing.Size(54, 20);
            this.tractiveEffort.TabIndex = 2;
            // 
            // tractivePowerLabel
            // 
            this.tractivePowerLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tractivePowerLabel.AutoSize = true;
            this.tractivePowerLabel.Location = new System.Drawing.Point(3, 106);
            this.tractivePowerLabel.Name = "tractivePowerLabel";
            this.tractivePowerLabel.Size = new System.Drawing.Size(130, 13);
            this.tractivePowerLabel.TabIndex = 3;
            this.tractivePowerLabel.Text = "Tractive Power (kW)";
            // 
            // tractivePower
            // 
            this.tractivePower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tractivePower.Location = new System.Drawing.Point(73, 103);
            this.tractivePower.Name = "tractivePower";
            this.tractivePower.ReadOnly = true;
            this.tractivePower.Size = new System.Drawing.Size(54, 20);
            this.tractivePower.TabIndex = 3;
            // 
            // variable1Label
            // 
            this.variable1Label.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.variable1Label.AutoSize = true;
            this.variable1Label.Location = new System.Drawing.Point(3, 131);
            this.variable1Label.Name = "variable1Label";
            this.variable1Label.Size = new System.Drawing.Size(130, 13);
            this.variable1Label.TabIndex = 4;
            this.variable1Label.Text = "Variable 1";
            // 
            // variable1
            // 
            this.variable1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable1.Location = new System.Drawing.Point(73, 128);
            this.variable1.Name = "variable1";
            this.variable1.ReadOnly = true;
            this.variable1.Size = new System.Drawing.Size(54, 20);
            this.variable1.TabIndex = 4;
            // 
            // variable2Label
            // 
            this.variable2Label.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.variable2Label.AutoSize = true;
            this.variable2Label.Location = new System.Drawing.Point(3, 156);
            this.variable2Label.Name = "variable2Label";
            this.variable2Label.Size = new System.Drawing.Size(130, 13);
            this.variable2Label.TabIndex = 5;
            this.variable2Label.Text = "Variable 2";
            // 
            // variable2
            // 
            this.variable2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable2.Location = new System.Drawing.Point(73, 153);
            this.variable2.Name = "variable2";
            this.variable2.ReadOnly = true;
            this.variable2.Size = new System.Drawing.Size(54, 20);
            this.variable2.TabIndex = 5;
            // 
            // variable2BoosterLabel
            // 
            this.variable2BoosterLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.variable2BoosterLabel.AutoSize = true;
            this.variable2BoosterLabel.Location = new System.Drawing.Point(3, 181);
            this.variable2BoosterLabel.Name = "variable2BoosterLabel";
            this.variable2BoosterLabel.Size = new System.Drawing.Size(130, 13);
            this.variable2BoosterLabel.TabIndex = 6;
            this.variable2BoosterLabel.Text = "Booster Variable 2";
            // 
            // variable2Booster
            // 
            this.variable2Booster.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable2Booster.Location = new System.Drawing.Point(73, 178);
            this.variable2Booster.Name = "variable2Booster";
            this.variable2Booster.ReadOnly = true;
            this.variable2Booster.Size = new System.Drawing.Size(54, 20);
            this.variable2Booster.TabIndex = 6;
            // 
            // variable3Label
            // 
            this.variable3Label.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.variable3Label.AutoSize = true;
            this.variable3Label.Location = new System.Drawing.Point(3, 206);
            this.variable3Label.Name = "variable3Label";
            this.variable3Label.Size = new System.Drawing.Size(130, 13);
            this.variable3Label.TabIndex = 7;
            this.variable3Label.Text = "Variable 3";
            // 
            // variable3
            // 
            this.variable3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.variable3.Location = new System.Drawing.Point(73, 203);
            this.variable3.Name = "variable3";
            this.variable3.ReadOnly = true;
            this.variable3.Size = new System.Drawing.Size(54, 20);
            this.variable3.TabIndex = 7;
            // 
            // engineRPMLabel
            // 
            this.engineRPMLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.engineRPMLabel.AutoSize = true;
            this.engineRPMLabel.Location = new System.Drawing.Point(3, 231);
            this.engineRPMLabel.Name = "engineRPMLabel";
            this.engineRPMLabel.Size = new System.Drawing.Size(130, 13);
            this.engineRPMLabel.TabIndex = 8;
            this.engineRPMLabel.Text = "Engine RPM";
            // 
            // engineRPM
            // 
            this.engineRPM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.engineRPM.Location = new System.Drawing.Point(73, 228);
            this.engineRPM.Name = "engineRPM";
            this.engineRPM.ReadOnly = true;
            this.engineRPM.Size = new System.Drawing.Size(54, 20);
            this.engineRPM.TabIndex = 8;
            // 
            // enginePowerLabel
            // 
            this.enginePowerLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.enginePowerLabel.AutoSize = true;
            this.enginePowerLabel.Location = new System.Drawing.Point(3, 256);
            this.enginePowerLabel.Name = "enginePowerLabel";
            this.enginePowerLabel.Size = new System.Drawing.Size(130, 13);
            this.enginePowerLabel.TabIndex = 9;
            this.enginePowerLabel.Text = "Engine Power (kW)";
            // 
            // enginePower
            // 
            this.enginePower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.enginePower.Location = new System.Drawing.Point(73, 253);
            this.enginePower.Name = "enginePower";
            this.enginePower.ReadOnly = true;
            this.enginePower.Size = new System.Drawing.Size(54, 20);
            this.enginePower.TabIndex = 9;
            // 
            // engineTorqueLabel
            // 
            this.engineTorqueLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.engineTorqueLabel.AutoSize = true;
            this.engineTorqueLabel.Location = new System.Drawing.Point(3, 281);
            this.engineTorqueLabel.Name = "engineTorqueLabel";
            this.engineTorqueLabel.Size = new System.Drawing.Size(130, 13);
            this.engineTorqueLabel.TabIndex = 10;
            this.engineTorqueLabel.Text = "Engine Torque (N-m)";
            // 
            // engineTorque
            // 
            this.engineTorque.Dock = System.Windows.Forms.DockStyle.Fill;
            this.engineTorque.Location = new System.Drawing.Point(73, 278);
            this.engineTorque.Name = "engineTorque";
            this.engineTorque.ReadOnly = true;
            this.engineTorque.Size = new System.Drawing.Size(54, 20);
            this.engineTorque.TabIndex = 10;
            // 
            // backPressureLabel
            // 
            this.backPressureLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.backPressureLabel.AutoSize = true;
            this.backPressureLabel.Location = new System.Drawing.Point(3, 306);
            this.backPressureLabel.Name = "backPressureLabel";
            this.backPressureLabel.Size = new System.Drawing.Size(130, 13);
            this.backPressureLabel.TabIndex = 11;
            this.backPressureLabel.Text = "Back Pressure (psi)";
            // 
            // backPressure
            // 
            this.backPressure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.backPressure.Location = new System.Drawing.Point(73, 303);
            this.backPressure.Name = "backPressure";
            this.backPressure.ReadOnly = true;
            this.backPressure.Size = new System.Drawing.Size(54, 20);
            this.backPressure.TabIndex = 11;
            // 
            // brakeCylLabel
            // 
            this.brakeCylLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.brakeCylLabel.AutoSize = true;
            this.brakeCylLabel.Location = new System.Drawing.Point(3, 331);
            this.brakeCylLabel.Name = "brakeCylLabel";
            this.brakeCylLabel.Size = new System.Drawing.Size(130, 13);
            this.brakeCylLabel.TabIndex = 12;
            this.brakeCylLabel.Text = "Brake Cylinder (psi)";
            // 
            // brakeCyl
            // 
            this.brakeCyl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.brakeCyl.Location = new System.Drawing.Point(73, 328);
            this.brakeCyl.Name = "brakeCyl";
            this.brakeCyl.ReadOnly = true;
            this.brakeCyl.Size = new System.Drawing.Size(54, 20);
            this.brakeCyl.TabIndex = 12;
            // 
            // curveForceLabel
            // 
            this.curveForceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.curveForceLabel.AutoSize = true;
            this.curveForceLabel.Location = new System.Drawing.Point(3, 356);
            this.curveForceLabel.Name = "curveForceLabel";
            this.curveForceLabel.Size = new System.Drawing.Size(130, 13);
            this.curveForceLabel.TabIndex = 13;
            this.curveForceLabel.Text = "Curve Force (N)";
            // 
            // curveForce
            // 
            this.curveForce.Dock = System.Windows.Forms.DockStyle.Fill;
            this.curveForce.Location = new System.Drawing.Point(73, 353);
            this.curveForce.Name = "curveForce";
            this.curveForce.ReadOnly = true;
            this.curveForce.Size = new System.Drawing.Size(54, 20);
            this.curveForce.TabIndex = 13;
            // 
            // angleOfAttackLabel
            // 
            this.angleOfAttackLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.angleOfAttackLabel.AutoSize = true;
            this.angleOfAttackLabel.Location = new System.Drawing.Point(3, 381);
            this.angleOfAttackLabel.Name = "angleOfAttackLabel";
            this.angleOfAttackLabel.Size = new System.Drawing.Size(130, 13);
            this.angleOfAttackLabel.TabIndex = 14;
            this.angleOfAttackLabel.Text = "Angle of Attack (mRad)";
            // 
            // angleOfAttack
            // 
            this.angleOfAttack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.angleOfAttack.Location = new System.Drawing.Point(73, 378);
            this.angleOfAttack.Name = "angleOfAttack";
            this.angleOfAttack.ReadOnly = true;
            this.angleOfAttack.Size = new System.Drawing.Size(54, 20);
            this.angleOfAttack.TabIndex = 14;
            // 
            // carFrictionLabel
            // 
            this.carFrictionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.carFrictionLabel.AutoSize = true;
            this.carFrictionLabel.Location = new System.Drawing.Point(3, 406);
            this.carFrictionLabel.Name = "carFrictionLabel";
            this.carFrictionLabel.Size = new System.Drawing.Size(130, 13);
            this.carFrictionLabel.TabIndex = 15;
            this.carFrictionLabel.Text = "Car Friction Coefficient";
            // 
            // carFriction
            // 
            this.carFriction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.carFriction.Location = new System.Drawing.Point(73, 403);
            this.carFriction.Name = "carFriction";
            this.carFriction.ReadOnly = true;
            this.carFriction.Size = new System.Drawing.Size(54, 20);
            this.carFriction.TabIndex = 15;
            // 
            // carTunnelDistanceLabel
            // 
            this.carTunnelDistanceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.carTunnelDistanceLabel.AutoSize = true;
            this.carTunnelDistanceLabel.Location = new System.Drawing.Point(3, 431);
            this.carTunnelDistanceLabel.Name = "carTunnelDistanceLabel";
            this.carTunnelDistanceLabel.Size = new System.Drawing.Size(130, 13);
            this.carTunnelDistanceLabel.TabIndex = 16;
            this.carTunnelDistanceLabel.Text = "Car Tunnel Distance (m)";
            // 
            // carTunnelDistance
            // 
            this.carTunnelDistance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.carTunnelDistance.Location = new System.Drawing.Point(73, 428);
            this.carTunnelDistance.Name = "carTunnelDistance";
            this.carTunnelDistance.ReadOnly = true;
            this.carTunnelDistance.Size = new System.Drawing.Size(54, 20);
            this.carTunnelDistance.TabIndex = 16;
            // 
            // distanceLabel
            // 
            this.distanceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.distanceLabel.AutoSize = true;
            this.distanceLabel.Location = new System.Drawing.Point(3, 456);
            this.distanceLabel.Name = "distanceLabel";
            this.distanceLabel.Size = new System.Drawing.Size(130, 13);
            this.distanceLabel.TabIndex = 17;
            this.distanceLabel.Text = "Distance (m)";
            // 
            // distance
            // 
            this.distance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.distance.Location = new System.Drawing.Point(73, 453);
            this.distance.Name = "distance";
            this.distance.ReadOnly = true;
            this.distance.Size = new System.Drawing.Size(54, 20);
            this.distance.TabIndex = 17;
            // 
            // distanceSquaredLabel
            // 
            this.distanceSquaredLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.distanceSquaredLabel.AutoSize = true;
            this.distanceSquaredLabel.Location = new System.Drawing.Point(3, 481);
            this.distanceSquaredLabel.Name = "carDistanceTrackLabel";
            this.distanceSquaredLabel.Size = new System.Drawing.Size(130, 13);
            this.distanceSquaredLabel.TabIndex = 18;
            this.distanceSquaredLabel.Text = "Distance Squared (m²)";
            // 
            // distanceSquared
            // 
            this.distanceSquared.Dock = System.Windows.Forms.DockStyle.Fill;
            this.distanceSquared.Location = new System.Drawing.Point(73, 478);
            this.distanceSquared.Name = "distanceSquared";
            this.distanceSquared.ReadOnly = true;
            this.distanceSquared.Size = new System.Drawing.Size(54, 20);
            this.distanceSquared.TabIndex = 18;
            // 
            // smsVolumeLabel
            // 
            this.smsVolumeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.smsVolumeLabel.AutoSize = true;
            this.smsVolumeLabel.Location = new System.Drawing.Point(3, 506);
            this.smsVolumeLabel.Name = "smsVolumeLabel";
            this.smsVolumeLabel.Size = new System.Drawing.Size(130, 13);
            this.smsVolumeLabel.TabIndex = 19;
            this.smsVolumeLabel.Text = "Volume";
            // 
            // smsVolume
            // 
            this.smsVolume.Dock = System.Windows.Forms.DockStyle.Fill;
            this.smsVolume.Location = new System.Drawing.Point(73, 503);
            this.smsVolume.Name = "smsVolume";
            this.smsVolume.ReadOnly = true;
            this.smsVolume.Size = new System.Drawing.Size(54, 20);
            this.smsVolume.TabIndex = 19;
            // 
            // smsFrequencyLabel
            // 
            this.smsFrequencyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.smsFrequencyLabel.AutoSize = true;
            this.smsFrequencyLabel.Location = new System.Drawing.Point(3, 531);
            this.smsFrequencyLabel.Name = "smsFrequencyLabel";
            this.smsFrequencyLabel.Size = new System.Drawing.Size(130, 13);
            this.smsFrequencyLabel.TabIndex = 20;
            this.smsFrequencyLabel.Text = "Frequency (Hz)";
            // 
            // smsFrequency
            // 
            this.smsFrequency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.smsFrequency.Location = new System.Drawing.Point(73, 528);
            this.smsFrequency.Name = "smsFrequency";
            this.smsFrequency.ReadOnly = true;
            this.smsFrequency.Size = new System.Drawing.Size(54, 20);
            this.smsFrequency.TabIndex = 20;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.waveLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.waves, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.alSourcesLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.alSources, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(170, 51);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // waveLabel
            // 
            this.waveLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.waveLabel.AutoSize = true;
            this.waveLabel.Location = new System.Drawing.Point(3, 6);
            this.waveLabel.Name = "waveLabel";
            this.waveLabel.Size = new System.Drawing.Size(61, 13);
            this.waveLabel.TabIndex = 1;
            this.waveLabel.Text = "Cached Wave Files";
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
            // alSourcesLabel
            // 
            this.alSourcesLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.alSourcesLabel.AutoSize = true;
            this.alSourcesLabel.Location = new System.Drawing.Point(3, 31);
            this.alSourcesLabel.Name = "alSourcesLabel";
            this.alSourcesLabel.Size = new System.Drawing.Size(0, 13);
            this.alSourcesLabel.TabIndex = 3;
            this.alSourcesLabel.Text = "AL Sound Sources";
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
            // SoundDebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 700);
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
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label speedLabel;
        private System.Windows.Forms.TextBox speed;
        private System.Windows.Forms.Label wheelRPMLabel;
        private System.Windows.Forms.TextBox wheelRPM;
        private System.Windows.Forms.Label tractiveEffortLabel;
        private System.Windows.Forms.TextBox tractiveEffort;
        private System.Windows.Forms.Label tractivePowerLabel;
        private System.Windows.Forms.TextBox tractivePower;
        private System.Windows.Forms.Label variable1Label;
        private System.Windows.Forms.TextBox variable1;
        private System.Windows.Forms.Label variable2Label;
        private System.Windows.Forms.TextBox variable2;
        private System.Windows.Forms.Label variable2BoosterLabel;
        private System.Windows.Forms.TextBox variable2Booster;
        private System.Windows.Forms.Label variable3Label;
        private System.Windows.Forms.TextBox variable3;
        private System.Windows.Forms.Label engineRPMLabel;
        private System.Windows.Forms.TextBox engineRPM;
        private System.Windows.Forms.Label enginePowerLabel;
        private System.Windows.Forms.TextBox enginePower;
        private System.Windows.Forms.Label engineTorqueLabel;
        private System.Windows.Forms.TextBox engineTorque;
        private System.Windows.Forms.Label backPressureLabel;
        private System.Windows.Forms.TextBox backPressure;
        private System.Windows.Forms.Label brakeCylLabel;
        private System.Windows.Forms.TextBox brakeCyl;
        private System.Windows.Forms.Label curveForceLabel;
        private System.Windows.Forms.TextBox curveForce;
        private System.Windows.Forms.Label angleOfAttackLabel;
        private System.Windows.Forms.TextBox angleOfAttack;
        private System.Windows.Forms.Label carFrictionLabel;
        private System.Windows.Forms.TextBox carFriction;
        private System.Windows.Forms.Label carTunnelDistanceLabel;
        private System.Windows.Forms.TextBox carTunnelDistance;
        private System.Windows.Forms.Label distanceLabel;
        private System.Windows.Forms.TextBox distance;
        private System.Windows.Forms.Label distanceSquaredLabel;
        private System.Windows.Forms.TextBox distanceSquared;
        private System.Windows.Forms.Label smsVolumeLabel;
        private System.Windows.Forms.TextBox smsVolume;
        private System.Windows.Forms.Label smsFrequencyLabel;
        private System.Windows.Forms.TextBox smsFrequency;
        private System.Windows.Forms.CheckBox sound3D;
        private System.Windows.Forms.CheckBox concreteSleepers;
        private System.Windows.Forms.CheckBox carInTunnel;
        private System.Windows.Forms.Label waveLabel;
        private System.Windows.Forms.TextBox waves;
        private System.Windows.Forms.Label alSourcesLabel;
        private System.Windows.Forms.TextBox alSources;
    }
}
