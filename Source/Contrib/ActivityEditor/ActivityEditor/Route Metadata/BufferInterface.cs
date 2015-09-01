using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibAE;
using LibAE.Formats;
using Orts.Formats.OR;

namespace ActivityEditor.Engine
{
    public partial class BufferInterface : Form
    {
        public List<string> AllowedDirections;
        public bool ToRemove = false;
        public AEBufferItem buffer;
        public BufferInterface(AEBufferItem givenBuffer)
        {
            buffer = givenBuffer;
            List<string> availValues = givenBuffer.getAllowedDirections();
            InitializeComponent();
            BufDirCB.DataSource = availValues;
            BufDirCB.Text = availValues[(int)givenBuffer.getDirBuffer()];
            bufferLab.Text = givenBuffer.NameBuffer;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (bufferLab.Text == "" || bufferLab.Text == "Please, give a name")
            {
                this.bufferLab.Text = "Please, give a name";
            }
            else
            {
                buffer.NameBuffer = bufferLab.Text;
                buffer.setDirBuffer(BufDirCB.Text);
                Close();
            }
        }

        private void RemButton_Click(object sender, EventArgs e)
        {
            ToRemove = true;
            Close();
        }
    }
}
