using System;
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
