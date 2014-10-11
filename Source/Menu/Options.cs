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
using ORTS.Settings;
using ORTS.Updater;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XNA = Microsoft.Xna.Framework.Input;

namespace ORTS
{
    public partial class OptionsForm : Form
    {
        readonly UserSettings Settings;
        readonly UpdateManager UpdateManager;

        private GettextResourceManager catalog = new GettextResourceManager("Menu");
        private Boolean Initialized = false;

        public class Language
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public OptionsForm(UserSettings settings, UpdateManager updateManager)
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
            comboBoxLanguage.DataSource = 
                new[] { new Language { Code = "", Name = "System" } }
                .Union(languageCodes
                    .Select(lc => new Language { Code = lc, Name = CultureInfo.GetCultureInfo(lc).NativeName })
                    .OrderBy(l => l.Name)
                )
                .ToList();
            comboBoxLanguage.DisplayMember = "Name";
            comboBoxLanguage.ValueMember = "Code";
            comboBoxLanguage.SelectedValue = Settings.Language;

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;
            AdhesionLevelValue.Font = new Font(Font, FontStyle.Bold);

            // General tab
            checkAlerter.Checked = Settings.Alerter;
            checkAlerterDisableExternal.Checked = Settings.AlerterDisableExternal;
            checkGraduatedRelease.Checked = Settings.GraduatedRelease;
            numericBrakePipeChargingRate.Value = Settings.BrakePipeChargingRate;
            comboBoxPressureUnit.Text = Settings.PressureUnit;
            checkSuppressConfirmations.Checked = Settings.SuppressConfirmations;
            checkViewDispatcher.Checked = Settings.ViewDispatcher;
            comboBoxLanguage.Text = Settings.Language;

            // Audio tab
            numericSoundDetailLevel.Value = Settings.SoundDetailLevel;
            checkMSTSBINSound.Checked = Settings.MSTSBINSound;
            numericSoundVolumePercent.Value = Settings.SoundVolumePercent;

            // Video tab
            numericWorldObjectDensity.Value = Settings.WorldObjectDensity;
            comboWindowSize.Text = Settings.WindowSize;
            checkBoxVerticalSync.Checked = Settings.VerticalSync;
            checkWire.Checked = Settings.Wire;
            checkDynamicShadows.Checked = Settings.DynamicShadows;
            checkWindowGlass.Checked = Settings.WindowGlass;
            numericViewingFOV.Value = Settings.ViewingFOV;
            numericCab2DStretch.Value = Settings.Cab2DStretch;
            numericViewingDistance.Value = settings.ViewingDistance;

            // Simulation tab
            checkUseAdvancedAdhesion.Checked = Settings.UseAdvancedAdhesion;
            numericAdhesionMovingAverageFilterSize.Value = settings.AdhesionMovingAverageFilterSize;
            AdhesionFactorTrackBar1.Value = settings.AdhesionFactor;
            AdhesionFactorChangeTrackBar1.Value = settings.AdhesionFactorChange;
            AdhesionPropToWeatherCheckBox.Checked = settings.AdhesionProportionalToWeather;
            SetAdhesionLevelValue();

            checkBreakCouplers.Checked = Settings.BreakCouplers;
            checkOverrideNonElectrifiedRoutes.Checked = Settings.OverrideNonElectrifiedRoutes;
            checkCurveResistanceSpeedDependent.Checked = settings.CurveResistanceSpeedDependent;
            checkCurveSpeedDependent.Checked = settings.CurveSpeedDependent;
            checkHotStart.Checked = settings.HotStart;

            // Keyboard tab
            InitializeKeyboardSettings();

            // DataLogger tab
            Dictionary<string, string> dictionaryDataLoggerSeparator = new Dictionary<string, string>();
            dictionaryDataLoggerSeparator.Add("comma",catalog.GetString("comma"));
            dictionaryDataLoggerSeparator.Add("semicolon", catalog.GetString("semicolon"));
            dictionaryDataLoggerSeparator.Add("tab", catalog.GetString("tab"));
            dictionaryDataLoggerSeparator.Add("space", catalog.GetString("space"));
            comboDataLoggerSeparator.DataSource = new BindingSource(dictionaryDataLoggerSeparator, null);
            comboDataLoggerSeparator.DisplayMember = "Value";
            comboDataLoggerSeparator.ValueMember = "Key";
            comboDataLoggerSeparator.Text = catalog.GetString(settings.DataLoggerSeparator);

