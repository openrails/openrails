using System;
using System.Windows.Forms;

namespace ActivityEditor.Activity
{
    public partial class WaitActivity : Form
    {
        public WaitActivity()
        {
            InitializeComponent();
        }

        private void SW_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
