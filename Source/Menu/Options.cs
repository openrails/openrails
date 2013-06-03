// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using XNA = Microsoft.Xna.Framework.Input;

namespace ORTS
{
    public partial class OptionsForm : Form
    {
        readonly UserSettings Settings;
        UserCommandInput[] DefaultCommands = new UserCommandInput[Enum.GetNames(typeof(UserCommands)).Length];
        bool SetAllDefaults = false;

        public OptionsForm(UserSettings settings)
        {
            InitializeComponent();

            Settings = settings;

#if !DEBUG
            buttonDebug.Visible = false;
#endif

            InputSettings.SetDefaults();
            for (var i = 0; i < Enum.GetNames(typeof(UserCommands)).Length; ++i)
                DefaultCommands[i] = InputSettings.Commands[i];
            InputSettings.SetDefaults();
            try
            {
                InputSettings.LoadUserSettings(new string[0]);
                PopulateKeyAssignmentForm();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message + " while parsing key assignments from registry. Reset to defaults.", Application.ProductName);
            }

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            string[] strContents = 
            {
                "1024x768",
                "1152x864",
                "1280x720",
                "1280x768",
                "1280x800",
                "1280x960",
                "1280x1024",
                "1360x768",
                "1440x900",
                "1600x900",
                "1680x1050",
                "1600x1200",
                "1768x992",
                "1920x1080",
                "1920x1200"
            };

            numericWorldObjectDensity.Value = 10;
            numericSoundDetailLevel.Value = 5;
            comboBoxWindowSize.Items.AddRange(strContents);
            comboBoxWindowSize.Text = "1024x768";
            numericBrakePipeChargingRatePSIpS.Value = 21;

            numericWorldObjectDensity.Value = Settings.WorldObjectDensity;
            numericSoundDetailLevel.Value = Settings.SoundDetailLevel;
            comboBoxWindowSize.Text = Settings.WindowSize;
            checkBoxAlerter.Checked = Settings.Alerter;
            checkBoxTrainLights.Checked = Settings.TrainLights;
            checkBoxPrecipitation.Checked = Settings.Precipitation;
            checkBoxWire.Checked = Settings.Wire;
            numericBrakePipeChargingRatePSIpS.Value = Settings.BrakePipeChargingRate;
            checkBoxGraduatedRelease.Checked = Settings.GraduatedRelease;
            checkBoxShadows.Checked = Settings.DynamicShadows;
            checkBoxWindowGlass.Checked = Settings.WindowGlass;
            checkBoxBINSound.Checked = Settings.MSTSBINSound;
            checkBoxSuppressConfirmations.Checked = Settings.SuppressConfirmations;
            checkDispatcher.Checked = Settings.ViewDispatcher;
            numericUpDownFOV.Value = Settings.ViewingFOV;
            numericCab2DStretch.Value = Settings.Cab2DStretch;
            checkBoxAdvancedAdhesion.Checked = Settings.UseAdvancedAdhesion;
            checkBoxBreakCouplers.Checked = Settings.BreakCouplers;
			soundVolume.Value = Settings.SoundVolumePercent;
            ElevationAmount.Value = Settings.UseSuperElevation;
            MinLengthChoice.Value = Settings.SuperElevationMinLen;
            SuperElevationGauge.Value = Settings.SuperElevationGauge;
            distanceMountain.Checked = settings.DistantMountains;
            DMDistance.Value = settings.DistantMountainsViewingTiles * 2;
            DMLoweringValue.Value = settings.DistantMountainsLoweringValue;
            NormalViewingDistance.Value = settings.ViewingDistance;
            LODExtention.Checked = settings.LODViewingExtention;
            checkDoubleWire.Checked = settings.DoubleWire;
        }

        string ParseCategoryFrom(string name)
        {
            var len = name.IndexOf(' ');
            if (len == -1)
                return "";
            else
                return name.Substring(0, len);
        }


        string ParseDescriptorFrom(string name)
        {
            var len = name.IndexOf(' ');
            if (len == -1)
                return name;
            else
                return name.Substring(len + 1);
        }

