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

namespace ActivityEditor.Route_Metadata
{
    public partial class TrackSegmentInterface : Form
    {
        public TrackSegment segment;
        public List<string> destination;
        public TrackSegmentInterface(TrackSegment trackSegment, List<string> givenDestination)
        {
            List<string> destination = givenDestination;
            segment = trackSegment;
            InitializeComponent();
        }

        private void TrackItf_OK_Click(object sender, EventArgs e)
        {
            if (segmentLab.Text == "" || segmentLab.Text == "Please, give a name")
            {
                this.segmentLab.Text = "Please, give a name";
            }
            else
            {
                segment.segmentLabel = segmentLab.Text;
                //segment.setDirBuffer(BufDirCB.Text);
                Close();
            }

        }
    }
}
