// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

using GNU.Gettext;
using GNU.Gettext.WinForms;
using MSTS;
using ORTS.Settings;
using ORTS.Updater;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ORTS
{
    public partial class OptionsForm : Form
    {
        readonly UserSettings Settings;
        readonly UpdateManager UpdateManager;

        private GettextResourceManager catalog = new GettextResourceManager("Menu");

        public class ComboBoxMember
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public class ContentFolder
        {
            public string Name { get; set; }
            public string Path { get; set; }

            public ContentFolder()
            {
                Name = "";
                Path = "";
            }
        }

        public OptionsForm(UserSettings settings, UpdateManager updateManager, bool initialContentSetup)
        {
            InitializeComponent();

            Localizer.Localize(this, catalog);

            Settings = settings;
            UpdateManager = updateManager;

            // Collect all the available language codes by searching for
            // localisation files, but always include English (base language).
            var languageCodes = new List<string> { "en" };
            foreach (var path in Directory.GetDirectories(Path.GetDirectoryName(Application.ExecutablePath)))
                if (Directory.GetFiles(path, "*.Messages.resources.dll").Length > 0)
                    languageCodes.Add(Path.GetFileName(path));

            // Turn the list of codes in to a list of code + name pairs for
            // displaying in the dropdown list.
            comboLanguage.DataSource =
                new[] { new ComboBoxMember { Code = "", Name = "System" } }
                .Union(languageCodes
                    .SelectMany(lc =>
                    {
                        try
                        {
                            return new[] { new ComboBoxMember { Code = lc, Name = CultureInfo.GetCultureInfo(lc).NativeName } };
                        }
                        catch (ArgumentException)
                        {
                            return new ComboBoxMember[0];
                        }
                    })
                    .OrderBy(l => l.Name)
                )
                .ToList();
            comboLanguage.DisplayMember = "Name";
            comboLanguage.ValueMember = "Code";
            comboLanguage.SelectedValue = Settings.Language;
            if (comboLanguage.SelectedValue == null) comboLanguage.SelectedIndex = 0;

            comboBoxOtherUnits.DataSource = new[] {
                new ComboBoxMember { Code = "Route", Name = catalog.GetString("Route") },
                new ComboBoxMember { Code = "Automatic", Name = catalog.GetString("Player's location") },
                new ComboBoxMember { Code = "Metric", Name = catalog.GetString("Metric") },
                new ComboBoxMember { Code = "US", Name = catalog.GetString("Imperial US") },
                new ComboBoxMember { Code = "UK", Name = catalog.GetString("Imperial UK") },
            }.ToList();
            comboBoxOtherUnits.DisplayMember = "Name";
            comboBoxOtherUnits.ValueMember = "Code";
            comboBoxOtherUnits.SelectedValue = Settings.Units;

            comboPressureUnit.DataSource = new[] {
                new ComboBoxMember { Code = "Automatic", Name = catalog.GetString("Automatic") },
                new ComboBoxMember { Code = "bar", Name = catalog.GetString("bar") },
                new ComboBoxMember { Code = "PSI", Name = catalog.GetString("psi") },
                new ComboBoxMember { Code = "inHg", Name = catalog.GetString("inHg") },
                new ComboBoxMember { Code = "kgf/cm^2", Name = catalog.GetString("kgf/cm²") },
            }.ToList();
            comboPressureUnit.DisplayMember = "Name";
            comboPressureUnit.ValueMember = "Code";
            comboPressureUnit.SelectedValue = Settings.PressureUnit;

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;
            AdhesionLevelValue.Font = new Font(Font, FontStyle.Bold);

            // Fix up the TrackBars on TabPanels to match the current theme.
            if (!Application.RenderWithVisualStyles)
            {
                trackAdhesionFactor.BackColor = BackColor;
                trackAdhesionFactorChange.BackColor = BackColor;
                trackDayAmbientLight.BackColor = BackColor;
                trackLODBias.BackColor = BackColor;
            }

            // General tab
            checkAlerter.Checked = Settings.Alerter;
            checkAlerterExternal.Enabled = Settings.Alerter;
            checkAlerterExternal.Checked = Settings.Alerter && !Settings.AlerterDisableExternal;
            checkSpeedControl.Checked = Settings.SpeedControl;
            checkConfirmations.Checked = !Settings.SuppressConfirmations;
            checkViewDispatcher.Checked = Settings.ViewDispatcher;
            checkUseLargeAddressAware.Checked = Settings.UseLargeAddressAware;
            checkRetainers.Checked = Settings.RetainersOnAllCars;
            checkGraduatedRelease.Checked = Settings.GraduatedRelease;
            numericBrakePipeChargingRate.Value = Settings.BrakePipeChargingRate;
            comboLanguage.Text = Settings.Language;
            comboPressureUnit.Text = Settings.PressureUnit;
            comboBoxOtherUnits.Text = settings.Units;
            checkDisableTCSScripts.Checked = Settings.DisableTCSScripts;


            // Audio tab
            checkMSTSBINSound.Checked = Settings.MSTSBINSound;
            numericSoundVolumePercent.Value = Settings.SoundVolumePercent;
            numericSoundDetailLevel.Value = Settings.SoundDetailLevel;
            numericExternalSoundPassThruPercent.Value = Settings.ExternalSoundPassThruPercent;

            // Video tab
            checkDynamicShadows.Checked = Settings.DynamicShadows;
            checkShadowAllShapes.Checked = Settings.ShadowAllShapes;
            checkFastFullScreenAltTab.Checked = Settings.FastFullScreenAltTab;
            checkWindowGlass.Checked = Settings.WindowGlass;
            checkModelInstancing.Checked = Settings.ModelInstancing;
            checkWire.Checked = Settings.Wire;
            checkVerticalSync.Checked = Settings.VerticalSync;
            checkEnableMultisampling.Checked = Settings.EnableMultisampling;
            numericCab2DStretch.Value = Settings.Cab2DStretch;
            numericViewingDistance.Value = Settings.ViewingDistance;
            checkDistantMountains.Checked = Settings.DistantMountains;
            labelDistantMountainsViewingDistance.Enabled = checkDistantMountains.Checked;
            numericDistantMountainsViewingDistance.Enabled = checkDistantMountains.Checked;
            numericDistantMountainsViewingDistance.Value = Settings.DistantMountainsViewingDistance / 1000;
            numericViewingFOV.Value = Settings.ViewingFOV;
            numericWorldObjectDensity.Value = Settings.WorldObjectDensity;
            comboWindowSize.Text = Settings.WindowSize;
            trackDayAmbientLight.Value = Settings.DayAmbientLight;
            trackDayAmbientLight_ValueChanged(null, null);
            checkDoubleWire.Checked = Settings.DoubleWire;

            // Simulation tab
            checkUseAdvancedAdhesion.Checked = Settings.UseAdvancedAdhesion;
            labelAdhesionMovingAverageFilterSize.Enabled = checkUseAdvancedAdhesion.Checked;
            numericAdhesionMovingAverageFilterSize.Enabled = checkUseAdvancedAdhesion.Checked; 
            numericAdhesionMovingAverageFilterSize.Value = Settings.AdhesionMovingAverageFilterSize;
            checkBreakCouplers.Checked = Settings.BreakCouplers;
            checkCurveResistanceDependent.Checked = Settings.CurveResistanceDependent;
            checkCurveSpeedDependent.Checked = Settings.CurveSpeedDependent;
            checkTunnelResistanceDependent.Checked = Settings.TunnelResistanceDependent;
            checkWindResistanceDependent.Checked = Settings.WindResistanceDependent;
            checkOverrideNonElectrifiedRoutes.Checked = Settings.OverrideNonElectrifiedRoutes;
            checkHotStart.Checked = Settings.HotStart;
            checkAutopilot.Checked = Settings.Autopilot;
            checkForcedRedAtStationStops.Checked = !Settings.NoForcedRedAtStationStops;
            checkExtendedAIShunting.Checked = Settings.ExtendedAIShunting;
            checkDoorsAITrains.Checked = Settings.OpenDoorsInAITrains;

            // Keyboard tab
            InitializeKeyboardSettings();

            // DataLogger tab
            var dictionaryDataLoggerSeparator = new Dictionary<string, string>();
            dictionaryDataLoggerSeparator.Add("comma", catalog.GetString("comma"));
            dictionaryDataLoggerSeparator.Add("semicolon", catalog.GetString("semicolon"));
            dictionaryDataLoggerSeparator.Add("tab", catalog.GetString("tab"));
            dictionaryDataLoggerSeparator.Add("space", catalog.GetString("space"));
            comboDataLoggerSeparator.DataSource = new BindingSource(dictionaryDataLoggerSeparator, null);
            comboDataLoggerSeparator.DisplayMember = "Value";
            comboDataLoggerSeparator.ValueMember = "Key";
            comboDataLoggerSeparator.Text = catalog.GetString(Settings.DataLoggerSeparator);
            var dictionaryDataLogSpeedUnits = new Dictionary<string, string>();
            dictionaryDataLogSpeedUnits.Add("route", catalog.GetString("route"));
            dictionaryDataLogSpeedUnits.Add("mps", catalog.GetString("m/s"));
            dictionaryDataLogSpeedUnits.Add("kmph", catalog.GetString("km/h"));
            dictionaryDataLogSpeedUnits.Add("mph", catalog.GetString("mph"));
            comboDataLogSpeedUnits.DataSource = new BindingSource(dictionaryDataLogSpeedUnits, null);
            comboDataLogSpeedUnits.DisplayMember = "Value";
            comboDataLogSpeedUnits.ValueMember = "Key";
            comboDataLogSpeedUnits.Text = catalog.GetString(Settings.DataLogSpeedUnits);
            checkDataLogger.Checked = Settings.DataLogger;
            checkDataLogPerformance.Checked = Settings.DataLogPerformance;
            checkDataLogPhysics.Checked = Settings.DataLogPhysics;
            checkDataLogMisc.Checked = Settings.DataLogMisc;
            checkDataLogSteamPerformance.Checked = Settings.DataLogSteamPerformance;

            // Evaluation tab
            checkDataLogTrainSpeed.Checked = Settings.DataLogTrainSpeed;
            labelDataLogTSInterval.Enabled = checkDataLogTrainSpeed.Checked;
            numericDataLogTSInterval.Enabled = checkDataLogTrainSpeed.Checked;
            checkListDataLogTSContents.Enabled = checkDataLogTrainSpeed.Checked;  
            numericDataLogTSInterval.Value = Settings.DataLogTSInterval;
            checkListDataLogTSContents.Items.AddRange(new object[] {
                catalog.GetString("Time"),
                catalog.GetString("Train Speed"),
                catalog.GetString("Max. Speed"),
                catalog.GetString("Signal State"),
                catalog.GetString("Track Elevation"),
                catalog.GetString("Direction"),
                catalog.GetString("Control Mode"),
                catalog.GetString("Distance Travelled"),
                catalog.GetString("Throttle %"),
                catalog.GetString("Brake Cyl Press"),
                catalog.GetString("Dyn Brake %"),
                catalog.GetString("Gear Setting")
            });
            for (var i = 0; i < checkListDataLogTSContents.Items.Count; i++)
                checkListDataLogTSContents.SetItemChecked(i, Settings.DataLogTSContents[i] == 1);
            checkDataLogStationStops.Checked = Settings.DataLogStationStops;

            // Content tab
            bindingSourceContent.DataSource = (from folder in Settings.Folders.Folders
                                               orderby folder.Key
                                               select new ContentFolder() { Name = folder.Key, Path = folder.Value }).ToList();
            if (initialContentSetup)
            {
                tabOptions.SelectedTab = tabPageContent;
                buttonContentBrowse.Enabled = false; // Initial state because browsing a null path leads to an exception
                try
                {
                    bindingSourceContent.Add(new ContentFolder() { Name = "Train Simulator", Path = MSTSPath.Base() });
                }
                catch { }
            }

            // Updater tab
            var updateChannelNames = new Dictionary<string, string> {
                { "stable", catalog.GetString("Stable (recommended)") },
                { "testing", catalog.GetString("Testing") },
                { "unstable", catalog.GetString("Unstable") },
                { "", catalog.GetString("None") },
            };
            var updateChannelDescriptions = new Dictionary<string, string> {
                { "stable", catalog.GetString("Infrequent updates to official, hand-picked versions. Recommended for most users.") },
                { "testing", catalog.GetString("Weekly updates which may contain noticable defects. For project supporters.") },
                { "unstable", catalog.GetString("Daily updates which may contain serious defects. For developers only.") },
                { "", catalog.GetString("No updates.") },
            };
            var spacing = labelUpdateChannel.Margin.Size;
            var indent = 20;
            var top = labelUpdateChannel.Bottom + spacing.Height;
            foreach (var channel in UpdateManager.GetChannels())
            {
                var radio = new RadioButton()
                {
                    Text = updateChannelNames[channel.ToLowerInvariant()],
                    Margin = labelUpdateChannel.Margin,
                    Left = spacing.Width,
                    Top = top,
                    Checked = updateManager.ChannelName.Equals(channel, StringComparison.InvariantCultureIgnoreCase),
                    AutoSize = true,
                    Tag = channel,
                };
                tabPageUpdater.Controls.Add(radio);
                top += radio.Height + spacing.Height;
                var label = new Label()
                {
                    Text = updateChannelDescriptions[channel.ToLowerInvariant()],
                    Margin = labelUpdateChannel.Margin,
                    Left = spacing.Width + indent,
                    Top = top,
                    Width = tabPageUpdater.ClientSize.Width - indent - spacing.Width * 2,
                    AutoSize = true,
                };
                tabPageUpdater.Controls.Add(label);
                top += label.Height + spacing.Height;
            }

            // Experimental tab
            numericUseSuperElevation.Value = Settings.UseSuperElevation;
            numericSuperElevationMinLen.Value = Settings.SuperElevationMinLen;
            numericSuperElevationGauge.Value = Settings.SuperElevationGauge;
            checkPerformanceTuner.Checked = Settings.PerformanceTuner;
            labelPerformanceTunerTarget.Enabled = checkPerformanceTuner.Checked;
            numericPerformanceTunerTarget.Enabled = checkPerformanceTuner.Checked;
            numericPerformanceTunerTarget.Value = Settings.PerformanceTunerTarget;
            trackLODBias.Value = Settings.LODBias;
            trackLODBias_ValueChanged(null, null);
            checkConditionalLoadOfNightTextures.Checked = Settings.ConditionalLoadOfDayOrNightTextures;
            checkSignalLightGlow.Checked = Settings.SignalLightGlow;
            checkCircularSpeedGauge.Checked = Settings.CircularSpeedGauge;
            checkLODViewingExtention.Checked = Settings.LODViewingExtention;
            checkPreferDDSTexture.Checked = Settings.PreferDDSTexture;
            checkUseLocationPassingPaths.Checked = Settings.UseLocationPassingPaths;
            checkUseMSTSEnv.Checked = Settings.UseMSTSEnv;
            trackAdhesionFactor.Value = Settings.AdhesionFactor;
            checkAdhesionPropToWeather.Checked = Settings.AdhesionProportionalToWeather;
            trackAdhesionFactorChange.Value = Settings.AdhesionFactorChange;
            trackAdhesionFactor_ValueChanged(null, null);
            checkShapeWarnings.Checked = !Settings.SuppressShapeWarnings;
            precipitationBoxHeight.Value = Settings.PrecipitationBoxHeight;
            precipitationBoxWidth.Value = Settings.PrecipitationBoxWidth;
            precipitationBoxLength.Value = Settings.PrecipitationBoxLength;
            checkCorrectQuestionableBrakingParams.Checked = Settings.CorrectQuestionableBrakingParams;
            numericActRandomizationLevel.Value = Settings.ActRandomizationLevel;
            numericActWeatherRandomizationLevel.Value = Settings.ActWeatherRandomizationLevel;
        }

        static string ParseCategoryFrom(string name)
        {
            var len = name.IndexOf(' ');
            if (len == -1)
                return "";
            else
                return name.Substring(0, len);
        }


        static string ParseDescriptorFrom(string name)
        {
            var len = name.IndexOf(' ');
            if (len == -1)
                return name;
            else
                return name.Substring(len + 1);
        }

        void InitializeKeyboardSettings()
        {
            panelKeys.Controls.Clear();
            var columnWidth = (panelKeys.ClientSize.Width - 20) / 2;

            var tempLabel = new Label();
            var tempKIC = new KeyInputControl(Settings.Input.Commands[(int)UserCommands.GameQuit], InputSettings.DefaultCommands[(int)UserCommands.GameQuit]);
            var rowTop = Math.Max(tempLabel.Margin.Top, tempKIC.Margin.Top);
            var rowHeight = tempKIC.Height;
            var rowSpacing = rowHeight + tempKIC.Margin.Vertical;

            var lastCategory = "";
            var i = 0;
            foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
            {
                var name = InputSettings.GetPrettyLocalizedName(command);
                var category = ParseCategoryFrom(name);
                var descriptor = ParseDescriptorFrom(name);

                if (category != lastCategory)
                {
                    var catlabel = new Label();
                    catlabel.Location = new Point(tempLabel.Margin.Left, rowTop + rowSpacing * i);
                    catlabel.Size = new Size(columnWidth - tempLabel.Margin.Horizontal, rowHeight);
                    catlabel.Text = category;
                    catlabel.TextAlign = ContentAlignment.MiddleCenter;
                    catlabel.Font = new Font(catlabel.Font, FontStyle.Bold);
                    panelKeys.Controls.Add(catlabel);

                    lastCategory = category;
                    ++i;
                }

                var label = new Label();
                label.Location = new Point(tempLabel.Margin.Left, rowTop + rowSpacing * i);
                label.Size = new Size(columnWidth - tempLabel.Margin.Horizontal, rowHeight);
                label.Text = descriptor;
                label.TextAlign = ContentAlignment.MiddleRight;
                panelKeys.Controls.Add(label);

                var keyInputControl = new KeyInputControl(Settings.Input.Commands[(int)command], InputSettings.DefaultCommands[(int)command]);
                keyInputControl.Location = new Point(columnWidth + tempKIC.Margin.Left, rowTop + rowSpacing * i);
                keyInputControl.Size = new Size(columnWidth - tempKIC.Margin.Horizontal, rowHeight);
                keyInputControl.ReadOnly = true;
                keyInputControl.Tag = command;
                panelKeys.Controls.Add(keyInputControl);
                toolTip1.SetToolTip(keyInputControl, catalog.GetString("Click to change this key"));

                ++i;
            }
        }

        void SaveKeyboardSettings()
        {
            foreach (Control control in panelKeys.Controls)
                if (control is KeyInputControl)
                    Settings.Input.Commands[(int)control.Tag].PersistentDescriptor = (control as KeyInputControl).UserInput.PersistentDescriptor;
        }

        void buttonOK_Click(object sender, EventArgs e)
        {
            var result = Settings.Input.CheckForErrors();
            if (result != "" && DialogResult.Yes != MessageBox.Show(catalog.GetString("Continue with conflicting key assignments?\n\n") + result, Application.ProductName, MessageBoxButtons.YesNo))
                return;

            DialogResult = DialogResult.OK;
            if (Settings.Language != comboLanguage.SelectedValue.ToString())
                DialogResult = DialogResult.Retry;

            // General tab
            Settings.Alerter = checkAlerter.Checked;
            Settings.AlerterDisableExternal = !checkAlerterExternal.Checked;
            Settings.SpeedControl = checkSpeedControl.Checked;
            Settings.SuppressConfirmations = !checkConfirmations.Checked;
            Settings.ViewDispatcher = checkViewDispatcher.Checked;
            Settings.UseLargeAddressAware = checkUseLargeAddressAware.Checked;
            Settings.RetainersOnAllCars = checkRetainers.Checked;
            Settings.GraduatedRelease = checkGraduatedRelease.Checked;
            Settings.BrakePipeChargingRate = (int)numericBrakePipeChargingRate.Value;
            Settings.Language = comboLanguage.SelectedValue.ToString();
            Settings.PressureUnit = comboPressureUnit.SelectedValue.ToString();
            Settings.Units = comboBoxOtherUnits.SelectedValue.ToString();
            Settings.DisableTCSScripts = checkDisableTCSScripts.Checked;

            // Audio tab
            Settings.MSTSBINSound = checkMSTSBINSound.Checked;
            Settings.SoundVolumePercent = (int)numericSoundVolumePercent.Value;
            Settings.SoundDetailLevel = (int)numericSoundDetailLevel.Value;
            Settings.ExternalSoundPassThruPercent = (int)numericExternalSoundPassThruPercent.Value;

            // Video tab
            Settings.DynamicShadows = checkDynamicShadows.Checked;
            Settings.ShadowAllShapes = checkShadowAllShapes.Checked;
            Settings.FastFullScreenAltTab = checkFastFullScreenAltTab.Checked;
            Settings.WindowGlass = checkWindowGlass.Checked;
            Settings.ModelInstancing = checkModelInstancing.Checked;
            Settings.Wire = checkWire.Checked;
            Settings.VerticalSync = checkVerticalSync.Checked;
            Settings.EnableMultisampling = checkEnableMultisampling.Checked;
            Settings.Cab2DStretch = (int)numericCab2DStretch.Value;
            Settings.ViewingDistance = (int)numericViewingDistance.Value;
            Settings.DistantMountains = checkDistantMountains.Checked;
            Settings.DistantMountainsViewingDistance = (int)numericDistantMountainsViewingDistance.Value * 1000;
            Settings.ViewingFOV = (int)numericViewingFOV.Value;
            Settings.WorldObjectDensity = (int)numericWorldObjectDensity.Value;
            Settings.WindowSize = comboWindowSize.Text;
            Settings.DayAmbientLight = (int)trackDayAmbientLight.Value;
            Settings.DoubleWire = checkDoubleWire.Checked;

            // Simulation tab
            Settings.UseAdvancedAdhesion = checkUseAdvancedAdhesion.Checked;
            Settings.AdhesionMovingAverageFilterSize = (int)numericAdhesionMovingAverageFilterSize.Value;
            Settings.BreakCouplers = checkBreakCouplers.Checked;
            Settings.CurveResistanceDependent = checkCurveResistanceDependent.Checked;
            Settings.CurveSpeedDependent = checkCurveSpeedDependent.Checked;
            Settings.TunnelResistanceDependent = checkTunnelResistanceDependent.Checked;
            Settings.WindResistanceDependent = checkWindResistanceDependent.Checked;
            Settings.OverrideNonElectrifiedRoutes = checkOverrideNonElectrifiedRoutes.Checked;
            Settings.HotStart = checkHotStart.Checked;
            Settings.Autopilot = checkAutopilot.Checked;
            Settings.NoForcedRedAtStationStops = !checkForcedRedAtStationStops.Checked;
            Settings.ExtendedAIShunting = checkExtendedAIShunting.Checked;
            Settings.OpenDoorsInAITrains = checkDoorsAITrains.Checked;

            // Keyboard tab
            // These are edited live.

            // DataLogger tab
            Settings.DataLoggerSeparator = comboDataLoggerSeparator.SelectedValue.ToString();
            Settings.DataLogSpeedUnits = comboDataLogSpeedUnits.SelectedValue.ToString();
            Settings.DataLogger = checkDataLogger.Checked;
            Settings.DataLogPerformance = checkDataLogPerformance.Checked;
            Settings.DataLogPhysics = checkDataLogPhysics.Checked;
            Settings.DataLogMisc = checkDataLogMisc.Checked;
            Settings.DataLogSteamPerformance = checkDataLogSteamPerformance.Checked;

            // Evaluation tab
            Settings.DataLogTrainSpeed = checkDataLogTrainSpeed.Checked;
            Settings.DataLogTSInterval = (int)numericDataLogTSInterval.Value;
            for (var i = 0; i < checkListDataLogTSContents.Items.Count; i++)
                Settings.DataLogTSContents[i] = checkListDataLogTSContents.GetItemChecked(i) ? 1 : 0;
            Settings.DataLogStationStops = checkDataLogStationStops.Checked;

            // Content tab
            Settings.Folders.Folders.Clear();
            foreach (var folder in bindingSourceContent.DataSource as List<ContentFolder>)
                Settings.Folders.Folders.Add(folder.Name, folder.Path);

            // Updater tab
            foreach (Control control in tabPageUpdater.Controls)
                if ((control is RadioButton) && (control as RadioButton).Checked)
                    UpdateManager.SetChannel((string)control.Tag);

            // Experimental tab
            Settings.UseSuperElevation = (int)numericUseSuperElevation.Value;
            Settings.SuperElevationMinLen = (int)numericSuperElevationMinLen.Value;
            Settings.SuperElevationGauge = (int)numericSuperElevationGauge.Value;
            Settings.PerformanceTuner = checkPerformanceTuner.Checked;
            Settings.PerformanceTunerTarget = (int)numericPerformanceTunerTarget.Value;
            Settings.LODBias = trackLODBias.Value;
            Settings.ConditionalLoadOfDayOrNightTextures = checkConditionalLoadOfNightTextures.Checked;
            Settings.SignalLightGlow = checkSignalLightGlow.Checked;
            Settings.CircularSpeedGauge = checkCircularSpeedGauge.Checked;
            Settings.LODViewingExtention = checkLODViewingExtention.Checked;
            Settings.PreferDDSTexture = checkPreferDDSTexture.Checked;
            Settings.UseLocationPassingPaths = checkUseLocationPassingPaths.Checked;
            Settings.UseMSTSEnv = checkUseMSTSEnv.Checked;
            Settings.AdhesionFactor = (int)trackAdhesionFactor.Value;
            Settings.AdhesionProportionalToWeather = checkAdhesionPropToWeather.Checked;
            Settings.AdhesionFactorChange = (int)trackAdhesionFactorChange.Value;
            Settings.SuppressShapeWarnings = !checkShapeWarnings.Checked;
            Settings.PrecipitationBoxHeight = (int)precipitationBoxHeight.Value;
            Settings.PrecipitationBoxWidth = (int)precipitationBoxWidth.Value;
            Settings.PrecipitationBoxLength = (int)precipitationBoxLength.Value;
            Settings.CorrectQuestionableBrakingParams = checkCorrectQuestionableBrakingParams.Checked;
            Settings.ActRandomizationLevel = (int)numericActRandomizationLevel.Value;
            Settings.ActWeatherRandomizationLevel = (int)numericActWeatherRandomizationLevel.Value;

            Settings.Save();
        }

        void buttonDefaultKeys_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show(catalog.GetString("Remove all custom key assignments?"), Application.ProductName, MessageBoxButtons.YesNo))
            {
                Settings.Input.Reset();
                InitializeKeyboardSettings();
            }
        }

        void buttonExport_Click(object sender, EventArgs e)
        {
            var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Open Rails Keyboard.txt");
            Settings.Input.DumpToText(outputPath);
            MessageBox.Show(catalog.GetString("A listing of all keyboard commands and keys has been placed here:\n\n") + outputPath, Application.ProductName);
        }

        void buttonCheckKeys_Click(object sender, EventArgs e)
        {
            var errors = Settings.Input.CheckForErrors();
            if (errors != "")
                MessageBox.Show(errors, Application.ProductName);
            else
                MessageBox.Show(catalog.GetString("No errors found."), Application.ProductName);
        }

        private void numericUpDownFOV_ValueChanged(object sender, EventArgs e)
        {
            labelFOVHelp.Text = catalog.GetStringFmt("{0:F0}° vertical FOV is the same as:\n{1:F0}° horizontal FOV on 4:3\n{2:F0}° horizontal FOV on 16:9", numericViewingFOV.Value, numericViewingFOV.Value * 4 / 3, numericViewingFOV.Value * 16 / 9);
        }

        private void trackBarDayAmbientLight_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trackDayAmbientLight, (trackDayAmbientLight.Value * 5).ToString() + " %");
        }

        private void trackAdhesionFactor_ValueChanged(object sender, EventArgs e)
        {
            SetAdhesionLevelValue();
            AdhesionFactorValueLabel.Text = trackAdhesionFactor.Value.ToString() + "%";
            AdhesionFactorChangeValueLabel.Text = trackAdhesionFactorChange.Value.ToString() + "%";
        }

        private void SetAdhesionLevelValue()
        {
            int level = trackAdhesionFactor.Value - trackAdhesionFactorChange.Value;
            if (checkAdhesionPropToWeather.Checked)
                level -= 40;

            if (level > 159)
                AdhesionLevelValue.Text = catalog.GetString("Very easy");
            else if (level > 139)
                AdhesionLevelValue.Text = catalog.GetString("Easy");
            else if (level > 119)
                AdhesionLevelValue.Text = catalog.GetString("MSTS Compatible");
            else if (level > 89)
                AdhesionLevelValue.Text = catalog.GetString("Normal");
            else if (level > 69)
                AdhesionLevelValue.Text = catalog.GetString("Hard");
            else if (level > 59)
                AdhesionLevelValue.Text = catalog.GetString("Very Hard");
            else
                AdhesionLevelValue.Text = catalog.GetString("Good luck!");
        }

        private void AdhesionPropToWeatherCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SetAdhesionLevelValue();
        }

        private void trackDayAmbientLight_ValueChanged(object sender, EventArgs e)
        {
            labelDayAmbientLight.Text = catalog.GetStringFmt("{0}%", trackDayAmbientLight.Value * 5);
        }

        private void trackLODBias_ValueChanged(object sender, EventArgs e)
        {
            if (trackLODBias.Value == -100)
                labelLODBias.Text = catalog.GetStringFmt("No detail (-{0}%)", -trackLODBias.Value);
            else if (trackLODBias.Value < 0)
                labelLODBias.Text = catalog.GetStringFmt("Less detail (-{0}%)", -trackLODBias.Value);
            else if (trackLODBias.Value == 0)
                labelLODBias.Text = catalog.GetStringFmt("Default detail (+{0}%)", trackLODBias.Value);
            else if (trackLODBias.Value < 100)
                labelLODBias.Text = catalog.GetStringFmt("More detail (+{0}%)", trackLODBias.Value);
            else
                labelLODBias.Text = catalog.GetStringFmt("All detail (+{0}%)", trackLODBias.Value);
        }

        private void dataGridViewContent_SelectionChanged(object sender, EventArgs e)
        {
            var current = bindingSourceContent.Current as ContentFolder;
            textBoxContentName.Enabled = buttonContentBrowse.Enabled = current != null;
            if (current == null)
            {
                textBoxContentName.Text = textBoxContentPath.Text = "";
            }
            else
            {
                textBoxContentName.Text = current.Name;
                textBoxContentPath.Text = current.Path;
            }
        }

        private void buttonContentAdd_Click(object sender, EventArgs e)
        {
            bindingSourceContent.AddNew();
            buttonContentBrowse_Click(sender, e);
        }

        private void buttonContentDelete_Click(object sender, EventArgs e)
        {
            bindingSourceContent.RemoveCurrent();
            // ResetBindings() is to work around a bug in the binding and/or data grid where by deleting the bottom item doesn't show the selection moving to the new bottom item.
            bindingSourceContent.ResetBindings(false);
        }

        private void buttonContentBrowse_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = textBoxContentPath.Text;
                folderBrowser.Description = catalog.GetString("Select an installation profile (MSTS folder) to add:");
                folderBrowser.ShowNewFolderButton = false;
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    var current = bindingSourceContent.Current as ContentFolder;
                    System.Diagnostics.Debug.Assert(current != null, "List should not be empty");
                    textBoxContentPath.Text = current.Path = folderBrowser.SelectedPath;
                    if (String.IsNullOrEmpty(current.Name))
                        // Don't need to set current.Name here as next statement triggers event textBoxContentName_TextChanged()
                        // which does that and also checks for duplicate names 
                        textBoxContentName.Text = Path.GetFileName(textBoxContentPath.Text);
                    bindingSourceContent.ResetCurrentItem();
                }
            }
        }

        /// <summary>
        /// Edits to the input field are copied back to the list of content.
        /// They are also checked for duplicate names which would lead to an exception when saving.
        /// if duplicate, then " copy" is silently appended to the entry in list of content.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxContentName_TextChanged(object sender, EventArgs e)
        {
            var current = bindingSourceContent.Current as ContentFolder;
            if (current != null && current.Name != textBoxContentName.Text)
            {
                // Duplicate names lead to an exception, so append " copy" if not unique
                var suffix = "";
                var isNameUnique = true;
                while (isNameUnique)
                {
                    isNameUnique = false; // to exit after a single pass
                    foreach (var item in bindingSourceContent)
                        if (((ContentFolder)item).Name == textBoxContentName.Text + suffix)
                        {
                            suffix += " copy"; // To ensure uniqueness
                            isNameUnique = true; // to force another pass
                            break;
                        }
                }
                current.Name = textBoxContentName.Text + suffix;
                bindingSourceContent.ResetCurrentItem();
            }
        }

        private void checkAlerter_CheckedChanged(object sender, EventArgs e)
        {
            //Disable checkAlerterExternal when checkAlerter is not checked
            if (checkAlerter.Checked )
            {
                checkAlerterExternal.Enabled = true; 
            }
            else
            {
                checkAlerterExternal.Enabled = false;
                checkAlerterExternal.Checked = false; 
            }
        }

        private void checkDistantMountains_Click(object sender, EventArgs e)
        {
           labelDistantMountainsViewingDistance.Enabled = checkDistantMountains.Checked;
           numericDistantMountainsViewingDistance.Enabled = checkDistantMountains.Checked;
        }

        private void checkUseAdvancedAdhesion_Click(object sender, EventArgs e)
        {
            labelAdhesionMovingAverageFilterSize.Enabled = checkUseAdvancedAdhesion.Checked;
            numericAdhesionMovingAverageFilterSize.Enabled = checkUseAdvancedAdhesion.Checked;
        }

        private void checkDataLogTrainSpeed_Click(object sender, EventArgs e)
        {
            checkListDataLogTSContents.Enabled = checkDataLogTrainSpeed.Checked;
            labelDataLogTSInterval.Enabled = checkDataLogTrainSpeed.Checked;
            numericDataLogTSInterval.Enabled = checkDataLogTrainSpeed.Checked;
        }

        private void checkPerformanceTuner_Click(object sender, EventArgs e)
        {
            numericPerformanceTunerTarget.Enabled = checkPerformanceTuner.Checked;
            labelPerformanceTunerTarget.Enabled = checkPerformanceTuner.Checked;
        }
    }
}