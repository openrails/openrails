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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GNU.Gettext;
using GNU.Gettext.WinForms;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Settings;
using ORTS.Updater;

namespace Menu
{
    public partial class OptionsForm : Form
    {
        readonly UserSettings Settings;
        readonly UpdateManager UpdateManager;
        readonly TelemetryManager TelemetryManager;
        readonly string BaseDocumentationUrl;

        private GettextResourceManager catalog = new GettextResourceManager("Menu");

        public class ComboBoxMember
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public OptionsForm(UserSettings settings, UpdateManager updateManager, TelemetryManager telemetryManager, string baseDocumentationUrl)
        {
            InitializeComponent();

            Localizer.Localize(this, catalog);

            Settings = settings;
            UpdateManager = updateManager;
            TelemetryManager = telemetryManager;
            BaseDocumentationUrl = baseDocumentationUrl;

            InitializeHelpIcons();

            // Collect all the available language codes by searching for
            // localisation files, but always include English (base language).
            var languageCodes = new List<string> { "en" };
            foreach (var path in Directory.GetDirectories(ApplicationInfo.ProcessDirectory))
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

            comboOtherUnits.DataSource = new[] {
                new ComboBoxMember { Code = "Route", Name = catalog.GetString("Route") },
                new ComboBoxMember { Code = "Automatic", Name = catalog.GetString("Player's location") },
                new ComboBoxMember { Code = "Metric", Name = catalog.GetString("Metric") },
                new ComboBoxMember { Code = "US", Name = catalog.GetString("Imperial US") },
                new ComboBoxMember { Code = "UK", Name = catalog.GetString("Imperial UK") },
            }.ToList();
            comboOtherUnits.DisplayMember = "Name";
            comboOtherUnits.ValueMember = "Code";
            comboOtherUnits.SelectedValue = Settings.Units;

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
            checkOverspeedMonitor.Checked = Settings.SpeedControl;
            checkRetainers.Checked = Settings.RetainersOnAllCars;
            checkGraduatedRelease.Checked = Settings.GraduatedRelease;
            numericBrakePipeChargingRate.Value = Settings.BrakePipeChargingRate;
            comboPressureUnit.Text = Settings.PressureUnit;
            comboOtherUnits.Text = settings.Units;
            checkEnableTCSScripts.Checked = !Settings.DisableTCSScripts;    // Inverted as "Enable scripts" is better UI than "Disable scripts"
            checkAutoSaveActive.Checked = Settings.AutoSaveActive;
            ButtonAutoSave15.Checked = checkAutoSaveActive.Checked & Settings.AutoSaveInterval == 15;
            ButtonAutoSave30.Checked = checkAutoSaveActive.Checked & Settings.AutoSaveInterval == 30;
            ButtonAutoSave60.Checked = checkAutoSaveActive.Checked & Settings.AutoSaveInterval == 60;

            // Audio tab
            numericSoundVolumePercent.Value = Settings.SoundVolumePercent;
            numericSoundDetailLevel.Value = Settings.SoundDetailLevel;
            numericExternalSoundPassThruPercent.Value = Settings.ExternalSoundPassThruPercent;

            // Video tab
            checkDynamicShadows.Checked = Settings.DynamicShadows;
            checkShadowAllShapes.Checked = Settings.ShadowAllShapes;
            checkModelInstancing.Checked = Settings.ModelInstancing;
            checkWire.Checked = Settings.Wire;
            checkVerticalSync.Checked = Settings.VerticalSync;
            numericViewingDistance.Value = Settings.ViewingDistance;
            checkDistantMountains.Checked = Settings.DistantMountains;
            labelDistantMountainsViewingDistance.Enabled = checkDistantMountains.Checked;
            numericDistantMountainsViewingDistance.Enabled = checkDistantMountains.Checked;
            numericDistantMountainsViewingDistance.Value = Settings.DistantMountainsViewingDistance / 1000;
            checkLODViewingExtension.Checked = Settings.LODViewingExtension;
            numericViewingFOV.Value = Settings.ViewingFOV;
            numericWorldObjectDensity.Value = Settings.WorldObjectDensity;
            trackDayAmbientLight.Value = Settings.DayAmbientLight;
            trackDayAmbientLight_ValueChanged(null, null);
            trackAntiAliasing.Value = Settings.AntiAliasing;
            trackAntiAliasing_ValueChanged(null, null);
            checkDoubleWire.Checked = Settings.DoubleWire;

            // Simulation tab

            checkSimpleControlsPhysics.Checked = Settings.SimpleControlPhysics;
            checkUseAdvancedAdhesion.Checked = Settings.UseAdvancedAdhesion;
            checkBreakCouplers.Checked = Settings.BreakCouplers;
            checkCurveSpeedDependent.Checked = Settings.CurveSpeedDependent;
            checkBoilerPreheated.Checked = Settings.HotStart;
            checkForcedRedAtStationStops.Checked = !Settings.NoForcedRedAtStationStops;
            checkDoorsAITrains.Checked = Settings.OpenDoorsInAITrains;
            checkDieselEnginesStarted.Checked = !Settings.NoDieselEngineStart; // Inverted as "EngineStart" is better UI than "NoEngineStart"
            checkElectricPowerConnected.Checked = Settings.ElectricHotStart;

            // Keyboard tab
            InitializeKeyboardSettings();

            // Raildriver Tab
            InitializeRailDriverSettings();

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
            checkDataLogSteamPerformance.Checked = Settings.DataLogExclusiveSteamPerformance;
            checkDataLogSteamPowerCurve.Checked = Settings.DataLogExclusiveSteamPowerCurve;
            dataLoggerInterval.Value = Settings.DataLoggerInterval;
            checkVerboseConfigurationMessages.Checked = Settings.VerboseConfigurationMessages;

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

            // System tab
            DrawSystemTab(updateManager);

            checkWindowed.Checked = !Settings.FullScreen;
            comboWindowSize.Text = Settings.WindowSize;
            checkWindowGlass.Checked = Settings.WindowGlass;

            // keep values in line with enum Orts.Simulation.ConfirmLevel
            // see also function Message(CabControl control, ConfirmLevel level, string message)
            // in Source\Orts.Simulation\Simulation\Confirmer.cs
            comboControlConfirmations.DataSource = new[] {
                new ComboBoxMember { Code = "None", Name = catalog.GetString("None") },
                new ComboBoxMember { Code = "Information", Name = catalog.GetString("Information") },
                new ComboBoxMember { Code = "Warning", Name = catalog.GetString("Warning") },
                new ComboBoxMember { Code = "Error", Name = catalog.GetString("Error") },
            }.ToList();
            comboControlConfirmations.DisplayMember = "Name";
            comboControlConfirmations.ValueMember = "Code";
            comboControlConfirmations.SelectedIndex = Settings.SuppressConfirmations;

            numericWebServerPort.Value = Settings.WebServerPort;
            checkPerformanceTuner.Checked = Settings.PerformanceTuner;
            labelPerformanceTunerTarget.Enabled = checkPerformanceTuner.Checked;
            numericPerformanceTunerTarget.Value = Settings.PerformanceTunerTarget;
            numericPerformanceTunerTarget.Enabled = checkPerformanceTuner.Checked;

            // Experimental tab
            checkUseSuperElevation.Checked = Settings.LegacySuperElevation;
            numericSuperElevationGauge.Value = Settings.SuperElevationGauge;
            trackLODBias.Value = Settings.LODBias;
            trackLODBias_ValueChanged(null, null);
            checkSignalLightGlow.Checked = Settings.SignalLightGlow;
            checkUseLocationPassingPaths.Checked = Settings.UseLocationPassingPaths;
            checkUseMSTSEnv.Checked = Settings.UseMSTSEnv;
            trackAdhesionFactor.Value = Settings.AdhesionFactor;
            trackAdhesionFactorChange.Value = Settings.AdhesionFactorChange;
            trackAdhesionFactor_ValueChanged(null, null);
            checkShapeWarnings.Checked = !Settings.SuppressShapeWarnings;   // Inverted as "Show warnings" is better UI than "Suppress warnings"
            checkEnableHotReloading.Checked = Settings.EnableHotReloading;
            checkCorrectQuestionableBrakingParams.Checked = Settings.CorrectQuestionableBrakingParams;
            numericActRandomizationLevel.Value = Settings.ActRandomizationLevel;
            numericActWeatherRandomizationLevel.Value = Settings.ActWeatherRandomizationLevel;
        }

