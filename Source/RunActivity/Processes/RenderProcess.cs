using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Threading;
using System.Diagnostics;


namespace ORTS
{
    public class ElapsedTime
    {
        public float ClockSeconds;
        public float RealSeconds;

        public static ElapsedTime Zero = new ElapsedTime();

		public static ElapsedTime operator +(ElapsedTime a, ElapsedTime b) { return new ElapsedTime() { ClockSeconds = a.ClockSeconds + b.ClockSeconds, RealSeconds = a.RealSeconds + b.RealSeconds }; }

		public void Reset()
		{
			ClockSeconds = 0;
			RealSeconds = 0;
		}
	}


    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class RenderProcess : Microsoft.Xna.Framework.Game
    {
        System.Windows.Forms.Form Form;    // the 3D view is drawn on this form
        public readonly Viewer3D Viewer;
		public readonly Profiler Profiler = new Profiler("Render");
        public GraphicsDeviceManager GraphicsDeviceManager;

        RenderFrame CurrentFrame;   // a frame contains a list of primitives to draw at a specified time
        RenderFrame NextFrame;      // we prepare the next frame in the background while the current one is rendering,

        public bool Stopped = false;  // use for shutdown

        public void ToggleFullScreen() { ToggleFullScreenRequested = true; } // Interprocess signalling.
        private bool ToggleFullScreenRequested = false;

        public new bool IsMouseVisible = false;  // handles cross thread issues by signalling RenderProcess of a change

        // Diagnostic information
        public float FrameRate = -1; // frames-per-second, information displayed by InfoViewer in upper left
        public float FrameTime = -1; // seconds
        public float FrameJitter = -1; // seconds
		public float SmoothedFrameRate = -1;
		public float SmoothedFrameTime = -1;
		public float SmoothedFrameJitter = -1;
		public int[] PrimitiveCount = new int[(int)RenderPrimitiveSequence.Sentinel];
		public int[] PrimitivePerFrame = new int[(int)RenderPrimitiveSequence.Sentinel];
		public int RenderStateChangesCount = 0;
        public int RenderStateChangesPerFrame = 0;
        public int ImageChangesCount = 0;
        public int ImageChangesPerFrame = 0;

		double LastUpdateTime = 0;

        public RenderProcess( Viewer3D viewer3D )
        {
            //Thread.CurrentThread.Priority = ThreadPriority.Highest;
            
            System.Windows.Forms.Control control = System.Windows.Forms.Control.FromHandle(this.Window.Handle);
            Form = control.FindForm();
            Viewer = viewer3D;
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            Viewer.Configure(this);
		}

        /// <summary>
        /// Allows the game to perform any initialization it needs after the graphics device has started
        /// </summary>
		protected override void Initialize()
        {
			Thread.CurrentThread.Name = "Render Process";

			Materials.Initialize(this);
            Viewer.Initialize(this);
            Viewer.LoadPrep();  // Does initial load before 3D window is displayed
            Viewer.Load(this);  // after this Load is done in a background thread.
            Viewer.LoaderProcess.Run();
            Viewer.SoundProcess.Run();
            CurrentFrame = new RenderFrame(this);
            NextFrame = new RenderFrame(this);
            Viewer.UpdaterProcess.Run();
            base.Initialize();
            Viewer.Simulator.Paused = false;
        }

        /// <summary>
        /// Called regularly.   Used to update the simulator class when
        /// the window is minimized.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            double totalRealSeconds = gameTime.TotalRealTime.TotalSeconds;
			// Keep the everything running at a slower pace while the window is minimized.
			if (Form.WindowState == System.Windows.Forms.FormWindowState.Minimized && totalRealSeconds - LastUpdateTime > 0.1)
			{
				FrameUpdate(totalRealSeconds);
			}
			LastUpdateTime = totalRealSeconds;

            if (IsMouseVisible != base.IsMouseVisible)
                base.IsMouseVisible = IsMouseVisible;

            if (ToggleFullScreenRequested)
            {
                GraphicsDeviceManager.ToggleFullScreen();
                ToggleFullScreenRequested = false;
            }

            if (Stopped)
            {
                Terminate();
                this.Exit();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called once per frame when the game should draw itself.
        /// In a multiprocessor environement, it starts the background UpdateProcessor
        /// task preparing the next frame, while it renders this frame.
        /// In a single processor environment, it does the update/draw in
        /// sequence using this thread alone.
        /// </summary>
		int ProfileFrames = 1000;
		protected override void Draw(GameTime gameTime)
        {
			if (Viewer.SettingsBool[(int)BoolSettings.Profiling])
				if (--ProfileFrames == 0)
					Viewer.Stop();

            if (gameTime.ElapsedRealTime.TotalSeconds > 0.001)
            {  // a zero elapsed time indicates the window needs to be redrawn with the same content
                // ie after restoring from minimized, or uncovering a window
                FrameUpdate(gameTime.TotalRealTime.TotalSeconds);
            }

			Profiler.Start();

			Viewer.DisplaySize.X = GraphicsDevice.Viewport.Width;
			Viewer.DisplaySize.Y = GraphicsDevice.Viewport.Height;

			/* When using SynchronizeWithVerticalRetrace = true, then this isn't required
			// if the loader is running slow, limit render's frame rates to give loader some GPU time
			if (LoaderSlow )
			{
				Thread.Sleep(10);
			}
			 */

			try
			{
				CurrentFrame.Draw(GraphicsDevice);
				Viewer.WindowManager.Draw(GraphicsDevice);

				for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
				{
					PrimitivePerFrame[i] = PrimitiveCount[i];
					PrimitiveCount[i] = 0;
				}
				RenderStateChangesPerFrame = RenderStateChangesCount;
				RenderStateChangesCount = 0;
				ImageChangesPerFrame = ImageChangesCount;
				ImageChangesCount = 0;

				base.Draw(gameTime);
			}
			catch (Exception error)
			{
				Viewer.ProcessReportError(error);
			}

			Profiler.Stop();
        }

        private void FrameUpdate(double totalRealSeconds)
        {
			// Wait for updater to finish.
			Viewer.UpdaterProcess.WaitTillFinished();

			// Time to read the keyboard - must be done in XNA Game thread.
			UserInput.Update(Viewer);

			// Swap frames and start the next update (non-threaded updater does the whole update).
			SwapFrames(ref CurrentFrame, ref NextFrame);
			Viewer.UpdaterProcess.StartUpdate(NextFrame, totalRealSeconds);
        }

        private void SwapFrames(ref RenderFrame frame1, ref RenderFrame frame2)
        {
            RenderFrame temp = frame1;
            frame1 = frame2;
            frame2 = temp;
        }

        /// <summary>
        /// This signal is caught in the Update
        /// </summary>
        public void Stop()
        {
            Stopped = true;
        }

        /// <summary>
        /// Shut down other processes and unload content
        /// </summary>
        private void Terminate()
        {
            if (Viewer.UpdaterProcess != null) Viewer.UpdaterProcess.Stop();
            Viewer.LoaderProcess.Stop();
            Viewer.SoundProcess.Stop();
            Viewer.Unload(this);
        }

        /// <summary>
        /// User closed the window without pressing the exit key
        /// </summary>
		protected override void OnExiting(object sender, EventArgs args)
        {
            Terminate();
            base.OnExiting(sender, args);
        }

		float lastElapsedTime = -1;

		public void ComputeFPS(float elapsedRealTime)
		{
			if (elapsedRealTime < 0.001)
				return;

			// Smoothing filter length
			if (lastElapsedTime < 0)
			{
				lastElapsedTime = 0;
			}
			else
			{
				float rate = 3.0f / elapsedRealTime;

				// Calculate current frame rate, time and jitter.
				FrameRate = 1.0f / elapsedRealTime;
				FrameTime = elapsedRealTime;
				FrameJitter = Math.Abs(lastElapsedTime - elapsedRealTime);
				lastElapsedTime = elapsedRealTime;

				// Update smoothed frame rate, time and jitter.
				if (SmoothedFrameRate < 0)
					SmoothedFrameRate = FrameRate;
				else
					SmoothedFrameRate = (SmoothedFrameRate * (rate - 1.0f) + FrameRate) / rate;
				if (SmoothedFrameTime < 0)
					SmoothedFrameTime = FrameTime;
				else
					SmoothedFrameTime = (SmoothedFrameTime * (rate - 1.0f) + FrameTime) / rate;
				if (SmoothedFrameJitter < 0)
					SmoothedFrameJitter = FrameJitter;
				else
					SmoothedFrameJitter = (SmoothedFrameJitter * (rate - 1.0f) + FrameJitter) / rate;
			}

		}
    }// RenderProcess
}
