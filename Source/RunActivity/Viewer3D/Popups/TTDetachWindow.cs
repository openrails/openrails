// COPYRIGHT 2012, 2013 by the Open Rails project.
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
using Microsoft.Xna.Framework;
using Orts.Simulation.Timetables;
using ORTS.Common;

namespace Orts.Viewer3D.Popups
{
    public class TTDetachWindow : Window
    {
        const int WindowImageSizeWidth = 600;
        const int WindowImageSizeHeightFactor = 7;

        Label ThisPortionLine;
        Label OtherPortionLine;

        Label ButtonDetach;

        DetachInfo reqDetach = null;
        TTTrain reqTrain;

        public TTDetachWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + WindowImageSizeWidth, Window.DecorationSize.Y + owner.TextFontDefault.Height * WindowImageSizeHeightFactor + ControlLayout.SeparatorSize * 2, Viewer.Catalog.GetString("Timetable Detach Menu"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            var boxWidth = vbox.RemainingWidth;

            var hbox1 = vbox.AddLayoutHorizontalLineOfText();
            hbox1.Add(new Label(boxWidth, hbox1.RemainingHeight, Viewer.Catalog.GetString("This train is about to split."), LabelAlignment.Left));

            var hbox2 = vbox.AddLayoutHorizontalLineOfText();
            hbox2.Add(ThisPortionLine = new Label(boxWidth, hbox2.RemainingHeight, "", LabelAlignment.Left));

            var hbox3 = vbox.AddLayoutHorizontalLineOfText();
            hbox3.Add(OtherPortionLine = new Label(boxWidth, hbox3.RemainingHeight, "", LabelAlignment.Left));

            var hbox4 = vbox.AddLayoutHorizontalLineOfText();
            hbox4.Add(new Label(boxWidth, hbox4.RemainingHeight, Viewer.Catalog.GetString("Use 'cab switch' command to select cab in required portion."), LabelAlignment.Left));

            vbox.AddSpace(0, Owner.TextFontDefault.Height);
            vbox.AddHorizontalSeparator();

            vbox.AddSpace(0, Owner.TextFontDefault.Height);
            vbox.Add(ButtonDetach = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Perform Detach"), LabelAlignment.Center));
            ButtonDetach.Click += new Action<Control, Point>(ButtonDetach_Click);

            return vbox;

        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                TTTrain playerTrain = Owner.Viewer.PlayerTrain as TTTrain;
                reqTrain = playerTrain as TTTrain;
                if (reqTrain.DetachActive[1] >= 0)
                {
                    List<DetachInfo> thisDetachList = reqTrain.DetachDetails[reqTrain.DetachActive[0]];
                    reqDetach = thisDetachList[reqTrain.DetachActive[1]];
                    string formedTrain = String.Copy(reqDetach.DetachFormedTrainName);
                    if (reqDetach.DetachFormedStatic)
                    {
                        if (String.IsNullOrEmpty(reqDetach.DetachFormedTrainName))
                        {
                            formedTrain = String.Concat(Viewer.Catalog.GetString("static consist"));

                        }
                        else
                        {
                            formedTrain = String.Concat(Viewer.Catalog.GetString("static consist"), " : ", reqDetach.DetachFormedTrainName);
                        }
                    }

                    string formedPortion = String.Copy(Viewer.Catalog.GetString("Rear"));
                    string otherPortion = String.Copy(Viewer.Catalog.GetString("Front"));
                    if (reqTrain.DetachPosition)
                    {
                        formedPortion = String.Copy(Viewer.Catalog.GetString("Front"));
                        otherPortion = String.Copy(Viewer.Catalog.GetString("Rear"));
                    }

                    if (reqDetach.CheckPlayerPowerPortion(reqTrain))
                    {
                        ThisPortionLine.Text = Viewer.Catalog.GetStringFmt("This portion will continue as train : {0}", reqTrain.Name.ToLower());
                        OtherPortionLine.Text = Viewer.Catalog.GetStringFmt("{0} portion will form train : {1}", formedPortion, formedTrain);
                    }
                    else
                    {
                        ThisPortionLine.Text = Viewer.Catalog.GetStringFmt("This portion will continue as train : {0}", formedTrain);
                        OtherPortionLine.Text = Viewer.Catalog.GetStringFmt("{0} portion will form train : {1}", otherPortion, reqTrain.Name.ToLower());
                    }
                }
            }
        }

        void ButtonDetach_Click(Control arg1, Point arg2)
        {
            if (reqDetach != null)
            {
                reqDetach.DetachPlayerTrain(reqTrain, reqDetach.DetachFormedTrain);
                if (reqTrain.DetachDetails.ContainsKey(reqTrain.DetachActive[0]))
                {
                    reqTrain.DetachDetails.Remove(reqTrain.DetachActive[0]);
                }

                reqDetach.Valid = false;
                this.Visible = false;
            }
        }

    }
}
