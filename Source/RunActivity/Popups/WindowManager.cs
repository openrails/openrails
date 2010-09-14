/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: Laurie Heath
/// Author: James Ross

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ORTS.Popups
{
	public class WindowManager
	{
		public const SpriteBlendMode BeginSpriteBlendMode = SpriteBlendMode.AlphaBlend;
		public const SpriteSortMode BeginSpriteSortMode = SpriteSortMode.Immediate;
		public const SaveStateMode BeginSaveStateMode = SaveStateMode.SaveState;

		public static Texture2D WhiteTexture;
		public static Texture2D ScrollbarTexture;

		public readonly Viewer3D Viewer;
		readonly List<Window> Windows = new List<Window>();
		readonly SpriteBatch SpriteBatch;
		Matrix XNAView = Matrix.Identity;
		Matrix XNAProjection = Matrix.Identity;
		internal Point ScreenSize = Point.Zero;
		ResolveTexture2D Screen;

		public WindowManager(Viewer3D viewer)
		{
			Viewer = viewer;
			SpriteBatch = new SpriteBatch(viewer.GraphicsDevice);
			ScreenSize = new Point(viewer.GraphicsDevice.PresentationParameters.BackBufferWidth, viewer.GraphicsDevice.PresentationParameters.BackBufferHeight);

			if (WhiteTexture == null)
			{
				WhiteTexture = new Texture2D(viewer.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
				WhiteTexture.SetData(new[] { Color.White });
			}
			if (ScrollbarTexture == null)
				ScrollbarTexture = viewer.RenderProcess.Content.Load<Texture2D>("WindowScrollbar");
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
			if (Viewer.SettingsBool[(int)BoolSettings.WindowGlass])
			{
				// Buffer for screen texture, also same size as viewport and using the backbuffer format.
				if (Screen == null)
					Screen = new ResolveTexture2D(graphicsDevice, ScreenSize.X, ScreenSize.Y, 1, graphicsDevice.PresentationParameters.BackBufferFormat);

                foreach (var window in VisibleWindows)
                {
                    var xnaWorld = window.XNAWorld;

                    graphicsDevice.ResolveBackBuffer(Screen);
                    material.SetState(graphicsDevice, Screen);
                    material.Render(graphicsDevice, window, ref xnaWorld, ref XNAView, ref XNAProjection);
                    material.ResetState(graphicsDevice);

                    SpriteBatch.Begin(BeginSpriteBlendMode, BeginSpriteSortMode, BeginSaveStateMode);
                    window.Draw(SpriteBatch);
                    SpriteBatch.End();
                }
			}
			else
			{
                foreach (var window in VisibleWindows)
                {
                    var xnaWorld = window.XNAWorld;

                    material.SetState(graphicsDevice, Screen);
                    material.Render(graphicsDevice, window, ref xnaWorld, ref XNAView, ref XNAProjection);
                    material.ResetState(graphicsDevice);

                    SpriteBatch.Begin(BeginSpriteBlendMode, BeginSpriteSortMode, BeginSaveStateMode);
                    window.Draw(SpriteBatch);
                    SpriteBatch.End();
                }
			}
		}

		internal void Add(Window window)
		{
			Windows.Add(window);
		}

		public bool HasVisiblePopupWindows()
		{
			return Windows.Any(w => w.Visible);
		}

		public IEnumerable<Window> VisibleWindows
		{
			get
			{
				return Windows.Where(w => w.Visible);
			}
		}

		public const int DragMinimumDistance = 2;

		Point mouseDownPosition;
		public Point MouseDownPosition { get { return mouseDownPosition; } }

		Window mouseActiveWindow;
		public Window MouseActiveWindow { get { return mouseActiveWindow; } }

		double LastUpdateTime;
		public void HandleUserInput()
		{
			if (UserInput.IsMouseLeftButtonPressed())
			{
				mouseDownPosition = new Point(UserInput.MouseState.X, UserInput.MouseState.Y);
				mouseActiveWindow = VisibleWindows.LastOrDefault(w => w.Location.Contains(mouseDownPosition));
				if (mouseActiveWindow != null)
				{
					Windows.Remove(mouseActiveWindow);
					Windows.Add(mouseActiveWindow);
				}
			}

			if (mouseActiveWindow != null)
			{
				if (UserInput.IsMouseLeftButtonPressed())
					mouseActiveWindow.MouseDown();
				else if (UserInput.IsMouseLeftButtonReleased())
					mouseActiveWindow.MouseUp();

				if (UserInput.IsMouseMoved())
					mouseActiveWindow.MouseMove();

				if (Program.RealTime - LastUpdateTime >= 0.1)
				{
					LastUpdateTime = Program.RealTime;
					mouseActiveWindow.HandleUserInput();
				}

				if (UserInput.IsMouseLeftButtonReleased())
					mouseActiveWindow = null;
			}
		}
	}
}
