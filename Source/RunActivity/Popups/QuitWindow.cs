// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;

namespace ORTS.Popups
{
    public class QuitWindow : Window
    {
        public QuitWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 12, Window.DecorationSize.Y + owner.TextFontDefault.Height * 6, "Pause Menu")
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonQuit, buttonSave, buttonContinue;
            var vbox = base.Layout(layout).AddLayoutVertical();
            var heightForLabels = 10;
			if (!MultiPlayer.MPManager.IsMultiPlayer())
				heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 3;
			else heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 2;
            var spacing = (heightForLabels - Owner.TextFontDefault.Height) / 2;
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonQuit = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, String.Format("Quit {1} ({0})", InputSettings.Commands[(int)UserCommands.GameQuit], Application.ProductName), LabelAlignment.Center));
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
			if (!MultiPlayer.MPManager.IsMultiPlayer())
			{
				buttonSave = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, String.Format("Save your game ({0})", InputSettings.Commands[(int)UserCommands.GameSave]), LabelAlignment.Center);
				vbox.AddSpace(0, spacing);
				vbox.Add(buttonSave);
				vbox.AddSpace(0, spacing);
				vbox.AddHorizontalSeparator();
				buttonSave.Click += new Action<Control, Point>(buttonSave_Click);
			}
            vbox.AddSpace(0, spacing);
            vbox.Add(buttonContinue = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, String.Format("Continue playing ({0})", InputSettings.Commands[(int)UserCommands.GamePauseMenu]), LabelAlignment.Center));
            buttonQuit.Click += new Action<Control, Point>(buttonQuit_Click);
            buttonContinue.Click += new Action<Control, Point>(buttonContinue_Click);
            return vbox;
        }

        void buttonQuit_Click(Control arg1, Point arg2)
        {
            Owner.Viewer.Stop();
        }

        void buttonSave_Click(Control arg1, Point arg2)
        {
            Program.Save();
        }

        void buttonContinue_Click(Control arg1, Point arg2)
        {
            Visible = Owner.Viewer.Simulator.Paused = false;
        }
    }
}
