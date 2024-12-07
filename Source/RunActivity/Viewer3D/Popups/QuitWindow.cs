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
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{
    public class QuitWindow : Window
    {
        public QuitWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 12, Window.DecorationSize.Y + owner.TextFontDefault.Height * 6, Viewer.Catalog.GetString("Pause Menu"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonQuit, buttonSave, buttonContinue;
            var vbox = base.Layout(layout).AddLayoutVertical();
            var heightForLabels = 10;
			if (!Orts.MultiPlayer.MPManager.IsMultiPlayer())
				heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 3;
			else heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 2;
            var spacing = (heightForLabels - Owner.TextFontDefault.Height) / 2;
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonQuit = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Quit {1} ({0})", Owner.Viewer.Settings.Input.Commands[(int)UserCommand.GameQuit], Application.ProductName), LabelAlignment.Center));
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
			if (!Orts.MultiPlayer.MPManager.IsMultiPlayer())
			{
                buttonSave = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Save your game ({0})", Owner.Viewer.Settings.Input.Commands[(int)UserCommand.GameSave]), LabelAlignment.Center);
				vbox.AddSpace(0, spacing);
				vbox.Add(buttonSave);
				vbox.AddSpace(0, spacing);
				vbox.AddHorizontalSeparator();
				buttonSave.Click += new Action<Control, Point>(buttonSave_Click);
			}
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonContinue = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Continue playing ({0})", Owner.Viewer.Settings.Input.Commands[(int)UserCommand.GamePauseMenu]), LabelAlignment.Center));
            buttonQuit.Click += new Action<Control, Point>(buttonQuit_Click);
            buttonContinue.Click += new Action<Control, Point>(buttonContinue_Click);
            return vbox;
        }

        void buttonQuit_Click(Control arg1, Point arg2)
        {
            Owner.Viewer.Game.PopState();
        }

        void buttonSave_Click(Control arg1, Point arg2)
        {
            GameStateRunActivity.Save();
            Owner.Viewer.AutoSaveDueAt = Owner.Viewer.RealTime + 60 * Owner.Viewer.Simulator.Settings.AutoSaveInterval;
        }

        void buttonContinue_Click(Control arg1, Point arg2)
        {
            Visible = Owner.Viewer.Simulator.Paused = false;
            if( Owner.Viewer.Log.PauseState == ReplayPauseState.During ) {
                Owner.Viewer.Log.PauseState = ReplayPauseState.Done;
            }
            Owner.Viewer.ResumeReplaying();
        }
    }
}
