using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ActivityEditor.Engine;

namespace ActivityEditor.Activity
{
    public partial class StartActivity : Form
    {
        AEConfig config;

        public StartActivity(AEConfig aeConfig)
        {
            InitializeComponent();
            config = aeConfig;
            ActName.Text = aeConfig.getActivityName();
            ActDescr.Text = aeConfig.getActivityDescr();
        }

        private void SA_OK_Click(object sender, EventArgs e)
        {
            if (ActName.Text.Length <= 0)
                return;
            config.setActivityName(ActName.Text);
            config.setActivityDescr(ActDescr.Text);
            Close();
        }
    }
}