        private void DrawSystemTab(UpdateManager updateManager)
        {
            comboLanguage.Text = Settings.Language;

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
            var spacing = labelUpdateMode.Margin.Size;
            var indent = 180;
            var top = labelUpdateMode.Bottom + spacing.Height;
            // Positioning gives maximum spave for lengthy Russian text.
            foreach (var channel in UpdateManager.GetChannels())
            {
                var radio = new RadioButton()
                {
                    Text = updateChannelNames[channel.ToLowerInvariant()],
                    Margin = labelUpdateMode.Margin,
                    Left = spacing.Width + 32, // to leave room for HelpIcon
                    Top = top,
                    Checked = updateManager.ChannelName.Equals(channel, StringComparison.InvariantCultureIgnoreCase),
                    AutoSize = true,
                    Tag = channel,
                };
                tabPageSystem.Controls.Add(radio);
                var label = new Label()
                {
                    Text = updateChannelDescriptions[channel.ToLowerInvariant()],
                    Margin = labelUpdateMode.Margin,
                    Left = spacing.Width + 30, // to leave room for HelpIcon
                    Top = top + spacing.Height + 15, // Offset to place below radio button
                    Width = tabPageSystem.ClientSize.Width - indent - spacing.Width * 2,
                    AutoSize = true,
                };
                tabPageSystem.Controls.Add(label);
                top += (label.Height + spacing.Height) * 2 - 5; // -3 to close them up a bit
            }
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
            var tempKIC = new KeyInputControl(Settings.Input.Commands[(int)UserCommand.GameQuit], InputSettings.DefaultCommands[(int)UserCommand.GameQuit]);
            var rowTop = Math.Max(tempLabel.Margin.Top, tempKIC.Margin.Top);
            var rowHeight = tempKIC.Height;
            var rowSpacing = rowHeight + tempKIC.Margin.Vertical;

            var lastCategory = "";
            var i = 0;
            foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
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
            Settings.SpeedControl = checkOverspeedMonitor.Checked;
            Settings.RetainersOnAllCars = checkRetainers.Checked;
            Settings.GraduatedRelease = checkGraduatedRelease.Checked;
            Settings.BrakePipeChargingRate = (int)numericBrakePipeChargingRate.Value;
            Settings.PressureUnit = comboPressureUnit.SelectedValue.ToString();
            Settings.Units = comboOtherUnits.SelectedValue.ToString();
            Settings.DisableTCSScripts = !checkEnableTCSScripts.Checked; // Inverted as "Enable scripts" is better UI than "Disable scripts"
            Settings.AutoSaveActive = checkAutoSaveActive.Checked;
            Settings.AutoSaveInterval = ButtonAutoSave15.Checked ? 15 : ButtonAutoSave30.Checked ? 30 : 60;

            // Audio tab
            Settings.SoundVolumePercent = (int)numericSoundVolumePercent.Value;
            Settings.SoundDetailLevel = (int)numericSoundDetailLevel.Value;
            Settings.ExternalSoundPassThruPercent = (int)numericExternalSoundPassThruPercent.Value;

            // Video tab
            Settings.DynamicShadows = checkDynamicShadows.Checked;
            Settings.ShadowAllShapes = checkShadowAllShapes.Checked;
            Settings.ModelInstancing = checkModelInstancing.Checked;
            Settings.Wire = checkWire.Checked;
            Settings.VerticalSync = checkVerticalSync.Checked;
            Settings.ViewingDistance = (int)numericViewingDistance.Value;
            Settings.DistantMountains = checkDistantMountains.Checked;
            Settings.DistantMountainsViewingDistance = (int)numericDistantMountainsViewingDistance.Value * 1000;
            Settings.LODViewingExtension = checkLODViewingExtension.Checked;
            Settings.ViewingFOV = (int)numericViewingFOV.Value;
            Settings.WorldObjectDensity = (int)numericWorldObjectDensity.Value;

            Settings.DayAmbientLight = (int)trackDayAmbientLight.Value;
            Settings.DoubleWire = checkDoubleWire.Checked;
            Settings.AntiAliasing = trackAntiAliasing.Value;

            // Simulation tab
            Settings.SimpleControlPhysics = checkSimpleControlsPhysics.Checked;
            Settings.UseAdvancedAdhesion = checkUseAdvancedAdhesion.Checked;
            Settings.BreakCouplers = checkBreakCouplers.Checked;
            Settings.CurveSpeedDependent = checkCurveSpeedDependent.Checked;
            Settings.HotStart = checkBoilerPreheated.Checked;
            Settings.NoForcedRedAtStationStops = !checkForcedRedAtStationStops.Checked;
            Settings.OpenDoorsInAITrains = checkDoorsAITrains.Checked;
            Settings.NoDieselEngineStart = !checkDieselEnginesStarted.Checked; // Inverted as "EngineStart" is better UI than "NoEngineStart"
            Settings.ElectricHotStart = checkElectricPowerConnected.Checked;

            // Keyboard tab
            // These are edited live.

            // Raildriver Tab
            SaveRailDriverSettings();

            // DataLogger tab
            Settings.DataLoggerSeparator = comboDataLoggerSeparator.SelectedValue.ToString();
            Settings.DataLogSpeedUnits = comboDataLogSpeedUnits.SelectedValue.ToString();
            Settings.DataLogger = checkDataLogger.Checked;
            Settings.DataLogPerformance = checkDataLogPerformance.Checked;
            Settings.DataLogPhysics = checkDataLogPhysics.Checked;
            Settings.DataLogExclusiveSteamPerformance = checkDataLogSteamPerformance.Checked;
            Settings.DataLogExclusiveSteamPowerCurve = checkDataLogSteamPowerCurve.Checked;
            Settings.DataLoggerInterval = (int)dataLoggerInterval.Value;
            Settings.VerboseConfigurationMessages = checkVerboseConfigurationMessages.Checked;

            // Evaluation tab
            Settings.DataLogTrainSpeed = checkDataLogTrainSpeed.Checked;
            Settings.DataLogTSInterval = (int)numericDataLogTSInterval.Value;
            for (var i = 0; i < checkListDataLogTSContents.Items.Count; i++)
                Settings.DataLogTSContents[i] = checkListDataLogTSContents.GetItemChecked(i) ? 1 : 0;
            Settings.DataLogStationStops = checkDataLogStationStops.Checked;

            // System tab
            Settings.Language = comboLanguage.SelectedValue.ToString();
            foreach (Control control in tabPageSystem.Controls)
                if ((control is RadioButton) && (control as RadioButton).Checked)
                    UpdateManager.SetChannel((string)control.Tag);
            Settings.FullScreen = !checkWindowed.Checked;
            Settings.WindowSize = GetValidWindowSize(comboWindowSize.Text);
            Settings.WindowGlass = checkWindowGlass.Checked;
            Settings.SuppressConfirmations = comboControlConfirmations.SelectedIndex;
            Settings.WebServerPort = (int)numericWebServerPort.Value;
            Settings.PerformanceTuner = checkPerformanceTuner.Checked;
            Settings.PerformanceTunerTarget = (int)numericPerformanceTunerTarget.Value;

            // Experimental tab
            Settings.LegacySuperElevation = checkUseSuperElevation.Checked;
            Settings.SuperElevationGauge = (int)numericSuperElevationGauge.Value;
            Settings.LODBias = trackLODBias.Value;
            Settings.SignalLightGlow = checkSignalLightGlow.Checked;
            Settings.UseLocationPassingPaths = checkUseLocationPassingPaths.Checked;
            Settings.UseMSTSEnv = checkUseMSTSEnv.Checked;
            Settings.AdhesionFactor = (int)trackAdhesionFactor.Value;
            Settings.AdhesionFactorChange = (int)trackAdhesionFactorChange.Value;
            Settings.SuppressShapeWarnings = !checkShapeWarnings.Checked;
            Settings.EnableHotReloading = checkEnableHotReloading.Checked;
            Settings.CorrectQuestionableBrakingParams = checkCorrectQuestionableBrakingParams.Checked;
            Settings.ActRandomizationLevel = (int)numericActRandomizationLevel.Value;
            Settings.ActWeatherRandomizationLevel = (int)numericActWeatherRandomizationLevel.Value;

            Settings.Save();
        }

