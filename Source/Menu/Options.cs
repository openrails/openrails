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
            this.comboBox1.Items.AddRange(strContents);
            this.comboBox1.Text = "1024x768";

            

            // Restore retained settings
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
            if (RK != null)
            {
                this.numericWorldObjectDensity.Value = (int)RK.GetValue("WorldObjectDensity", (int)numericWorldObjectDensity.Value);
                this.numericSoundDetailLevel.Value = (int)RK.GetValue("SoundDetailLevel", (int)numericSoundDetailLevel.Value);
                this.comboBox1.Text = (string)RK.GetValue("WindowSize", (string)comboBox1.Text);
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
                RK.SetValue("WindowSize", (string)this.comboBox1.Text);
            }

            Close();

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
