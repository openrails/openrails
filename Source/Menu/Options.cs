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
using Microsoft.Win32;

namespace ORTS
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();

            this.numericWorldObjectDensity.Value = 10;
            this.numericSoundDetailLevel.Value = 5;

            // Restore retained settings
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
            if (RK != null)
            {
                this.numericWorldObjectDensity.Value = (int)RK.GetValue("WorldObjectDensity", (int)numericWorldObjectDensity.Value);
                this.numericSoundDetailLevel.Value = (int)RK.GetValue("SoundDetailLevel", (int)numericSoundDetailLevel.Value);

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
            }

            Close();

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