        void PopulateKeyAssignmentForm()
        {
            // TODO read from registry

            panelKeys.Controls.Clear();
            panelKeys.Controls.Clear();

            var i = 0;
            var lastCategory = "";
            foreach (UserCommands eCommand in Enum.GetValues(typeof(UserCommands)))
            {
                var name = InputSettings.FormatCommandName(eCommand);
                var category = ParseCategoryFrom(name);
                var descriptor = ParseDescriptorFrom(name);

#if (!DEBUG )
                if ( category.ToUpper() == "DEBUG" )
                    continue;
#endif
                var keyPressControl = new KeyPressControl();
                var label = new Label();
                panelKeys.Controls.Add(keyPressControl);
                panelKeys.Controls.Add(label);

                if (category != lastCategory)
                {
                    // 
                    // category label
                    // 
                    var catlabel = new Label();
                    panelKeys.Controls.Add(catlabel);
                    catlabel.Location = new Point(32, 11 + i * 22);
                    catlabel.Name = "Label";
                    catlabel.Size = new Size(180, 17);
                    catlabel.TabIndex = 0;
                    catlabel.Text = category;
                    catlabel.TextAlign = ContentAlignment.TopLeft;
                    catlabel.Font = new Font(catlabel.Font, FontStyle.Bold);
                    lastCategory = category;
                    ++i;
                }

                // 
                // label
                // 
                label.Location = new Point(12, 11 + i * 22);
                label.Name = "Label";
                label.Size = new Size(180, 17);
                label.TabIndex = 0;
                label.Text = descriptor;
                label.TextAlign = ContentAlignment.TopRight;
                // 
                // keyPressControl
                // 
                keyPressControl.Location = new Point(200, 8 + i * 22);
                keyPressControl.Name = "KeyPressControl";
                keyPressControl.Size = new Size(200, 20);
                keyPressControl.TabIndex = 1;
                keyPressControl.ReadOnly = true;
                keyPressControl.InitFrom(eCommand, DefaultCommands[(int)eCommand]);
                toolTip1.SetToolTip(keyPressControl, "Click here to change this key.");

                ++i;
            }
        }


        // Keys that use optional modifiers must have their modifiers listed as 'ignore' keys in their CommandInput class.
        // This function sets the 'ignore' keys according to the modifiers used by the command.
        // For example, if the user changed the CameraMoveFast key from ALT to CTRL, then all camera movement commands must ignore CTRL
        // If the modifier key conflicts with the assigned keys, proceed anyway, it will be caught by InputSettings.CheckForErrors()
        void FixModifiableKey(UserCommands eCommand, UserCommands[] eModifiers)
        {
            var command = (UserCommandModifiableKeyInput)InputSettings.Commands[(int)eCommand];
            command.IgnoreControl = false;
            command.IgnoreAlt = false;
            command.IgnoreShift = false;

            foreach (UserCommands eModifier in eModifiers)
            {
                var modifier = (UserCommandModifierInput)InputSettings.Commands[(int)eModifier];
                if (modifier.Control) command.IgnoreControl = true;
                if (modifier.Alt) command.IgnoreAlt = true;
                if (modifier.Shift) command.IgnoreShift = true;
            }
        }

        void FixModifiableKeys()
        {
            // for now this is a manual fixup process

            // these ones use the DisplayNextWindowTab modifier
            FixModifiableKey(UserCommands.DisplayHelpWindow, new UserCommands[] { UserCommands.DisplayNextWindowTab });
            FixModifiableKey(UserCommands.DisplayHUD, new UserCommands[] { UserCommands.DisplayNextWindowTab });

            // these ones use the CameraMoveFast and CameraMoveSlow modifier
            foreach (UserCommands eCommand in new UserCommands[] { UserCommands.CameraPanLeft, UserCommands.CameraPanRight, 
                        UserCommands.CameraPanUp, UserCommands.CameraPanDown, UserCommands.CameraZoomIn, UserCommands.CameraZoomOut, 
                        UserCommands.CameraRotateLeft, UserCommands.CameraRotateRight, UserCommands.CameraRotateUp, UserCommands.CameraRotateDown })

                FixModifiableKey(eCommand, new UserCommands[] { UserCommands.CameraMoveFast, UserCommands.CameraMoveSlow });

        }

        bool MatchesDefaults(UserCommands eCommand)
        {
            int scan1, scan2;
            XNA.Keys vkey1, vkey2;
            bool ctrl1, ctrl2;
            bool alt1, alt2;
            bool shift1, shift2;
            bool ictrl1, ictrl2;
            bool ialt1, ialt2;
            bool ishift1, ishift2;

            var currentKeyCombo = InputSettings.Commands[(int)eCommand];
            var defaultKeyCombo = DefaultCommands[(int)eCommand];
            defaultKeyCombo.ToValue(out scan1, out vkey1, out ctrl1, out alt1, out shift1, out ictrl1, out ialt1, out ishift1);
            currentKeyCombo.ToValue(out scan2, out vkey2, out ctrl2, out alt2, out shift2, out ictrl2, out ialt2, out ishift2);

            return scan1 == scan2 && vkey1 == vkey2 && ctrl1 == ctrl2 && alt1 == alt2 && shift1 == shift2
                                                    && ictrl1 == ictrl2 && ialt1 == ialt2 && ishift1 == ishift2;
        }

        void WriteInputSettingsToRegistry()
        {
            // When we see this condition, do a general cleanup.
            if (SetAllDefaults)
            {
                try
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(InputSettings.RegistryKey);
                }
                catch (ArgumentException) { }
            }

