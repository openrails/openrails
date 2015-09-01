using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibAE.Formats;
using Orts.Formats.OR;

namespace ActivityEditor.ActionProperties
{
    public partial class HornProperties : Form
    {
        public AuxActionHorn Action { get; protected set; }
        public HornProperties(AuxActionRef action)
        {
            Action = (AuxActionHorn)action;
            InitializeComponent();
            textBox1.Text = Action.Delay.ToString();
            textBox2.Text = Action.RequiredDistance.ToString();
        }

        private void HornOK_Click(object sender, EventArgs e)
        {
            Close();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int delay = Convert.ToInt32(textBox1.Text);
            if (delay > 1 && delay < 5)
                Action.Delay = delay;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            float distance = Convert.ToInt32(textBox2.Text);
            if (distance > 10 && distance < 500)
                Action.RequiredDistance = distance;
        }
    }
}
