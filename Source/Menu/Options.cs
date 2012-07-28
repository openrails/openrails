/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

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
using Microsoft.Win32;
using XNA = Microsoft.Xna.Framework.Input;

namespace ORTS
{
    public partial class OptionsForm : Form
    {
        public UserCommandInput[] DefaultCommands = new UserCommandInput[Enum.GetNames(typeof(UserCommands)).Length];
        public bool SetAllDefaults = false;

        public OptionsForm()
        {
            InitializeComponent();

#if !DEBUG
            buttonDebug.Visible = false;
#endif

            InputSettings.SetDefaults();
            for (int i = 0; i < Enum.GetNames(typeof(UserCommands)).Length; ++i)
                DefaultCommands[i] = InputSettings.Commands[i];
            InputSettings.SetDefaults();
            try
            {
                InputSettings.LoadUserSettings(new string[0]);
                PopulateKeyAssignmentForm();
            }
            catch( System.Exception error)
            {
                MessageBox.Show(error.Message + "while parsing key assignments from registry.  Reset to defaults.", "ERROR");
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

            this.numericWorldObjectDensity.Value = 10;
            this.numericSoundDetailLevel.Value = 5;
            this.comboBoxWindowSize.Items.AddRange(strContents);
            this.comboBoxWindowSize.Text = "1024x768";
            this.numericBrakePipeChargingRatePSIpS.Value = 21;

            // Restore retained settings
            using (var RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey))
            {
                if (RK != null)
                {
                    this.numericWorldObjectDensity.Value = (int)RK.GetValue("WorldObjectDensity", (int)numericWorldObjectDensity.Value);
                    this.numericSoundDetailLevel.Value = (int)RK.GetValue("SoundDetailLevel", (int)numericSoundDetailLevel.Value);
                    this.comboBoxWindowSize.Text = (string)RK.GetValue("WindowSize", (string)comboBoxWindowSize.Text);
                    this.checkBoxAlerter.Checked = (1 == (int)RK.GetValue("Alerter", 0));
                    this.checkBoxTrainLights.Checked = (1 == (int)RK.GetValue("TrainLights", 0));
                    this.checkBoxPrecipitation.Checked = (1 == (int)RK.GetValue("Precipitation", 0));
                    this.checkBoxWire.Checked = (1 == (int)RK.GetValue("Wire", 0));
                    this.numericBrakePipeChargingRatePSIpS.Value = (int)RK.GetValue("BrakePipeChargingRate", (int)numericBrakePipeChargingRatePSIpS.Value);
                    this.checkBoxGraduatedRelease.Checked = (1 == (int)RK.GetValue("GraduatedRelease", 0));
                    this.checkBoxShadows.Checked = (1 == (int)RK.GetValue("DynamicShadows", 0));
                    this.checkBoxWindowGlass.Checked = (1 == (int)RK.GetValue("WindowGlass", 0));
                    this.checkBoxBINSound.Checked = (1 == (int)RK.GetValue("MSTSBINSound", 0));
                    this.checkBoxSuppressConfirmations.Checked = (1 == (int)RK.GetValue("SuppressConfirmations", 0));
					this.checkDispatcher.Checked = (1 == (int)RK.GetValue("ViewDispatcher", 0));
                }
            }
        }

        string ParseCategoryFrom( string name)
        {
            int len = name.IndexOf(' ');
            if (len == -1)
                return "";
            else
                return name.Substring(0, len);
        }


        string ParseDescriptorFrom(string name)
        {
            int len = name.IndexOf(' ');
            if (len == -1)
                return name;
            else
                return name.Substring(len + 1);
        }

