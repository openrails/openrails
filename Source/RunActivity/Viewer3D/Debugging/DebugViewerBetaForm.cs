using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using GNU.Gettext.WinForms;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using Orts.Viewer3D.Popups;
using ORTS.Common;

namespace Orts.Viewer3D.Debugging
{
    public partial class DispatchViewerBeta : Form
    {
        /// <summary>
        /// Used to periodically check if we should shift the view when the
        /// user is holding down a "shift view" button.
        /// </summary>
        private Timer UITimer;

        public Viewer Viewer;

        public DispatchViewerBeta(Simulator simulator, Viewer viewer)
        {
            InitializeComponent();

            Viewer = viewer;

            initializeForm();

            // Initialise the timer used to handle user input
            UITimer = new Timer();
            UITimer.Interval = 100;
            UITimer.Tick += new EventHandler(UITimer_Tick);
            UITimer.Start();
        }

        void initializeForm()
        {
            if (MPManager.IsMultiPlayer() && MPManager.IsServer())
            {
                dispatcherInfoPanel.Visible = true;
            }
        }

        void UITimer_Tick(object sender, EventArgs e)
        {
            if (Viewer.DebugViewerBetaEnabled == false) // Ctrl+9 sets this true to initialise the window and make it visible
            {
                Visible = false;
                return;
            }
            Visible = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }
    }
}
