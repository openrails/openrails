/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: James Ross
/// 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
	public class CompassWindow : Window
	{
		PopupCompass Compass;
		Label Latitude;
		Label Longitude;

		public CompassWindow(WindowManager owner)
			: base(owner, 250, 95, "Compass")
		{
			AlignTop();
			AlignCenterH();
		}

		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			vbox.Add(Compass = new PopupCompass(vbox.RemainingWidth, 50));
			{
				var hbox = vbox.AddLayoutHorizontal(16);
				var w = hbox.RemainingWidth / 9;
				hbox.Add(new Label(1 * w, hbox.RemainingHeight, "Lat:", LabelAlignment.Right));
				hbox.Add(Latitude = new Label(3 * w, hbox.RemainingHeight, "000.000000", LabelAlignment.Right));
				hbox.AddSpace(w, hbox.RemainingHeight);
				hbox.Add(new Label(1 * w, hbox.RemainingHeight, "Lon:", LabelAlignment.Right));
				hbox.Add(Longitude = new Label(3 * w, hbox.RemainingHeight, "000.000000", LabelAlignment.Right));
			}
			return vbox;
		}

		public void Update(float heading)
		{
			Compass.Heading = MathHelper.ToDegrees(heading);
		}

		public void UpdateText(float latitude, float longitude)
		{
			Latitude.Text = MathHelper.ToDegrees(latitude).ToString("F6");
			Longitude.Text = MathHelper.ToDegrees(longitude).ToString("F6");
		}
	}

	public class PopupCompass : Control
	{
		static Texture2D CompassTexture;
		static int[] HeadingHalfWidths;
		public float Heading;

		public PopupCompass(int width, int height)
			: base(0, 0, width, height)
		{
		}

		internal override void Draw(SpriteBatch spriteBatch, Point offset)
		{
			if (CompassTexture == null)
			{
				CompassTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
				CompassTexture.SetData(new[] { Color.White });
			}
			if (HeadingHalfWidths == null)
			{
				HeadingHalfWidths = new int[12];
				for (var i = 0; i < 12; i++)
					HeadingHalfWidths[i] = (int)(Materials.PopupWindowMaterial.DefaultFont.MeasureString((i * 30).ToString()).X / 2);
			}
			const int headingScale = 2;
			var height = (int)((Position.Height - 16) / 3);
			for (float heading = 0; heading < 360; heading += 10)
			{
				var x = Position.Width / 2 + (int)(((heading - Heading + 360 + 180) % 360 - 180) * headingScale);
				if ((x >= 0) && (x < Position.Width))
				{
					if (heading % 30 == 0)
					{
						var textHalfWidth = HeadingHalfWidths[(int)heading / 30];
						if ((x - textHalfWidth >= 0) && (x + textHalfWidth < Position.Width))
							spriteBatch.DrawString(Materials.PopupWindowMaterial.DefaultFont, heading.ToString(), new Vector2(offset.X + Position.X + x - textHalfWidth, offset.Y + Position.Y), Color.White);
						spriteBatch.Draw(CompassTexture, new Rectangle(offset.X + Position.X + x, offset.Y + Position.Y + 16, 1, height * 2), Color.White);
					}
					else
					{
						spriteBatch.Draw(CompassTexture, new Rectangle(offset.X + Position.X + x, offset.Y + Position.Y + 16, 1, height), Color.White);
					}
				}
			}
			spriteBatch.Draw(CompassTexture, new Rectangle(offset.X + Position.X + Position.Width / 2, offset.Y + Position.Bottom - height, 1, height), Color.White);
		}
	}
}
