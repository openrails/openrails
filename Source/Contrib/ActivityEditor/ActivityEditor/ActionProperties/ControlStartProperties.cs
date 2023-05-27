using System;
using System.Windows.Forms;
using Orts.Formats.OR;

namespace ActivityEditor.ActionProperties
{
    public partial class ControlStartProperties : Form
    {
        public AuxControlStart Action { get; protected set; }
        public ControlStartProperties(AuxActionRef action)
        {
            Action = (AuxControlStart)action;
            InitializeComponent();
            textBox1.Text = Action.ActivationDelay.ToString();
            textBox2.Text = Action.ActionDuration.ToString();
        }

        private void HornOK_Click(object sender, EventArgs e)
        {
            Close();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int delay = Convert.ToInt32(textBox1.Text);
            if (delay >= 1 && delay <= 20 && delay < Action.ActionDuration)
            {
                Action.ActivationDelay = delay;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            int duration = Convert.ToInt32(textBox2.Text);
            if (duration >= 10 && duration <= 25)
                Action.ActionDuration = duration;
        }
    }
}