        void PopulateKeyAssignmentForm()
        {
            // TODO read from registry

            this.panelKeys.Controls.Clear();
            this.panelKeys.Controls.Clear();

            int i = 0;
            string lastCategory = "";
            foreach (UserCommands eCommand in Enum.GetValues(typeof(UserCommands)))
            {
                string name = InputSettings.FormatCommandName(eCommand);
                string category = ParseCategoryFrom(name);
                string descriptor = ParseDescriptorFrom(name);

#if (!DEBUG )
                if ( category.ToUpper() == "DEBUG" )
                    continue;
#endif
                KeyPressControl keyPressControl;
                System.Windows.Forms.Label label;

                keyPressControl = new ORTS.KeyPressControl();
                label = new System.Windows.Forms.Label();
                this.panelKeys.Controls.Add(keyPressControl);
                this.panelKeys.Controls.Add(label);

                if (category != lastCategory)
                {
                    // 
                    // category label
                    // 
                    System.Windows.Forms.Label catlabel = new System.Windows.Forms.Label();
                    this.panelKeys.Controls.Add(catlabel);
                    catlabel.Location = new System.Drawing.Point(32, 11 + i * 22);
                    catlabel.Name = "Label";
                    catlabel.Size = new System.Drawing.Size(180, 17);
                    catlabel.TabIndex = 0;
                    catlabel.Text = category;
                    catlabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
                    catlabel.Font = new Font( catlabel.Font, FontStyle.Bold );
                    lastCategory = category;
                    ++i;
                }

                // 
                // label
                // 
                label.Location = new System.Drawing.Point(12, 11 + i * 22);
                label.Name = "Label";
                label.Size = new System.Drawing.Size(180, 17);
                label.TabIndex = 0;
                label.Text = descriptor;
                label.TextAlign = System.Drawing.ContentAlignment.TopRight;
                // 
                // keyPressControl
                // 
                keyPressControl.Location = new System.Drawing.Point(200, 8 + i * 22);
                keyPressControl.Name = "KeyPressControl";
                keyPressControl.Size = new System.Drawing.Size(200, 20);
                keyPressControl.TabIndex = 1;
                keyPressControl.ReadOnly = true;
                keyPressControl.InitFrom( eCommand, DefaultCommands[(int)eCommand] );
                this.toolTip1.SetToolTip(keyPressControl, "Click here to change this key.");

                ++i;
            }
        }


        // Keys that use optional modifiers must have their modifiers listed as 'ignore' keys in their CommandInput class.
        // This function sets the 'ignore' keys according to the modifiers used by the command.
        // For example, if the user changed the CameraMoveFast key from ALT to CTRL, then all camera movement commands must ignore CTRL
        // If the modifier key conflicts with the assigned keys, proceed anyway, it will be caught by InputSettings.CheckForErrors()
        void FixModifiableKey(UserCommands eCommand, UserCommands[] eModifiers)
        {
            UserCommandModifiableKeyInput command = (UserCommandModifiableKeyInput) InputSettings.Commands[(int)eCommand];
            command.IgnoreControl = false;
            command.IgnoreAlt = false;
            command.IgnoreShift = false;

            foreach (UserCommands eModifier in eModifiers)
            {
                UserCommandModifierInput modifier = (UserCommandModifierInput)InputSettings.Commands[(int)eModifier];
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
                        UserCommands.CameraPanUp, UserCommands.CameraPanDown, UserCommands.CameraPanIn, UserCommands.CameraPanOut, 
                        UserCommands.CameraRotateLeft, UserCommands.CameraRotateRight, UserCommands.CameraRotateUp, UserCommands.CameraRotateDown })

                FixModifiableKey(eCommand, new UserCommands[] { UserCommands.CameraMoveFast, UserCommands.CameraMoveSlow });

        }

        private bool MatchesDefaults( UserCommands eCommand )
        {
            int scan1, scan2;
            XNA.Keys vkey1, vkey2;
            bool ctrl1, ctrl2;
            bool alt1, alt2;
            bool shift1, shift2;
            bool ictrl1, ictrl2;
            bool ialt1, ialt2;
            bool ishift1, ishift2;

            UserCommandInput currentKeyCombo = InputSettings.Commands[(int)eCommand];
            UserCommandInput defaultKeyCombo = DefaultCommands[(int)eCommand];
            defaultKeyCombo.ToValue(out scan1, out vkey1, out ctrl1, out alt1, out shift1, out ictrl1, out ialt1, out ishift1);
            currentKeyCombo.ToValue(out scan2, out vkey2, out ctrl2, out alt2, out shift2, out ictrl2, out ialt2, out ishift2);

            return scan1 == scan2 && vkey1 == vkey2 && ctrl1 == ctrl2 && alt1 == alt2 && shift1 == shift2
                                                    && ictrl1 == ictrl2 && ialt1 == ialt2 && ishift1 == ishift2;
        }

