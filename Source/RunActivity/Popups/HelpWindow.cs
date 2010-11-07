/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: James Ross
/// 
/// Help; used to display the keyboard shortcuts and other help.
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Popups
{
	public class HelpWindow : Window
	{
		public HelpWindow(WindowManager owner)
			: base(owner, 600, 450, "Help")
		{
			AlignCenter();
		}

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var hbox = base.Layout(layout).AddLayoutHorizontal();
			var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
			scrollbox.Add(new Label(scrollbox.RemainingWidth, 16, "Keyboard Shortcuts", LabelAlignment.Center));
			scrollbox.AddHorizontalSeparator();
			foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
			{
				var line = scrollbox.AddLayoutHorizontal(16);
				var width = line.RemainingWidth / 2;
				line.Add(new Label(width, line.RemainingHeight, UserInput.FormatCommandName(command)));
				line.Add(new Label(width, line.RemainingHeight, UserInput.Commands[(int)command].ToString()));
			}
			return hbox;
		}
	}
}
