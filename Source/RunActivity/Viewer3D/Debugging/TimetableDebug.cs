// COPYRIGHT 2009 - 2020 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// Principal Author:
//     Author: Charlie Salts / Signalsoft Rail Consultancy Ltd.
// Contributor:
//    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
// 

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using GNU.Gettext.WinForms;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using Orts.Viewer3D.Popups;
using ORTS.Common;
using Control = System.Windows.Forms.Control;
using Image = System.Drawing.Image;

namespace Orts.Viewer3D.Debugging
{
    public class TimetableDebug
    {
        public TimetableDebug(DispatchViewer form, Simulator simulator, Viewer viewer, TrackNode[] nodes)
        {
            form.Text = "Timetable Debug Window";

            // Hide DispatchWindow controls
            form.Controls.Find("MSG", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("messages", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("composeMSG", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("msgAll", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("msgSelected", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("reply2Selected", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkAllowNew", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkShowAvatars", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkAllowUserSwitch", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkBoxPenalty", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkPreferGreen", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkBoxPenalty", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkDrawPath", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkPickSignals", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("chkPickSwitches", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("btnSeeInGame", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("btnAssist", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("btnNormal", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("btnSeeInGame", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("refreshButton", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("rmvButton", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("btnFollow", true)[0].Visible = !simulator.TimetableMode;
            form.Controls.Find("AvatarView", true)[0].Visible = !simulator.TimetableMode;

            // Reveal Timetable Debug controls
            form.Controls.Find("lblSimulationTimeText", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("lblSimulationTimeValue", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("lblShow", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("cbShowPlatforms", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("cbShowSidings", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("cbShowSignals", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("cbSignalState", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("cbAllTrains", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("cbActiveTrains", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("lblDaylightOffsetH", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("numDaylightOffsetHours", true)[0].Visible = simulator.TimetableMode;
            form.Controls.Find("bBackgroundColor", true)[0].Visible = simulator.TimetableMode;

            form.trainFont = new Font("Arial", 11, FontStyle.Regular);
            form.sidingFont = new Font("Arial", 11, FontStyle.Regular);

            form.trainBrush = new SolidBrush(Color.Red);
            form.sidingBrush = new SolidBrush(Color.Blue);
        }
    }
}