        void WriteInputSettingsToRegistry()
        {
            if (this.SetAllDefaults)
            {
                // When we see this condition, do a general cleanup.
                Registry.CurrentUser.DeleteSubKeyTree( InputSettings.RegistryKey );
            }

            using (var RK = Registry.CurrentUser.CreateSubKey(InputSettings.RegistryKey))
            {                
                // for every user command
                foreach (var eCommand in Enum.GetValues(typeof(UserCommands)))
                {
                    UserCommandInput keyCombo = InputSettings.Commands[(int)eCommand];

                    if (MatchesDefaults((UserCommands)eCommand))
                    {
                        RK.DeleteValue(eCommand.ToString(), false);
                    }
                    else
                    {
                        string setting = keyCombo.ToRegString();
                        RK.SetValue(eCommand.ToString(), setting);
                    }
                }
            }
        }


        void buttonOK_Click(object sender, EventArgs e)
        {

            FixModifiableKeys();

            string result = InputSettings.CheckForErrors( false);

            if (result != "")
            {
                if( DialogResult.Cancel == MessageBox.Show( result, "Warning", MessageBoxButtons.OKCancel ))
                    return;
            }

            WriteInputSettingsToRegistry();

            // Retain settings for convenience
            using (var RK = Registry.CurrentUser.CreateSubKey(Program.RegistryKey))
            {
                RK.SetValue("WorldObjectDensity", (int)this.numericWorldObjectDensity.Value);
                RK.SetValue("SoundDetailLevel", (int)this.numericSoundDetailLevel.Value);
                RK.SetValue("WindowSize", (string)this.comboBoxWindowSize.Text);
                RK.SetValue("Alerter", this.checkBoxAlerter.Checked ? 1 : 0);
                RK.SetValue("TrainLights", this.checkBoxTrainLights.Checked ? 1 : 0);
                RK.SetValue("Precipitation", this.checkBoxPrecipitation.Checked ? 1 : 0);
                RK.SetValue("Wire", this.checkBoxWire.Checked ? 1 : 0);
                RK.SetValue("BrakePipeChargingRate", (int)this.numericBrakePipeChargingRatePSIpS.Value);
                RK.SetValue("GraduatedRelease", this.checkBoxGraduatedRelease.Checked ? 1 : 0);
                RK.SetValue("DynamicShadows", this.checkBoxShadows.Checked ? 1 : 0);
				RK.SetValue("WindowGlass", this.checkBoxWindowGlass.Checked ? 1 : 0);
				RK.SetValue("MSTSBINSound", this.checkBoxBINSound.Checked ? 1 : 0);
                RK.SetValue("SuppressConfirmations", this.checkBoxSuppressConfirmations.Checked ? 1 : 0);
				RK.SetValue("ViewDispatcher", this.checkDispatcher.Checked ? 1 : 0);

            }

            DialogResult = DialogResult.OK;
        }

        private void buttonDefaultKeys_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == MessageBox.Show("You will loose all custom key assignments that you have made.", "Warning", MessageBoxButtons.OKCancel))
            {
                InputSettings.SetDefaults();
                PopulateKeyAssignmentForm();
                SetAllDefaults = true;
            }
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            string OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            InputSettings.DumpToText( OutputPath + @"\Keyboard.txt");
            //InputSettings.DumpToGraphic( OutputPath + @"\Keyboard.png");
            MessageBox.Show("Placed Keyboard.txt on your desktop");    
        }

        private void buttonCheckKeys_Click(object sender, EventArgs e)
        {
            string errors = InputSettings.CheckForErrors( false);

            if (errors != "")
                MessageBox.Show(errors, "Error");
            else
                MessageBox.Show("No errors found.", "Result");
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {

        }

        private void buttonDebug_Click(object sender, EventArgs e)
        {
            string errors = InputSettings.CheckForErrors(true);

            if (errors != "")
                MessageBox.Show(errors, "Error");
            else
                MessageBox.Show("No errors found.", "Result");
        }

    }
}