            using (var RK = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(InputSettings.RegistryKey))
            {
                // for every user command
                foreach (UserCommands eCommand in Enum.GetValues(typeof(UserCommands)))
                {
                    var keyCombo = InputSettings.Commands[(int)eCommand];

                    if (MatchesDefaults(eCommand))
                    {
                        RK.DeleteValue(eCommand.ToString(), false);
                    }
                    else
                    {
                        var setting = keyCombo.ToRegString();
                        RK.SetValue(eCommand.ToString(), setting);
                    }
                }
            }
        }


        void buttonOK_Click(object sender, EventArgs e)
        {
            FixModifiableKeys();

            var result = InputSettings.CheckForErrors(false);
            if (result != "" && DialogResult.Yes != MessageBox.Show(result + "\nContinue with conflicting key assignments?", Application.ProductName, MessageBoxButtons.YesNo))
                return;

            WriteInputSettingsToRegistry();

            Settings.WorldObjectDensity = (int)numericWorldObjectDensity.Value;
            Settings.SoundDetailLevel = (int)numericSoundDetailLevel.Value;
            Settings.WindowSize = comboBoxWindowSize.Text;
            Settings.Alerter = checkBoxAlerter.Checked;
            Settings.TrainLights = checkBoxTrainLights.Checked;
            Settings.Precipitation = checkBoxPrecipitation.Checked;
            Settings.Wire = checkBoxWire.Checked;
            Settings.BrakePipeChargingRate = (int)numericBrakePipeChargingRatePSIpS.Value;
            Settings.GraduatedRelease = checkBoxGraduatedRelease.Checked;
            Settings.DynamicShadows = checkBoxShadows.Checked;
            Settings.WindowGlass = checkBoxWindowGlass.Checked;
            Settings.MSTSBINSound = checkBoxBINSound.Checked;
            Settings.SuppressConfirmations = checkBoxSuppressConfirmations.Checked;
            Settings.ViewDispatcher = checkDispatcher.Checked;
            Settings.ViewingFOV = (int)numericUpDownFOV.Value;
            Settings.Cab2DStretch = (int)numericCab2DStretch.Value;
            Settings.UseAdvancedAdhesion = checkBoxAdvancedAdhesion.Checked;
            Settings.BreakCouplers = checkBoxBreakCouplers.Checked;
			Settings.SoundVolumePercent = (int)soundVolume.Value;
            Settings.UseSuperElevation = (int)ElevationAmount.Value;
            Settings.SuperElevationMinLen = (int)MinLengthChoice.Value;
            Settings.SuperElevationGauge = (int)SuperElevationGauge.Value;
            Settings.DistantMountains = distanceMountain.Checked;
            Settings.DistantMountainsViewingTiles = (int) DMDistance.Value / 2;
            Settings.DistantMountainsLoweringValue = (int)DMLoweringValue.Value;
            Settings.ViewingDistance = (int)NormalViewingDistance.Value;
            Settings.LODViewingExtention = LODExtention.Checked;
            Settings.DoubleWire = checkDoubleWire.Checked;
            Settings.Save();

            DialogResult = DialogResult.OK;
        }

        void buttonDefaultKeys_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show("Remove all custom key assignments?", Application.ProductName, MessageBoxButtons.YesNo))
            {
                InputSettings.SetDefaults();
                PopulateKeyAssignmentForm();
                SetAllDefaults = true;
            }
        }

        void buttonExport_Click(object sender, EventArgs e)
        {
            var OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            InputSettings.DumpToText(OutputPath + @"\Keyboard.txt");
            //InputSettings.DumpToGraphic( OutputPath + @"\Keyboard.png");
            MessageBox.Show("Placed Keyboard.txt on your desktop", Application.ProductName);
        }

        void buttonCheckKeys_Click(object sender, EventArgs e)
        {
            var errors = InputSettings.CheckForErrors(false);
            if (errors != "")
                MessageBox.Show(errors, Application.ProductName);
            else
                MessageBox.Show("No errors found.", Application.ProductName);
        }

        void buttonDebug_Click(object sender, EventArgs e)
        {
            var errors = InputSettings.CheckForErrors(true);
            if (errors != "")
                MessageBox.Show(errors, Application.ProductName);
            else
                MessageBox.Show("No errors found.", Application.ProductName);
        }

        private void comboBoxWindowSize_SelectedIndexChanged( object sender, EventArgs e ) {
            var windowSizeParts = comboBoxWindowSize.Text.Split( new[] { 'x' }, 2 );
            double width = Convert.ToDouble( windowSizeParts[0] );
            double height = Convert.ToDouble( windowSizeParts[1] );
            double aspectRatio = width / height;
            bool wideScreen = aspectRatio > (4.0 / 3.0); 
            numericCab2DStretch.Enabled = wideScreen;
        }

        private void numericUpDownFOV_ValueChanged(object sender, EventArgs e)
        {
            labelFOVHelp.Text = String.Format("{0:F0}° vertical FOV is the same as:\n{1:F0}° horizontal FOV on 4:3\n{2:F0}° horizontal FOV on 16:9", numericUpDownFOV.Value, numericUpDownFOV.Value * 4 / 3, numericUpDownFOV.Value * 16 / 9);
        }
    }
}