        /// <summary>
        /// Returns user's [width]x[height] if expression is valid and values are sane, else returns previous value of setting.
        /// </summary>
        private string GetValidWindowSize(string text)
        {
            var match = Regex.Match(text, @"^\s*([1-9]\d{2,3})\s*[Xx]\s*([1-9]\d{2,3})\s*$");//capturing 2 groups of 3-4digits, separated by X or x, ignoring whitespace in beginning/end and in between
            if (match.Success)
            {
                return $"{match.Groups[1]}x{match.Groups[2]}";
            }
            return Settings.WindowSize; // i.e. no change or message. Just ignore non-numeric entries
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

        private void trackAntiAliasing_ValueChanged(object sender, EventArgs e)
        {
            string method;
            switch ((UserSettings.AntiAliasingMethod)trackAntiAliasing.Value)
            {
                case UserSettings.AntiAliasingMethod.None:
                    method = "Disabled";
                    break;
                case UserSettings.AntiAliasingMethod.MSAA2x:
                    method = "2x MSAA";
                    break;
                case UserSettings.AntiAliasingMethod.MSAA4x:
                    method = "4x MSAA";
                    break;
                case UserSettings.AntiAliasingMethod.MSAA8x:
                    method = "8x MSAA";
                    break;
                case UserSettings.AntiAliasingMethod.MSAA16x:
                    method = "16x MSAA";
                    break;
                case UserSettings.AntiAliasingMethod.MSAA32x:
                    method = "32x MSAA";
                    break;
                default:
                    method = "";
                    break;
            }
            labelAntiAliasingValue.Text = method;
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

        private void checkAutoSave_checkchanged(object sender, EventArgs e)
        {
            if (checkAutoSaveActive.Checked)
            {
                ButtonAutoSave15.Enabled = true;
                ButtonAutoSave15.Checked = Settings.AutoSaveInterval == 15;
                ButtonAutoSave30.Enabled = true;
                ButtonAutoSave30.Checked = Settings.AutoSaveInterval == 30;
                ButtonAutoSave60.Enabled = true;
                ButtonAutoSave60.Checked = Settings.AutoSaveInterval == 60;
            }
            else
            {
                ButtonAutoSave15.Checked = false;
                ButtonAutoSave15.Enabled = false;
                ButtonAutoSave30.Checked = false;
                ButtonAutoSave30.Enabled = false;
                ButtonAutoSave60.Checked = false;
                ButtonAutoSave60.Enabled = false;
            }
        }

        private void buttonAutoSaveInterval_checkchanged(object sender, EventArgs e)
        {
            if (ButtonAutoSave15.Checked)
            {
                Settings.AutoSaveInterval = 15;
                ButtonAutoSave30.Checked = false;
                ButtonAutoSave60.Checked = false;
            }
            else if (ButtonAutoSave30.Checked)
            {
                Settings.AutoSaveInterval = 30;
                ButtonAutoSave15.Checked = false;
                ButtonAutoSave60.Checked = false;
            }
            else if (ButtonAutoSave60.Checked)
            {
                Settings.AutoSaveInterval = 60;
                ButtonAutoSave15.Checked = false;
                ButtonAutoSave30.Checked = false;
            }
        }

        #region Help for Options
        // The icons all share the same code which assumes they are named according to a simple scheme as follows:
        //   1. To add a new Help Icon, copy an existing one and paste it onto the tab.
        //   2. Give it the same name as the associated control but change the prefix to "pb" for Picture Box.
        //   3. Add a Click event named HelpIcon_Click to each HelpIcon
        //      Do not add code for this event (or press Return/double click in the Properties field which creates a code stub for you). 
        //   4. Add MouseEnter/Leave events to each HelpIcon, label and checkbox:
        //     - MouseEnter event named HelpIcon_MouseEnter
        //     - MouseLeave event named HelpIcon_MouseLeave
        //     Numeric controls do not have MouseEnter/Leave events so, for them, use:
        //     - Enter event named HelpIcon_MouseEnter
        //     - Leave event named HelpIcon_MouseLeave
        //      Do not add code for these events (or press Return/double click in the Properties field which creates a code stub for you). 
        //   5. Add an entry to InitializeHelpIcons() which links the icon to the control and, if there is one, the label.
        //      This link will highlight the icon when the user hovers (mouses over) the control or the label.
        //   6. Add an entry to HelpIcon_Click() which opens the user's browser with the correct help page.
        //      The URL can be found from visiting the page and hovering over the title of the section.

        /// <summary>
        /// Allows multiple controls to change a single help icon with their hover events.
        /// </summary>
        private class HelpIconHover
        {
            private readonly PictureBox Icon;
            private int HoverCount = 0;

            public HelpIconHover(PictureBox pb)
            {
                Icon = pb;
            }

            public void Enter()
            {
                HoverCount++;
                SetImage();
            }

            public void Leave()
            {
                HoverCount--;
                SetImage();
            }

            private void SetImage()
            {
                Icon.Image = HoverCount > 0 ? Properties.Resources.info_18_hover : Properties.Resources.info_18;
            }
        }

        private readonly IDictionary<Control, HelpIconHover> ToHelpIcon = new Dictionary<Control, HelpIconHover>();

        private void InitializeHelpIcons()
        {
            // static mapping of picture boxes to controls
            var helpIconControls = new (PictureBox, Control[])[]
            {
                // General
                (pbAlerter, new[] { checkAlerter }),
                (pbRetainers, new[] { checkRetainers }),
                (pbGraduatedRelease, new[] { checkGraduatedRelease }),
                (pbBrakePipeChargingRate, new[] { lBrakePipeChargingRate }),
                (pbPressureUnit, new Control[] { labelPressureUnit, comboPressureUnit }),
                (pbOtherUnits, new Control[] { labelOtherUnits, comboOtherUnits }),
                (pbEnableTcsScripts, new[] { checkEnableTCSScripts }),
                (pbAutoSave, new[] { checkAutoSaveActive }),
                (pbOverspeedMonitor, new[] { checkOverspeedMonitor }),
                (pbTelemetry, new[] { buttonTelemetry }),

                // Audio tab
                (pbSoundVolumePercent, new Control[] { labelSoundVolume, numericSoundVolumePercent }),
                (pbSoundDetailLevel, new Control[]  { labelSoundDetailLevel, numericSoundDetailLevel }),
                (pbExternalSoundPassThruPercent, new Control[]  { labelExternalSound, numericExternalSoundPassThruPercent }),

                // Video tab
                (pbViewingDistance, new Control[] { labelViewingDistance, numericViewingDistance }),
                (pbDistantMountains, new Control[] { checkDistantMountains, numericDistantMountainsViewingDistance, labelDistantMountainsViewingDistance }),
                (pbLODViewingExtension, new[] { checkLODViewingExtension }),
                (pbDynamicShadows, new[] { checkDynamicShadows }),
                (pbShadowAllShapes, new[] { checkShadowAllShapes }),
                (pbWire, new[] { checkWire }),
                (pbDoubleWire, new[] { checkDoubleWire }),
                (pbSignalLightGlow, new[] { checkSignalLightGlow }),
                (pbDayAmbientLight, new Control[] { labelAmbientDaylightBrightness, trackDayAmbientLight }),
                (pbModelInstancing, new[] { checkModelInstancing }),
                (pbVerticalSync, new[] { checkVerticalSync }),
                (pbAntiAliasing, new Control[] { labelAntiAliasingValue, trackAntiAliasing }),
                (pbWorldObjectDensity, new Control[] { labelWorldObjectDensity, numericWorldObjectDensity }),
                (pbLODBias, new Control[] { labelLODBias, trackLODBias }),
                (pbViewingFOV, new Control[] { labelViewingVerticalFOV, numericViewingFOV }),

                // System
                (pbLanguage, new Control[] { labelLanguage, comboLanguage }),
                (pbUpdateMode, new Control[] { labelUpdateMode }),
                (pbWindowed, new Control[] { checkWindowed, labelWindowSize, comboWindowSize }),
                (pbWindowGlass, new[] { checkWindowGlass }),
                (pbControlConfirmations, new Control[] { labelControlConfirmations, comboControlConfirmations }),
                (pbWebServerPort, new Control[] { labelWebServerPort }),
                (pbPerformanceTuner, new Control[] { checkPerformanceTuner, labelPerformanceTunerTarget }),

                // Simulation tab
                (pbAdvancedAdhesionModel, new[] { checkUseAdvancedAdhesion }),
                (pbBreakCouplers, new[] { checkBreakCouplers }),
                (pbCurveDependentSpeedLimit, new[] { checkCurveSpeedDependent }),   
                (pbAtGameStartSteamPreHeatBoiler, new[] { checkBoilerPreheated }),
                (pbAtGameStartDieselRunEngines, new[] { checkDieselEnginesStarted }),
                (pbAtGameStartElectricPowerConnected, new[] { checkElectricPowerConnected }),
                (pbSimpleControlAndPhysics, new[] { checkSimpleControlsPhysics }),
                (pbForcedRedAtStationStops, new[] { checkForcedRedAtStationStops }),
                (pbOpenCloseDoorsOnAiTrains, new[] { checkDoorsAITrains }),
                (pbLocationLinkedPassingPathProcessing, new[] { checkUseLocationPassingPaths }),

                // Experimental tab
                (pbSuperElevation, new Control [] { ElevationText, checkUseSuperElevation, label8}),
                (pbShowShapeWarnings, new[] { checkShapeWarnings }),
                (pbEnableHotReloading, new [] { checkEnableHotReloading }),
                (pbCorrectQuestionableBrakingParameters, new[] { checkCorrectQuestionableBrakingParams }),
                (pbActivityRandomization, new Control [] { label13, label12 }),
                (pbActivityWeatherRandomization, new Control [] { label26, label27 }),
                (pbMstsEnvironments, new[] { checkUseMSTSEnv }),
                (pbAdhesionFactorCorrection, new Control [] { label9,  trackAdhesionFactor}),
                (pbAdhesionFactorRandomChange, new Control [] { label16, trackAdhesionFactorChange}),
            };
            foreach ((PictureBox pb, Control[] controls) in helpIconControls)
            {
                var hover = new HelpIconHover(pb);
                ToHelpIcon[pb] = hover;
                foreach (Control control in controls)
                    ToHelpIcon[control] = hover;
            }
        }

        /// <summary>
        /// Loads a relevant page from the manual maintained by James Ross's automatic build.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpIcon_Click(object sender, EventArgs _)
        {
            var urls = new Dictionary<object, string>
            {
                // General tab
                {
                    pbAlerter,
                    BaseDocumentationUrl + "/options.html#alerter-in-cab"
                },
                {
                    pbRetainers,
                    BaseDocumentationUrl + "/options.html#retainer-valve-on-all-cars"
                },
                {
                    pbGraduatedRelease,
                    BaseDocumentationUrl + "/options.html#graduated-release-air-brakes"
                },
                {
                    pbBrakePipeChargingRate,
                    BaseDocumentationUrl + "/options.html#brake-pipe-charging-rate"
                },
                {
                    pbPressureUnit,
                    BaseDocumentationUrl + "/options.html#pressure-unit"
                },
                {
                    pbOtherUnits,
                    BaseDocumentationUrl + "/options.html#other-units"
                },
                {
                    pbEnableTcsScripts,
                    BaseDocumentationUrl + "/options.html#disable-tcs-scripts"
                },
                {
                    pbAutoSave,
                    BaseDocumentationUrl + "/options.html#auto-save"
                },
                {
                    pbOverspeedMonitor,
                    BaseDocumentationUrl + "/options.html#overspeed-monitor"
                },
                {
                    pbTelemetry,
                    BaseDocumentationUrl + "/options.html#telemetry"
                },

                // Audio tab
                {
                    pbSoundVolumePercent,
                    BaseDocumentationUrl + "/options.html#audio-options"
                },
                {
                    pbSoundDetailLevel,
                    BaseDocumentationUrl + "/options.html#audio-options"
                },
                {
                    pbExternalSoundPassThruPercent,
                    BaseDocumentationUrl + "/options.html#audio-options"
                },

                // Video tab
                {
                    pbViewingDistance,
                    BaseDocumentationUrl + "/options.html#viewing-distance"
                },
                {
                    pbDistantMountains,
                    BaseDocumentationUrl + "/options.html#distant-mountains"
                },
                {
                    pbLODViewingExtension,
                    BaseDocumentationUrl + "/options.html#extend-object-maximum-viewing-distance-to-horizon"
                },
                {
                    pbDynamicShadows,
                    BaseDocumentationUrl + "/options.html#dynamic-shadows"
                },
                {
                    pbShadowAllShapes,
                    BaseDocumentationUrl + "/options.html#shadow-for-all-shapes"
                },
                {
                    pbWire,
                    BaseDocumentationUrl + "/options.html#overhead-wire"
                },
                {
                    pbDoubleWire,
                    BaseDocumentationUrl + "/options.html#double-overhead-wires"
                },
                {
                    pbSignalLightGlow,
                    BaseDocumentationUrl + "/options.html#signal-light-glow"
                },
                {
                    pbDayAmbientLight,
                    BaseDocumentationUrl + "/options.html#ambient-daylight-brightness"
                },
                {
                    pbModelInstancing,
                    BaseDocumentationUrl + "/options.html#model-instancing"
                },
                {
                    pbVerticalSync,
                    BaseDocumentationUrl + "/options.html#vertical-sync"
                },
                {
                    pbAntiAliasing,
                    BaseDocumentationUrl + "/options.html#anti-aliasing"
                },
                {
                    pbWorldObjectDensity,
                    BaseDocumentationUrl + "/options.html#world-object-density"
                },
                {
                    pbLODBias,
                    BaseDocumentationUrl + "/options.html#level-of-detail-bias"
                },
                {
                    pbViewingFOV,
                    BaseDocumentationUrl + "/options.html#viewing-vertical-fov"
                },

                // System tab
                {
                    pbLanguage,
                    BaseDocumentationUrl + "/options.html#language"
                },
                {
                    pbUpdateMode,
                    BaseDocumentationUrl + "/options.html#updater-options"
                },
                {
                    pbWindowed,
                    BaseDocumentationUrl + "/options.html#windowed"
                },
                {
                    pbWindowGlass,
                    BaseDocumentationUrl + "/options.html#window-glass"
                },
                {
                    pbControlConfirmations,
                    BaseDocumentationUrl + "/options.html#control-confirmations"
                },
                {
                    pbWebServerPort,
                    BaseDocumentationUrl + "/options.html#web-server-port"
                },
                {
                    pbPerformanceTuner,
                    BaseDocumentationUrl + "/options.html#performance-tuner"
                },

                // Simulation tab
                {
                    pbAdvancedAdhesionModel,
                    BaseDocumentationUrl + "/options.html#advanced-adhesion-model"
                },
                {
                    pbBreakCouplers,
                    BaseDocumentationUrl + "/options.html#break-couplers"
                },
                {
                    pbCurveDependentSpeedLimit,
                    BaseDocumentationUrl + "/options.html#curve-dependent-speed-limit"
                },
                {
                    pbAtGameStartSteamPreHeatBoiler,
                    BaseDocumentationUrl + "/options.html#at-game-start-steam-pre-heat-boiler"
                },
                {
                    pbAtGameStartDieselRunEngines,
                    BaseDocumentationUrl + "/options.html#at-game-start-diesel-run-engines"
                },
                {
                    pbAtGameStartElectricPowerConnected,
                    BaseDocumentationUrl + "/options.html#at-game-start-electric-power-connected"
                },
                {
                    pbSimpleControlAndPhysics,
                    BaseDocumentationUrl + "/options.html#simple-control-and-physics"
                },
                {
                    pbForcedRedAtStationStops,
                    BaseDocumentationUrl + "/options.html#forced-red-at-station-stops"
                },
{
                    pbOpenCloseDoorsOnAiTrains,
                    BaseDocumentationUrl + "/options.html#open-close-doors-on-ai-trains"
                },
                {
                    pbLocationLinkedPassingPathProcessing,
                    BaseDocumentationUrl + "/options.html#location-linked-passing-path-processing"
                },

                // Keyboard tab
                {
                    pbKeyboardOptions,
                    BaseDocumentationUrl + "/options.html#keyboard-options"
                },

                // Raildriver tab
                {
                    pbRailDriverOptions,
                    BaseDocumentationUrl + "/options.html#raildriver-options"
                },

                // Data Logger Options
                {
                    pbDataLoggerOptions,
                    BaseDocumentationUrl + "/options.html#data-logger-options"
                },                

                // Experimental tab
                {
                    pbSuperElevation,
                    BaseDocumentationUrl + "/options.html#superelevation"
                },
                {
                    pbShowShapeWarnings,
                    BaseDocumentationUrl + "/options.html#show-shape-warnings"
                },
                {
                    pbEnableHotReloading,
                    BaseDocumentationUrl + "/options.html#enable-hot-reloading-of-simulator-files"
                },
                {
                    pbCorrectQuestionableBrakingParameters,
                    BaseDocumentationUrl + "/options.html#correct-questionable-braking-parameters"
                },
                {
                    pbActivityRandomization,
                    BaseDocumentationUrl + "/options.html#activity-randomization"
                },
                {
                    pbActivityWeatherRandomization,
                    BaseDocumentationUrl + "/options.html#activity-weather-randomization"
                },
                {
                    pbMstsEnvironments,
                    BaseDocumentationUrl + "/options.html#msts-environments"
                },
                {
                    pbAdhesionFactorCorrection,
                    BaseDocumentationUrl + "/options.html#adhesion-factor-correction"
                },
                {
                    pbAdhesionFactorRandomChange,
                    BaseDocumentationUrl + "/options.html#adhesion-factor-random-change"
                }
            };
            if (urls.TryGetValue(sender, out var url))
            {
                // This method is also compatible with .NET Core 3
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Highlight the Help Icon if the user mouses over the icon or its control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="_"></param>
        private void HelpIcon_MouseEnter(object sender, EventArgs _)
        {
            if (sender is Control control && ToHelpIcon.TryGetValue(control, out var hover))
                hover.Enter();
        }

        private void HelpIcon_MouseLeave(object sender, EventArgs _)
        {
            if (sender is Control control && ToHelpIcon.TryGetValue(control, out var hover))
                hover.Leave();
        }
        #endregion

        private void buttonTelemetry_Click(object sender, EventArgs e)
        {
            using (var telemetryForm = new TelemetryForm(TelemetryManager))
            {
                telemetryForm.ShowDialog(this);
            }
        }
    }
}
