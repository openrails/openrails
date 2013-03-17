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
            this.buttonOK = new System.Windows.Forms.Button();
            this.numericBrakePipeChargingRatePSIpS = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBoxGraduatedRelease = new System.Windows.Forms.CheckBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkBoxAlerter = new System.Windows.Forms.CheckBox();
            this.checkBoxSuppressConfirmations = new System.Windows.Forms.CheckBox();
            this.checkDispatcher = new System.Windows.Forms.CheckBox();
            this.tabOptions = new System.Windows.Forms.TabControl();
            this.tabPageGeneral = new System.Windows.Forms.TabPage();
            this.tabPageKeyboard = new System.Windows.Forms.TabPage();
            this.buttonExport = new System.Windows.Forms.Button();
            this.buttonDefaultKeys = new System.Windows.Forms.Button();
            this.buttonDebug = new System.Windows.Forms.Button();
            this.buttonCheckKeys = new System.Windows.Forms.Button();
            this.panelKeys = new System.Windows.Forms.Panel();
            this.tabPageSimulation = new System.Windows.Forms.TabPage();
            this.checkBoxBreakCouplers = new System.Windows.Forms.CheckBox();
            this.checkBoxAdvancedAdhesion = new System.Windows.Forms.CheckBox();
            this.tabPageExperimental = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.ElevationAmount = new System.Windows.Forms.NumericUpDown();
            this.ElevationText = new System.Windows.Forms.Label();
            this.checkBoxAvoidTreeonTrack = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabPageVideo = new System.Windows.Forms.TabPage();
            this.tabPageAudio = new System.Windows.Forms.TabPage();
            this.numericCab2DStretch = new System.Windows.Forms.NumericUpDown();
            this.labelCab2DStretch = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.numericWorldObjectDensity = new System.Windows.Forms.NumericUpDown();
            this.comboBoxWindowSize = new System.Windows.Forms.ComboBox();
            this.checkBoxWindowGlass = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxTrainLights = new System.Windows.Forms.CheckBox();
            this.checkBoxShadows = new System.Windows.Forms.CheckBox();
            this.checkBoxWire = new System.Windows.Forms.CheckBox();
            this.checkBoxPrecipitation = new System.Windows.Forms.CheckBox();
            this.soundVolume = new System.Windows.Forms.NumericUpDown();
            this.soundVolumeLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericSoundDetailLevel = new System.Windows.Forms.NumericUpDown();
            this.checkBoxBINSound = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericBrakePipeChargingRatePSIpS)).BeginInit();
            this.tabOptions.SuspendLayout();
            this.tabPageGeneral.SuspendLayout();
            this.tabPageKeyboard.SuspendLayout();
            this.tabPageSimulation.SuspendLayout();
            this.tabPageExperimental.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ElevationAmount)).BeginInit();
            this.tabPageVideo.SuspendLayout();
            this.tabPageAudio.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericCab2DStretch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericWorldObjectDensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.soundVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundDetailLevel)).BeginInit();
            this.SuspendLayout();
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
            // numericBrakePipeChargingRatePSIpS
            // 
            this.numericBrakePipeChargingRatePSIpS.Location = new System.Drawing.Point(6, 52);
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
            this.label4.Location = new System.Drawing.Point(64, 54);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(166, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Brake Pipe Charging Rate (PSI/s)";
            // 
            // checkBoxGraduatedRelease
            // 
            this.checkBoxGraduatedRelease.AutoSize = true;
            this.checkBoxGraduatedRelease.Location = new System.Drawing.Point(6, 29);
            this.checkBoxGraduatedRelease.Name = "checkBoxGraduatedRelease";
            this.checkBoxGraduatedRelease.Size = new System.Drawing.Size(169, 17);
            this.checkBoxGraduatedRelease.TabIndex = 10;
            this.checkBoxGraduatedRelease.Text = "Graduated Release Air Brakes";
            this.checkBoxGraduatedRelease.UseVisualStyleBackColor = true;
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
            // checkBoxAlerter
            // 
            this.checkBoxAlerter.AutoSize = true;
            this.checkBoxAlerter.Location = new System.Drawing.Point(6, 6);
            this.checkBoxAlerter.Name = "checkBoxAlerter";
            this.checkBoxAlerter.Size = new System.Drawing.Size(56, 17);
            this.checkBoxAlerter.TabIndex = 6;
            this.checkBoxAlerter.Text = "Alerter";
            this.checkBoxAlerter.UseVisualStyleBackColor = true;
            // 
            // checkBoxSuppressConfirmations
            // 
            this.checkBoxSuppressConfirmations.AutoSize = true;
            this.checkBoxSuppressConfirmations.Location = new System.Drawing.Point(6, 78);
            this.checkBoxSuppressConfirmations.Name = "checkBoxSuppressConfirmations";
            this.checkBoxSuppressConfirmations.Size = new System.Drawing.Size(170, 17);
            this.checkBoxSuppressConfirmations.TabIndex = 16;
            this.checkBoxSuppressConfirmations.Text = "Suppress control confirmations";
            this.checkBoxSuppressConfirmations.UseVisualStyleBackColor = true;
            // 
            // checkDispatcher
            // 
            this.checkDispatcher.AutoSize = true;
            this.checkDispatcher.Location = new System.Drawing.Point(6, 101);
            this.checkDispatcher.Name = "checkDispatcher";
            this.checkDispatcher.Size = new System.Drawing.Size(145, 17);
            this.checkDispatcher.TabIndex = 17;
            this.checkDispatcher.Text = "View Dispatcher Window";
            this.checkDispatcher.UseVisualStyleBackColor = true;
            // 
            // tabOptions
            // 
            this.tabOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabOptions.Controls.Add(this.tabPageGeneral);
            this.tabOptions.Controls.Add(this.tabPageAudio);
            this.tabOptions.Controls.Add(this.tabPageVideo);
            this.tabOptions.Controls.Add(this.tabPageSimulation);
            this.tabOptions.Controls.Add(this.tabPageKeyboard);
            this.tabOptions.Controls.Add(this.tabPageExperimental);
            this.tabOptions.Location = new System.Drawing.Point(12, 12);
            this.tabOptions.Name = "tabOptions";
            this.tabOptions.SelectedIndex = 0;
            this.tabOptions.Size = new System.Drawing.Size(452, 429);
            this.tabOptions.TabIndex = 0;
            // 
            // tabPageGeneral
            // 
            this.tabPageGeneral.Controls.Add(this.checkDispatcher);
            this.tabPageGeneral.Controls.Add(this.checkBoxSuppressConfirmations);
            this.tabPageGeneral.Controls.Add(this.checkBoxAlerter);
            this.tabPageGeneral.Controls.Add(this.numericBrakePipeChargingRatePSIpS);
            this.tabPageGeneral.Controls.Add(this.checkBoxGraduatedRelease);
            this.tabPageGeneral.Controls.Add(this.label4);
            this.tabPageGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabPageGeneral.Name = "tabPageGeneral";
            this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPageGeneral.Size = new System.Drawing.Size(444, 403);
            this.tabPageGeneral.TabIndex = 0;
            this.tabPageGeneral.Text = "General";
            this.tabPageGeneral.UseVisualStyleBackColor = true;
            // 
            // tabPageKeyboard
            // 
            this.tabPageKeyboard.AutoScroll = true;
            this.tabPageKeyboard.Controls.Add(this.buttonExport);
            this.tabPageKeyboard.Controls.Add(this.buttonDefaultKeys);
            this.tabPageKeyboard.Controls.Add(this.buttonDebug);
            this.tabPageKeyboard.Controls.Add(this.buttonCheckKeys);
            this.tabPageKeyboard.Controls.Add(this.panelKeys);
            this.tabPageKeyboard.Location = new System.Drawing.Point(4, 22);
            this.tabPageKeyboard.Name = "tabPageKeyboard";
            this.tabPageKeyboard.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPageKeyboard.Size = new System.Drawing.Size(444, 403);
            this.tabPageKeyboard.TabIndex = 1;
            this.tabPageKeyboard.Text = "Keyboard";
            this.tabPageKeyboard.UseVisualStyleBackColor = true;
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
            // tabPageSimulation
            // 
            this.tabPageSimulation.Controls.Add(this.checkBoxBreakCouplers);
            this.tabPageSimulation.Controls.Add(this.checkBoxAdvancedAdhesion);
            this.tabPageSimulation.Location = new System.Drawing.Point(4, 22);
            this.tabPageSimulation.Name = "tabPageSimulation";
            this.tabPageSimulation.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPageSimulation.Size = new System.Drawing.Size(346, 148);
            this.tabPageSimulation.TabIndex = 2;
            this.tabPageSimulation.Text = "Simulation";
            this.tabPageSimulation.UseVisualStyleBackColor = true;
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
            // tabPageExperimental
            // 
            this.tabPageExperimental.Controls.Add(this.label5);
            this.tabPageExperimental.Controls.Add(this.ElevationAmount);
            this.tabPageExperimental.Controls.Add(this.ElevationText);
            this.tabPageExperimental.Controls.Add(this.checkBoxAvoidTreeonTrack);
            this.tabPageExperimental.Location = new System.Drawing.Point(4, 22);
            this.tabPageExperimental.Name = "tabPageExperimental";
            this.tabPageExperimental.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageExperimental.Size = new System.Drawing.Size(444, 403);
            this.tabPageExperimental.TabIndex = 3;
            this.tabPageExperimental.Text = "Experimental";
            this.tabPageExperimental.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.SystemColors.Highlight;
            this.label5.Location = new System.Drawing.Point(6, 6);
            this.label5.Margin = new System.Windows.Forms.Padding(3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(354, 13);
            this.label5.TabIndex = 33;
            this.label5.Text = "Experimental features that may slow down the game, use at your own risk.";
            // 
            // ElevationAmount
            // 
            this.ElevationAmount.Location = new System.Drawing.Point(6, 48);
            this.ElevationAmount.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.ElevationAmount.Name = "ElevationAmount";
            this.ElevationAmount.Size = new System.Drawing.Size(52, 20);
            this.ElevationAmount.TabIndex = 32;
            this.toolTip1.SetToolTip(this.ElevationAmount, "Elevation 0-3");
            // 
            // ElevationText
            // 
            this.ElevationText.AutoSize = true;
            this.ElevationText.Location = new System.Drawing.Point(64, 50);
            this.ElevationText.Name = "ElevationText";
            this.ElevationText.Size = new System.Drawing.Size(121, 13);
            this.ElevationText.TabIndex = 31;
            this.ElevationText.Text = "Super Elevation Amount";
            // 
            // checkBoxAvoidTreeonTrack
            // 
            this.checkBoxAvoidTreeonTrack.AutoSize = true;
            this.checkBoxAvoidTreeonTrack.Location = new System.Drawing.Point(6, 25);
            this.checkBoxAvoidTreeonTrack.Name = "checkBoxAvoidTreeonTrack";
            this.checkBoxAvoidTreeonTrack.Size = new System.Drawing.Size(124, 17);
            this.checkBoxAvoidTreeonTrack.TabIndex = 30;
            this.checkBoxAvoidTreeonTrack.Text = "Avoid Tree on Track";
            this.checkBoxAvoidTreeonTrack.UseVisualStyleBackColor = true;
            // 
            // tabPageVideo
            // 
            this.tabPageVideo.Controls.Add(this.numericCab2DStretch);
            this.tabPageVideo.Controls.Add(this.labelCab2DStretch);
            this.tabPageVideo.Controls.Add(this.label1);
            this.tabPageVideo.Controls.Add(this.numericWorldObjectDensity);
            this.tabPageVideo.Controls.Add(this.comboBoxWindowSize);
            this.tabPageVideo.Controls.Add(this.checkBoxWindowGlass);
            this.tabPageVideo.Controls.Add(this.label3);
            this.tabPageVideo.Controls.Add(this.checkBoxTrainLights);
            this.tabPageVideo.Controls.Add(this.checkBoxShadows);
            this.tabPageVideo.Controls.Add(this.checkBoxWire);
            this.tabPageVideo.Controls.Add(this.checkBoxPrecipitation);
            this.tabPageVideo.Location = new System.Drawing.Point(4, 22);
            this.tabPageVideo.Name = "tabPageVideo";
            this.tabPageVideo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageVideo.Size = new System.Drawing.Size(346, 148);
            this.tabPageVideo.TabIndex = 4;
            this.tabPageVideo.Text = "Video";
            this.tabPageVideo.UseVisualStyleBackColor = true;
            // 
            // tabPageAudio
            // 
            this.tabPageAudio.Controls.Add(this.soundVolume);
            this.tabPageAudio.Controls.Add(this.soundVolumeLabel);
            this.tabPageAudio.Controls.Add(this.label2);
            this.tabPageAudio.Controls.Add(this.numericSoundDetailLevel);
            this.tabPageAudio.Controls.Add(this.checkBoxBINSound);
            this.tabPageAudio.Location = new System.Drawing.Point(4, 22);
            this.tabPageAudio.Name = "tabPageAudio";
            this.tabPageAudio.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAudio.Size = new System.Drawing.Size(346, 148);
            this.tabPageAudio.TabIndex = 5;
            this.tabPageAudio.Text = "Audio";
            this.tabPageAudio.UseVisualStyleBackColor = true;
            // 
            // numericCab2DStretch
            // 
            this.numericCab2DStretch.Increment = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.numericCab2DStretch.Location = new System.Drawing.Point(6, 174);
            this.numericCab2DStretch.Name = "numericCab2DStretch";
            this.numericCab2DStretch.Size = new System.Drawing.Size(52, 20);
            this.numericCab2DStretch.TabIndex = 36;
            this.toolTip1.SetToolTip(this.numericCab2DStretch, "0 to clip cab view, 100 to stretch it. For cab views that match the display, use " +
                    "100.");
            // 
            // labelCab2DStretch
            // 
            this.labelCab2DStretch.AutoSize = true;
            this.labelCab2DStretch.Location = new System.Drawing.Point(64, 176);
            this.labelCab2DStretch.Name = "labelCab2DStretch";
            this.labelCab2DStretch.Size = new System.Drawing.Size(91, 13);
            this.labelCab2DStretch.TabIndex = 35;
            this.labelCab2DStretch.Text = "% Cab 2D Stretch";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(64, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 27;
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
            this.numericWorldObjectDensity.TabIndex = 26;
            // 
            // comboBoxWindowSize
            // 
            this.comboBoxWindowSize.FormattingEnabled = true;
            this.comboBoxWindowSize.Location = new System.Drawing.Point(6, 32);
            this.comboBoxWindowSize.Name = "comboBoxWindowSize";
            this.comboBoxWindowSize.Size = new System.Drawing.Size(121, 21);
            this.comboBoxWindowSize.TabIndex = 28;
            // 
            // checkBoxWindowGlass
            // 
            this.checkBoxWindowGlass.AutoSize = true;
            this.checkBoxWindowGlass.Location = new System.Drawing.Point(6, 151);
            this.checkBoxWindowGlass.Name = "checkBoxWindowGlass";
            this.checkBoxWindowGlass.Size = new System.Drawing.Size(171, 17);
            this.checkBoxWindowGlass.TabIndex = 34;
            this.checkBoxWindowGlass.Text = "Use glass on in-game windows";
            this.checkBoxWindowGlass.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 29;
            this.label3.Text = "Window size";
            // 
            // checkBoxTrainLights
            // 
            this.checkBoxTrainLights.AutoSize = true;
            this.checkBoxTrainLights.Location = new System.Drawing.Point(6, 59);
            this.checkBoxTrainLights.Name = "checkBoxTrainLights";
            this.checkBoxTrainLights.Size = new System.Drawing.Size(81, 17);
            this.checkBoxTrainLights.TabIndex = 30;
            this.checkBoxTrainLights.Text = "Train Lights";
            this.checkBoxTrainLights.UseVisualStyleBackColor = true;
            // 
            // checkBoxShadows
            // 
            this.checkBoxShadows.AutoSize = true;
            this.checkBoxShadows.Location = new System.Drawing.Point(6, 128);
            this.checkBoxShadows.Name = "checkBoxShadows";
            this.checkBoxShadows.Size = new System.Drawing.Size(114, 17);
            this.checkBoxShadows.TabIndex = 33;
            this.checkBoxShadows.Text = "Dynamic Shadows";
            this.checkBoxShadows.UseVisualStyleBackColor = true;
            // 
            // checkBoxWire
            // 
            this.checkBoxWire.AutoSize = true;
            this.checkBoxWire.Location = new System.Drawing.Point(6, 105);
            this.checkBoxWire.Name = "checkBoxWire";
            this.checkBoxWire.Size = new System.Drawing.Size(98, 17);
            this.checkBoxWire.TabIndex = 32;
            this.checkBoxWire.Text = "Overhead Wire";
            this.checkBoxWire.UseVisualStyleBackColor = true;
            // 
            // checkBoxPrecipitation
            // 
            this.checkBoxPrecipitation.AutoSize = true;
            this.checkBoxPrecipitation.Location = new System.Drawing.Point(6, 82);
            this.checkBoxPrecipitation.Name = "checkBoxPrecipitation";
            this.checkBoxPrecipitation.Size = new System.Drawing.Size(84, 17);
            this.checkBoxPrecipitation.TabIndex = 31;
            this.checkBoxPrecipitation.Text = "Precipitation";
            this.checkBoxPrecipitation.UseVisualStyleBackColor = true;
            // 
            // soundVolume
            // 
            this.soundVolume.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.soundVolume.Location = new System.Drawing.Point(6, 55);
            this.soundVolume.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.soundVolume.Name = "soundVolume";
            this.soundVolume.Size = new System.Drawing.Size(52, 20);
            this.soundVolume.TabIndex = 32;
            this.toolTip1.SetToolTip(this.soundVolume, "Sound Volume 0-100");
            this.soundVolume.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // soundVolumeLabel
            // 
            this.soundVolumeLabel.AutoSize = true;
            this.soundVolumeLabel.Location = new System.Drawing.Point(64, 57);
            this.soundVolumeLabel.Name = "soundVolumeLabel";
            this.soundVolumeLabel.Size = new System.Drawing.Size(87, 13);
            this.soundVolumeLabel.TabIndex = 31;
            this.soundVolumeLabel.Text = "% Sound Volume";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(64, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(97, 13);
            this.label2.TabIndex = 29;
            this.label2.Text = "Sound Detail Level";
            // 
            // numericSoundDetailLevel
            // 
            this.numericSoundDetailLevel.Location = new System.Drawing.Point(6, 6);
            this.numericSoundDetailLevel.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericSoundDetailLevel.Name = "numericSoundDetailLevel";
            this.numericSoundDetailLevel.Size = new System.Drawing.Size(52, 20);
            this.numericSoundDetailLevel.TabIndex = 28;
            // 
            // checkBoxBINSound
            // 
            this.checkBoxBINSound.AutoSize = true;
            this.checkBoxBINSound.Location = new System.Drawing.Point(6, 32);
            this.checkBoxBINSound.Name = "checkBoxBINSound";
            this.checkBoxBINSound.Size = new System.Drawing.Size(185, 17);
            this.checkBoxBINSound.TabIndex = 30;
            this.checkBoxBINSound.Text = "Use MSTS BIN compatible sound";
            this.checkBoxBINSound.UseVisualStyleBackColor = true;
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(476, 482);
            this.Controls.Add(this.tabOptions);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericBrakePipeChargingRatePSIpS)).EndInit();
            this.tabOptions.ResumeLayout(false);
            this.tabPageGeneral.ResumeLayout(false);
            this.tabPageGeneral.PerformLayout();
            this.tabPageKeyboard.ResumeLayout(false);
            this.tabPageSimulation.ResumeLayout(false);
            this.tabPageSimulation.PerformLayout();
            this.tabPageExperimental.ResumeLayout(false);
            this.tabPageExperimental.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ElevationAmount)).EndInit();
            this.tabPageVideo.ResumeLayout(false);
            this.tabPageVideo.PerformLayout();
            this.tabPageAudio.ResumeLayout(false);
            this.tabPageAudio.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericCab2DStretch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericWorldObjectDensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.soundVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundDetailLevel)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.NumericUpDown numericBrakePipeChargingRatePSIpS;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBoxGraduatedRelease;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkBoxAlerter;
        private System.Windows.Forms.CheckBox checkBoxSuppressConfirmations;
		private System.Windows.Forms.CheckBox checkDispatcher;
        private System.Windows.Forms.TabControl tabOptions;
        private System.Windows.Forms.TabPage tabPageGeneral;
        private System.Windows.Forms.TabPage tabPageKeyboard;
        private System.Windows.Forms.Button buttonDefaultKeys;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonCheckKeys;
        private System.Windows.Forms.Panel panelKeys;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.Button buttonDebug;
        private System.Windows.Forms.TabPage tabPageSimulation;
        private System.Windows.Forms.CheckBox checkBoxAdvancedAdhesion;
        private System.Windows.Forms.CheckBox checkBoxBreakCouplers;
        private System.Windows.Forms.TabPage tabPageExperimental;
        private System.Windows.Forms.NumericUpDown ElevationAmount;
        private System.Windows.Forms.Label ElevationText;
        private System.Windows.Forms.CheckBox checkBoxAvoidTreeonTrack;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tabPageAudio;
        private System.Windows.Forms.TabPage tabPageVideo;
        private System.Windows.Forms.NumericUpDown soundVolume;
        private System.Windows.Forms.Label soundVolumeLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericSoundDetailLevel;
        private System.Windows.Forms.CheckBox checkBoxBINSound;
        private System.Windows.Forms.NumericUpDown numericCab2DStretch;
        private System.Windows.Forms.Label labelCab2DStretch;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericWorldObjectDensity;
        private System.Windows.Forms.ComboBox comboBoxWindowSize;
        private System.Windows.Forms.CheckBox checkBoxWindowGlass;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxTrainLights;
        private System.Windows.Forms.CheckBox checkBoxShadows;
        private System.Windows.Forms.CheckBox checkBoxWire;
        private System.Windows.Forms.CheckBox checkBoxPrecipitation;
    }
}