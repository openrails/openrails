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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.Timetables;
using ORTS.Common;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Orts.Viewer3D.Popups
{
    public class TTRequestStopWindow : Window
    {
        const int WindowImageSizeWidth = 700;
        const int WindowImageSizeHeightFactor = 9;

        Label ButtonConfirm;

        Label ReqStop1;
        Label ReqStop2;
        Label ReqStop3;
        Label ReqStop4;
        Label ReqStop5;

        TTTrain reqTrain;

        public TTRequestStopWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + WindowImageSizeWidth, Window.DecorationSize.Y + owner.TextFontDefault.Height * WindowImageSizeHeightFactor + ControlLayout.SeparatorSize * 2, Viewer.Catalog.GetString("Timetable Detach Menu"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            var boxWidth = vbox.RemainingWidth;

            var hbox1 = vbox.AddLayoutHorizontalLineOfText();
            hbox1.Add(new Label(boxWidth, hbox1.RemainingHeight, Viewer.Catalog.GetString("Request Stops :"), LabelAlignment.Left));

            var hbox2 = vbox.AddLayoutHorizontalLineOfText();
            hbox2.Add(ReqStop1 = new Label(boxWidth, hbox2.RemainingHeight, "", LabelAlignment.Left));

            var hbox3 = vbox.AddLayoutHorizontalLineOfText();
            hbox3.Add(ReqStop2 = new Label(boxWidth, hbox3.RemainingHeight, "", LabelAlignment.Left));

            var hbox4 = vbox.AddLayoutHorizontalLineOfText();
            hbox4.Add(ReqStop3 = new Label(boxWidth, hbox4.RemainingHeight, "", LabelAlignment.Left));

            var hbox5 = vbox.AddLayoutHorizontalLineOfText();
            hbox5.Add(ReqStop4 = new Label(boxWidth, hbox5.RemainingHeight, "", LabelAlignment.Left));

            var hbox6 = vbox.AddLayoutHorizontalLineOfText();
            hbox6.Add(ReqStop5 = new Label(boxWidth, hbox6.RemainingHeight, "", LabelAlignment.Left));

            vbox.AddSpace(0, Owner.TextFontDefault.Height);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, Owner.TextFontDefault.Height);
            vbox.Add(ButtonConfirm = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Confirm"), LabelAlignment.Center));
            ButtonConfirm.Click += new Action<Control, Point>(ButtonConfirm_Click);

            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                TTTrain playerTrain = Owner.Viewer.PlayerTrain as TTTrain;
                reqTrain = playerTrain as TTTrain;

                if (reqTrain.RequestStopMessages.Count >= 1)
                {
                    ReqStop1.Text = reqTrain.RequestStopMessages[0];
                }
                if (reqTrain.RequestStopMessages.Count >= 2)
                {
                    ReqStop2.Text = reqTrain.RequestStopMessages[1];
                }
                if (reqTrain.RequestStopMessages.Count >= 3)
                {
                    ReqStop3.Text = reqTrain.RequestStopMessages[2];
                }
                if (reqTrain.RequestStopMessages.Count >= 4)
                {
                    ReqStop4.Text = reqTrain.RequestStopMessages[3];
                }
                if (reqTrain.RequestStopMessages.Count >= 5)
                {
                    ReqStop5.Text = reqTrain.RequestStopMessages[4];
                }
            }
        }

        void ButtonConfirm_Click(Control arg1, Point arg2)
        {
            this.Visible = false;
            TTTrain playerTrain = Owner.Viewer.PlayerTrain as TTTrain;
            reqTrain = playerTrain as TTTrain;
            reqTrain.RequestStopMessages.Clear();
        }

    }
}
