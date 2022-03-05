// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Common;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{
    public class EOTListWindow : Window
    {
        public EOTListWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 20, Window.DecorationSize.Y + (owner.Viewer.Simulator.FullEOTPaths == null ?
                  owner.TextFontDefault.Height * 2 : owner.TextFontDefault.Height * (owner.Viewer.Simulator.FullEOTPaths.Count + 2)), Viewer.Catalog.GetString("EOT List"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (Owner.Viewer.Simulator.FullEOTPaths != null)
            {
                var colWidth = (vbox.RemainingWidth - vbox.TextHeight * 2) / 2;
                {
                    var line = vbox.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Filename")));
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Folder Name"), LabelAlignment.Left));
                }
                vbox.AddHorizontalSeparator();
                var scrollbox = vbox.AddLayoutScrollboxVertical(vbox.RemainingWidth);
                var playerLocomotive = Owner.Viewer.Simulator.PlayerLocomotive;
                foreach (var eotType in Owner.Viewer.Simulator.FullEOTPaths)
                {
                    var line = scrollbox.AddLayoutHorizontalLineOfText();
                    EOTLabel filename, foldername;
                    line.Add(filename = new EOTLabel(colWidth, line.RemainingHeight, Owner.Viewer, eotType, Path.GetFileNameWithoutExtension(eotType), LabelAlignment.Left));
                    line.Add(foldername = new EOTLabel(colWidth - Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, eotType, (Path.GetDirectoryName(eotType)).Remove(0, Owner.Viewer.Simulator.EOTPath.Length), LabelAlignment.Left));
                    if (playerLocomotive?.Train != null && eotType.ToLower() == playerLocomotive.Train.EOT?.WagFilePath.ToLower()) 
                    {                       
                        filename.Color = Color.Red;
                        foldername.Color = Color.Red;
                    }
                }
             }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull && Owner.Viewer.Simulator.FullEOTPaths != null)
            {
                Layout();
            }
        }
    }

    class EOTLabel : Label
    {
        readonly Viewer Viewer;
        readonly string PickedEOTTypeFromList;

        public EOTLabel(int width, int height, Viewer viewer, string eotType, String eotString, LabelAlignment alignment)
            : base(width, height, eotString, alignment)
        {
            Viewer = viewer;
            PickedEOTTypeFromList = eotType;
            Click += new Action<Control, Point>(EOTListLabel_Click);
        }

        void EOTListLabel_Click(Control arg1, Point arg2)
        {
            if (PickedEOTTypeFromList != "")
            {
                if (Viewer.PlayerLocomotive.AbsSpeedMpS > Simulator.MaxStoppedMpS)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Can't attach EOT if player train not stopped"));
                    return;
                }
                if (PickedEOTTypeFromList.ToLower() != Viewer.PlayerLocomotive.Train.Cars[Viewer.PlayerLocomotive.Train.Cars.Count - 1].WagFilePath.ToLower())
                {
                    if (Viewer.PlayerLocomotive.Train?.EOT != null)
                    {
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Player train already has a mounted EOT"));
                        return;
                    }
                    //Ask to mount EOT
                    new EOTMountCommand(Viewer.Log, true, PickedEOTTypeFromList);
                }
                else if (PickedEOTTypeFromList.ToLower() == Viewer.PlayerLocomotive.Train.Cars[Viewer.PlayerLocomotive.Train.Cars.Count - 1].WagFilePath.ToLower())
                {
                    new EOTMountCommand(Viewer.Log, false, PickedEOTTypeFromList);
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Can't mount an EOT if another one is mounted"));
                    return;
                }
            }
        }
    }
 }
