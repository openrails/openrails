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
		readonly SpriteBatch SpriteBatch;
		Matrix XNAView = Matrix.Identity;
		Matrix XNAProjection = Matrix.Identity;
		internal Point ScreenSize = Point.Zero;
		ResolveTexture2D Screen;

		public PopupWindows(Viewer3D viewer)
		{
			Viewer = viewer;
			SpriteBatch = new SpriteBatch(viewer.GraphicsDevice);
			ScreenSize = new Point(viewer.GraphicsDevice.PresentationParameters.BackBufferWidth, viewer.GraphicsDevice.PresentationParameters.BackBufferHeight);
		}

		public void Draw(GraphicsDevice graphicsDevice)
		{
			if ((ScreenSize.X != graphicsDevice.PresentationParameters.BackBufferWidth) || (ScreenSize.Y != graphicsDevice.PresentationParameters.BackBufferHeight))
			{
				var oldScreenSize = ScreenSize;
				ScreenSize.X = graphicsDevice.PresentationParameters.BackBufferWidth;
				ScreenSize.Y = graphicsDevice.PresentationParameters.BackBufferHeight;

				// Reset the screen buffer for glass rendering if necessary.
				if (Screen != null)
					Screen.Dispose();
				Screen = null;

				// Reposition all the windows.
				foreach (var window in Windows)
					window.MoveTo((ScreenSize.X - window.Location.Width) * window.Location.X / (oldScreenSize.X - window.Location.Width), (ScreenSize.Y - window.Location.Height) * window.Location.Y / (oldScreenSize.Y - window.Location.Height));
			}

			// Nothing visible? Nothing more to do!
			if (Windows.All(w => !w.Visible))
				return;

			// Construct a view where (0, 0) is the top-left and (width, height) is
			// bottom-right, so that popups can act more like normal window things.
			XNAView = Matrix.CreateTranslation(-ScreenSize.X / 2, -ScreenSize.Y / 2, 0) *
				Matrix.CreateTranslation(-0.5f, -0.5f, 0) *
				Matrix.CreateScale(1, -1, 1);
			// Project into a flat view of the same size as the viewport.
			XNAProjection = Matrix.CreateOrthographic(ScreenSize.X, ScreenSize.Y, 0, 100);

			var material = Materials.PopupWindowMaterial;
			if (Viewer.WindowGlass)
			{
				// Buffer for screen texture, also same size as viewport and using the backbuffer format.
				if (Screen == null)
					Screen = new ResolveTexture2D(graphicsDevice, ScreenSize.X, ScreenSize.Y, 1, graphicsDevice.PresentationParameters.BackBufferFormat);

				foreach (var window in VisibleWindows)
				{
					var xnaWorld = window.XNAWorld;

					graphicsDevice.ResolveBackBuffer(Screen);
					material.SetState(graphicsDevice, Screen);
					material.Render(graphicsDevice, null, window, ref xnaWorld, ref XNAView, ref XNAProjection);
					material.ResetState(graphicsDevice, null);

					SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
					window.Draw(SpriteBatch);
					SpriteBatch.End();
				}
			}
			else
			{
				foreach (var window in VisibleWindows)
				{
					var xnaWorld = window.XNAWorld;

					material.SetState(graphicsDevice, null);
					material.Render(graphicsDevice, null, window, ref xnaWorld, ref XNAView, ref XNAProjection);
					material.ResetState(graphicsDevice, null);

					SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
					window.Draw(SpriteBatch);
					SpriteBatch.End();
				}
			}
		}

		internal void Add(PopupWindow window)
		{
			Windows.Add(window);
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

		bool Dragging;
		PopupWindow DragWindow;
		Point DragStart;
		public void HandleMouseMovement(MouseState mouseState)
		{
			if (!Dragging && (mouseState.LeftButton == ButtonState.Pressed))
			{
				var mousePoint = new Point(mouseState.X, mouseState.Y);
				Dragging = true;
				DragWindow = VisibleWindows.Reverse().FirstOrDefault(w => w.Location.Contains(mousePoint));
				if (DragWindow != null)
				{
					DragStart = new Point(mouseState.X - DragWindow.Location.X, mouseState.Y - DragWindow.Location.Y);
					Windows.Remove(DragWindow);
					Windows.Add(DragWindow);
				}
			}
			else if (Dragging && (mouseState.LeftButton == ButtonState.Released))
			{
				Dragging = false;
				DragWindow = null;
			}
			else if (Dragging && (DragWindow != null) && (mouseState.LeftButton == ButtonState.Pressed))
			{
				DragWindow.MoveTo(mouseState.X - DragStart.X, mouseState.Y - DragStart.Y);
			}
		}
	}

	public abstract class PopupWindow : RenderPrimitive
	{
		public Matrix XNAWorld;
		PopupWindows Owner;
		bool visible = false;
		Rectangle location = new Rectangle(0, 0, 100, 100);
		string Caption;
		PopupControlLayout PopupWindowLayout;
		VertexBuffer WindowVertexBuffer;
		IndexBuffer WindowIndexBuffer;

		public PopupWindow(PopupWindows owner, int width, int height, string caption)
		{
			Owner = owner;
			location = new Rectangle(0, 0, width, height);
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
			Layout();
			WindowVertexBuffer = null;
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
			x = (int)MathHelper.Clamp(x, 0, Owner.ScreenSize.X - location.Width);
			y = (int)MathHelper.Clamp(y, 0, Owner.ScreenSize.Y - location.Height);

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
			MoveTo(location.X, Owner.ScreenSize.Y);
		}

		public void AlignLeft()
		{
			MoveTo(0, location.Y);
		}

		public void AlignRight()
		{
			MoveTo(Owner.ScreenSize.X, location.Y);
		}

		public void AlignCenterV()
		{
			MoveTo(location.X, (Owner.ScreenSize.Y - location.Height) / 2);
		}

		public void AlignCenterH()
		{
			MoveTo((Owner.ScreenSize.X - location.Width) / 2, location.Y);
		}

		public void AlignCenter()
		{
			MoveTo((Owner.ScreenSize.X - location.Width) / 2, (Owner.ScreenSize.Y - location.Height) / 2);
		}

		protected void Layout()
		{
			PopupWindowLayout = new PopupControlLayout(0, 0, location.Width, location.Height);
			Layout(PopupWindowLayout);
		}

		protected virtual PopupControlLayout Layout(PopupControlLayout layout)
		{
			// Pad window by 4px, add caption and space between to content area.
			var content = layout.AddLayoutOffset(4, 4, 4, 4).AddLayoutVertical();
			content.Add(new PopupLabel(content.RemainingWidth, 16, Caption, PopupLabelAlignment.Center));
			content.AddSpace(0, 5);
			return content;
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			if (WindowVertexBuffer == null)
			{
				// Edges/corners are 32px (1/4th image size).
				var vertexData = new[] {
					//  0  1  2  3
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 0 * location.Height + 00, 0), new Vector2(0.00f, 0.00f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + 32, 0 * location.Height + 00, 0), new Vector2(0.25f, 0.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 32, 0 * location.Height + 00, 0), new Vector2(0.75f, 0.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 0 * location.Height + 00, 0), new Vector2(1.00f, 0.00f)),
					//  4  5  6  7
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 0 * location.Height + 32, 0), new Vector2(0.00f, 0.25f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + 32, 0 * location.Height + 32, 0), new Vector2(0.25f, 0.25f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 32, 0 * location.Height + 32, 0), new Vector2(0.75f, 0.25f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 0 * location.Height + 32, 0), new Vector2(1.00f, 0.25f)),
					//  8  9 10 11
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 1 * location.Height - 32, 0), new Vector2(0.00f, 0.75f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + 32, 1 * location.Height - 32, 0), new Vector2(0.25f, 0.75f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 32, 1 * location.Height - 32, 0), new Vector2(0.75f, 0.75f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 1 * location.Height - 32, 0), new Vector2(1.00f, 0.75f)),
					// 12 13 14 15
					new VertexPositionTexture(new Vector3(0 * location.Width + 00, 1 * location.Height - 00, 0), new Vector2(0.00f, 1.00f)),
					new VertexPositionTexture(new Vector3(0 * location.Width + 32, 1 * location.Height - 00, 0), new Vector2(0.25f, 1.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 32, 1 * location.Height - 00, 0), new Vector2(0.75f, 1.00f)),
					new VertexPositionTexture(new Vector3(1 * location.Width - 00, 1 * location.Height - 00, 0), new Vector2(1.00f, 1.00f)),
				};
				WindowVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), vertexData.Length, BufferUsage.WriteOnly);
				WindowVertexBuffer.SetData(vertexData);
			}
			if (WindowIndexBuffer == null)
			{
				var indexData = new int[] {
					0, 4, 1, 5, 2, 6, 3, 7,
					4, 8, 5, 9, 6, 10, 7, 11,
					8, 12, 9, 13, 10, 14, 11, 15,
				};
				WindowIndexBuffer = new IndexBuffer(graphicsDevice, typeof(int), indexData.Length, BufferUsage.WriteOnly);
				WindowIndexBuffer.SetData(indexData);
			}

			graphicsDevice.VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionTexture.VertexElements);
			graphicsDevice.Vertices[0].SetSource(WindowVertexBuffer, 0, VertexPositionTexture.SizeInBytes);
			graphicsDevice.Indices = WindowIndexBuffer;
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 16, 0, 6);
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 16, 8, 6);
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 16, 16, 6);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			PopupWindowLayout.Draw(spriteBatch, Location.Location);
		}

		////////////////////////////////////////////////////////////////////////

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
			: base(owner, text.Length * 10 + 50, 150, "")
		{
			this.AlignCenter();
			tbText = new TextBox(25, 100, f);
			tbText.text = text;
			//this.AddTextbox(tbText);           
		}
	}
}
