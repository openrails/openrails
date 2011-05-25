// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

// Add DEBUG_WINDOW_ZORDER to project defines to record window visibility and z-order changes.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ORTS.Popups
{
	public class WindowManager : RenderPrimitive
	{
		public static Texture2D WhiteTexture;
		public static Texture2D ScrollbarTexture;
		public static Texture2D LabelShadowTexture;

        // This is all a bit of a hack, since SpriteBatch does not expose its own internal Flush() method. What we do
        // is draw with a different texture to anything else; the change of texture triggers an internal flush. The
        // texture is initialised to transparent black so although we draw it in a visible area, it will not actually
        // be visible on screen.
        static Texture2D FlushTexture;
        public static void Flush(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(FlushTexture, Vector2.Zero, Color.Black);
        }

		public readonly Viewer3D Viewer;
        public readonly WindowTextManager TextManager;
        public readonly WindowTextFont TextFontDefault;
        public readonly WindowTextFont TextFontDefaultOutlined;

        readonly Material WindowManagerMaterial = new BasicBlendedMaterial("WindowManager");
        readonly List<Window> Windows = new List<Window>();
        List<Window> WindowsZOrder = new List<Window>();
        SpriteBatch SpriteBatch;
        Matrix Identity = Matrix.Identity;
        Matrix XNAView = Matrix.Identity;
		Matrix XNAProjection = Matrix.Identity;
        internal Point ScreenSize = new Point(10000, 10000); // Arbitrary but necessary.
		ResolveTexture2D Screen;

		public WindowManager(Viewer3D viewer)
		{
			Viewer = viewer;
            TextManager = new WindowTextManager();
            TextFontDefault = TextManager.Get("Arial", 9, System.Drawing.FontStyle.Regular);
            TextFontDefaultOutlined = TextManager.Get("Arial", 9, System.Drawing.FontStyle.Regular, 2);
        }

        public void Initialize()
        {
            SpriteBatch = new SpriteBatch(Viewer.GraphicsDevice);

            if (WhiteTexture == null)
            {
                WhiteTexture = new Texture2D(Viewer.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
                WhiteTexture.SetData(new[] { Color.White });
            }
            if (FlushTexture == null)
            {
                FlushTexture = new Texture2D(Viewer.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
                FlushTexture.SetData(new[] { Color.TransparentBlack });
            }
            if (ScrollbarTexture == null)
                ScrollbarTexture = Viewer.RenderProcess.Content.Load<Texture2D>("WindowScrollbar");
            if (LabelShadowTexture == null)
                LabelShadowTexture = Viewer.RenderProcess.Content.Load<Texture2D>("WindowLabelShadow");

            ScreenChanged();
            UpdateTopMost();

            foreach (var window in Windows)
            {
                window.Initialize();
                window.Layout();
            }
        }

        public void Save(BinaryWriter outf)
        {
            foreach (var window in Windows)
                window.Save(outf);
        }

        public void Restore(BinaryReader inf)
        {
            foreach (var window in Windows)
                window.Restore(inf);
        }

		[CallOnThread("Updater")]
		public void ScreenChanged()
		{
			var oldScreenSize = ScreenSize;
			ScreenSize = Viewer.DisplaySize;

			// Buffer for screen texture, also same size as viewport and using the backbuffer format.
			if (Viewer.Settings.WindowGlass)
			{
				if (Screen != null)
					Screen.Dispose();
				Screen = new ResolveTexture2D(Viewer.GraphicsDevice, ScreenSize.X, ScreenSize.Y, 1, Viewer.GraphicsDevice.PresentationParameters.BackBufferFormat);
			}

			// Reposition all the windows.
            foreach (var window in Windows)
            {
                window.MoveTo((ScreenSize.X - window.Location.Width) * window.Location.X / (oldScreenSize.X - window.Location.Width), (ScreenSize.Y - window.Location.Height) * window.Location.Y / (oldScreenSize.Y - window.Location.Height));
                window.ScreenChanged();
            }
		}

        double LastPrepareRealTime;
        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var updateFull = false;
            if (Viewer.RealTime - LastPrepareRealTime >= 0.25)
            {
                updateFull = true;
                LastPrepareRealTime = Viewer.RealTime;
            }

            foreach (var window in VisibleWindows)
                window.PrepareFrame(frame, elapsedTime, updateFull);

            frame.AddPrimitive(WindowManagerMaterial, this, RenderPrimitiveGroup.Overlay, ref Identity);
        }

        [CallOnThread("Render")]
		public override void Draw(GraphicsDevice graphicsDevice)
		{
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

            var rs = graphicsDevice.RenderState;
			var material = Materials.PopupWindowMaterial;
			foreach (var window in VisibleWindows)
			{
				var xnaWorld = window.XNAWorld;

				if (Screen != null)
					graphicsDevice.ResolveBackBuffer(Screen);
				material.SetState(graphicsDevice, Screen);
				material.Render(graphicsDevice, window, ref xnaWorld, ref XNAView, ref XNAProjection);
				material.ResetState(graphicsDevice);

                SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
				window.Draw(SpriteBatch);
				SpriteBatch.End();
			}
            rs.AlphaBlendEnable = false;
            rs.AlphaFunction = CompareFunction.Always;
            rs.AlphaTestEnable = false;
            rs.DepthBufferEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
		}

		internal void Add(Window window)
		{
			Windows.Add(window);
            WindowsZOrder.Add(window);
        }

		public bool HasVisiblePopupWindows()
		{
            return WindowsZOrder.Any(w => w.Visible);
		}

		public IEnumerable<Window> VisibleWindows
		{
			get
			{
                return WindowsZOrder.Where(w => w.Visible);
			}
		}

		public const int DragMinimumDistance = 2;

		Point mouseDownPosition;
		public Point MouseDownPosition { get { return mouseDownPosition; } }

		Window mouseActiveWindow;
		public Window MouseActiveWindow { get { return mouseActiveWindow; } }

		double LastUpdateRealTime;
		[CallOnThread("Updater")]
        public void HandleUserInput(ElapsedTime elapsedTime)
        {
			if (UserInput.IsMouseLeftButtonPressed())
			{
				mouseDownPosition = new Point(UserInput.MouseState.X, UserInput.MouseState.Y);
                mouseActiveWindow = VisibleWindows.LastOrDefault(w => w.Interactive && w.Location.Contains(mouseDownPosition));
                if ((mouseActiveWindow != null) && (mouseActiveWindow != WindowsZOrder.Last()))
				{
                    WindowsZOrder.Remove(mouseActiveWindow);
                    WindowsZOrder.Add(mouseActiveWindow);
                    UpdateTopMost();
                    WriteWindowZOrder();
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

				if (Viewer.RealTime - LastUpdateRealTime >= 0.1)
				{
					LastUpdateRealTime = Viewer.RealTime;
					mouseActiveWindow.HandleUserInput();
				}

				if (UserInput.IsMouseLeftButtonReleased())
					mouseActiveWindow = null;
			}
		}

        void UpdateTopMost()
        {
            // Make sure all top-most windows sit above all normal windows.
            WindowsZOrder = WindowsZOrder.Where(w => !w.TopMost).Concat(WindowsZOrder.Where(w => w.TopMost)).ToList();
        }

        [Conditional("DEBUG_WINDOW_ZORDER")]
        internal void WriteWindowZOrder()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Windows: (bottom-to-top order, [V] = visible, [NI] = non-interactive)");
            Console.WriteLine("  Visible: {0}", String.Join(", ", VisibleWindows.Select(w => w.GetType().Name).ToArray()));
            Console.WriteLine("  All:     {0}", String.Join(", ", WindowsZOrder.Select(w => String.Format("{0}{1}{2}", w.GetType().Name, w.Interactive ? "" : "[NI]", w.Visible ? "[V]" : "")).ToArray()));
            Console.WriteLine();
        }
    }
}