            comboDataLogSpeedUnits.Text = settings.DataLogSpeedUnits;
            checkDataLogger.Checked = Settings.DataLogger;
            checkDataLogPerformance.Checked = settings.DataLogPerformance;
            checkDataLogPhysics.Checked = settings.DataLogPhysics;
            checkDataLogMisc.Checked = settings.DataLogMisc;

            // Evaluation tab
            this.checkedListBoxDataLogTSContents.Items.AddRange(new object[] {
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
            checkDataLogTrainSpeed.Checked = Settings.DataLogTrainSpeed;
            numericDataLogTSInterval.Value = Settings.DataLogTSInterval;
            for (var i = 0; i < checkedListBoxDataLogTSContents.Items.Count; i++)
                checkedListBoxDataLogTSContents.SetItemChecked(i, Settings.DataLogTSContents[i] == 1);
            checkDataLogStationStops.Checked = Settings.DataLogStationStops;

            // Updater tab
            var updateChannelNames = new Dictionary<string, string> {
                { "release", catalog.GetString("Release channel (Recommended for users)") },
                { "experimental", catalog.GetString("Experimental channel (For supporters)") },
                { "nightly", catalog.GetString("Nightly channel (For developers)") },
                { "", catalog.GetString("None") },
            };
            var updateChannelDescriptions = new Dictionary<string, string> {
                { "release", catalog.GetString("The release channel contains only official, hand-picked stable versions.") },
                { "experimental", catalog.GetString("The experimental channel contains automatically generated weekly versions.") },
                { "nightly", catalog.GetString("The nightly channel contains every single version created.") },
                { "", catalog.GetString("No automatic updates.") },
            };
            var spacing = labelUpdateChannel.Margin.All * 2;
            var indent = 20;
            var top = labelUpdateChannel.Bottom + spacing;
            foreach (var channel in UpdateManager.GetChannels())
            {
                var radio = new RadioButton() {
                    Text = updateChannelNames[channel.ToLowerInvariant()],
                    Margin = labelUpdateChannel.Margin,
                    Left = spacing,
                    Top = top,
                    Checked = updateManager.ChannelName.Equals(channel, StringComparison.InvariantCultureIgnoreCase),
                    AutoSize = true,
                    Tag = channel,
                };
                tabPageUpdater.Controls.Add(radio);
                top += radio.Height + spacing;
                var label = new Label() {
                    Text = updateChannelDescriptions[channel.ToLowerInvariant()],
                    Margin = labelUpdateChannel.Margin,
                    Left = spacing + indent,
                    Top = top,
                    Width = tabPageUpdater.ClientSize.Width - indent - spacing * 2,
                    AutoSize = true,
                };
                tabPageUpdater.Controls.Add(label);
                top += label.Height + spacing;
            }

            // Experimental tab
            numericUseSuperElevation.Value = Settings.UseSuperElevation;
            numericSuperElevationMinLen.Value = Settings.SuperElevationMinLen;
            numericSuperElevationGauge.Value = Settings.SuperElevationGauge;
            checkDistantMountains.Checked = settings.DistantMountains;
            numericDistantMountainsViewingDistance.Value = settings.DistantMountainsViewingDistance / 1000;
            checkLODAlwaysMaximum.Checked = settings.LODAlwaysMaximum;
            checkLODViewingExtention.Checked = settings.LODViewingExtention;
            checkBoxModelInstancing.Checked = settings.ModelInstancing;
            checkPerformanceTuner.Checked = settings.PerformanceTuner;
            numericPerformanceTunerTarget.Value = settings.PerformanceTunerTarget;
            checkDoubleWire.Checked = settings.DoubleWire;
            trackDayAmbientLight.Value = settings.DayAmbientLight;
            checkUseMSTSEnv.Checked = settings.UseMSTSEnv;
            checkUseLocationPassingPaths.Checked = settings.UseLocationPassingPaths;
            checkPreferDDSTexture.Checked = Settings.PreferDDSTexture;
            checkFastFullScreenAltTab.Checked = Settings.FastFullScreenAltTab;
            checkUseLargeAddressAware.Checked = Settings.UseLargeAddressAware;
            checkBoxCircularSpeedGauge.Checked = Settings.CircularSpeedGauge;
            checkBoxSignalLightGlow.Checked = Settings.SignalLightGlow;
            checkBoxEnhancedActCompatibility.Checked = Settings.EnhancedActCompatibility;
            checkBoxNoForcedRedAtStationStops.Checked = Settings.NoForcedRedAtStationStops;
            checkBoxConditionalLoadOfNightTextures.Checked = Settings.ConditionalLoadOfNightTextures;
            checkBoxExtendedAIShunting.Checked = Settings.ExtendedAIShunting;

            Initialized = true;
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

            // General tab
            Settings.Alerter = checkAlerter.Checked;
            Settings.AlerterDisableExternal = checkAlerterDisableExternal.Checked;
            Settings.GraduatedRelease = checkGraduatedRelease.Checked;
            Settings.BrakePipeChargingRate = (int)numericBrakePipeChargingRate.Value;
            Settings.PressureUnit = comboBoxPressureUnit.Text;
            Settings.SuppressConfirmations = checkSuppressConfirmations.Checked;
            Settings.ViewDispatcher = checkViewDispatcher.Checked;
            Settings.Language = comboBoxLanguage.SelectedValue.ToString();
            
            // Audio tab
            Settings.SoundDetailLevel = (int)numericSoundDetailLevel.Value;
            Settings.MSTSBINSound = checkMSTSBINSound.Checked;
			Settings.SoundVolumePercent = (int)numericSoundVolumePercent.Value;
            
            // Video tab
            Settings.WorldObjectDensity = (int)numericWorldObjectDensity.Value;
            Settings.WindowSize = comboWindowSize.Text;
            Settings.VerticalSync = checkBoxVerticalSync.Checked;
            Settings.Wire = checkWire.Checked;
            Settings.DynamicShadows = checkDynamicShadows.Checked;
            Settings.WindowGlass = checkWindowGlass.Checked;
            Settings.ViewingFOV = (int)numericViewingFOV.Value;
            Settings.Cab2DStretch = (int)numericCab2DStretch.Value;
            Settings.ViewingDistance = (int)numericViewingDistance.Value;
            
            // Simulation tab
            Settings.UseAdvancedAdhesion = checkUseAdvancedAdhesion.Checked;
            Settings.AdhesionMovingAverageFilterSize = (int)numericAdhesionMovingAverageFilterSize.Value;
            Settings.AdhesionFactor = (int)AdhesionFactorTrackBar1.Value;
            Settings.AdhesionFactorChange = (int)AdhesionFactorChangeTrackBar1.Value;
            Settings.AdhesionProportionalToWeather = AdhesionPropToWeatherCheckBox.Checked;
            Settings.BreakCouplers = checkBreakCouplers.Checked;
            Settings.OverrideNonElectrifiedRoutes = checkOverrideNonElectrifiedRoutes.Checked;
            Settings.CurveResistanceSpeedDependent = checkCurveResistanceSpeedDependent.Checked;
            Settings.CurveSpeedDependent = checkCurveSpeedDependent.Checked;
            Settings.HotStart = checkHotStart.Checked;
            
            // Keyboard tab
            // These are edited live.
            
            // DataLogger tab
            Settings.DataLoggerSeparator = ((KeyValuePair<string, string>)comboDataLoggerSeparator.SelectedItem).Key;
            Settings.DataLogSpeedUnits = comboDataLogSpeedUnits.Text;
            Settings.DataLogger = checkDataLogger.Checked;
            Settings.DataLogPerformance = checkDataLogPerformance.Checked;
            Settings.DataLogPhysics = checkDataLogPhysics.Checked;
            Settings.DataLogMisc = checkDataLogMisc.Checked;
            
            // Evaluation tab
            Settings.DataLogTrainSpeed = checkDataLogTrainSpeed.Checked;
            Settings.DataLogTSInterval = (int)numericDataLogTSInterval.Value;
            for (var i = 0; i < checkedListBoxDataLogTSContents.Items.Count; i++)
                Settings.DataLogTSContents[i] = checkedListBoxDataLogTSContents.GetItemChecked(i) ? 1 : 0;
            Settings.DataLogStationStops = checkDataLogStationStops.Checked;
            
            // Updater tab
            foreach (Control control in tabPageUpdater.Controls)
                if ((control is RadioButton) && (control as RadioButton).Checked)
                    UpdateManager.SetChannel((string)control.Tag);

            // Experimental tab
            Settings.UseSuperElevation = (int)numericUseSuperElevation.Value;
            Settings.SuperElevationMinLen = (int)numericSuperElevationMinLen.Value;
            Settings.SuperElevationGauge = (int)numericSuperElevationGauge.Value;
            Settings.DistantMountains = checkDistantMountains.Checked;
            Settings.DistantMountainsViewingDistance = (int)numericDistantMountainsViewingDistance.Value * 1000;
            Settings.LODAlwaysMaximum = checkLODAlwaysMaximum.Checked;
            Settings.LODViewingExtention = checkLODViewingExtention.Checked;
            Settings.ModelInstancing = checkBoxModelInstancing.Checked;
            Settings.PerformanceTuner = checkPerformanceTuner.Checked;
            Settings.PerformanceTunerTarget = (int)numericPerformanceTunerTarget.Value;
            Settings.DoubleWire = checkDoubleWire.Checked;
            Settings.DayAmbientLight = (int)trackDayAmbientLight.Value;
            Settings.UseMSTSEnv = checkUseMSTSEnv.Checked;
            Settings.UseLocationPassingPaths = checkUseLocationPassingPaths.Checked;
            Settings.PreferDDSTexture = checkPreferDDSTexture.Checked;
            Settings.FastFullScreenAltTab = checkFastFullScreenAltTab.Checked;
            Settings.UseLargeAddressAware = checkUseLargeAddressAware.Checked;
            Settings.CircularSpeedGauge = checkBoxCircularSpeedGauge.Checked;
            Settings.SignalLightGlow = checkBoxSignalLightGlow.Checked;
            Settings.EnhancedActCompatibility = checkBoxEnhancedActCompatibility.Checked;
            Settings.NoForcedRedAtStationStops =  checkBoxNoForcedRedAtStationStops.Checked;
            Settings.ConditionalLoadOfNightTextures = checkBoxConditionalLoadOfNightTextures.Checked;
            Settings.ExtendedAIShunting = checkBoxExtendedAIShunting.Checked;

            Settings.Save();

            DialogResult = DialogResult.OK;
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

        private void comboBoxWindowSize_SelectedIndexChanged( object sender, EventArgs e ) {
            var windowSizeParts = comboWindowSize.Text.Split( new[] { 'x' }, 2 );
            double width = Convert.ToDouble( windowSizeParts[0] );
            double height = Convert.ToDouble( windowSizeParts[1] );
            double aspectRatio = width / height;
            bool wideScreen = aspectRatio > (4.0 / 3.0); 
            numericCab2DStretch.Enabled = wideScreen;
        }

        private void numericUpDownFOV_ValueChanged(object sender, EventArgs e)
        {
            labelFOVHelp.Text = catalog.GetStringFmt("{0:F0}° vertical FOV is the same as:\n{1:F0}° horizontal FOV on 4:3\n{2:F0}° horizontal FOV on 16:9", numericViewingFOV.Value, numericViewingFOV.Value * 4 / 3, numericViewingFOV.Value * 16 / 9);
        }

        private void trackBarDayAmbientLight_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trackDayAmbientLight, (trackDayAmbientLight.Value * 5).ToString() + " %");
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Initialized)
                MessageBox.Show(catalog.GetString("Please restart Open Rails in order to load the new language."));
        }

        private void AdhesionFactorTrackBar1_ValueChanged(object sender, EventArgs e)
        {
            SetAdhesionLevelValue();
            AdhesionFactorValueLabel.Text = AdhesionFactorTrackBar1.Value.ToString() + "%";
            AdhesionFactorChangeValueLabel.Text = AdhesionFactorChangeTrackBar1.Value.ToString() + "%";
        }

        private void SetAdhesionLevelValue()
        {
            int level = AdhesionFactorTrackBar1.Value - AdhesionFactorChangeTrackBar1.Value;
            if (AdhesionPropToWeatherCheckBox.Checked)
                level -= 40;

            if (level > 159)
                AdhesionLevelValue.Text = catalog.GetString("Very easy");
            else if (level > 139)
                AdhesionLevelValue.Text = catalog.GetString("Easy");
            else if (level > 119)
                AdhesionLevelValue.Text = catalog.GetString("MSTS Compatibile");
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
    }
}
