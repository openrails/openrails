namespace ORTS
{
    partial class OptionsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsForm));
            this.label1 = new System.Windows.Forms.Label();
            this.numericWorldObjectDensity = new System.Windows.Forms.NumericUpDown();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.numericSoundDetailLevel = new System.Windows.Forms.NumericUpDown();
            this.comboBoxWindowSize = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxTrainLights = new System.Windows.Forms.CheckBox();
            this.numericBrakePipeChargingRatePSIpS = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBoxPrecipitation = new System.Windows.Forms.CheckBox();
            this.checkBoxGraduatedRelease = new System.Windows.Forms.CheckBox();
            this.checkBoxWire = new System.Windows.Forms.CheckBox();
            this.checkBoxShadows = new System.Windows.Forms.CheckBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkBoxWindowGlass = new System.Windows.Forms.CheckBox();
            this.checkBoxBINSound = new System.Windows.Forms.CheckBox();
            this.checkBoxAlerter = new System.Windows.Forms.CheckBox();
            this.checkBoxSuppressConfirmations = new System.Windows.Forms.CheckBox();
            this.checkDispatcher = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.numericCab2DStretch = new System.Windows.Forms.NumericUpDown();
            this.labelCab2DStretch = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.buttonExport = new System.Windows.Forms.Button();
            this.buttonDefaultKeys = new System.Windows.Forms.Button();
            this.buttonDebug = new System.Windows.Forms.Button();
            this.buttonCheckKeys = new System.Windows.Forms.Button();
            this.panelKeys = new System.Windows.Forms.Panel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.checkBoxAdvancedAdhesion = new System.Windows.Forms.CheckBox();
            this.checkBoxBreakCouplers = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericWorldObjectDensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundDetailLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericBrakePipeChargingRatePSIpS)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericCab2DStretch)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(64, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "World Object Density";
            // 
            // numericWorldObjectDensity
            // 
            this.numericWorldObjectDensity.Location = new System.Drawing.Point(6, 6);
            this.numericWorldObjectDensity.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericWorldObjectDensity.Name = "numericWorldObjectDensity";
            this.numericWorldObjectDensity.Size = new System.Drawing.Size(52, 20);
            this.numericWorldObjectDensity.TabIndex = 0;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(308, 447);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(64, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(97, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Sound Detail Level";
            // 
            // numericSoundDetailLevel
            // 
            this.numericSoundDetailLevel.Location = new System.Drawing.Point(6, 32);
            this.numericSoundDetailLevel.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericSoundDetailLevel.Name = "numericSoundDetailLevel";
            this.numericSoundDetailLevel.Size = new System.Drawing.Size(52, 20);
            this.numericSoundDetailLevel.TabIndex = 2;
            // 
            // comboBoxWindowSize
            // 
            this.comboBoxWindowSize.FormattingEnabled = true;
            this.comboBoxWindowSize.Location = new System.Drawing.Point(6, 58);
            this.comboBoxWindowSize.Name = "comboBoxWindowSize";
            this.comboBoxWindowSize.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWindowSize.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Window size";
            // 
            // checkBoxTrainLights
            // 
            this.checkBoxTrainLights.AutoSize = true;
            this.checkBoxTrainLights.Location = new System.Drawing.Point(6, 107);
            this.checkBoxTrainLights.Name = "checkBoxTrainLights";
            this.checkBoxTrainLights.Size = new System.Drawing.Size(81, 17);
            this.checkBoxTrainLights.TabIndex = 7;
            this.checkBoxTrainLights.Text = "Train Lights";
            this.checkBoxTrainLights.UseVisualStyleBackColor = true;
            // 
            // numericBrakePipeChargingRatePSIpS
            // 
            this.numericBrakePipeChargingRatePSIpS.Location = new System.Drawing.Point(6, 200);
            this.numericBrakePipeChargingRatePSIpS.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericBrakePipeChargingRatePSIpS.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericBrakePipeChargingRatePSIpS.Name = "numericBrakePipeChargingRatePSIpS";
            this.numericBrakePipeChargingRatePSIpS.Size = new System.Drawing.Size(52, 20);
            this.numericBrakePipeChargingRatePSIpS.TabIndex = 11;
            this.numericBrakePipeChargingRatePSIpS.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(64, 202);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(166, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Brake Pipe Charging Rate (PSI/s)";
            // 
            // checkBoxPrecipitation
            // 
            this.checkBoxPrecipitation.AutoSize = true;
            this.checkBoxPrecipitation.Location = new System.Drawing.Point(6, 131);
            this.checkBoxPrecipitation.Name = "checkBoxPrecipitation";
            this.checkBoxPrecipitation.Size = new System.Drawing.Size(84, 17);
            this.checkBoxPrecipitation.TabIndex = 8;
            this.checkBoxPrecipitation.Text = "Precipitation";
            this.checkBoxPrecipitation.UseVisualStyleBackColor = true;
            // 
            // checkBoxGraduatedRelease
            // 
            this.checkBoxGraduatedRelease.AutoSize = true;
            this.checkBoxGraduatedRelease.Location = new System.Drawing.Point(6, 177);
            this.checkBoxGraduatedRelease.Name = "checkBoxGraduatedRelease";
            this.checkBoxGraduatedRelease.Size = new System.Drawing.Size(169, 17);
            this.checkBoxGraduatedRelease.TabIndex = 10;
            this.checkBoxGraduatedRelease.Text = "Graduated Release Air Brakes";
            this.checkBoxGraduatedRelease.UseVisualStyleBackColor = true;
            // 
            // checkBoxWire
            // 
            this.checkBoxWire.AutoSize = true;
            this.checkBoxWire.Location = new System.Drawing.Point(6, 154);
            this.checkBoxWire.Name = "checkBoxWire";
            this.checkBoxWire.Size = new System.Drawing.Size(98, 17);
            this.checkBoxWire.TabIndex = 9;
            this.checkBoxWire.Text = "Overhead Wire";
            this.checkBoxWire.UseVisualStyleBackColor = true;
            // 
            // checkBoxShadows
            // 
            this.checkBoxShadows.AutoSize = true;
            this.checkBoxShadows.Location = new System.Drawing.Point(6, 226);
            this.checkBoxShadows.Name = "checkBoxShadows";
            this.checkBoxShadows.Size = new System.Drawing.Size(114, 17);
            this.checkBoxShadows.TabIndex = 13;
            this.checkBoxShadows.Text = "Dynamic Shadows";
            this.checkBoxShadows.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(389, 447);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkBoxWindowGlass
            // 
            this.checkBoxWindowGlass.AutoSize = true;
            this.checkBoxWindowGlass.Location = new System.Drawing.Point(6, 249);
            this.checkBoxWindowGlass.Name = "checkBoxWindowGlass";
            this.checkBoxWindowGlass.Size = new System.Drawing.Size(171, 17);
            this.checkBoxWindowGlass.TabIndex = 14;
            this.checkBoxWindowGlass.Text = "Use glass on in-game windows";
            this.checkBoxWindowGlass.UseVisualStyleBackColor = true;
            // 
            // checkBoxBINSound
            // 
            this.checkBoxBINSound.AutoSize = true;
            this.checkBoxBINSound.Location = new System.Drawing.Point(6, 272);
            this.checkBoxBINSound.Name = "checkBoxBINSound";
            this.checkBoxBINSound.Size = new System.Drawing.Size(185, 17);
            this.checkBoxBINSound.TabIndex = 15;
            this.checkBoxBINSound.Text = "Use MSTS BIN compatible sound";
            this.checkBoxBINSound.UseVisualStyleBackColor = true;
            // 
            // checkBoxAlerter
            // 
            this.checkBoxAlerter.AutoSize = true;
            this.checkBoxAlerter.Location = new System.Drawing.Point(6, 85);
            this.checkBoxAlerter.Name = "checkBoxAlerter";
            this.checkBoxAlerter.Size = new System.Drawing.Size(56, 17);
            this.checkBoxAlerter.TabIndex = 6;
            this.checkBoxAlerter.Text = "Alerter";
            this.checkBoxAlerter.UseVisualStyleBackColor = true;
            // 
            // checkBoxSuppressConfirmations
            // 
            this.checkBoxSuppressConfirmations.AutoSize = true;
            this.checkBoxSuppressConfirmations.Location = new System.Drawing.Point(6, 295);
            this.checkBoxSuppressConfirmations.Name = "checkBoxSuppressConfirmations";
            this.checkBoxSuppressConfirmations.Size = new System.Drawing.Size(170, 17);
            this.checkBoxSuppressConfirmations.TabIndex = 16;
            this.checkBoxSuppressConfirmations.Text = "Suppress control confirmations";
            this.checkBoxSuppressConfirmations.UseVisualStyleBackColor = true;
            // 
            // checkDispatcher
            // 
            this.checkDispatcher.AutoSize = true;
            this.checkDispatcher.Location = new System.Drawing.Point(6, 318);
            this.checkDispatcher.Name = "checkDispatcher";
            this.checkDispatcher.Size = new System.Drawing.Size(145, 17);
            this.checkDispatcher.TabIndex = 17;
            this.checkDispatcher.Text = "View Dispatcher Window";
            this.checkDispatcher.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(452, 429);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.numericCab2DStretch);
            this.tabPage1.Controls.Add(this.labelCab2DStretch);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.checkDispatcher);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.checkBoxSuppressConfirmations);
            this.tabPage1.Controls.Add(this.numericWorldObjectDensity);
            this.tabPage1.Controls.Add(this.checkBoxAlerter);
            this.tabPage1.Controls.Add(this.numericSoundDetailLevel);
            this.tabPage1.Controls.Add(this.checkBoxBINSound);
            this.tabPage1.Controls.Add(this.comboBoxWindowSize);
            this.tabPage1.Controls.Add(this.checkBoxWindowGlass);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.checkBoxTrainLights);
            this.tabPage1.Controls.Add(this.checkBoxShadows);
            this.tabPage1.Controls.Add(this.numericBrakePipeChargingRatePSIpS);
            this.tabPage1.Controls.Add(this.checkBoxGraduatedRelease);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.checkBoxWire);
            this.tabPage1.Controls.Add(this.checkBoxPrecipitation);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(444, 403);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // numericCab2DStretch
            // 
            this.numericCab2DStretch.Increment = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.numericCab2DStretch.Location = new System.Drawing.Point(6, 341);
            this.numericCab2DStretch.Name = "numericCab2DStretch";
            this.numericCab2DStretch.Size = new System.Drawing.Size(52, 20);
            this.numericCab2DStretch.TabIndex = 25;
            this.toolTip1.SetToolTip(this.numericCab2DStretch, "0 to clip cab view, 100 to stretch it. For cab views that match the display, use " +
                    "100.");
            // 
            // labelCab2DStretch
            // 
            this.labelCab2DStretch.AutoSize = true;
            this.labelCab2DStretch.Location = new System.Drawing.Point(64, 343);
            this.labelCab2DStretch.Name = "labelCab2DStretch";
            this.labelCab2DStretch.Size = new System.Drawing.Size(91, 13);
            this.labelCab2DStretch.TabIndex = 19;
            this.labelCab2DStretch.Text = "% Cab 2D Stretch";
            // 
            // tabPage2
            // 
            this.tabPage2.AutoScroll = true;
            this.tabPage2.Controls.Add(this.buttonExport);
            this.tabPage2.Controls.Add(this.buttonDefaultKeys);
            this.tabPage2.Controls.Add(this.buttonDebug);
            this.tabPage2.Controls.Add(this.buttonCheckKeys);
            this.tabPage2.Controls.Add(this.panelKeys);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(444, 403);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Keyboard";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // buttonExport
            // 
            this.buttonExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExport.Location = new System.Drawing.Point(363, 374);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(75, 23);
            this.buttonExport.TabIndex = 4;
            this.buttonExport.Text = "Export";
            this.toolTip1.SetToolTip(this.buttonExport, "Generate a listing of your keyboard assignments.  \r\nThe output is placed on your " +
                    "desktop.");
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.buttonExport_Click);
            // 
            // buttonDefaultKeys
            // 
            this.buttonDefaultKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDefaultKeys.Location = new System.Drawing.Point(87, 374);
            this.buttonDefaultKeys.Name = "buttonDefaultKeys";
            this.buttonDefaultKeys.Size = new System.Drawing.Size(75, 23);
            this.buttonDefaultKeys.TabIndex = 2;
            this.buttonDefaultKeys.Text = "Defaults";
            this.toolTip1.SetToolTip(this.buttonDefaultKeys, "Load the factory default key assignments.");
            this.buttonDefaultKeys.UseVisualStyleBackColor = true;
            this.buttonDefaultKeys.Click += new System.EventHandler(this.buttonDefaultKeys_Click);
            // 
            // buttonDebug
            // 
            this.buttonDebug.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDebug.Location = new System.Drawing.Point(168, 374);
            this.buttonDebug.Name = "buttonDebug";
            this.buttonDebug.Size = new System.Drawing.Size(75, 23);
            this.buttonDebug.TabIndex = 3;
            this.buttonDebug.Text = "Debug";
            this.toolTip1.SetToolTip(this.buttonDebug, "Run a more complete check.");
            this.buttonDebug.UseVisualStyleBackColor = true;
            this.buttonDebug.Click += new System.EventHandler(this.buttonDebug_Click);
            // 
            // buttonCheckKeys
            // 
            this.buttonCheckKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCheckKeys.Location = new System.Drawing.Point(6, 374);
            this.buttonCheckKeys.Name = "buttonCheckKeys";
            this.buttonCheckKeys.Size = new System.Drawing.Size(75, 23);
            this.buttonCheckKeys.TabIndex = 1;
            this.buttonCheckKeys.Text = "Check";
            this.toolTip1.SetToolTip(this.buttonCheckKeys, "Check for incorrect key assignments.");
            this.buttonCheckKeys.UseVisualStyleBackColor = true;
            this.buttonCheckKeys.Click += new System.EventHandler(this.buttonCheckKeys_Click);
            // 
            // panelKeys
            // 
            this.panelKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panelKeys.AutoScroll = true;
            this.panelKeys.Location = new System.Drawing.Point(6, 6);
            this.panelKeys.Name = "panelKeys";
            this.panelKeys.Size = new System.Drawing.Size(432, 362);
            this.panelKeys.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.checkBoxBreakCouplers);
            this.tabPage3.Controls.Add(this.checkBoxAdvancedAdhesion);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(444, 403);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Simulation";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // checkBoxAdvancedAdhesion
            // 
            this.checkBoxAdvancedAdhesion.AutoSize = true;
            this.checkBoxAdvancedAdhesion.Location = new System.Drawing.Point(6, 6);
            this.checkBoxAdvancedAdhesion.Name = "checkBoxAdvancedAdhesion";
            this.checkBoxAdvancedAdhesion.Size = new System.Drawing.Size(176, 17);
            this.checkBoxAdvancedAdhesion.TabIndex = 0;
            this.checkBoxAdvancedAdhesion.Text = "Use Advanced Adhesion Model";
            this.checkBoxAdvancedAdhesion.UseVisualStyleBackColor = true;
            // 
            // checkBoxBreakCouplers
            // 
            this.checkBoxBreakCouplers.AutoSize = true;
            this.checkBoxBreakCouplers.Location = new System.Drawing.Point(6, 29);
            this.checkBoxBreakCouplers.Name = "checkBoxBreakCouplers";
            this.checkBoxBreakCouplers.Size = new System.Drawing.Size(98, 17);
            this.checkBoxBreakCouplers.TabIndex = 1;
            this.checkBoxBreakCouplers.Text = "Break Couplers";
            this.checkBoxBreakCouplers.UseVisualStyleBackColor = true;
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(476, 482);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericWorldObjectDensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundDetailLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericBrakePipeChargingRatePSIpS)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericCab2DStretch)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericWorldObjectDensity;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericSoundDetailLevel;
        private System.Windows.Forms.ComboBox comboBoxWindowSize;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxTrainLights;
        private System.Windows.Forms.NumericUpDown numericBrakePipeChargingRatePSIpS;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBoxPrecipitation;
        private System.Windows.Forms.CheckBox checkBoxGraduatedRelease;
        private System.Windows.Forms.CheckBox checkBoxWire;
        private System.Windows.Forms.CheckBox checkBoxShadows;
        private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.CheckBox checkBoxWindowGlass;
        private System.Windows.Forms.CheckBox checkBoxBINSound;
        private System.Windows.Forms.CheckBox checkBoxAlerter;
        private System.Windows.Forms.CheckBox checkBoxSuppressConfirmations;
		private System.Windows.Forms.CheckBox checkDispatcher;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button buttonDefaultKeys;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonCheckKeys;
        private System.Windows.Forms.Panel panelKeys;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.Button buttonDebug;
        private System.Windows.Forms.Label labelCab2DStretch;
        private System.Windows.Forms.NumericUpDown numericCab2DStretch;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.CheckBox checkBoxAdvancedAdhesion;
        private System.Windows.Forms.CheckBox checkBoxBreakCouplers;
    }
}