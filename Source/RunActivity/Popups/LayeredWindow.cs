// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
	public abstract class LayeredWindow : Window
	{
		public LayeredWindow(WindowManager owner, int width, int height, string caption)
			: base(owner, width, height, caption)
		{
		}

		protected override ControlLayout Layout(ControlLayout layout)
		{
			return layout;
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			// Don't draw the normal window stuff here.
		}
	}
}
