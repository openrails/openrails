// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

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
