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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.buttonOK = new System.Windows.Forms.Button();
            this.numericBrakePipeChargingRate = new System.Windows.Forms.NumericUpDown();
            this.lBrakePipeChargingRate = new System.Windows.Forms.Label();
            this.checkGraduatedRelease = new System.Windows.Forms.CheckBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkAlerter = new System.Windows.Forms.CheckBox();
            this.checkControlConfirmations = new System.Windows.Forms.CheckBox();
            this.checkViewMapWindow = new System.Windows.Forms.CheckBox();
            this.tabOptions = new System.Windows.Forms.TabControl();
            this.tabPageGeneral = new System.Windows.Forms.TabPage();
            this.pbOverspeedMonitor = new System.Windows.Forms.PictureBox();
            this.pbEnableWebServer = new System.Windows.Forms.PictureBox();
            this.pbDisableTcsScripts = new System.Windows.Forms.PictureBox();
            this.pbOtherUnits = new System.Windows.Forms.PictureBox();
            this.pbPressureUnit = new System.Windows.Forms.PictureBox();
            this.pbLanguage = new System.Windows.Forms.PictureBox();
            this.pbBrakePipeChargingRate = new System.Windows.Forms.PictureBox();
            this.pbGraduatedRelease = new System.Windows.Forms.PictureBox();
            this.pbRetainers = new System.Windows.Forms.PictureBox();
            this.pbLAA = new System.Windows.Forms.PictureBox();
            this.pbAlerter = new System.Windows.Forms.PictureBox();
            this.pbControlConfirmations = new System.Windows.Forms.PictureBox();
            this.pbMapWindow = new System.Windows.Forms.PictureBox();
            this.labelPortNumber = new System.Windows.Forms.Label();
            this.numericWebServerPort = new System.Windows.Forms.NumericUpDown();
            this.checkEnableWebServer = new System.Windows.Forms.CheckBox();
            this.checkOverspeedMonitor = new System.Windows.Forms.CheckBox();
            this.checkDisableTCSScripts = new System.Windows.Forms.CheckBox();
            this.labelOtherUnits = new System.Windows.Forms.Label();
            this.labelPressureUnit = new System.Windows.Forms.Label();
            this.comboOtherUnits = new System.Windows.Forms.ComboBox();
            this.checkUseLargeAddressAware = new System.Windows.Forms.CheckBox();
            this.comboPressureUnit = new System.Windows.Forms.ComboBox();
            this.labelLanguage = new System.Windows.Forms.Label();
            this.comboLanguage = new System.Windows.Forms.ComboBox();
            this.checkAlerterExternal = new System.Windows.Forms.CheckBox();
            this.checkRetainers = new System.Windows.Forms.CheckBox();
            this.tabPageAudio = new System.Windows.Forms.TabPage();
            this.numericExternalSoundPassThruPercent = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.numericSoundVolumePercent = new System.Windows.Forms.NumericUpDown();
            this.soundVolumeLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericSoundDetailLevel = new System.Windows.Forms.NumericUpDown();
            this.checkMSTSBINSound = new System.Windows.Forms.CheckBox();
            this.tabPageVideo = new System.Windows.Forms.TabPage();
            this.labelAntiAliasingValue = new System.Windows.Forms.Label();
            this.labelAntiAliasing = new System.Windows.Forms.Label();
            this.trackAntiAliasing = new System.Windows.Forms.TrackBar();
            this.checkShadowAllShapes = new System.Windows.Forms.CheckBox();
            this.checkDoubleWire = new System.Windows.Forms.CheckBox();
            this.label15 = new System.Windows.Forms.Label();
            this.labelDayAmbientLight = new System.Windows.Forms.Label();
            this.checkModelInstancing = new System.Windows.Forms.CheckBox();
            this.trackDayAmbientLight = new System.Windows.Forms.TrackBar();
            this.checkVerticalSync = new System.Windows.Forms.CheckBox();
            this.labelDistantMountainsViewingDistance = new System.Windows.Forms.Label();
            this.numericDistantMountainsViewingDistance = new System.Windows.Forms.NumericUpDown();
            this.checkFastFullScreenAltTab = new System.Windows.Forms.CheckBox();
            this.checkDistantMountains = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.numericViewingDistance = new System.Windows.Forms.NumericUpDown();
            this.labelFOVHelp = new System.Windows.Forms.Label();
            this.numericViewingFOV = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.numericCab2DStretch = new System.Windows.Forms.NumericUpDown();
            this.labelCab2DStretch = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.numericWorldObjectDensity = new System.Windows.Forms.NumericUpDown();
            this.comboWindowSize = new System.Windows.Forms.ComboBox();
            this.checkWindowGlass = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkDynamicShadows = new System.Windows.Forms.CheckBox();
            this.checkWire = new System.Windows.Forms.CheckBox();
            this.tabPageSimulation = new System.Windows.Forms.TabPage();
            this.checkBoxNoDieselEngineStart = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkUseLocationPassingPaths = new System.Windows.Forms.CheckBox();
            this.checkDoorsAITrains = new System.Windows.Forms.CheckBox();
            this.checkForcedRedAtStationStops = new System.Windows.Forms.CheckBox();
            this.checkHotStart = new System.Windows.Forms.CheckBox();
            this.checkSimpleControlsPhysics = new System.Windows.Forms.CheckBox();
            this.checkCurveSpeedDependent = new System.Windows.Forms.CheckBox();
            this.checkCurveResistanceDependent = new System.Windows.Forms.CheckBox();
            this.checkTunnelResistanceDependent = new System.Windows.Forms.CheckBox();
            this.checkWindResistanceDependent = new System.Windows.Forms.CheckBox();
            this.checkOverrideNonElectrifiedRoutes = new System.Windows.Forms.CheckBox();
            this.labelAdhesionMovingAverageFilterSize = new System.Windows.Forms.Label();
            this.numericAdhesionMovingAverageFilterSize = new System.Windows.Forms.NumericUpDown();
            this.checkBreakCouplers = new System.Windows.Forms.CheckBox();
            this.checkUseAdvancedAdhesion = new System.Windows.Forms.CheckBox();
            this.tabPageKeyboard = new System.Windows.Forms.TabPage();
            this.buttonExport = new System.Windows.Forms.Button();
            this.buttonDefaultKeys = new System.Windows.Forms.Button();
            this.buttonCheckKeys = new System.Windows.Forms.Button();
            this.panelKeys = new System.Windows.Forms.Panel();
            this.tabPageDataLogger = new System.Windows.Forms.TabPage();
            this.comboDataLogSpeedUnits = new System.Windows.Forms.ComboBox();
            this.comboDataLoggerSeparator = new System.Windows.Forms.ComboBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.checkDataLogMisc = new System.Windows.Forms.CheckBox();
            this.checkDataLogPerformance = new System.Windows.Forms.CheckBox();
            this.checkDataLogger = new System.Windows.Forms.CheckBox();
            this.label17 = new System.Windows.Forms.Label();
            this.checkDataLogPhysics = new System.Windows.Forms.CheckBox();
            this.checkDataLogSteamPerformance = new System.Windows.Forms.CheckBox();
            this.checkVerboseConfigurationMessages = new System.Windows.Forms.CheckBox();
            this.tabPageEvaluate = new System.Windows.Forms.TabPage();
            this.checkListDataLogTSContents = new System.Windows.Forms.CheckedListBox();
            this.labelDataLogTSInterval = new System.Windows.Forms.Label();
            this.checkDataLogStationStops = new System.Windows.Forms.CheckBox();
            this.numericDataLogTSInterval = new System.Windows.Forms.NumericUpDown();
            this.checkDataLogTrainSpeed = new System.Windows.Forms.CheckBox();
            this.tabPageContent = new System.Windows.Forms.TabPage();
            this.labelContent = new System.Windows.Forms.Label();
            this.buttonContentDelete = new System.Windows.Forms.Button();
            this.groupBoxContent = new System.Windows.Forms.GroupBox();
            this.buttonContentBrowse = new System.Windows.Forms.Button();
            this.textBoxContentPath = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.textBoxContentName = new System.Windows.Forms.TextBox();
            this.buttonContentAdd = new System.Windows.Forms.Button();
            this.panelContent = new System.Windows.Forms.Panel();
            this.dataGridViewContent = new System.Windows.Forms.DataGridView();
            this.nameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bindingSourceContent = new System.Windows.Forms.BindingSource(this.components);
            this.tabPageUpdater = new System.Windows.Forms.TabPage();
            this.labelUpdateChannel = new System.Windows.Forms.Label();
            this.tabPageExperimental = new System.Windows.Forms.TabPage();
            this.label27 = new System.Windows.Forms.Label();
            this.numericActWeatherRandomizationLevel = new System.Windows.Forms.NumericUpDown();
            this.label26 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.numericActRandomizationLevel = new System.Windows.Forms.NumericUpDown();
            this.checkCorrectQuestionableBrakingParams = new System.Windows.Forms.CheckBox();
            this.label25 = new System.Windows.Forms.Label();
            this.precipitationBoxLength = new System.Windows.Forms.NumericUpDown();
            this.label24 = new System.Windows.Forms.Label();
            this.precipitationBoxWidth = new System.Windows.Forms.NumericUpDown();
            this.label23 = new System.Windows.Forms.Label();
            this.precipitationBoxHeight = new System.Windows.Forms.NumericUpDown();
            this.label16 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.AdhesionFactorChangeValueLabel = new System.Windows.Forms.Label();
            this.AdhesionFactorValueLabel = new System.Windows.Forms.Label();
            this.labelLODBias = new System.Windows.Forms.Label();
            this.checkShapeWarnings = new System.Windows.Forms.CheckBox();
            this.trackLODBias = new System.Windows.Forms.TrackBar();
            this.checkConditionalLoadOfNightTextures = new System.Windows.Forms.CheckBox();
            this.AdhesionLevelValue = new System.Windows.Forms.Label();
            this.AdhesionLevelLabel = new System.Windows.Forms.Label();
            this.trackAdhesionFactorChange = new System.Windows.Forms.TrackBar();
            this.trackAdhesionFactor = new System.Windows.Forms.TrackBar();
            this.checkAdhesionPropToWeather = new System.Windows.Forms.CheckBox();
            this.checkCircularSpeedGauge = new System.Windows.Forms.CheckBox();
            this.checkSignalLightGlow = new System.Windows.Forms.CheckBox();
            this.checkUseMSTSEnv = new System.Windows.Forms.CheckBox();
            this.labelPerformanceTunerTarget = new System.Windows.Forms.Label();
            this.numericPerformanceTunerTarget = new System.Windows.Forms.NumericUpDown();
            this.checkPerformanceTuner = new System.Windows.Forms.CheckBox();
            this.checkLODViewingExtention = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.numericSuperElevationGauge = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.numericSuperElevationMinLen = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.numericUseSuperElevation = new System.Windows.Forms.NumericUpDown();
            this.ElevationText = new System.Windows.Forms.Label();
            this.checkPreferDDSTexture = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numericBrakePipeChargingRate)).BeginInit();
            this.tabOptions.SuspendLayout();
            this.tabPageGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOverspeedMonitor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEnableWebServer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDisableTcsScripts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbOtherUnits)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbPressureUnit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLanguage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrakePipeChargingRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbGraduatedRelease)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbRetainers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLAA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAlerter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbControlConfirmations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMapWindow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericWebServerPort)).BeginInit();
            this.tabPageAudio.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericExternalSoundPassThruPercent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundVolumePercent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundDetailLevel)).BeginInit();
            this.tabPageVideo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackAntiAliasing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackDayAmbientLight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDistantMountainsViewingDistance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericViewingDistance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericViewingFOV)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCab2DStretch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericWorldObjectDensity)).BeginInit();
            this.tabPageSimulation.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericAdhesionMovingAverageFilterSize)).BeginInit();
            this.tabPageKeyboard.SuspendLayout();
            this.tabPageDataLogger.SuspendLayout();
            this.tabPageEvaluate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericDataLogTSInterval)).BeginInit();
            this.tabPageContent.SuspendLayout();
            this.groupBoxContent.SuspendLayout();
            this.panelContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewContent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceContent)).BeginInit();
            this.tabPageUpdater.SuspendLayout();
            this.tabPageExperimental.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericActWeatherRandomizationLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericActRandomizationLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.precipitationBoxLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.precipitationBoxWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.precipitationBoxHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackLODBias)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAdhesionFactorChange)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAdhesionFactor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPerformanceTunerTarget)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSuperElevationGauge)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSuperElevationMinLen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUseSuperElevation)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(466, 446);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // numericBrakePipeChargingRate
            // 
            this.numericBrakePipeChargingRate.Location = new System.Drawing.Point(32, 167);
            this.numericBrakePipeChargingRate.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericBrakePipeChargingRate.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericBrakePipeChargingRate.Name = "numericBrakePipeChargingRate";
            this.numericBrakePipeChargingRate.Size = new System.Drawing.Size(54, 20);
            this.numericBrakePipeChargingRate.TabIndex = 7;
            this.numericBrakePipeChargingRate.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lBrakePipeChargingRate
            // 
            this.lBrakePipeChargingRate.AutoSize = true;
            this.lBrakePipeChargingRate.Location = new System.Drawing.Point(89, 169);
            this.lBrakePipeChargingRate.Margin = new System.Windows.Forms.Padding(3);
            this.lBrakePipeChargingRate.Name = "lBrakePipeChargingRate";
            this.lBrakePipeChargingRate.Size = new System.Drawing.Size(198, 13);
            this.lBrakePipeChargingRate.TabIndex = 8;
            this.lBrakePipeChargingRate.Text = "Brake pipe charging rate (PSI/s)             ";
            this.lBrakePipeChargingRate.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.lBrakePipeChargingRate.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // checkGraduatedRelease
            // 
            this.checkGraduatedRelease.AutoSize = true;
            this.checkGraduatedRelease.Location = new System.Drawing.Point(32, 144);
            this.checkGraduatedRelease.Name = "checkGraduatedRelease";
            this.checkGraduatedRelease.Size = new System.Drawing.Size(258, 17);
            this.checkGraduatedRelease.TabIndex = 6;
            this.checkGraduatedRelease.Text = "Graduated release air brakes                                ";
            this.checkGraduatedRelease.UseVisualStyleBackColor = true;
            this.checkGraduatedRelease.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkGraduatedRelease.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(547, 446);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkAlerter
            // 
            this.checkAlerter.AutoSize = true;
            this.checkAlerter.Location = new System.Drawing.Point(32, 6);
            this.checkAlerter.Name = "checkAlerter";
            this.checkAlerter.Size = new System.Drawing.Size(256, 17);
            this.checkAlerter.TabIndex = 0;
            this.checkAlerter.Text = "Alerter in cab                                                        ";
            this.checkAlerter.UseVisualStyleBackColor = true;
            this.checkAlerter.CheckedChanged += new System.EventHandler(this.checkAlerter_CheckedChanged);
            this.checkAlerter.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkAlerter.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // checkControlConfirmations
            // 
            this.checkControlConfirmations.AutoSize = true;
            this.checkControlConfirmations.Location = new System.Drawing.Point(32, 52);
            this.checkControlConfirmations.Name = "checkControlConfirmations";
            this.checkControlConfirmations.Size = new System.Drawing.Size(256, 17);
            this.checkControlConfirmations.TabIndex = 4;
            this.checkControlConfirmations.Text = "Control confirmations                                            ";
            this.checkControlConfirmations.UseVisualStyleBackColor = true;
            this.checkControlConfirmations.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkControlConfirmations.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // checkViewMapWindow
            // 
            this.checkViewMapWindow.AutoSize = true;
            this.checkViewMapWindow.Location = new System.Drawing.Point(32, 75);
            this.checkViewMapWindow.Name = "checkViewMapWindow";
            this.checkViewMapWindow.Size = new System.Drawing.Size(254, 17);
            this.checkViewMapWindow.TabIndex = 2;
            this.checkViewMapWindow.Text = "Map window                                                        ";
            this.checkViewMapWindow.UseVisualStyleBackColor = true;
            this.checkViewMapWindow.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkViewMapWindow.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
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
            this.tabOptions.Controls.Add(this.tabPageDataLogger);
            this.tabOptions.Controls.Add(this.tabPageEvaluate);
            this.tabOptions.Controls.Add(this.tabPageContent);
            this.tabOptions.Controls.Add(this.tabPageUpdater);
            this.tabOptions.Controls.Add(this.tabPageExperimental);
            this.tabOptions.Location = new System.Drawing.Point(13, 12);
            this.tabOptions.Name = "tabOptions";
            this.tabOptions.SelectedIndex = 0;
            this.tabOptions.Size = new System.Drawing.Size(610, 428);
            this.tabOptions.TabIndex = 0;
            // 
            // tabPageGeneral
            // 
            this.tabPageGeneral.Controls.Add(this.pbOverspeedMonitor);
            this.tabPageGeneral.Controls.Add(this.pbEnableWebServer);
            this.tabPageGeneral.Controls.Add(this.pbDisableTcsScripts);
            this.tabPageGeneral.Controls.Add(this.pbOtherUnits);
            this.tabPageGeneral.Controls.Add(this.pbPressureUnit);
            this.tabPageGeneral.Controls.Add(this.pbLanguage);
            this.tabPageGeneral.Controls.Add(this.pbBrakePipeChargingRate);
            this.tabPageGeneral.Controls.Add(this.pbGraduatedRelease);
            this.tabPageGeneral.Controls.Add(this.pbRetainers);
            this.tabPageGeneral.Controls.Add(this.pbLAA);
            this.tabPageGeneral.Controls.Add(this.pbAlerter);
            this.tabPageGeneral.Controls.Add(this.pbControlConfirmations);
            this.tabPageGeneral.Controls.Add(this.pbMapWindow);
            this.tabPageGeneral.Controls.Add(this.labelPortNumber);
            this.tabPageGeneral.Controls.Add(this.numericWebServerPort);
            this.tabPageGeneral.Controls.Add(this.checkEnableWebServer);
            this.tabPageGeneral.Controls.Add(this.checkOverspeedMonitor);
            this.tabPageGeneral.Controls.Add(this.checkDisableTCSScripts);
            this.tabPageGeneral.Controls.Add(this.labelOtherUnits);
            this.tabPageGeneral.Controls.Add(this.labelPressureUnit);
            this.tabPageGeneral.Controls.Add(this.comboOtherUnits);
            this.tabPageGeneral.Controls.Add(this.checkUseLargeAddressAware);
            this.tabPageGeneral.Controls.Add(this.comboPressureUnit);
            this.tabPageGeneral.Controls.Add(this.labelLanguage);
            this.tabPageGeneral.Controls.Add(this.comboLanguage);
            this.tabPageGeneral.Controls.Add(this.checkViewMapWindow);
            this.tabPageGeneral.Controls.Add(this.checkControlConfirmations);
            this.tabPageGeneral.Controls.Add(this.checkAlerterExternal);
            this.tabPageGeneral.Controls.Add(this.numericBrakePipeChargingRate);
            this.tabPageGeneral.Controls.Add(this.checkRetainers);
            this.tabPageGeneral.Controls.Add(this.checkGraduatedRelease);
            this.tabPageGeneral.Controls.Add(this.lBrakePipeChargingRate);
            this.tabPageGeneral.Controls.Add(this.checkAlerter);
            this.tabPageGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabPageGeneral.Name = "tabPageGeneral";
            this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGeneral.Size = new System.Drawing.Size(602, 402);
            this.tabPageGeneral.TabIndex = 0;
            this.tabPageGeneral.Text = "General";
            this.tabPageGeneral.UseVisualStyleBackColor = true;
            // 
            // pbOverspeedMonitor
            // 
            this.pbOverspeedMonitor.Image = global::ORTS.Properties.Resources.info_18;
            this.pbOverspeedMonitor.Location = new System.Drawing.Point(296, 7);
            this.pbOverspeedMonitor.Name = "pbOverspeedMonitor";
            this.pbOverspeedMonitor.Size = new System.Drawing.Size(18, 18);
            this.pbOverspeedMonitor.TabIndex = 30;
            this.pbOverspeedMonitor.TabStop = false;
            this.pbOverspeedMonitor.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbOverspeedMonitor.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbOverspeedMonitor.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbEnableWebServer
            // 
            this.pbEnableWebServer.Image = global::ORTS.Properties.Resources.info_18;
            this.pbEnableWebServer.Location = new System.Drawing.Point(6, 309);
            this.pbEnableWebServer.Name = "pbEnableWebServer";
            this.pbEnableWebServer.Size = new System.Drawing.Size(18, 18);
            this.pbEnableWebServer.TabIndex = 29;
            this.pbEnableWebServer.TabStop = false;
            this.pbEnableWebServer.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbEnableWebServer.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbEnableWebServer.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbDisableTcsScripts
            // 
            this.pbDisableTcsScripts.Image = global::ORTS.Properties.Resources.info_18;
            this.pbDisableTcsScripts.Location = new System.Drawing.Point(6, 286);
            this.pbDisableTcsScripts.Name = "pbDisableTcsScripts";
            this.pbDisableTcsScripts.Size = new System.Drawing.Size(18, 18);
            this.pbDisableTcsScripts.TabIndex = 28;
            this.pbDisableTcsScripts.TabStop = false;
            this.pbDisableTcsScripts.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbDisableTcsScripts.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbDisableTcsScripts.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbOtherUnits
            // 
            this.pbOtherUnits.Image = global::ORTS.Properties.Resources.info_18;
            this.pbOtherUnits.Location = new System.Drawing.Point(6, 257);
            this.pbOtherUnits.Name = "pbOtherUnits";
            this.pbOtherUnits.Size = new System.Drawing.Size(18, 18);
            this.pbOtherUnits.TabIndex = 27;
            this.pbOtherUnits.TabStop = false;
            this.pbOtherUnits.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbOtherUnits.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbOtherUnits.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbPressureUnit
            // 
            this.pbPressureUnit.Image = global::ORTS.Properties.Resources.info_18;
            this.pbPressureUnit.Location = new System.Drawing.Point(6, 230);
            this.pbPressureUnit.Name = "pbPressureUnit";
            this.pbPressureUnit.Size = new System.Drawing.Size(18, 18);
            this.pbPressureUnit.TabIndex = 26;
            this.pbPressureUnit.TabStop = false;
            this.pbPressureUnit.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbPressureUnit.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbPressureUnit.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbLanguage
            // 
            this.pbLanguage.Image = global::ORTS.Properties.Resources.info_18;
            this.pbLanguage.Location = new System.Drawing.Point(6, 203);
            this.pbLanguage.Name = "pbLanguage";
            this.pbLanguage.Size = new System.Drawing.Size(18, 18);
            this.pbLanguage.TabIndex = 25;
            this.pbLanguage.TabStop = false;
            this.pbLanguage.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbLanguage.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbLanguage.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbBrakePipeChargingRate
            // 
            this.pbBrakePipeChargingRate.Image = global::ORTS.Properties.Resources.info_18;
            this.pbBrakePipeChargingRate.Location = new System.Drawing.Point(6, 168);
            this.pbBrakePipeChargingRate.Name = "pbBrakePipeChargingRate";
            this.pbBrakePipeChargingRate.Size = new System.Drawing.Size(18, 18);
            this.pbBrakePipeChargingRate.TabIndex = 24;
            this.pbBrakePipeChargingRate.TabStop = false;
            this.pbBrakePipeChargingRate.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbBrakePipeChargingRate.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbBrakePipeChargingRate.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbGraduatedRelease
            // 
            this.pbGraduatedRelease.Image = global::ORTS.Properties.Resources.info_18;
            this.pbGraduatedRelease.Location = new System.Drawing.Point(6, 145);
            this.pbGraduatedRelease.Name = "pbGraduatedRelease";
            this.pbGraduatedRelease.Size = new System.Drawing.Size(18, 18);
            this.pbGraduatedRelease.TabIndex = 23;
            this.pbGraduatedRelease.TabStop = false;
            this.pbGraduatedRelease.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbGraduatedRelease.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbGraduatedRelease.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbRetainers
            // 
            this.pbRetainers.Image = global::ORTS.Properties.Resources.info_18;
            this.pbRetainers.Location = new System.Drawing.Point(6, 122);
            this.pbRetainers.Name = "pbRetainers";
            this.pbRetainers.Size = new System.Drawing.Size(18, 18);
            this.pbRetainers.TabIndex = 22;
            this.pbRetainers.TabStop = false;
            this.pbRetainers.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbRetainers.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbRetainers.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbLAA
            // 
            this.pbLAA.Image = global::ORTS.Properties.Resources.info_18;
            this.pbLAA.Location = new System.Drawing.Point(6, 99);
            this.pbLAA.Name = "pbLAA";
            this.pbLAA.Size = new System.Drawing.Size(18, 18);
            this.pbLAA.TabIndex = 21;
            this.pbLAA.TabStop = false;
            this.pbLAA.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbLAA.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbLAA.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbAlerter
            // 
            this.pbAlerter.Image = global::ORTS.Properties.Resources.info_18;
            this.pbAlerter.Location = new System.Drawing.Point(6, 7);
            this.pbAlerter.Name = "pbAlerter";
            this.pbAlerter.Size = new System.Drawing.Size(18, 18);
            this.pbAlerter.TabIndex = 20;
            this.pbAlerter.TabStop = false;
            this.pbAlerter.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbAlerter.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbAlerter.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbControlConfirmations
            // 
            this.pbControlConfirmations.Image = global::ORTS.Properties.Resources.info_18;
            this.pbControlConfirmations.Location = new System.Drawing.Point(6, 53);
            this.pbControlConfirmations.Name = "pbControlConfirmations";
            this.pbControlConfirmations.Size = new System.Drawing.Size(18, 18);
            this.pbControlConfirmations.TabIndex = 19;
            this.pbControlConfirmations.TabStop = false;
            this.pbControlConfirmations.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbControlConfirmations.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbControlConfirmations.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // pbMapWindow
            // 
            this.pbMapWindow.Image = global::ORTS.Properties.Resources.info_18;
            this.pbMapWindow.Location = new System.Drawing.Point(6, 76);
            this.pbMapWindow.Name = "pbMapWindow";
            this.pbMapWindow.Size = new System.Drawing.Size(18, 18);
            this.pbMapWindow.TabIndex = 18;
            this.pbMapWindow.TabStop = false;
            this.pbMapWindow.Click += new System.EventHandler(this.HelpIcon_Click);
            this.pbMapWindow.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.pbMapWindow.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // labelPortNumber
            // 
            this.labelPortNumber.AutoSize = true;
            this.labelPortNumber.Location = new System.Drawing.Point(105, 333);
            this.labelPortNumber.Name = "labelPortNumber";
            this.labelPortNumber.Size = new System.Drawing.Size(181, 13);
            this.labelPortNumber.TabIndex = 17;
            this.labelPortNumber.Text = "Port number                                       ";
            // 
            // numericWebServerPort
            // 
            this.numericWebServerPort.Location = new System.Drawing.Point(31, 331);
            this.numericWebServerPort.Maximum = new decimal(new int[] {
            65534,
            0,
            0,
            0});
            this.numericWebServerPort.Minimum = new decimal(new int[] {
            1025,
            0,
            0,
            0});
            this.numericWebServerPort.Name = "numericWebServerPort";
            this.numericWebServerPort.Size = new System.Drawing.Size(70, 20);
            this.numericWebServerPort.TabIndex = 16;
            this.numericWebServerPort.Value = new decimal(new int[] {
            1025,
            0,
            0,
            0});
            // 
            // checkEnableWebServer
            // 
            this.checkEnableWebServer.AutoSize = true;
            this.checkEnableWebServer.Location = new System.Drawing.Point(32, 308);
            this.checkEnableWebServer.Name = "checkEnableWebServer";
            this.checkEnableWebServer.Size = new System.Drawing.Size(255, 17);
            this.checkEnableWebServer.TabIndex = 15;
            this.checkEnableWebServer.Text = "Enable webserver                                                ";
            this.checkEnableWebServer.UseVisualStyleBackColor = true;
            this.checkEnableWebServer.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkEnableWebServer.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // checkOverspeedMonitor
            // 
            this.checkOverspeedMonitor.AutoSize = true;
            this.checkOverspeedMonitor.Location = new System.Drawing.Point(322, 6);
            this.checkOverspeedMonitor.Name = "checkOverspeedMonitor";
            this.checkOverspeedMonitor.Size = new System.Drawing.Size(274, 17);
            this.checkOverspeedMonitor.TabIndex = 14;
            this.checkOverspeedMonitor.Text = "Overspeed monitor                                                     ";
            this.checkOverspeedMonitor.UseVisualStyleBackColor = true;
            this.checkOverspeedMonitor.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkOverspeedMonitor.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // checkDisableTCSScripts
            // 
            this.checkDisableTCSScripts.AutoSize = true;
            this.checkDisableTCSScripts.Location = new System.Drawing.Point(32, 285);
            this.checkDisableTCSScripts.Name = "checkDisableTCSScripts";
            this.checkDisableTCSScripts.Size = new System.Drawing.Size(256, 17);
            this.checkDisableTCSScripts.TabIndex = 13;
            this.checkDisableTCSScripts.Text = "Disable TCS scripts                                              ";
            this.checkDisableTCSScripts.UseVisualStyleBackColor = true;
            this.checkDisableTCSScripts.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkDisableTCSScripts.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // labelOtherUnits
            // 
            this.labelOtherUnits.AutoSize = true;
            this.labelOtherUnits.Location = new System.Drawing.Point(156, 257);
            this.labelOtherUnits.Margin = new System.Windows.Forms.Padding(3);
            this.labelOtherUnits.Name = "labelOtherUnits";
            this.labelOtherUnits.Size = new System.Drawing.Size(130, 13);
            this.labelOtherUnits.TabIndex = 9;
            this.labelOtherUnits.Text = "Other units                        ";
            this.labelOtherUnits.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.labelOtherUnits.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // labelPressureUnit
            // 
            this.labelPressureUnit.AutoSize = true;
            this.labelPressureUnit.Location = new System.Drawing.Point(156, 230);
            this.labelPressureUnit.Margin = new System.Windows.Forms.Padding(3);
            this.labelPressureUnit.Name = "labelPressureUnit";
            this.labelPressureUnit.Size = new System.Drawing.Size(131, 13);
            this.labelPressureUnit.TabIndex = 12;
            this.labelPressureUnit.Text = "Pressure unit                     ";
            this.labelPressureUnit.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.labelPressureUnit.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // comboOtherUnits
            // 
            this.comboOtherUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboOtherUnits.FormattingEnabled = true;
            this.comboOtherUnits.Location = new System.Drawing.Point(32, 254);
            this.comboOtherUnits.Name = "comboOtherUnits";
            this.comboOtherUnits.Size = new System.Drawing.Size(121, 21);
            this.comboOtherUnits.TabIndex = 8;
            // 
            // checkUseLargeAddressAware
            // 
            this.checkUseLargeAddressAware.AutoSize = true;
            this.checkUseLargeAddressAware.Location = new System.Drawing.Point(32, 98);
            this.checkUseLargeAddressAware.Name = "checkUseLargeAddressAware";
            this.checkUseLargeAddressAware.Size = new System.Drawing.Size(531, 17);
            this.checkUseLargeAddressAware.TabIndex = 3;
            this.checkUseLargeAddressAware.Text = "Large address aware binaries (for all 64bit and 3GB tuning on 32bit)             " +
    "                                                  ";
            this.checkUseLargeAddressAware.UseVisualStyleBackColor = true;
            this.checkUseLargeAddressAware.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkUseLargeAddressAware.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // comboPressureUnit
            // 
            this.comboPressureUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPressureUnit.FormattingEnabled = true;
            this.comboPressureUnit.Location = new System.Drawing.Point(32, 227);
            this.comboPressureUnit.Name = "comboPressureUnit";
            this.comboPressureUnit.Size = new System.Drawing.Size(121, 21);
            this.comboPressureUnit.TabIndex = 11;
            // 
            // labelLanguage
            // 
            this.labelLanguage.AutoSize = true;
            this.labelLanguage.Location = new System.Drawing.Point(156, 203);
            this.labelLanguage.Margin = new System.Windows.Forms.Padding(3);
            this.labelLanguage.Name = "labelLanguage";
            this.labelLanguage.Size = new System.Drawing.Size(130, 13);
            this.labelLanguage.TabIndex = 10;
            this.labelLanguage.Text = "Language                         ";
            this.labelLanguage.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.labelLanguage.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // comboLanguage
            // 
            this.comboLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboLanguage.FormattingEnabled = true;
            this.comboLanguage.Location = new System.Drawing.Point(32, 200);
            this.comboLanguage.Name = "comboLanguage";
            this.comboLanguage.Size = new System.Drawing.Size(121, 21);
            this.comboLanguage.TabIndex = 9;
            // 
            // checkAlerterExternal
            // 
            this.checkAlerterExternal.AutoSize = true;
            this.checkAlerterExternal.Location = new System.Drawing.Point(52, 29);
            this.checkAlerterExternal.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.checkAlerterExternal.Name = "checkAlerterExternal";
            this.checkAlerterExternal.Size = new System.Drawing.Size(235, 17);
            this.checkAlerterExternal.TabIndex = 1;
            this.checkAlerterExternal.Text = "Also in external views                                    ";
            this.checkAlerterExternal.UseVisualStyleBackColor = true;
            // 
            // checkRetainers
            // 
            this.checkRetainers.AutoSize = true;
            this.checkRetainers.Location = new System.Drawing.Point(32, 121);
            this.checkRetainers.Name = "checkRetainers";
            this.checkRetainers.Size = new System.Drawing.Size(257, 17);
            this.checkRetainers.TabIndex = 5;
            this.checkRetainers.Text = "Retainer valve on all cars                                     ";
            this.checkRetainers.UseVisualStyleBackColor = true;
            this.checkRetainers.MouseEnter += new System.EventHandler(this.HelpIcon_MouseEnter);
            this.checkRetainers.MouseLeave += new System.EventHandler(this.HelpIcon_MouseLeave);
            // 
            // tabPageAudio
            // 
            this.tabPageAudio.Controls.Add(this.numericExternalSoundPassThruPercent);
            this.tabPageAudio.Controls.Add(this.label11);
            this.tabPageAudio.Controls.Add(this.numericSoundVolumePercent);
            this.tabPageAudio.Controls.Add(this.soundVolumeLabel);
            this.tabPageAudio.Controls.Add(this.label2);
            this.tabPageAudio.Controls.Add(this.numericSoundDetailLevel);
            this.tabPageAudio.Controls.Add(this.checkMSTSBINSound);
            this.tabPageAudio.Location = new System.Drawing.Point(4, 22);
            this.tabPageAudio.Name = "tabPageAudio";
            this.tabPageAudio.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAudio.Size = new System.Drawing.Size(602, 402);
            this.tabPageAudio.TabIndex = 5;
            this.tabPageAudio.Text = "Audio";
            this.tabPageAudio.UseVisualStyleBackColor = true;
            // 
            // numericExternalSoundPassThruPercent
            // 
            this.numericExternalSoundPassThruPercent.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericExternalSoundPassThruPercent.Location = new System.Drawing.Point(6, 81);
            this.numericExternalSoundPassThruPercent.Name = "numericExternalSoundPassThruPercent";
            this.numericExternalSoundPassThruPercent.Size = new System.Drawing.Size(54, 20);
            this.numericExternalSoundPassThruPercent.TabIndex = 5;
            this.toolTip1.SetToolTip(this.numericExternalSoundPassThruPercent, "Min 0 Max 100. Higher: louder sound\r\n\r\n");
            this.numericExternalSoundPassThruPercent.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(66, 83);
            this.label11.Margin = new System.Windows.Forms.Padding(3);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(161, 13);
            this.label11.TabIndex = 6;
            this.label11.Text = "% external sound heard internally";
            // 
            // numericSoundVolumePercent
            // 
            this.numericSoundVolumePercent.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericSoundVolumePercent.Location = new System.Drawing.Point(6, 29);
            this.numericSoundVolumePercent.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericSoundVolumePercent.Name = "numericSoundVolumePercent";
            this.numericSoundVolumePercent.Size = new System.Drawing.Size(54, 20);
            this.numericSoundVolumePercent.TabIndex = 1;
            this.toolTip1.SetToolTip(this.numericSoundVolumePercent, "Sound Volume 0-100");
            this.numericSoundVolumePercent.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // soundVolumeLabel
            // 
            this.soundVolumeLabel.AutoSize = true;
            this.soundVolumeLabel.Location = new System.Drawing.Point(66, 31);
            this.soundVolumeLabel.Margin = new System.Windows.Forms.Padding(3);
            this.soundVolumeLabel.Name = "soundVolumeLabel";
            this.soundVolumeLabel.Size = new System.Drawing.Size(84, 13);
            this.soundVolumeLabel.TabIndex = 2;
            this.soundVolumeLabel.Text = "% sound volume";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(66, 57);
            this.label2.Margin = new System.Windows.Forms.Padding(3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Sound detail level";
            // 
            // numericSoundDetailLevel
            // 
            this.numericSoundDetailLevel.Location = new System.Drawing.Point(6, 55);
            this.numericSoundDetailLevel.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericSoundDetailLevel.Name = "numericSoundDetailLevel";
            this.numericSoundDetailLevel.Size = new System.Drawing.Size(54, 20);
            this.numericSoundDetailLevel.TabIndex = 3;
            // 
            // checkMSTSBINSound
            // 
            this.checkMSTSBINSound.AutoSize = true;
            this.checkMSTSBINSound.Location = new System.Drawing.Point(6, 6);
            this.checkMSTSBINSound.Name = "checkMSTSBINSound";
            this.checkMSTSBINSound.Size = new System.Drawing.Size(160, 17);
            this.checkMSTSBINSound.TabIndex = 0;
            this.checkMSTSBINSound.Text = "MSTS Bin compatible sound";
            this.checkMSTSBINSound.UseVisualStyleBackColor = true;
            // 
            // tabPageVideo
            // 
            this.tabPageVideo.Controls.Add(this.labelAntiAliasingValue);
            this.tabPageVideo.Controls.Add(this.labelAntiAliasing);
            this.tabPageVideo.Controls.Add(this.trackAntiAliasing);
            this.tabPageVideo.Controls.Add(this.checkShadowAllShapes);
            this.tabPageVideo.Controls.Add(this.checkDoubleWire);
            this.tabPageVideo.Controls.Add(this.label15);
            this.tabPageVideo.Controls.Add(this.labelDayAmbientLight);
            this.tabPageVideo.Controls.Add(this.checkModelInstancing);
            this.tabPageVideo.Controls.Add(this.trackDayAmbientLight);
            this.tabPageVideo.Controls.Add(this.checkVerticalSync);
            this.tabPageVideo.Controls.Add(this.labelDistantMountainsViewingDistance);
            this.tabPageVideo.Controls.Add(this.numericDistantMountainsViewingDistance);
            this.tabPageVideo.Controls.Add(this.checkFastFullScreenAltTab);
            this.tabPageVideo.Controls.Add(this.checkDistantMountains);
            this.tabPageVideo.Controls.Add(this.label14);
            this.tabPageVideo.Controls.Add(this.numericViewingDistance);
            this.tabPageVideo.Controls.Add(this.labelFOVHelp);
            this.tabPageVideo.Controls.Add(this.numericViewingFOV);
            this.tabPageVideo.Controls.Add(this.label10);
            this.tabPageVideo.Controls.Add(this.numericCab2DStretch);
            this.tabPageVideo.Controls.Add(this.labelCab2DStretch);
            this.tabPageVideo.Controls.Add(this.label1);
            this.tabPageVideo.Controls.Add(this.numericWorldObjectDensity);
            this.tabPageVideo.Controls.Add(this.comboWindowSize);
            this.tabPageVideo.Controls.Add(this.checkWindowGlass);
            this.tabPageVideo.Controls.Add(this.label3);
            this.tabPageVideo.Controls.Add(this.checkDynamicShadows);
            this.tabPageVideo.Controls.Add(this.checkWire);
            this.tabPageVideo.Location = new System.Drawing.Point(4, 22);
            this.tabPageVideo.Name = "tabPageVideo";
            this.tabPageVideo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageVideo.Size = new System.Drawing.Size(602, 402);
            this.tabPageVideo.TabIndex = 4;
            this.tabPageVideo.Text = "Video";
            this.tabPageVideo.UseVisualStyleBackColor = true;
            // 
            // labelAntiAliasingValue
            // 
            this.labelAntiAliasingValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAntiAliasingValue.Location = new System.Drawing.Point(376, 352);
            this.labelAntiAliasingValue.Margin = new System.Windows.Forms.Padding(3);
            this.labelAntiAliasingValue.Name = "labelAntiAliasingValue";
            this.labelAntiAliasingValue.Size = new System.Drawing.Size(220, 13);
            this.labelAntiAliasingValue.TabIndex = 23;
            this.labelAntiAliasingValue.Text = "XXX";
            this.labelAntiAliasingValue.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // labelAntiAliasing
            // 
            this.labelAntiAliasing.AutoSize = true;
            this.labelAntiAliasing.Location = new System.Drawing.Point(304, 352);
            this.labelAntiAliasing.Margin = new System.Windows.Forms.Padding(3);
            this.labelAntiAliasing.Name = "labelAntiAliasing";
            this.labelAntiAliasing.Size = new System.Drawing.Size(66, 13);
            this.labelAntiAliasing.TabIndex = 22;
            this.labelAntiAliasing.Text = "Anti-aliasing:";
            // 
            // trackAntiAliasing
            // 
            this.trackAntiAliasing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.trackAntiAliasing.AutoSize = false;
            this.trackAntiAliasing.BackColor = System.Drawing.SystemColors.Window;
            this.trackAntiAliasing.LargeChange = 2;
            this.trackAntiAliasing.Location = new System.Drawing.Point(304, 371);
            this.trackAntiAliasing.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.trackAntiAliasing.Maximum = 6;
            this.trackAntiAliasing.Minimum = 1;
            this.trackAntiAliasing.Name = "trackAntiAliasing";
            this.trackAntiAliasing.Size = new System.Drawing.Size(292, 26);
            this.trackAntiAliasing.TabIndex = 24;
            this.toolTip1.SetToolTip(this.trackAntiAliasing, "Default is 2x MSAA");
            this.trackAntiAliasing.Value = 2;
            this.trackAntiAliasing.ValueChanged += new System.EventHandler(this.trackAntiAliasing_ValueChanged);
            // 
            // checkShadowAllShapes
            // 
            this.checkShadowAllShapes.AutoSize = true;
            this.checkShadowAllShapes.Location = new System.Drawing.Point(6, 29);
            this.checkShadowAllShapes.Name = "checkShadowAllShapes";
            this.checkShadowAllShapes.Size = new System.Drawing.Size(130, 17);
            this.checkShadowAllShapes.TabIndex = 24;
            this.checkShadowAllShapes.Text = "Shadow for all shapes";
            this.checkShadowAllShapes.UseVisualStyleBackColor = true;
            // 
            // checkDoubleWire
            // 
            this.checkDoubleWire.AutoSize = true;
            this.checkDoubleWire.Location = new System.Drawing.Point(307, 29);
            this.checkDoubleWire.Name = "checkDoubleWire";
            this.checkDoubleWire.Size = new System.Drawing.Size(135, 17);
            this.checkDoubleWire.TabIndex = 23;
            this.checkDoubleWire.Text = "Double overhead wires";
            this.checkDoubleWire.UseVisualStyleBackColor = true;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 352);
            this.label15.Margin = new System.Windows.Forms.Padding(3);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(138, 13);
            this.label15.TabIndex = 20;
            this.label15.Text = "Ambient daylight brightness:";
            // 
            // labelDayAmbientLight
            // 
            this.labelDayAmbientLight.Location = new System.Drawing.Point(6, 352);
            this.labelDayAmbientLight.Margin = new System.Windows.Forms.Padding(3);
            this.labelDayAmbientLight.Name = "labelDayAmbientLight";
            this.labelDayAmbientLight.Size = new System.Drawing.Size(292, 13);
            this.labelDayAmbientLight.TabIndex = 22;
            this.labelDayAmbientLight.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // checkModelInstancing
            // 
            this.checkModelInstancing.AutoSize = true;
            this.checkModelInstancing.Location = new System.Drawing.Point(6, 98);
            this.checkModelInstancing.Name = "checkModelInstancing";
            this.checkModelInstancing.Size = new System.Drawing.Size(106, 17);
            this.checkModelInstancing.TabIndex = 3;
            this.checkModelInstancing.Text = "Model instancing";
            this.checkModelInstancing.UseVisualStyleBackColor = true;
            // 
            // trackDayAmbientLight
            // 
            this.trackDayAmbientLight.AutoSize = false;
            this.trackDayAmbientLight.BackColor = System.Drawing.SystemColors.Window;
            this.trackDayAmbientLight.LargeChange = 4;
            this.trackDayAmbientLight.Location = new System.Drawing.Point(6, 371);
            this.trackDayAmbientLight.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.trackDayAmbientLight.Maximum = 30;
            this.trackDayAmbientLight.Minimum = 15;
            this.trackDayAmbientLight.Name = "trackDayAmbientLight";
            this.trackDayAmbientLight.Size = new System.Drawing.Size(292, 26);
            this.trackDayAmbientLight.SmallChange = 2;
            this.trackDayAmbientLight.TabIndex = 21;
            this.toolTip1.SetToolTip(this.trackDayAmbientLight, "Default is 100%");
            this.trackDayAmbientLight.Value = 20;
            this.trackDayAmbientLight.ValueChanged += new System.EventHandler(this.trackDayAmbientLight_ValueChanged);
            // 
            // checkVerticalSync
            // 
            this.checkVerticalSync.AutoSize = true;
            this.checkVerticalSync.Location = new System.Drawing.Point(6, 121);
            this.checkVerticalSync.Name = "checkVerticalSync";
            this.checkVerticalSync.Size = new System.Drawing.Size(86, 17);
            this.checkVerticalSync.TabIndex = 5;
            this.checkVerticalSync.Text = "Vertical sync";
            this.checkVerticalSync.UseVisualStyleBackColor = true;
            // 
            // labelDistantMountainsViewingDistance
            // 
            this.labelDistantMountainsViewingDistance.AutoSize = true;
            this.labelDistantMountainsViewingDistance.Location = new System.Drawing.Point(86, 249);
            this.labelDistantMountainsViewingDistance.Margin = new System.Windows.Forms.Padding(3);
            this.labelDistantMountainsViewingDistance.Name = "labelDistantMountainsViewingDistance";
            this.labelDistantMountainsViewingDistance.Size = new System.Drawing.Size(110, 13);
            this.labelDistantMountainsViewingDistance.TabIndex = 12;
            this.labelDistantMountainsViewingDistance.Text = "Viewing distance (km)";
            // 
            // numericDistantMountainsViewingDistance
            // 
            this.numericDistantMountainsViewingDistance.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericDistantMountainsViewingDistance.Location = new System.Drawing.Point(26, 247);
            this.numericDistantMountainsViewingDistance.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericDistantMountainsViewingDistance.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericDistantMountainsViewingDistance.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericDistantMountainsViewingDistance.Name = "numericDistantMountainsViewingDistance";
            this.numericDistantMountainsViewingDistance.Size = new System.Drawing.Size(54, 20);
            this.numericDistantMountainsViewingDistance.TabIndex = 11;
            this.toolTip1.SetToolTip(this.numericDistantMountainsViewingDistance, "Distance to see mountains");
            this.numericDistantMountainsViewingDistance.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // checkFastFullScreenAltTab
            // 
            this.checkFastFullScreenAltTab.AutoSize = true;
            this.checkFastFullScreenAltTab.Location = new System.Drawing.Point(6, 52);
            this.checkFastFullScreenAltTab.Name = "checkFastFullScreenAltTab";
            this.checkFastFullScreenAltTab.Size = new System.Drawing.Size(129, 17);
            this.checkFastFullScreenAltTab.TabIndex = 1;
            this.checkFastFullScreenAltTab.Text = "Fast full-screen alt-tab";
            this.checkFastFullScreenAltTab.UseVisualStyleBackColor = true;
            // 
            // checkDistantMountains
            // 
            this.checkDistantMountains.AutoSize = true;
            this.checkDistantMountains.Location = new System.Drawing.Point(6, 224);
            this.checkDistantMountains.Name = "checkDistantMountains";
            this.checkDistantMountains.Size = new System.Drawing.Size(110, 17);
            this.checkDistantMountains.TabIndex = 10;
            this.checkDistantMountains.Text = "Distant mountains";
            this.checkDistantMountains.UseVisualStyleBackColor = true;
            this.checkDistantMountains.Click += new System.EventHandler(this.checkDistantMountains_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(66, 200);
            this.label14.Margin = new System.Windows.Forms.Padding(3);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(104, 13);
            this.label14.TabIndex = 9;
            this.label14.Text = "Viewing distance (m)";
            // 
            // numericViewingDistance
            // 
            this.numericViewingDistance.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericViewingDistance.Location = new System.Drawing.Point(6, 198);
            this.numericViewingDistance.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericViewingDistance.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numericViewingDistance.Name = "numericViewingDistance";
            this.numericViewingDistance.Size = new System.Drawing.Size(54, 20);
            this.numericViewingDistance.TabIndex = 8;
            this.numericViewingDistance.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // labelFOVHelp
            // 
            this.labelFOVHelp.AutoSize = true;
            this.labelFOVHelp.Location = new System.Drawing.Point(304, 275);
            this.labelFOVHelp.Margin = new System.Windows.Forms.Padding(3);
            this.labelFOVHelp.Name = "labelFOVHelp";
            this.labelFOVHelp.Size = new System.Drawing.Size(28, 13);
            this.labelFOVHelp.TabIndex = 15;
            this.labelFOVHelp.Text = "XXX";
            // 
            // numericViewingFOV
            // 
            this.numericViewingFOV.Location = new System.Drawing.Point(6, 273);
            this.numericViewingFOV.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numericViewingFOV.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericViewingFOV.Name = "numericViewingFOV";
            this.numericViewingFOV.Size = new System.Drawing.Size(54, 20);
            this.numericViewingFOV.TabIndex = 13;
            this.numericViewingFOV.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericViewingFOV.ValueChanged += new System.EventHandler(this.numericUpDownFOV_ValueChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(66, 275);
            this.label10.Margin = new System.Windows.Forms.Padding(3);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(105, 13);
            this.label10.TabIndex = 14;
            this.label10.Text = "Viewing vertical FOV";
            // 
            // numericCab2DStretch
            // 
            this.numericCab2DStretch.Increment = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.numericCab2DStretch.Location = new System.Drawing.Point(6, 172);
            this.numericCab2DStretch.Name = "numericCab2DStretch";
            this.numericCab2DStretch.Size = new System.Drawing.Size(54, 20);
            this.numericCab2DStretch.TabIndex = 6;
            this.toolTip1.SetToolTip(this.numericCab2DStretch, "0 to clip cab view, 100 to stretch it. For cab views that match the display, use " +
        "100.");
            // 
            // labelCab2DStretch
            // 
            this.labelCab2DStretch.AutoSize = true;
            this.labelCab2DStretch.Location = new System.Drawing.Point(66, 174);
            this.labelCab2DStretch.Margin = new System.Windows.Forms.Padding(3);
            this.labelCab2DStretch.Name = "labelCab2DStretch";
            this.labelCab2DStretch.Size = new System.Drawing.Size(88, 13);
            this.labelCab2DStretch.TabIndex = 7;
            this.labelCab2DStretch.Text = "% cab 2D stretch";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(66, 301);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 13);
            this.label1.TabIndex = 17;
            this.label1.Text = "World object density";
            // 
            // numericWorldObjectDensity
            // 
            this.numericWorldObjectDensity.Location = new System.Drawing.Point(6, 299);
            this.numericWorldObjectDensity.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.numericWorldObjectDensity.Name = "numericWorldObjectDensity";
            this.numericWorldObjectDensity.Size = new System.Drawing.Size(54, 20);
            this.numericWorldObjectDensity.TabIndex = 16;
            // 
            // comboWindowSize
            // 
            this.comboWindowSize.FormattingEnabled = true;
            this.comboWindowSize.Items.AddRange(new object[] {
            "800x600",
            "1024x768",
            "1280x720",
            "1280x800",
            "1280x1024",
            "1360x768",
            "1366x768",
            "1440x900",
            "1536x864",
            "1600x900",
            "1680x1050",
            "1920x1080",
            "1920x1200",
            "2560x1440"});
            this.comboWindowSize.Location = new System.Drawing.Point(6, 325);
            this.comboWindowSize.Name = "comboWindowSize";
            this.comboWindowSize.Size = new System.Drawing.Size(121, 21);
            this.comboWindowSize.TabIndex = 18;
            // 
            // checkWindowGlass
            // 
            this.checkWindowGlass.AutoSize = true;
            this.checkWindowGlass.Location = new System.Drawing.Point(6, 75);
            this.checkWindowGlass.Name = "checkWindowGlass";
            this.checkWindowGlass.Size = new System.Drawing.Size(151, 17);
            this.checkWindowGlass.TabIndex = 2;
            this.checkWindowGlass.Text = "Glass on in-game windows";
            this.checkWindowGlass.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 328);
            this.label3.Margin = new System.Windows.Forms.Padding(3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(255, 13);
            this.label3.TabIndex = 19;
            this.label3.Text = "Window size (type WIDTHxHEIGHT for custom size)";
            // 
            // checkDynamicShadows
            // 
            this.checkDynamicShadows.AutoSize = true;
            this.checkDynamicShadows.Location = new System.Drawing.Point(6, 6);
            this.checkDynamicShadows.Name = "checkDynamicShadows";
            this.checkDynamicShadows.Size = new System.Drawing.Size(112, 17);
            this.checkDynamicShadows.TabIndex = 0;
            this.checkDynamicShadows.Text = "Dynamic shadows";
            this.checkDynamicShadows.UseVisualStyleBackColor = true;
            // 
            // checkWire
            // 
            this.checkWire.AutoSize = true;
            this.checkWire.Location = new System.Drawing.Point(307, 6);
            this.checkWire.Name = "checkWire";
            this.checkWire.Size = new System.Drawing.Size(95, 17);
            this.checkWire.TabIndex = 4;
            this.checkWire.Text = "Overhead wire";
            this.checkWire.UseVisualStyleBackColor = true;
            // 
            // tabPageSimulation
            // 
            this.tabPageSimulation.Controls.Add(this.checkBoxNoDieselEngineStart);
            this.tabPageSimulation.Controls.Add(this.groupBox1);
            this.tabPageSimulation.Controls.Add(this.checkHotStart);
            this.tabPageSimulation.Controls.Add(this.checkSimpleControlsPhysics);
            this.tabPageSimulation.Controls.Add(this.checkCurveSpeedDependent);
            this.tabPageSimulation.Controls.Add(this.checkCurveResistanceDependent);
            this.tabPageSimulation.Controls.Add(this.checkTunnelResistanceDependent);
            this.tabPageSimulation.Controls.Add(this.checkWindResistanceDependent);
            this.tabPageSimulation.Controls.Add(this.checkOverrideNonElectrifiedRoutes);
            this.tabPageSimulation.Controls.Add(this.labelAdhesionMovingAverageFilterSize);
            this.tabPageSimulation.Controls.Add(this.numericAdhesionMovingAverageFilterSize);
            this.tabPageSimulation.Controls.Add(this.checkBreakCouplers);
            this.tabPageSimulation.Controls.Add(this.checkUseAdvancedAdhesion);
            this.tabPageSimulation.Location = new System.Drawing.Point(4, 22);
            this.tabPageSimulation.Name = "tabPageSimulation";
            this.tabPageSimulation.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSimulation.Size = new System.Drawing.Size(602, 402);
            this.tabPageSimulation.TabIndex = 2;
            this.tabPageSimulation.Text = "Simulation";
            this.tabPageSimulation.UseVisualStyleBackColor = true;
            // 
            // checkBoxNoDieselEngineStart
            // 
            this.checkBoxNoDieselEngineStart.AutoSize = true;
            this.checkBoxNoDieselEngineStart.Location = new System.Drawing.Point(6, 238);
            this.checkBoxNoDieselEngineStart.Name = "checkBoxNoDieselEngineStart";
            this.checkBoxNoDieselEngineStart.Size = new System.Drawing.Size(195, 17);
            this.checkBoxNoDieselEngineStart.TabIndex = 9;
            this.checkBoxNoDieselEngineStart.Text = "Diesel engines stopped after startup";
            this.checkBoxNoDieselEngineStart.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkUseLocationPassingPaths);
            this.groupBox1.Controls.Add(this.checkDoorsAITrains);
            this.groupBox1.Controls.Add(this.checkForcedRedAtStationStops);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(324, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(272, 168);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Activity Options";
            // 
            // checkUseLocationPassingPaths
            // 
            this.checkUseLocationPassingPaths.AutoSize = true;
            this.checkUseLocationPassingPaths.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkUseLocationPassingPaths.Location = new System.Drawing.Point(6, 71);
            this.checkUseLocationPassingPaths.Name = "checkUseLocationPassingPaths";
            this.checkUseLocationPassingPaths.Size = new System.Drawing.Size(215, 17);
            this.checkUseLocationPassingPaths.TabIndex = 46;
            this.checkUseLocationPassingPaths.Text = "Location-linked passing path processing";
            this.checkUseLocationPassingPaths.UseVisualStyleBackColor = true;
            // 
            // checkDoorsAITrains
            // 
            this.checkDoorsAITrains.AutoSize = true;
            this.checkDoorsAITrains.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkDoorsAITrains.Location = new System.Drawing.Point(6, 46);
            this.checkDoorsAITrains.Name = "checkDoorsAITrains";
            this.checkDoorsAITrains.Size = new System.Drawing.Size(163, 17);
            this.checkDoorsAITrains.TabIndex = 45;
            this.checkDoorsAITrains.Text = "Open/close doors in AI trains";
            this.checkDoorsAITrains.UseVisualStyleBackColor = true;
            // 
            // checkForcedRedAtStationStops
            // 
            this.checkForcedRedAtStationStops.AutoSize = true;
            this.checkForcedRedAtStationStops.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkForcedRedAtStationStops.Location = new System.Drawing.Point(6, 23);
            this.checkForcedRedAtStationStops.Name = "checkForcedRedAtStationStops";
            this.checkForcedRedAtStationStops.Size = new System.Drawing.Size(151, 17);
            this.checkForcedRedAtStationStops.TabIndex = 23;
            this.checkForcedRedAtStationStops.Text = "Forced red at station stops";
            this.checkForcedRedAtStationStops.UseVisualStyleBackColor = true;
            // 
            // checkHotStart
            // 
            this.checkHotStart.AutoSize = true;
            this.checkHotStart.Location = new System.Drawing.Point(6, 192);
            this.checkHotStart.Name = "checkHotStart";
            this.checkHotStart.Size = new System.Drawing.Size(151, 17);
            this.checkHotStart.TabIndex = 8;
            this.checkHotStart.Text = "Steam locomotive hot start";
            this.checkHotStart.UseVisualStyleBackColor = true;
            // 
            // checkSimpleControlsPhysics
            // 
            this.checkSimpleControlsPhysics.AutoSize = true;
            this.checkSimpleControlsPhysics.Location = new System.Drawing.Point(6, 215);
            this.checkSimpleControlsPhysics.Name = "checkSimpleControlsPhysics";
            this.checkSimpleControlsPhysics.Size = new System.Drawing.Size(156, 17);
            this.checkSimpleControlsPhysics.TabIndex = 8;
            this.checkSimpleControlsPhysics.Text = "Simple controls and physics";
            this.checkSimpleControlsPhysics.UseVisualStyleBackColor = true;
            // 
            // checkCurveSpeedDependent
            // 
            this.checkCurveSpeedDependent.AutoSize = true;
            this.checkCurveSpeedDependent.Location = new System.Drawing.Point(6, 100);
            this.checkCurveSpeedDependent.Name = "checkCurveSpeedDependent";
            this.checkCurveSpeedDependent.Size = new System.Drawing.Size(160, 17);
            this.checkCurveSpeedDependent.TabIndex = 5;
            this.checkCurveSpeedDependent.Text = "Curve dependent speed limit";
            this.checkCurveSpeedDependent.UseVisualStyleBackColor = true;
            // 
            // checkCurveResistanceDependent
            // 
            this.checkCurveResistanceDependent.AutoSize = true;
            this.checkCurveResistanceDependent.Location = new System.Drawing.Point(6, 77);
            this.checkCurveResistanceDependent.Name = "checkCurveResistanceDependent";
            this.checkCurveResistanceDependent.Size = new System.Drawing.Size(159, 17);
            this.checkCurveResistanceDependent.TabIndex = 4;
            this.checkCurveResistanceDependent.Text = "Curve dependent resistance";
            this.checkCurveResistanceDependent.UseVisualStyleBackColor = true;
            // 
            // checkTunnelResistanceDependent
            // 
            this.checkTunnelResistanceDependent.AutoSize = true;
            this.checkTunnelResistanceDependent.Location = new System.Drawing.Point(6, 123);
            this.checkTunnelResistanceDependent.Name = "checkTunnelResistanceDependent";
            this.checkTunnelResistanceDependent.Size = new System.Drawing.Size(164, 17);
            this.checkTunnelResistanceDependent.TabIndex = 6;
            this.checkTunnelResistanceDependent.Text = "Tunnel dependent resistance";
            this.checkTunnelResistanceDependent.UseVisualStyleBackColor = true;
            // 
            // checkWindResistanceDependent
            // 
            this.checkWindResistanceDependent.AutoSize = true;
            this.checkWindResistanceDependent.Location = new System.Drawing.Point(6, 146);
            this.checkWindResistanceDependent.Name = "checkWindResistanceDependent";
            this.checkWindResistanceDependent.Size = new System.Drawing.Size(156, 17);
            this.checkWindResistanceDependent.TabIndex = 4;
            this.checkWindResistanceDependent.Text = "Wind dependent resistance";
            this.checkWindResistanceDependent.UseVisualStyleBackColor = true;
            // 
            // checkOverrideNonElectrifiedRoutes
            // 
            this.checkOverrideNonElectrifiedRoutes.AutoSize = true;
            this.checkOverrideNonElectrifiedRoutes.Location = new System.Drawing.Point(6, 169);
            this.checkOverrideNonElectrifiedRoutes.Name = "checkOverrideNonElectrifiedRoutes";
            this.checkOverrideNonElectrifiedRoutes.Size = new System.Drawing.Size(219, 17);
            this.checkOverrideNonElectrifiedRoutes.TabIndex = 7;
            this.checkOverrideNonElectrifiedRoutes.Text = "Override non-electrified route line-voltage";
            this.checkOverrideNonElectrifiedRoutes.UseVisualStyleBackColor = true;
            // 
            // labelAdhesionMovingAverageFilterSize
            // 
            this.labelAdhesionMovingAverageFilterSize.AutoSize = true;
            this.labelAdhesionMovingAverageFilterSize.Location = new System.Drawing.Point(86, 31);
            this.labelAdhesionMovingAverageFilterSize.Margin = new System.Windows.Forms.Padding(3);
            this.labelAdhesionMovingAverageFilterSize.Name = "labelAdhesionMovingAverageFilterSize";
            this.labelAdhesionMovingAverageFilterSize.Size = new System.Drawing.Size(173, 13);
            this.labelAdhesionMovingAverageFilterSize.TabIndex = 2;
            this.labelAdhesionMovingAverageFilterSize.Text = "Adhesion moving average filter size";
            // 
            // numericAdhesionMovingAverageFilterSize
            // 
            this.numericAdhesionMovingAverageFilterSize.Location = new System.Drawing.Point(26, 29);
            this.numericAdhesionMovingAverageFilterSize.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericAdhesionMovingAverageFilterSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericAdhesionMovingAverageFilterSize.Name = "numericAdhesionMovingAverageFilterSize";
            this.numericAdhesionMovingAverageFilterSize.Size = new System.Drawing.Size(54, 20);
            this.numericAdhesionMovingAverageFilterSize.TabIndex = 1;
            this.numericAdhesionMovingAverageFilterSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // checkBreakCouplers
            // 
            this.checkBreakCouplers.AutoSize = true;
            this.checkBreakCouplers.Location = new System.Drawing.Point(6, 54);
            this.checkBreakCouplers.Name = "checkBreakCouplers";
            this.checkBreakCouplers.Size = new System.Drawing.Size(97, 17);
            this.checkBreakCouplers.TabIndex = 3;
            this.checkBreakCouplers.Text = "Break couplers";
            this.checkBreakCouplers.UseVisualStyleBackColor = true;
            // 
            // checkUseAdvancedAdhesion
            // 
            this.checkUseAdvancedAdhesion.AutoSize = true;
            this.checkUseAdvancedAdhesion.Location = new System.Drawing.Point(6, 6);
            this.checkUseAdvancedAdhesion.Name = "checkUseAdvancedAdhesion";
            this.checkUseAdvancedAdhesion.Size = new System.Drawing.Size(152, 17);
            this.checkUseAdvancedAdhesion.TabIndex = 0;
            this.checkUseAdvancedAdhesion.Text = "Advanced adhesion model";
            this.checkUseAdvancedAdhesion.UseVisualStyleBackColor = true;
            this.checkUseAdvancedAdhesion.Click += new System.EventHandler(this.checkUseAdvancedAdhesion_Click);
            // 
            // tabPageKeyboard
            // 
            this.tabPageKeyboard.AutoScroll = true;
            this.tabPageKeyboard.Controls.Add(this.buttonExport);
            this.tabPageKeyboard.Controls.Add(this.buttonDefaultKeys);
            this.tabPageKeyboard.Controls.Add(this.buttonCheckKeys);
            this.tabPageKeyboard.Controls.Add(this.panelKeys);
            this.tabPageKeyboard.Location = new System.Drawing.Point(4, 22);
            this.tabPageKeyboard.Name = "tabPageKeyboard";
            this.tabPageKeyboard.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageKeyboard.Size = new System.Drawing.Size(602, 402);
            this.tabPageKeyboard.TabIndex = 1;
            this.tabPageKeyboard.Text = "Keyboard";
            this.tabPageKeyboard.UseVisualStyleBackColor = true;
            // 
            // buttonExport
            // 
            this.buttonExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExport.Location = new System.Drawing.Point(521, 373);
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
            this.buttonDefaultKeys.Location = new System.Drawing.Point(87, 373);
            this.buttonDefaultKeys.Name = "buttonDefaultKeys";
            this.buttonDefaultKeys.Size = new System.Drawing.Size(75, 23);
            this.buttonDefaultKeys.TabIndex = 2;
            this.buttonDefaultKeys.Text = "Defaults";
            this.toolTip1.SetToolTip(this.buttonDefaultKeys, "Load the factory default key assignments.");
            this.buttonDefaultKeys.UseVisualStyleBackColor = true;
            this.buttonDefaultKeys.Click += new System.EventHandler(this.buttonDefaultKeys_Click);
            // 
            // buttonCheckKeys
            // 
            this.buttonCheckKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCheckKeys.Location = new System.Drawing.Point(6, 373);
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
            this.panelKeys.Size = new System.Drawing.Size(590, 361);
            this.panelKeys.TabIndex = 0;
            // 
            // tabPageDataLogger
            // 
            this.tabPageDataLogger.Controls.Add(this.comboDataLogSpeedUnits);
            this.tabPageDataLogger.Controls.Add(this.comboDataLoggerSeparator);
            this.tabPageDataLogger.Controls.Add(this.label19);
            this.tabPageDataLogger.Controls.Add(this.label18);
            this.tabPageDataLogger.Controls.Add(this.checkDataLogMisc);
            this.tabPageDataLogger.Controls.Add(this.checkDataLogPerformance);
            this.tabPageDataLogger.Controls.Add(this.checkDataLogger);
            this.tabPageDataLogger.Controls.Add(this.label17);
            this.tabPageDataLogger.Controls.Add(this.checkDataLogPhysics);
            this.tabPageDataLogger.Controls.Add(this.checkDataLogSteamPerformance);
            this.tabPageDataLogger.Controls.Add(this.checkVerboseConfigurationMessages);
            this.tabPageDataLogger.Location = new System.Drawing.Point(4, 22);
            this.tabPageDataLogger.Name = "tabPageDataLogger";
            this.tabPageDataLogger.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDataLogger.Size = new System.Drawing.Size(602, 402);
            this.tabPageDataLogger.TabIndex = 6;
            this.tabPageDataLogger.Text = "Data logger";
            this.tabPageDataLogger.UseVisualStyleBackColor = true;
            // 
            // comboDataLogSpeedUnits
            // 
            this.comboDataLogSpeedUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDataLogSpeedUnits.FormattingEnabled = true;
            this.comboDataLogSpeedUnits.Location = new System.Drawing.Point(5, 62);
            this.comboDataLogSpeedUnits.Margin = new System.Windows.Forms.Padding(2);
            this.comboDataLogSpeedUnits.Name = "comboDataLogSpeedUnits";
            this.comboDataLogSpeedUnits.Size = new System.Drawing.Size(121, 21);
            this.comboDataLogSpeedUnits.TabIndex = 3;
            // 
            // comboDataLoggerSeparator
            // 
            this.comboDataLoggerSeparator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDataLoggerSeparator.FormattingEnabled = true;
            this.comboDataLoggerSeparator.Location = new System.Drawing.Point(5, 37);
            this.comboDataLoggerSeparator.Margin = new System.Windows.Forms.Padding(2);
            this.comboDataLoggerSeparator.Name = "comboDataLoggerSeparator";
            this.comboDataLoggerSeparator.Size = new System.Drawing.Size(121, 21);
            this.comboDataLoggerSeparator.TabIndex = 1;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(131, 65);
            this.label19.Margin = new System.Windows.Forms.Padding(3);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(63, 13);
            this.label19.TabIndex = 4;
            this.label19.Text = "Speed units";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.ForeColor = System.Drawing.SystemColors.Highlight;
            this.label18.Location = new System.Drawing.Point(6, 6);
            this.label18.Margin = new System.Windows.Forms.Padding(3);
            this.label18.MaximumSize = new System.Drawing.Size(590, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(368, 26);
            this.label18.TabIndex = 0;
            this.label18.Text = "Use data logger to record your simulation data (in-game command: F12).\r\nPlease re" +
    "member that the size of the dump file grows with the simulation time!";
            // 
            // checkDataLogMisc
            // 
            this.checkDataLogMisc.AutoSize = true;
            this.checkDataLogMisc.Location = new System.Drawing.Point(6, 157);
            this.checkDataLogMisc.Name = "checkDataLogMisc";
            this.checkDataLogMisc.Size = new System.Drawing.Size(137, 17);
            this.checkDataLogMisc.TabIndex = 8;
            this.checkDataLogMisc.Text = "Log miscellaneous data";
            this.checkDataLogMisc.UseVisualStyleBackColor = true;
            // 
            // checkDataLogPerformance
            // 
            this.checkDataLogPerformance.AutoSize = true;
            this.checkDataLogPerformance.Location = new System.Drawing.Point(6, 111);
            this.checkDataLogPerformance.Name = "checkDataLogPerformance";
            this.checkDataLogPerformance.Size = new System.Drawing.Size(130, 17);
            this.checkDataLogPerformance.TabIndex = 6;
            this.checkDataLogPerformance.Text = "Log performance data";
            this.checkDataLogPerformance.UseVisualStyleBackColor = true;
            // 
            // checkDataLogger
            // 
            this.checkDataLogger.AutoSize = true;
            this.checkDataLogger.Location = new System.Drawing.Point(6, 88);
            this.checkDataLogger.Name = "checkDataLogger";
            this.checkDataLogger.Size = new System.Drawing.Size(197, 17);
            this.checkDataLogger.TabIndex = 5;
            this.checkDataLogger.Text = "Start logging with the simulation start";
            this.checkDataLogger.UseVisualStyleBackColor = true;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(131, 40);
            this.label17.Margin = new System.Windows.Forms.Padding(3);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(53, 13);
            this.label17.TabIndex = 2;
            this.label17.Text = "Separator";
            // 
            // checkDataLogPhysics
            // 
            this.checkDataLogPhysics.AutoSize = true;
            this.checkDataLogPhysics.Location = new System.Drawing.Point(6, 134);
            this.checkDataLogPhysics.Name = "checkDataLogPhysics";
            this.checkDataLogPhysics.Size = new System.Drawing.Size(106, 17);
            this.checkDataLogPhysics.TabIndex = 7;
            this.checkDataLogPhysics.Text = "Log physics data";
            this.checkDataLogPhysics.UseVisualStyleBackColor = true;
            // 
            // checkDataLogSteamPerformance
            // 
            this.checkDataLogSteamPerformance.AutoSize = true;
            this.checkDataLogSteamPerformance.Location = new System.Drawing.Point(6, 180);
            this.checkDataLogSteamPerformance.Name = "checkDataLogSteamPerformance";
            this.checkDataLogSteamPerformance.Size = new System.Drawing.Size(163, 17);
            this.checkDataLogSteamPerformance.TabIndex = 6;
            this.checkDataLogSteamPerformance.Text = "Log Steam performance data";
            this.checkDataLogSteamPerformance.UseVisualStyleBackColor = true;
            // 
            // checkVerboseConfigurationMessages
            // 
            this.checkVerboseConfigurationMessages.AutoSize = true;
            this.checkVerboseConfigurationMessages.Location = new System.Drawing.Point(6, 236);
            this.checkVerboseConfigurationMessages.Name = "checkVerboseConfigurationMessages";
            this.checkVerboseConfigurationMessages.Size = new System.Drawing.Size(236, 17);
            this.checkVerboseConfigurationMessages.TabIndex = 6;
            this.checkVerboseConfigurationMessages.Text = "Verbose ENG/WAG configuration messages";
            this.checkVerboseConfigurationMessages.UseVisualStyleBackColor = true;
            // 
            // tabPageEvaluate
            // 
            this.tabPageEvaluate.Controls.Add(this.checkListDataLogTSContents);
            this.tabPageEvaluate.Controls.Add(this.labelDataLogTSInterval);
            this.tabPageEvaluate.Controls.Add(this.checkDataLogStationStops);
            this.tabPageEvaluate.Controls.Add(this.numericDataLogTSInterval);
            this.tabPageEvaluate.Controls.Add(this.checkDataLogTrainSpeed);
            this.tabPageEvaluate.Location = new System.Drawing.Point(4, 22);
            this.tabPageEvaluate.Name = "tabPageEvaluate";
            this.tabPageEvaluate.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEvaluate.Size = new System.Drawing.Size(602, 402);
            this.tabPageEvaluate.TabIndex = 7;
            this.tabPageEvaluate.Text = "Evaluation";
            this.tabPageEvaluate.UseVisualStyleBackColor = true;
            // 
            // checkListDataLogTSContents
            // 
            this.checkListDataLogTSContents.FormattingEnabled = true;
            this.checkListDataLogTSContents.Location = new System.Drawing.Point(26, 55);
            this.checkListDataLogTSContents.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.checkListDataLogTSContents.Name = "checkListDataLogTSContents";
            this.checkListDataLogTSContents.Size = new System.Drawing.Size(148, 214);
            this.checkListDataLogTSContents.TabIndex = 3;
            // 
            // labelDataLogTSInterval
            // 
            this.labelDataLogTSInterval.AutoSize = true;
            this.labelDataLogTSInterval.Location = new System.Drawing.Point(86, 31);
            this.labelDataLogTSInterval.Margin = new System.Windows.Forms.Padding(3);
            this.labelDataLogTSInterval.Name = "labelDataLogTSInterval";
            this.labelDataLogTSInterval.Size = new System.Drawing.Size(68, 13);
            this.labelDataLogTSInterval.TabIndex = 2;
            this.labelDataLogTSInterval.Text = "Interval (sec)";
            // 
            // checkDataLogStationStops
            // 
            this.checkDataLogStationStops.AutoSize = true;
            this.checkDataLogStationStops.Location = new System.Drawing.Point(6, 280);
            this.checkDataLogStationStops.Name = "checkDataLogStationStops";
            this.checkDataLogStationStops.Size = new System.Drawing.Size(106, 17);
            this.checkDataLogStationStops.TabIndex = 4;
            this.checkDataLogStationStops.Text = "Log station stops";
            this.checkDataLogStationStops.UseVisualStyleBackColor = true;
            // 
            // numericDataLogTSInterval
            // 
            this.numericDataLogTSInterval.Location = new System.Drawing.Point(26, 29);
            this.numericDataLogTSInterval.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericDataLogTSInterval.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericDataLogTSInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericDataLogTSInterval.Name = "numericDataLogTSInterval";
            this.numericDataLogTSInterval.Size = new System.Drawing.Size(54, 20);
            this.numericDataLogTSInterval.TabIndex = 1;
            this.numericDataLogTSInterval.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // checkDataLogTrainSpeed
            // 
            this.checkDataLogTrainSpeed.AutoSize = true;
            this.checkDataLogTrainSpeed.Location = new System.Drawing.Point(6, 6);
            this.checkDataLogTrainSpeed.Name = "checkDataLogTrainSpeed";
            this.checkDataLogTrainSpeed.Size = new System.Drawing.Size(99, 17);
            this.checkDataLogTrainSpeed.TabIndex = 0;
            this.checkDataLogTrainSpeed.Text = "Log train speed";
            this.checkDataLogTrainSpeed.UseVisualStyleBackColor = true;
            this.checkDataLogTrainSpeed.Click += new System.EventHandler(this.checkDataLogTrainSpeed_Click);
            // 
            // tabPageContent
            // 
            this.tabPageContent.Controls.Add(this.labelContent);
            this.tabPageContent.Controls.Add(this.buttonContentDelete);
            this.tabPageContent.Controls.Add(this.groupBoxContent);
            this.tabPageContent.Controls.Add(this.buttonContentAdd);
            this.tabPageContent.Controls.Add(this.panelContent);
            this.tabPageContent.Location = new System.Drawing.Point(4, 22);
            this.tabPageContent.Name = "tabPageContent";
            this.tabPageContent.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageContent.Size = new System.Drawing.Size(602, 402);
            this.tabPageContent.TabIndex = 9;
            this.tabPageContent.Text = "Content";
            this.tabPageContent.UseVisualStyleBackColor = true;
            // 
            // labelContent
            // 
            this.labelContent.AutoSize = true;
            this.labelContent.ForeColor = System.Drawing.SystemColors.Highlight;
            this.labelContent.Location = new System.Drawing.Point(6, 6);
            this.labelContent.Margin = new System.Windows.Forms.Padding(3);
            this.labelContent.MaximumSize = new System.Drawing.Size(590, 0);
            this.labelContent.Name = "labelContent";
            this.labelContent.Size = new System.Drawing.Size(539, 13);
            this.labelContent.TabIndex = 3;
            this.labelContent.Text = "Installation profiles tell Open Rails where to look for game content. Add each fu" +
    "ll and mini-route MSTS installation.";
            // 
            // buttonContentDelete
            // 
            this.buttonContentDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonContentDelete.Location = new System.Drawing.Point(6, 364);
            this.buttonContentDelete.Name = "buttonContentDelete";
            this.buttonContentDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonContentDelete.TabIndex = 1;
            this.buttonContentDelete.Text = "Delete";
            this.buttonContentDelete.UseVisualStyleBackColor = true;
            this.buttonContentDelete.Click += new System.EventHandler(this.buttonContentDelete_Click);
            // 
            // groupBoxContent
            // 
            this.groupBoxContent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxContent.Controls.Add(this.buttonContentBrowse);
            this.groupBoxContent.Controls.Add(this.textBoxContentPath);
            this.groupBoxContent.Controls.Add(this.label20);
            this.groupBoxContent.Controls.Add(this.label22);
            this.groupBoxContent.Controls.Add(this.textBoxContentName);
            this.groupBoxContent.Location = new System.Drawing.Point(87, 317);
            this.groupBoxContent.Name = "groupBoxContent";
            this.groupBoxContent.Size = new System.Drawing.Size(509, 79);
            this.groupBoxContent.TabIndex = 2;
            this.groupBoxContent.TabStop = false;
            this.groupBoxContent.Text = "Installation profile";
            // 
            // buttonContentBrowse
            // 
            this.buttonContentBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonContentBrowse.Location = new System.Drawing.Point(428, 19);
            this.buttonContentBrowse.Name = "buttonContentBrowse";
            this.buttonContentBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonContentBrowse.TabIndex = 2;
            this.buttonContentBrowse.Text = "Change...";
            this.buttonContentBrowse.UseVisualStyleBackColor = true;
            this.buttonContentBrowse.Click += new System.EventHandler(this.buttonContentBrowse_Click);
            // 
            // textBoxContentPath
            // 
            this.textBoxContentPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxContentPath.Location = new System.Drawing.Point(50, 21);
            this.textBoxContentPath.Name = "textBoxContentPath";
            this.textBoxContentPath.ReadOnly = true;
            this.textBoxContentPath.Size = new System.Drawing.Size(372, 20);
            this.textBoxContentPath.TabIndex = 1;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(6, 52);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(38, 13);
            this.label20.TabIndex = 3;
            this.label20.Text = "Name:";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(6, 24);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(32, 13);
            this.label22.TabIndex = 0;
            this.label22.Text = "Path:";
            // 
            // textBoxContentName
            // 
            this.textBoxContentName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxContentName.Location = new System.Drawing.Point(50, 49);
            this.textBoxContentName.Name = "textBoxContentName";
            this.textBoxContentName.Size = new System.Drawing.Size(453, 20);
            this.textBoxContentName.TabIndex = 4;
            this.textBoxContentName.TextChanged += new System.EventHandler(this.textBoxContentName_TextChanged);
            // 
            // buttonContentAdd
            // 
            this.buttonContentAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonContentAdd.Location = new System.Drawing.Point(6, 336);
            this.buttonContentAdd.Name = "buttonContentAdd";
            this.buttonContentAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonContentAdd.TabIndex = 0;
            this.buttonContentAdd.Text = "Add...";
            this.buttonContentAdd.UseVisualStyleBackColor = true;
            this.buttonContentAdd.Click += new System.EventHandler(this.buttonContentAdd_Click);
            // 
            // panelContent
            // 
            this.panelContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelContent.Controls.Add(this.dataGridViewContent);
            this.panelContent.Location = new System.Drawing.Point(6, 38);
            this.panelContent.Name = "panelContent";
            this.panelContent.Size = new System.Drawing.Size(590, 273);
            this.panelContent.TabIndex = 2;
            // 
            // dataGridViewContent
            // 
            this.dataGridViewContent.AllowUserToAddRows = false;
            this.dataGridViewContent.AllowUserToDeleteRows = false;
            this.dataGridViewContent.AutoGenerateColumns = false;
            this.dataGridViewContent.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridViewContent.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridViewContent.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dataGridViewContent.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridViewContent.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.dataGridViewContent.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewContent.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewContent.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewContent.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.nameDataGridViewTextBoxColumn,
            this.pathDataGridViewTextBoxColumn});
            this.dataGridViewContent.DataSource = this.bindingSourceContent;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewContent.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewContent.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewContent.MultiSelect = false;
            this.dataGridViewContent.Name = "dataGridViewContent";
            this.dataGridViewContent.ReadOnly = true;
            this.dataGridViewContent.RowHeadersVisible = false;
            this.dataGridViewContent.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewContent.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewContent.Size = new System.Drawing.Size(588, 271);
            this.dataGridViewContent.TabIndex = 0;
            this.dataGridViewContent.SelectionChanged += new System.EventHandler(this.dataGridViewContent_SelectionChanged);
            // 
            // nameDataGridViewTextBoxColumn
            // 
            this.nameDataGridViewTextBoxColumn.DataPropertyName = "Name";
            this.nameDataGridViewTextBoxColumn.HeaderText = "Name";
            this.nameDataGridViewTextBoxColumn.Name = "nameDataGridViewTextBoxColumn";
            this.nameDataGridViewTextBoxColumn.ReadOnly = true;
            this.nameDataGridViewTextBoxColumn.Width = 60;
            // 
            // pathDataGridViewTextBoxColumn
            // 
            this.pathDataGridViewTextBoxColumn.DataPropertyName = "Path";
            this.pathDataGridViewTextBoxColumn.HeaderText = "Path";
            this.pathDataGridViewTextBoxColumn.Name = "pathDataGridViewTextBoxColumn";
            this.pathDataGridViewTextBoxColumn.ReadOnly = true;
            this.pathDataGridViewTextBoxColumn.Width = 54;
            // 
            // bindingSourceContent
            // 
            this.bindingSourceContent.DataSource = typeof(ORTS.OptionsForm.ContentFolder);
            // 
            // tabPageUpdater
            // 
            this.tabPageUpdater.Controls.Add(this.labelUpdateChannel);
            this.tabPageUpdater.Location = new System.Drawing.Point(4, 22);
            this.tabPageUpdater.Name = "tabPageUpdater";
            this.tabPageUpdater.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageUpdater.Size = new System.Drawing.Size(602, 402);
            this.tabPageUpdater.TabIndex = 8;
            this.tabPageUpdater.Text = "Updater";
            this.tabPageUpdater.UseVisualStyleBackColor = true;
            // 
            // labelUpdateChannel
            // 
            this.labelUpdateChannel.AutoSize = true;
            this.labelUpdateChannel.Location = new System.Drawing.Point(6, 6);
            this.labelUpdateChannel.Margin = new System.Windows.Forms.Padding(3);
            this.labelUpdateChannel.Name = "labelUpdateChannel";
            this.labelUpdateChannel.Size = new System.Drawing.Size(74, 13);
            this.labelUpdateChannel.TabIndex = 0;
            this.labelUpdateChannel.Text = "Update mode:";
            // 
            // tabPageExperimental
            // 
            this.tabPageExperimental.Controls.Add(this.label27);
            this.tabPageExperimental.Controls.Add(this.numericActWeatherRandomizationLevel);
            this.tabPageExperimental.Controls.Add(this.label26);
            this.tabPageExperimental.Controls.Add(this.label13);
            this.tabPageExperimental.Controls.Add(this.label12);
            this.tabPageExperimental.Controls.Add(this.numericActRandomizationLevel);
            this.tabPageExperimental.Controls.Add(this.checkCorrectQuestionableBrakingParams);
            this.tabPageExperimental.Controls.Add(this.label25);
            this.tabPageExperimental.Controls.Add(this.precipitationBoxLength);
            this.tabPageExperimental.Controls.Add(this.label24);
            this.tabPageExperimental.Controls.Add(this.precipitationBoxWidth);
            this.tabPageExperimental.Controls.Add(this.label23);
            this.tabPageExperimental.Controls.Add(this.precipitationBoxHeight);
            this.tabPageExperimental.Controls.Add(this.label16);
            this.tabPageExperimental.Controls.Add(this.label9);
            this.tabPageExperimental.Controls.Add(this.label21);
            this.tabPageExperimental.Controls.Add(this.AdhesionFactorChangeValueLabel);
            this.tabPageExperimental.Controls.Add(this.AdhesionFactorValueLabel);
            this.tabPageExperimental.Controls.Add(this.labelLODBias);
            this.tabPageExperimental.Controls.Add(this.checkShapeWarnings);
            this.tabPageExperimental.Controls.Add(this.trackLODBias);
            this.tabPageExperimental.Controls.Add(this.checkConditionalLoadOfNightTextures);
            this.tabPageExperimental.Controls.Add(this.AdhesionLevelValue);
            this.tabPageExperimental.Controls.Add(this.AdhesionLevelLabel);
            this.tabPageExperimental.Controls.Add(this.trackAdhesionFactorChange);
            this.tabPageExperimental.Controls.Add(this.trackAdhesionFactor);
            this.tabPageExperimental.Controls.Add(this.checkAdhesionPropToWeather);
            this.tabPageExperimental.Controls.Add(this.checkCircularSpeedGauge);
            this.tabPageExperimental.Controls.Add(this.checkSignalLightGlow);
            this.tabPageExperimental.Controls.Add(this.checkUseMSTSEnv);
            this.tabPageExperimental.Controls.Add(this.labelPerformanceTunerTarget);
            this.tabPageExperimental.Controls.Add(this.numericPerformanceTunerTarget);
            this.tabPageExperimental.Controls.Add(this.checkPerformanceTuner);
            this.tabPageExperimental.Controls.Add(this.checkLODViewingExtention);
            this.tabPageExperimental.Controls.Add(this.label8);
            this.tabPageExperimental.Controls.Add(this.numericSuperElevationGauge);
            this.tabPageExperimental.Controls.Add(this.label7);
            this.tabPageExperimental.Controls.Add(this.numericSuperElevationMinLen);
            this.tabPageExperimental.Controls.Add(this.label6);
            this.tabPageExperimental.Controls.Add(this.label5);
            this.tabPageExperimental.Controls.Add(this.numericUseSuperElevation);
            this.tabPageExperimental.Controls.Add(this.ElevationText);
            this.tabPageExperimental.Controls.Add(this.checkPreferDDSTexture);
            this.tabPageExperimental.Location = new System.Drawing.Point(4, 22);
            this.tabPageExperimental.Name = "tabPageExperimental";
            this.tabPageExperimental.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageExperimental.Size = new System.Drawing.Size(602, 402);
            this.tabPageExperimental.TabIndex = 3;
            this.tabPageExperimental.Text = "Experimental";
            this.tabPageExperimental.UseVisualStyleBackColor = true;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(526, 119);
            this.label27.Margin = new System.Windows.Forms.Padding(3);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(33, 13);
            this.label27.TabIndex = 51;
            this.label27.Text = "Level";
            // 
            // numericActWeatherRandomizationLevel
            // 
            this.numericActWeatherRandomizationLevel.Location = new System.Drawing.Point(466, 117);
            this.numericActWeatherRandomizationLevel.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericActWeatherRandomizationLevel.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numericActWeatherRandomizationLevel.Name = "numericActWeatherRandomizationLevel";
            this.numericActWeatherRandomizationLevel.Size = new System.Drawing.Size(54, 20);
            this.numericActWeatherRandomizationLevel.TabIndex = 50;
            this.toolTip1.SetToolTip(this.numericActWeatherRandomizationLevel, "0: no randomization, 1: moderate, 2: significant; 3: high (may be unrealistic)");
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(438, 96);
            this.label26.Margin = new System.Windows.Forms.Padding(3);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(150, 13);
            this.label26.TabIndex = 49;
            this.label26.Text = "Activity weather randomization";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(301, 96);
            this.label13.Margin = new System.Windows.Forms.Padding(3);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(109, 13);
            this.label13.TabIndex = 48;
            this.label13.Text = "Activity randomization";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(385, 119);
            this.label12.Margin = new System.Windows.Forms.Padding(3);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(33, 13);
            this.label12.TabIndex = 47;
            this.label12.Text = "Level";
            // 
            // numericActRandomizationLevel
            // 
            this.numericActRandomizationLevel.Location = new System.Drawing.Point(325, 117);
            this.numericActRandomizationLevel.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericActRandomizationLevel.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numericActRandomizationLevel.Name = "numericActRandomizationLevel";
            this.numericActRandomizationLevel.Size = new System.Drawing.Size(54, 20);
            this.numericActRandomizationLevel.TabIndex = 46;
            this.toolTip1.SetToolTip(this.numericActRandomizationLevel, "0: no randomization, 1: moderate, 2: significant; 3: high (may be unrealistic)");
            // 
            // checkCorrectQuestionableBrakingParams
            // 
            this.checkCorrectQuestionableBrakingParams.AutoSize = true;
            this.checkCorrectQuestionableBrakingParams.Location = new System.Drawing.Point(304, 74);
            this.checkCorrectQuestionableBrakingParams.Name = "checkCorrectQuestionableBrakingParams";
            this.checkCorrectQuestionableBrakingParams.Size = new System.Drawing.Size(216, 17);
            this.checkCorrectQuestionableBrakingParams.TabIndex = 43;
            this.checkCorrectQuestionableBrakingParams.Text = "Correct questionable braking parameters";
            this.checkCorrectQuestionableBrakingParams.UseVisualStyleBackColor = true;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(70, 363);
            this.label25.Margin = new System.Windows.Forms.Padding(3);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(134, 13);
            this.label25.TabIndex = 42;
            this.label25.Text = "Precipitation box length (m)";
            // 
            // precipitationBoxLength
            // 
            this.precipitationBoxLength.Increment = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.precipitationBoxLength.Location = new System.Drawing.Point(10, 361);
            this.precipitationBoxLength.Maximum = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            this.precipitationBoxLength.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.precipitationBoxLength.Name = "precipitationBoxLength";
            this.precipitationBoxLength.Size = new System.Drawing.Size(54, 20);
            this.precipitationBoxLength.TabIndex = 41;
            this.precipitationBoxLength.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(70, 337);
            this.label24.Margin = new System.Windows.Forms.Padding(3);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(130, 13);
            this.label24.TabIndex = 40;
            this.label24.Text = "Precipitation box width (m)";
            // 
            // precipitationBoxWidth
            // 
            this.precipitationBoxWidth.Increment = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.precipitationBoxWidth.Location = new System.Drawing.Point(10, 335);
            this.precipitationBoxWidth.Maximum = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            this.precipitationBoxWidth.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.precipitationBoxWidth.Name = "precipitationBoxWidth";
            this.precipitationBoxWidth.Size = new System.Drawing.Size(54, 20);
            this.precipitationBoxWidth.TabIndex = 39;
            this.precipitationBoxWidth.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(70, 311);
            this.label23.Margin = new System.Windows.Forms.Padding(3);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(134, 13);
            this.label23.TabIndex = 38;
            this.label23.Text = "Precipitation box height (m)";
            // 
            // precipitationBoxHeight
            // 
            this.precipitationBoxHeight.Increment = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.precipitationBoxHeight.Location = new System.Drawing.Point(10, 309);
            this.precipitationBoxHeight.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.precipitationBoxHeight.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.precipitationBoxHeight.Name = "precipitationBoxHeight";
            this.precipitationBoxHeight.Size = new System.Drawing.Size(54, 20);
            this.precipitationBoxHeight.TabIndex = 37;
            this.precipitationBoxHeight.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(301, 309);
            this.label16.Margin = new System.Windows.Forms.Padding(3);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(161, 13);
            this.label16.TabIndex = 30;
            this.label16.Text = "Adhesion factor random change:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(301, 258);
            this.label9.Margin = new System.Windows.Forms.Padding(3);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(134, 13);
            this.label9.TabIndex = 26;
            this.label9.Text = "Adhesion factor correction:";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(6, 258);
            this.label21.Margin = new System.Windows.Forms.Padding(3);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(98, 13);
            this.label21.TabIndex = 14;
            this.label21.Text = "Level of detail bias:";
            // 
            // AdhesionFactorChangeValueLabel
            // 
            this.AdhesionFactorChangeValueLabel.Location = new System.Drawing.Point(301, 309);
            this.AdhesionFactorChangeValueLabel.Margin = new System.Windows.Forms.Padding(3);
            this.AdhesionFactorChangeValueLabel.Name = "AdhesionFactorChangeValueLabel";
            this.AdhesionFactorChangeValueLabel.Size = new System.Drawing.Size(292, 13);
            this.AdhesionFactorChangeValueLabel.TabIndex = 31;
            this.AdhesionFactorChangeValueLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // AdhesionFactorValueLabel
            // 
            this.AdhesionFactorValueLabel.Location = new System.Drawing.Point(301, 258);
            this.AdhesionFactorValueLabel.Margin = new System.Windows.Forms.Padding(3);
            this.AdhesionFactorValueLabel.Name = "AdhesionFactorValueLabel";
            this.AdhesionFactorValueLabel.Size = new System.Drawing.Size(292, 13);
            this.AdhesionFactorValueLabel.TabIndex = 27;
            this.AdhesionFactorValueLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // labelLODBias
            // 
            this.labelLODBias.Location = new System.Drawing.Point(6, 258);
            this.labelLODBias.Margin = new System.Windows.Forms.Padding(3);
            this.labelLODBias.Name = "labelLODBias";
            this.labelLODBias.Size = new System.Drawing.Size(292, 13);
            this.labelLODBias.TabIndex = 15;
            this.labelLODBias.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // checkShapeWarnings
            // 
            this.checkShapeWarnings.AutoSize = true;
            this.checkShapeWarnings.Location = new System.Drawing.Point(6, 189);
            this.checkShapeWarnings.Name = "checkShapeWarnings";
            this.checkShapeWarnings.Size = new System.Drawing.Size(130, 17);
            this.checkShapeWarnings.TabIndex = 36;
            this.checkShapeWarnings.Text = "Show shape warnings";
            this.checkShapeWarnings.UseVisualStyleBackColor = true;
            // 
            // trackLODBias
            // 
            this.trackLODBias.AutoSize = false;
            this.trackLODBias.BackColor = System.Drawing.SystemColors.Window;
            this.trackLODBias.LargeChange = 10;
            this.trackLODBias.Location = new System.Drawing.Point(6, 277);
            this.trackLODBias.Maximum = 100;
            this.trackLODBias.Minimum = -100;
            this.trackLODBias.Name = "trackLODBias";
            this.trackLODBias.Size = new System.Drawing.Size(292, 26);
            this.trackLODBias.TabIndex = 16;
            this.trackLODBias.TickFrequency = 10;
            this.toolTip1.SetToolTip(this.trackLODBias, "Default is 0%");
            this.trackLODBias.ValueChanged += new System.EventHandler(this.trackLODBias_ValueChanged);
            // 
            // checkConditionalLoadOfNightTextures
            // 
            this.checkConditionalLoadOfNightTextures.AutoSize = true;
            this.checkConditionalLoadOfNightTextures.Location = new System.Drawing.Point(304, 28);
            this.checkConditionalLoadOfNightTextures.Name = "checkConditionalLoadOfNightTextures";
            this.checkConditionalLoadOfNightTextures.Size = new System.Drawing.Size(228, 17);
            this.checkConditionalLoadOfNightTextures.TabIndex = 17;
            this.checkConditionalLoadOfNightTextures.Text = "Load day/night textures only when needed";
            this.checkConditionalLoadOfNightTextures.UseVisualStyleBackColor = true;
            // 
            // AdhesionLevelValue
            // 
            this.AdhesionLevelValue.Location = new System.Drawing.Point(357, 360);
            this.AdhesionLevelValue.Margin = new System.Windows.Forms.Padding(3);
            this.AdhesionLevelValue.Name = "AdhesionLevelValue";
            this.AdhesionLevelValue.Size = new System.Drawing.Size(236, 13);
            this.AdhesionLevelValue.TabIndex = 34;
            // 
            // AdhesionLevelLabel
            // 
            this.AdhesionLevelLabel.Location = new System.Drawing.Point(301, 360);
            this.AdhesionLevelLabel.Margin = new System.Windows.Forms.Padding(3);
            this.AdhesionLevelLabel.Name = "AdhesionLevelLabel";
            this.AdhesionLevelLabel.Size = new System.Drawing.Size(50, 13);
            this.AdhesionLevelLabel.TabIndex = 33;
            this.AdhesionLevelLabel.Text = "Level:";
            // 
            // trackAdhesionFactorChange
            // 
            this.trackAdhesionFactorChange.AutoSize = false;
            this.trackAdhesionFactorChange.BackColor = System.Drawing.SystemColors.Window;
            this.trackAdhesionFactorChange.LargeChange = 10;
            this.trackAdhesionFactorChange.Location = new System.Drawing.Point(301, 328);
            this.trackAdhesionFactorChange.Maximum = 100;
            this.trackAdhesionFactorChange.Name = "trackAdhesionFactorChange";
            this.trackAdhesionFactorChange.Size = new System.Drawing.Size(292, 26);
            this.trackAdhesionFactorChange.TabIndex = 32;
            this.trackAdhesionFactorChange.TickFrequency = 10;
            this.toolTip1.SetToolTip(this.trackAdhesionFactorChange, "Default is 10%");
            this.trackAdhesionFactorChange.Value = 10;
            this.trackAdhesionFactorChange.ValueChanged += new System.EventHandler(this.trackAdhesionFactor_ValueChanged);
            // 
            // trackAdhesionFactor
            // 
            this.trackAdhesionFactor.AutoSize = false;
            this.trackAdhesionFactor.BackColor = System.Drawing.SystemColors.Window;
            this.trackAdhesionFactor.LargeChange = 10;
            this.trackAdhesionFactor.Location = new System.Drawing.Point(301, 277);
            this.trackAdhesionFactor.Maximum = 200;
            this.trackAdhesionFactor.Minimum = 10;
            this.trackAdhesionFactor.Name = "trackAdhesionFactor";
            this.trackAdhesionFactor.Size = new System.Drawing.Size(292, 26);
            this.trackAdhesionFactor.TabIndex = 28;
            this.trackAdhesionFactor.TickFrequency = 10;
            this.toolTip1.SetToolTip(this.trackAdhesionFactor, "Default is 130%");
            this.trackAdhesionFactor.Value = 130;
            this.trackAdhesionFactor.ValueChanged += new System.EventHandler(this.trackAdhesionFactor_ValueChanged);
            // 
            // checkAdhesionPropToWeather
            // 
            this.checkAdhesionPropToWeather.AutoSize = true;
            this.checkAdhesionPropToWeather.Location = new System.Drawing.Point(304, 235);
            this.checkAdhesionPropToWeather.Name = "checkAdhesionPropToWeather";
            this.checkAdhesionPropToWeather.Size = new System.Drawing.Size(210, 17);
            this.checkAdhesionPropToWeather.TabIndex = 29;
            this.checkAdhesionPropToWeather.Text = "Adhesion proportional to rain/snow/fog";
            this.checkAdhesionPropToWeather.UseVisualStyleBackColor = true;
            this.checkAdhesionPropToWeather.CheckedChanged += new System.EventHandler(this.AdhesionPropToWeatherCheckBox_CheckedChanged);
            // 
            // checkCircularSpeedGauge
            // 
            this.checkCircularSpeedGauge.AutoSize = true;
            this.checkCircularSpeedGauge.Location = new System.Drawing.Point(6, 212);
            this.checkCircularSpeedGauge.Name = "checkCircularSpeedGauge";
            this.checkCircularSpeedGauge.Size = new System.Drawing.Size(156, 17);
            this.checkCircularSpeedGauge.TabIndex = 21;
            this.checkCircularSpeedGauge.Text = "ETCS circular speed gauge";
            this.checkCircularSpeedGauge.UseVisualStyleBackColor = true;
            // 
            // checkSignalLightGlow
            // 
            this.checkSignalLightGlow.AutoSize = true;
            this.checkSignalLightGlow.Location = new System.Drawing.Point(304, 51);
            this.checkSignalLightGlow.Name = "checkSignalLightGlow";
            this.checkSignalLightGlow.Size = new System.Drawing.Size(102, 17);
            this.checkSignalLightGlow.TabIndex = 18;
            this.checkSignalLightGlow.Text = "Signal light glow";
            this.checkSignalLightGlow.UseVisualStyleBackColor = true;
            // 
            // checkUseMSTSEnv
            // 
            this.checkUseMSTSEnv.AutoSize = true;
            this.checkUseMSTSEnv.Location = new System.Drawing.Point(304, 212);
            this.checkUseMSTSEnv.Name = "checkUseMSTSEnv";
            this.checkUseMSTSEnv.Size = new System.Drawing.Size(122, 17);
            this.checkUseMSTSEnv.TabIndex = 25;
            this.checkUseMSTSEnv.Text = "MSTS environments";
            this.checkUseMSTSEnv.UseVisualStyleBackColor = true;
            // 
            // labelPerformanceTunerTarget
            // 
            this.labelPerformanceTunerTarget.AutoSize = true;
            this.labelPerformanceTunerTarget.Location = new System.Drawing.Point(86, 144);
            this.labelPerformanceTunerTarget.Margin = new System.Windows.Forms.Padding(3);
            this.labelPerformanceTunerTarget.Name = "labelPerformanceTunerTarget";
            this.labelPerformanceTunerTarget.Size = new System.Drawing.Size(88, 13);
            this.labelPerformanceTunerTarget.TabIndex = 10;
            this.labelPerformanceTunerTarget.Text = "Target frame rate";
            // 
            // numericPerformanceTunerTarget
            // 
            this.numericPerformanceTunerTarget.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericPerformanceTunerTarget.Location = new System.Drawing.Point(26, 142);
            this.numericPerformanceTunerTarget.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericPerformanceTunerTarget.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numericPerformanceTunerTarget.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericPerformanceTunerTarget.Name = "numericPerformanceTunerTarget";
            this.numericPerformanceTunerTarget.Size = new System.Drawing.Size(54, 20);
            this.numericPerformanceTunerTarget.TabIndex = 9;
            this.toolTip1.SetToolTip(this.numericPerformanceTunerTarget, "Distance to see mountains");
            this.numericPerformanceTunerTarget.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // checkPerformanceTuner
            // 
            this.checkPerformanceTuner.AutoSize = true;
            this.checkPerformanceTuner.Location = new System.Drawing.Point(6, 120);
            this.checkPerformanceTuner.Name = "checkPerformanceTuner";
            this.checkPerformanceTuner.Size = new System.Drawing.Size(277, 17);
            this.checkPerformanceTuner.TabIndex = 8;
            this.checkPerformanceTuner.Text = "Automatically tune settings to keep performance level";
            this.checkPerformanceTuner.UseVisualStyleBackColor = true;
            this.checkPerformanceTuner.Click += new System.EventHandler(this.checkPerformanceTuner_Click);
            // 
            // checkLODViewingExtention
            // 
            this.checkLODViewingExtention.AutoSize = true;
            this.checkLODViewingExtention.Location = new System.Drawing.Point(304, 166);
            this.checkLODViewingExtention.Name = "checkLODViewingExtention";
            this.checkLODViewingExtention.Size = new System.Drawing.Size(268, 17);
            this.checkLODViewingExtention.TabIndex = 22;
            this.checkLODViewingExtention.Text = "Extend object maximum viewing distance to horizon";
            this.checkLODViewingExtention.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(86, 96);
            this.label8.Margin = new System.Windows.Forms.Padding(3);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 13);
            this.label8.TabIndex = 7;
            this.label8.Text = "Gauge (mm)";
            // 
            // numericSuperElevationGauge
            // 
            this.numericSuperElevationGauge.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericSuperElevationGauge.Location = new System.Drawing.Point(26, 94);
            this.numericSuperElevationGauge.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericSuperElevationGauge.Maximum = new decimal(new int[] {
            1800,
            0,
            0,
            0});
            this.numericSuperElevationGauge.Minimum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.numericSuperElevationGauge.Name = "numericSuperElevationGauge";
            this.numericSuperElevationGauge.Size = new System.Drawing.Size(54, 20);
            this.numericSuperElevationGauge.TabIndex = 6;
            this.numericSuperElevationGauge.Value = new decimal(new int[] {
            600,
            0,
            0,
            0});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(86, 70);
            this.label7.Margin = new System.Windows.Forms.Padding(3);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(97, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "Minimum length (m)";
            // 
            // numericSuperElevationMinLen
            // 
            this.numericSuperElevationMinLen.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericSuperElevationMinLen.Location = new System.Drawing.Point(26, 68);
            this.numericSuperElevationMinLen.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericSuperElevationMinLen.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericSuperElevationMinLen.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericSuperElevationMinLen.Name = "numericSuperElevationMinLen";
            this.numericSuperElevationMinLen.Size = new System.Drawing.Size(54, 20);
            this.numericSuperElevationMinLen.TabIndex = 4;
            this.toolTip1.SetToolTip(this.numericSuperElevationMinLen, "Shortest curve to have elevation");
            this.numericSuperElevationMinLen.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(86, 44);
            this.label6.Margin = new System.Windows.Forms.Padding(3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(33, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "Level";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.SystemColors.Highlight;
            this.label5.Location = new System.Drawing.Point(6, 6);
            this.label5.Margin = new System.Windows.Forms.Padding(3);
            this.label5.MaximumSize = new System.Drawing.Size(590, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(354, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Experimental features that may slow down the game, use at your own risk.";
            // 
            // numericUseSuperElevation
            // 
            this.numericUseSuperElevation.Location = new System.Drawing.Point(26, 42);
            this.numericUseSuperElevation.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.numericUseSuperElevation.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUseSuperElevation.Name = "numericUseSuperElevation";
            this.numericUseSuperElevation.Size = new System.Drawing.Size(54, 20);
            this.numericUseSuperElevation.TabIndex = 2;
            this.toolTip1.SetToolTip(this.numericUseSuperElevation, "0: no elevation, 1: 9cm max; 10: 18cm max");
            // 
            // ElevationText
            // 
            this.ElevationText.AutoSize = true;
            this.ElevationText.Location = new System.Drawing.Point(6, 23);
            this.ElevationText.Margin = new System.Windows.Forms.Padding(3);
            this.ElevationText.Name = "ElevationText";
            this.ElevationText.Size = new System.Drawing.Size(81, 13);
            this.ElevationText.TabIndex = 1;
            this.ElevationText.Text = "Super-elevation";
            // 
            // checkPreferDDSTexture
            // 
            this.checkPreferDDSTexture.AutoSize = true;
            this.checkPreferDDSTexture.Location = new System.Drawing.Point(304, 189);
            this.checkPreferDDSTexture.Name = "checkPreferDDSTexture";
            this.checkPreferDDSTexture.Size = new System.Drawing.Size(217, 17);
            this.checkPreferDDSTexture.TabIndex = 23;
            this.checkPreferDDSTexture.Text = "Load DDS textures in preference to ACE";
            this.checkPreferDDSTexture.UseVisualStyleBackColor = true;
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(634, 481);
            this.Controls.Add(this.tabOptions);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericBrakePipeChargingRate)).EndInit();
            this.tabOptions.ResumeLayout(false);
            this.tabPageGeneral.ResumeLayout(false);
            this.tabPageGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOverspeedMonitor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEnableWebServer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDisableTcsScripts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbOtherUnits)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbPressureUnit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLanguage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrakePipeChargingRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbGraduatedRelease)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbRetainers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLAA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAlerter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbControlConfirmations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbMapWindow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericWebServerPort)).EndInit();
            this.tabPageAudio.ResumeLayout(false);
            this.tabPageAudio.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericExternalSoundPassThruPercent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundVolumePercent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSoundDetailLevel)).EndInit();
            this.tabPageVideo.ResumeLayout(false);
            this.tabPageVideo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackAntiAliasing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackDayAmbientLight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDistantMountainsViewingDistance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericViewingDistance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericViewingFOV)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCab2DStretch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericWorldObjectDensity)).EndInit();
            this.tabPageSimulation.ResumeLayout(false);
            this.tabPageSimulation.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericAdhesionMovingAverageFilterSize)).EndInit();
            this.tabPageKeyboard.ResumeLayout(false);
            this.tabPageDataLogger.ResumeLayout(false);
            this.tabPageDataLogger.PerformLayout();
            this.tabPageEvaluate.ResumeLayout(false);
            this.tabPageEvaluate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericDataLogTSInterval)).EndInit();
            this.tabPageContent.ResumeLayout(false);
            this.tabPageContent.PerformLayout();
            this.groupBoxContent.ResumeLayout(false);
            this.groupBoxContent.PerformLayout();
            this.panelContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewContent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSourceContent)).EndInit();
            this.tabPageUpdater.ResumeLayout(false);
            this.tabPageUpdater.PerformLayout();
            this.tabPageExperimental.ResumeLayout(false);
            this.tabPageExperimental.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericActWeatherRandomizationLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericActRandomizationLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.precipitationBoxLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.precipitationBoxWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.precipitationBoxHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackLODBias)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAdhesionFactorChange)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackAdhesionFactor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericPerformanceTunerTarget)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSuperElevationGauge)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSuperElevationMinLen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUseSuperElevation)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.NumericUpDown numericBrakePipeChargingRate;
        private System.Windows.Forms.Label lBrakePipeChargingRate;
        private System.Windows.Forms.CheckBox checkGraduatedRelease;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkAlerter;
        private System.Windows.Forms.CheckBox checkControlConfirmations;
		private System.Windows.Forms.CheckBox checkViewMapWindow;
        private System.Windows.Forms.TabControl tabOptions;
        private System.Windows.Forms.TabPage tabPageGeneral;
        private System.Windows.Forms.TabPage tabPageKeyboard;
        private System.Windows.Forms.Button buttonDefaultKeys;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonCheckKeys;
        private System.Windows.Forms.Panel panelKeys;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.TabPage tabPageSimulation;
        private System.Windows.Forms.CheckBox checkUseAdvancedAdhesion;
        private System.Windows.Forms.CheckBox checkBreakCouplers;
        private System.Windows.Forms.TabPage tabPageExperimental;
        private System.Windows.Forms.NumericUpDown numericUseSuperElevation;
        private System.Windows.Forms.Label ElevationText;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tabPageAudio;
        private System.Windows.Forms.TabPage tabPageVideo;
        private System.Windows.Forms.NumericUpDown numericSoundVolumePercent;
        private System.Windows.Forms.Label soundVolumeLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericSoundDetailLevel;
        private System.Windows.Forms.CheckBox checkMSTSBINSound;
        private System.Windows.Forms.NumericUpDown numericCab2DStretch;
        private System.Windows.Forms.Label labelCab2DStretch;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericWorldObjectDensity;
        private System.Windows.Forms.ComboBox comboWindowSize;
        private System.Windows.Forms.CheckBox checkWindowGlass;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkDynamicShadows;
        private System.Windows.Forms.CheckBox checkWire;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numericSuperElevationMinLen;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numericSuperElevationGauge;
        private System.Windows.Forms.Label labelFOVHelp;
        private System.Windows.Forms.NumericUpDown numericViewingFOV;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.NumericUpDown numericViewingDistance;
        private System.Windows.Forms.CheckBox checkLODViewingExtention;
        private System.Windows.Forms.TabPage tabPageDataLogger;
        private System.Windows.Forms.ComboBox comboDataLoggerSeparator;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.CheckBox checkDataLogPhysics;
        private System.Windows.Forms.CheckBox checkDataLogPerformance;
        private System.Windows.Forms.CheckBox checkDataLogSteamPerformance;
        private System.Windows.Forms.CheckBox checkVerboseConfigurationMessages;
        private System.Windows.Forms.CheckBox checkDataLogger;
        private System.Windows.Forms.CheckBox checkDataLogMisc;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.ComboBox comboDataLogSpeedUnits;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label labelAdhesionMovingAverageFilterSize;
        private System.Windows.Forms.NumericUpDown numericAdhesionMovingAverageFilterSize;
        private System.Windows.Forms.Label labelPerformanceTunerTarget;
        private System.Windows.Forms.NumericUpDown numericPerformanceTunerTarget;
        private System.Windows.Forms.CheckBox checkPerformanceTuner;
        private System.Windows.Forms.CheckBox checkOverrideNonElectrifiedRoutes;
        private System.Windows.Forms.TabPage tabPageEvaluate;
        private System.Windows.Forms.CheckedListBox checkListDataLogTSContents;
        private System.Windows.Forms.Label labelDataLogTSInterval;
        private System.Windows.Forms.CheckBox checkDataLogStationStops;
        private System.Windows.Forms.NumericUpDown numericDataLogTSInterval;
        private System.Windows.Forms.CheckBox checkDataLogTrainSpeed;
        private System.Windows.Forms.CheckBox checkUseMSTSEnv;
        private System.Windows.Forms.CheckBox checkPreferDDSTexture;
        private System.Windows.Forms.CheckBox checkCurveResistanceDependent;
        private System.Windows.Forms.CheckBox checkTunnelResistanceDependent;
        private System.Windows.Forms.CheckBox checkWindResistanceDependent;
        private System.Windows.Forms.Label labelLanguage;
        private System.Windows.Forms.ComboBox comboLanguage;
        private System.Windows.Forms.Label labelDistantMountainsViewingDistance;
        private System.Windows.Forms.NumericUpDown numericDistantMountainsViewingDistance;
        private System.Windows.Forms.CheckBox checkDistantMountains;
        private System.Windows.Forms.CheckBox checkAlerterExternal;
        private System.Windows.Forms.CheckBox checkCurveSpeedDependent;
        private System.Windows.Forms.CheckBox checkHotStart;
        private System.Windows.Forms.CheckBox checkSimpleControlsPhysics;
        private System.Windows.Forms.CheckBox checkFastFullScreenAltTab;
        private System.Windows.Forms.CheckBox checkVerticalSync;
        private System.Windows.Forms.ComboBox comboPressureUnit;
        private System.Windows.Forms.Label labelPressureUnit;
        private System.Windows.Forms.CheckBox checkCircularSpeedGauge;
        private System.Windows.Forms.CheckBox checkSignalLightGlow;
        private System.Windows.Forms.TabPage tabPageUpdater;
        private System.Windows.Forms.Label labelUpdateChannel;
        private System.Windows.Forms.Label AdhesionFactorChangeValueLabel;
        private System.Windows.Forms.Label AdhesionFactorValueLabel;
        private System.Windows.Forms.Label AdhesionLevelValue;
        private System.Windows.Forms.Label AdhesionLevelLabel;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TrackBar trackAdhesionFactorChange;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TrackBar trackAdhesionFactor;
        private System.Windows.Forms.CheckBox checkAdhesionPropToWeather;
        private System.Windows.Forms.CheckBox checkModelInstancing;
        private System.Windows.Forms.CheckBox checkUseLargeAddressAware;
        private System.Windows.Forms.TrackBar trackDayAmbientLight;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.CheckBox checkConditionalLoadOfNightTextures;
        private System.Windows.Forms.CheckBox checkRetainers;
        private System.Windows.Forms.Label labelLODBias;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TrackBar trackLODBias;
        private System.Windows.Forms.Label labelOtherUnits;
        private System.Windows.Forms.ComboBox comboOtherUnits;
        private System.Windows.Forms.TabPage tabPageContent;
        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.DataGridView dataGridViewContent;
        private System.Windows.Forms.BindingSource bindingSourceContent;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn pathDataGridViewTextBoxColumn;
        private System.Windows.Forms.GroupBox groupBoxContent;
        private System.Windows.Forms.TextBox textBoxContentPath;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label22;
        public System.Windows.Forms.TextBox textBoxContentName;
        private System.Windows.Forms.Button buttonContentDelete;
        private System.Windows.Forms.Button buttonContentBrowse;
        private System.Windows.Forms.Button buttonContentAdd;
        private System.Windows.Forms.Label labelContent;
        private System.Windows.Forms.CheckBox checkShapeWarnings;
        private System.Windows.Forms.Label labelDayAmbientLight;
        private System.Windows.Forms.CheckBox checkDisableTCSScripts;
        private System.Windows.Forms.NumericUpDown precipitationBoxHeight;
        private System.Windows.Forms.NumericUpDown precipitationBoxWidth;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.NumericUpDown precipitationBoxLength;
        private System.Windows.Forms.CheckBox checkCorrectQuestionableBrakingParams;
        private System.Windows.Forms.CheckBox checkOverspeedMonitor;
        private System.Windows.Forms.NumericUpDown numericExternalSoundPassThruPercent;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox checkDoubleWire;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkDoorsAITrains;
        private System.Windows.Forms.CheckBox checkForcedRedAtStationStops;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown numericActRandomizationLevel;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.NumericUpDown numericActWeatherRandomizationLevel;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.CheckBox checkShadowAllShapes;
        private System.Windows.Forms.CheckBox checkEnableWebServer;
        private System.Windows.Forms.NumericUpDown numericWebServerPort;
        private System.Windows.Forms.Label labelPortNumber;
        private System.Windows.Forms.CheckBox checkUseLocationPassingPaths;
        private System.Windows.Forms.CheckBox checkBoxNoDieselEngineStart;
        private System.Windows.Forms.PictureBox pbMapWindow;
        private System.Windows.Forms.PictureBox pbControlConfirmations;
        private System.Windows.Forms.PictureBox pbAlerter;
        private System.Windows.Forms.PictureBox pbLAA;
        private System.Windows.Forms.PictureBox pbRetainers;
        private System.Windows.Forms.PictureBox pbGraduatedRelease;
        private System.Windows.Forms.PictureBox pbBrakePipeChargingRate;
        private System.Windows.Forms.PictureBox pbLanguage;
        private System.Windows.Forms.PictureBox pbPressureUnit;
        private System.Windows.Forms.PictureBox pbOtherUnits;
        private System.Windows.Forms.PictureBox pbEnableWebServer;
        private System.Windows.Forms.PictureBox pbDisableTcsScripts;
        private System.Windows.Forms.PictureBox pbOverspeedMonitor;
        private System.Windows.Forms.TrackBar trackAntiAliasing;
        private System.Windows.Forms.Label labelAntiAliasingValue;
        private System.Windows.Forms.Label labelAntiAliasing;
    }
}
