/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Win32;

namespace ORTS
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();

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
                "1600x1200",
                "1680x1050",
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
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
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
			}
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // Retain settings for convenience
            RegistryKey RK = Registry.CurrentUser.CreateSubKey(Program.RegistryKey);
            if (RK != null)
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
            }
			Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
