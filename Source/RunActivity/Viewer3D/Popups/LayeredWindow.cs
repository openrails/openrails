// COPYRIGHT 2010, 2011 by the Open Rails project.
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

using Microsoft.Xna.Framework.Graphics;

namespace Orts.Viewer3D.Popups
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
