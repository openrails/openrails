/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Autor Laurie Heath

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ORTS
{
	public class PopupWindows
	{
		public readonly Viewer3D Viewer;
		readonly List<PopupWindow> Windows = new List<PopupWindow>();
		readonly PopupWindowsScreen PopupWindowScreen = new PopupWindowsScreen();
		readonly SpriteBatch SpriteBatch;
		Matrix XNAView = new Matrix();
		Matrix XNAProjection = new Matrix();
		PopupWindow activeWindow = null;

		public PopupWindows(Viewer3D viewer)
		{
			Viewer = viewer;
			SpriteBatch = new SpriteBatch(viewer.GraphicsDevice);
		}

		public void Draw(GraphicsDevice graphicsDevice)
		{
			if (Windows.All(w => !w.Visible))
				return;

			// Construct a view where (0, 0) is the top-left and (width, height) is
			// bottom-right, so that popups can act more like normal window things.
			XNAView = Matrix.CreateTranslation(-graphicsDevice.Viewport.Width / 2, -graphicsDevice.Viewport.Height / 2, 0) *
				Matrix.CreateTranslation(-0.5f, -0.5f, 0) *
				Matrix.CreateScale(1, -1, 1);
			// Project into a flat view of the same size as the viewpoer.
			XNAProjection = Matrix.CreateOrthographic(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 100);
			// Buffer for screen texture, also same size as viewport and using the backbuffer format.
			var screen = new ResolveTexture2D(graphicsDevice, graphicsDevice.PresentationParameters.BackBufferWidth, graphicsDevice.PresentationParameters.BackBufferHeight, 1, graphicsDevice.PresentationParameters.BackBufferFormat);
			graphicsDevice.ResolveBackBuffer(screen);

			PopupWindowScreen.PrepareFrame(graphicsDevice);

			var material = Materials.PopupWindowMaterial;
			material.SetState(graphicsDevice, screen);
			material.Render(graphicsDevice, null, PopupWindowScreen, ref PopupWindowScreen.XNAWorld, ref XNAView, ref XNAProjection);
			foreach (PopupWindow window in VisibleWindows)
			{
				var xnaWorld = window.XNAWorld;
				material.Render(graphicsDevice, null, window, ref xnaWorld, ref XNAView, ref XNAProjection);
			}
			material.ResetState(graphicsDevice, null);

			SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
			foreach (PopupWindow window in VisibleWindows)
				window.Draw(SpriteBatch);
			SpriteBatch.End();
		}

		internal void Add(PopupWindow window)
		{
			Windows.Add(window);
		}

		public void SelectWindow(int x, int y)
		{
			foreach (PopupWindow window in Windows)
			{
				if (window.Visible)
				{
					if (window.Location.Contains(x, y))
					{
						if (!window.isCloseClicked(x, y))
						{
							activeWindow = window;
						}
						return;
					}
				}
			}
		}

		public void DelselectWindow()
		{
			if (activeWindow != null)
			{
				activeWindow.ActiveChanged();
				activeWindow = null;
			}
		}

		public PopupWindow ActiveWindow
		{
			get
			{
				return activeWindow;
			}
		}

		public void MoveWindow(int dx, int dy)
		{
			if (ActiveWindow != null)
			{
				if (ActiveWindow.Selected) ActiveWindow.MoveBy(dx, dy);
			}
		}

		public void PopupMessage(string text, SpriteFont f)
		{
			PopupMessage popMsgbox;

			popMsgbox = new PopupMessage(this, text, f, 5.0);
			// Add(popMsgbox);
		}

		public bool HasVisiblePopupWindows()
		{
			return Windows.Any(w => w.Visible);
		}

		public IEnumerable<PopupWindow> VisibleWindows
		{
			get
			{
				return Windows.Where(w => w.Visible);
			}
		}
	}

	public class PopupWindowsScreen : RenderPrimitive
	{
		public Matrix XNAWorld = Matrix.CreateWorld(new Vector3(0, 0, 0), -Vector3.UnitZ, Vector3.UnitY);

		public void PrepareFrame(GraphicsDevice graphicsDevice)
		{
			XNAWorld = Matrix.CreateWorld(new Vector3(0, 0, 0), -Vector3.UnitZ, Vector3.UnitY);
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionTexture.VertexElements);
			graphicsDevice.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleFan,
				new[] {
					new VertexPositionTexture(new Vector3(0, graphicsDevice.Viewport.Height, 0), new Vector2(0, 0)),
					new VertexPositionTexture(new Vector3(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0), new Vector2(0, 0)),
					new VertexPositionTexture(new Vector3(graphicsDevice.Viewport.Width, 0, 0), new Vector2(0, 0)),
					new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
				}, 0, 2);
		}
	}

	public abstract class PopupWindow : RenderPrimitive
	{
		public Matrix XNAWorld;
		PopupWindows Owner;
		bool visible = false;
		Rectangle location = new Rectangle(0, 0, 100, 100);
		string Caption;

		//private Texture2D backgroundTexture;
		//private Texture2D closeTexture;
		//private Color[] backgroundColours;
		//private bool isVisible = false;
		//private bool isDown = false;
		//private bool isGraphics = false;
		//private bool isSelected = false;
		//private SD.Graphics GR;
		//private SD.Bitmap bmpBackground;
		//private List<TextBox> textBoxes = new List<TextBox>();  // List of text fields

		//private int spriteX, spriteY, spriteW, spriteH;   //  coordinates relative to main display.

		public PopupWindow(PopupWindows owner, string caption)
		{
			Owner = owner;
			Caption = caption;
			Owner.Add(this);
			VisibilityChanged();
			LocationChanged();
			SizeChanged();
		}

		protected virtual void VisibilityChanged()
		{
		}

		protected virtual void LocationChanged()
		{
			XNAWorld = Matrix.CreateWorld(new Vector3(location.X, location.Y, 0), -Vector3.UnitZ, Vector3.UnitY);
		}

		protected virtual void SizeChanged()
		{
		}

		internal virtual void ActiveChanged()
		{
		}

		public bool Visible
		{
			get
			{
				return visible;
			}
			set
			{
				if (visible != value)
				{
					visible = value;
					VisibilityChanged();
				}
			}
		}

		public Rectangle Location
		{
			get
			{
				return location;
			}
		}

		public void MoveTo(int x, int y)
		{
			x = (int)MathHelper.Clamp(x, 0, Owner.Viewer.GraphicsDevice.Viewport.Width - location.Width);
			y = (int)MathHelper.Clamp(y, 0, Owner.Viewer.GraphicsDevice.Viewport.Height - location.Height);

			if ((location.X != x) || (location.Y != y))
			{
				location.X = x;
				location.Y = y;
				LocationChanged();
			}
		}

		public void MoveBy(int dx, int dy)
		{
			MoveTo(location.X + dx, location.Y + dy);
		}

		public void SizeTo(int width, int height)
		{
			if ((location.Width != width) || (location.Height != height))
			{
				location.Width = width;
				location.Height = height;
				MoveTo(location.X, location.Y);
				SizeChanged();
			}
		}

		public void AlignTop()
		{
			MoveTo(location.X, 0);
		}

		public void AlignBottom()
		{
			MoveTo(location.X, Owner.Viewer.GraphicsDevice.Viewport.Height);
		}

		public void AlignLeft()
		{
			MoveTo(0, location.Y);
		}

		public void AlignRight()
		{
			MoveTo(Owner.Viewer.GraphicsDevice.Viewport.Width, location.Y);
		}

		public void AlignCenterV()
		{
			MoveTo(location.X, (Owner.Viewer.GraphicsDevice.Viewport.Height - location.Height) / 2);
		}

		public void AlignCenterH()
		{
			MoveTo((Owner.Viewer.GraphicsDevice.Viewport.Width - location.Width) / 2, location.Y);
		}

		public void AlignCenter()
		{
			MoveTo((Owner.Viewer.GraphicsDevice.Viewport.Width - location.Width) / 2, (Owner.Viewer.GraphicsDevice.Viewport.Height - location.Height) / 2);
		}

		public bool Selected
		{
			get
			{
				return Owner.ActiveWindow == this;
			}
		}

		////////////////////////////////////////////////////////////////////////

		public virtual void Initialize(PopupWindows popupWindows)
		{
			//graphicsDevice = popupWindows.Viewer.GraphicsDevice;
			//backgroundColours = new Color[spriteW * spriteH];
			//backgroundTexture = new Texture2D(graphicsDevice, spriteW, spriteH, 1, TextureUsage.None, SurfaceFormat.Color);

			//for (int i = 0; i < backgroundColours.Length; i++)
			//{
			//    backgroundColours[i] = Color.TransparentBlack;
			//}
			//backgroundTexture.SetData(backgroundColours);
			//CreateCloseIcon(graphicsDevice);
		}

		//
		//      Creates a close icon for the window in the top right hand corner
		//
		//public void CreateCloseIcon(GraphicsDevice device)
		//{
		//    int w = 12;
		//    int h = 12;
		//    Color[] data = new Color[w * h];
		//    int[,] icondata = new int[,]
		//    {
		//         {1,1,1,1,1,1,1,1,1,1,1,1},
		//         {1,0,0,0,0,0,0,0,0,0,0,1},
		//         {1,0,2,0,0,0,0,0,0,2,0,1},
		//         {1,0,0,2,0,0,0,0,2,0,0,1},
		//         {1,0,0,0,2,0,0,2,0,0,0,1},
		//         {1,0,0,0,0,2,2,0,0,0,0,1},
		//         {1,0,0,0,0,2,2,0,0,0,0,1},
		//         {1,0,0,0,2,0,0,2,0,0,0,1},
		//         {1,0,0,2,0,0,0,0,2,0,0,1},
		//         {1,0,2,0,0,0,0,0,0,2,0,1},
		//         {1,0,0,0,0,0,0,0,0,0,0,1},
		//         {1,1,1,1,1,1,1,1,1,1,1,1},
		//    };

		//    int i = 0;
		//    for (int x = 0; x < w; x++)
		//    {
		//        for (int y = 0; y < h; y++)
		//        {
		//            switch (icondata[x, y])
		//            {
		//                case 1:
		//                    data[i] = Color.Gray;
		//                    break;
		//                case 2:
		//                    data[i] = Color.Red;
		//                    break;

		//                default:
		//                    data[i] = Color.TransparentBlack;
		//                    break;
		//            }
		//            i++;
		//        }
		//    }

		//    closeTexture = new Texture2D(device, w, h, 1, TextureUsage.None, SurfaceFormat.Color);
		//    closeTexture.SetData(data);

		//}

		//
		//  This method is invoked if bitmap is used to display the information
		//
		//private void SetupGrahics()
		//{
		//    bmpBackground = new SD.Bitmap(spriteW, spriteH);
		//    GR = SD.Graphics.FromImage(bmpBackground);
		//    GR.Clear(SD.Color.FromArgb(0, 0, 0, 0));
		//}

		//
		//  This method copies the bitmap to the texture
		//  Indebted to Florian Block for this code snippet.
		//
		//public void UpdateGraphics()
		//{
		//    SDI.BitmapData bmpData = bmpBackground.LockBits(new System.Drawing.Rectangle(0, 0, bmpBackground.Width, bmpBackground.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpBackground.PixelFormat);
		//    int bufferSize = bmpData.Height * bmpData.Stride;
		//    byte[] texBytes = new byte[bufferSize];
		//    Marshal.Copy(bmpData.Scan0, texBytes, 0, texBytes.Length);
		//    backgroundTexture.SetData<Byte>(texBytes);
		//    bmpBackground.UnlockBits(bmpData);
		//}

		//public void UseGraphics()
		//{
		//    SetupGrahics();
		//    UpdateGraphics();
		//    isGraphics = true;
		//}

		//
		//  Make winow invisible if close icon clicked
		//
		public bool isCloseClicked(int x, int y)
		{
			//if ((x > (spriteX + spriteW - closeTexture.Width)) && (x < (spriteX + spriteW)))
			//{
			//    if ((y > spriteY) && (y < (spriteY + closeTexture.Height)))
			//    {
			//        isVisible = false;
			//        return true;
			//    }
			//}
			return false;
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			// Edges/corners are 16px (1/8th image size).
			var vertexData = new[] {
				//  0  1  2  3
				new VertexPositionTexture(new Vector3(0 * location.Width + 00, 1 * location.Height - 00, 0), new Vector2(0.000f, 0.000f)),
				new VertexPositionTexture(new Vector3(0 * location.Width + 16, 1 * location.Height - 00, 0), new Vector2(0.000f, 0.125f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 16, 1 * location.Height - 00, 0), new Vector2(0.000f, 0.875f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 00, 1 * location.Height - 00, 0), new Vector2(0.000f, 1.000f)),
				//  4  5  6  7
				new VertexPositionTexture(new Vector3(0 * location.Width + 00, 1 * location.Height - 16, 0), new Vector2(0.125f, 0.000f)),
				new VertexPositionTexture(new Vector3(0 * location.Width + 16, 1 * location.Height - 16, 0), new Vector2(0.125f, 0.125f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 16, 1 * location.Height - 16, 0), new Vector2(0.125f, 0.875f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 00, 1 * location.Height - 16, 0), new Vector2(0.125f, 1.000f)),
				//  8  9 10 11
				new VertexPositionTexture(new Vector3(0 * location.Width + 00, 0 * location.Height + 16, 0), new Vector2(0.875f, 0.000f)),
				new VertexPositionTexture(new Vector3(0 * location.Width + 16, 0 * location.Height + 16, 0), new Vector2(0.875f, 0.125f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 16, 0 * location.Height + 16, 0), new Vector2(0.875f, 0.875f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 00, 0 * location.Height + 16, 0), new Vector2(0.875f, 1.000f)),
				// 12 13 14 15
				new VertexPositionTexture(new Vector3(0 * location.Width + 00, 0 * location.Height + 00, 0), new Vector2(1.000f, 0.000f)),
				new VertexPositionTexture(new Vector3(0 * location.Width + 16, 0 * location.Height + 00, 0), new Vector2(1.000f, 0.125f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 16, 0 * location.Height + 00, 0), new Vector2(1.000f, 0.875f)),
				new VertexPositionTexture(new Vector3(1 * location.Width - 00, 0 * location.Height + 00, 0), new Vector2(1.000f, 1.000f)),
			};
			var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), vertexData.Length, BufferUsage.WriteOnly);
			vertexBuffer.SetData(vertexData);

			var indexData = new int[] {
				0, 4, 1, 5, 2, 6, 3, 7,
				4, 8, 5, 9, 6, 10, 7, 11,
				8, 12, 9, 13, 10, 14, 11, 15,
			};
			var indexBuffer = new IndexBuffer(graphicsDevice, typeof(int), indexData.Length, BufferUsage.WriteOnly);
			indexBuffer.SetData(indexData);

			graphicsDevice.VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionTexture.VertexElements);
			graphicsDevice.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
			graphicsDevice.Indices = indexBuffer;
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, vertexData.Length, 0, 6);
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, vertexData.Length, 8, 6);
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, vertexData.Length, 16, 6);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.DrawString(Materials.PopupWindowMaterial.DefaultFont, Caption, new Vector2(location.X + 8, location.Y + 8), Color.White);
			//Rectangle rect = new Rectangle(spriteX, spriteY, spriteW, spriteH);
			//spritebatch.Draw(backgroundTexture, rect, Color.White);
			//spritebatch.Draw(closeTexture, new Rectangle(spriteX + spriteW - closeTexture.Width, spriteY, closeTexture.Width, closeTexture.Height), Color.White);
			//if (textBoxes.Count > 0)
			//{
			//    foreach (TextBox tb in textBoxes)
			//    {
			//        tb.Draw(spriteBatch, spriteX, spriteY);
			//    }
			//}
		}

		//public void AddTextbox(TextBox tb)
		//{
		//    textBoxes.Add(tb);
		//}

		//
		//      Sets the background clour for the window
		//
		public Color backgroundColour
		{
			set
			{
				//Color[] colData = new Color[spriteW * spriteH];
				//backgroundTexture.GetData<Color>(colData);
				//for (int i = 0; i < colData.Length; i++)
				//{
				//    colData[i] = value;
				//}
				//backgroundTexture.SetData<Color>(colData);
			}
		}


		//public SD.Graphics puGraphics
		//{
		//    get
		//    {
		//        return GR;
		//    }
		//}

		//public bool Selected
		//{
		//    get
		//    {
		//        return isSelected;
		//    }
		//    set
		//    {
		//        isSelected = value;
		//    }
		//}
	}

	//
	//  Creates a text field within a window 
	//
	public class TextBox
	{
		SpriteFont font;
		int spriteX, spriteY;         // Coordinates relative to the main window
		String spriteText = "";
		Color textColour = Color.White;

		public TextBox(int x, int y, SpriteFont f)
		{
			font = f;
			spriteX = x;
			spriteY = y;
		}

		//
		//  Renders the text field (ox & oy are coordinates of main window.
		//
		public void Draw(SpriteBatch spritebatch, int ox, int oy)
		{
			spritebatch.DrawString(font, spriteText, new Vector2((float)(ox + spriteX), (float)(oy + spriteY)), textColour);
		}

		public string text
		{
			get
			{
				return spriteText;
			}
			set
			{
				spriteText = value;
			}
		}
	}

	//
	//  Creates a message boz (not complete)
	//
	public class PopupMessage : PopupWindow
	{
		TextBox tbText;

		public PopupMessage(PopupWindows owner, string text, SpriteFont f, double displayTime)
			: base(owner, "")
		{
			this.SizeTo(text.Length * 10 + 50, 150);
			this.AlignCenter();
			tbText = new TextBox(25, 100, f);
			tbText.text = text;
			//this.AddTextbox(tbText);           
		}
	}
}
