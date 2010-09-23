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
        public Viewer3D Viewer;
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
		public bool UpdateSlow = false;  // true if the render loop finishes faster than the update loop.
        public bool LoaderSlow = false;  // true if the loader loop is falling behind
		public int[] PrimitiveCount = new int[(int)RenderPrimitiveSequence.Sentinel];
		public int[] PrimitivePerFrame = new int[(int)RenderPrimitiveSequence.Sentinel];
		public int RenderStateChangesCount = 0;
        public int RenderStateChangesPerFrame = 0;
        public int ImageChangesCount = 0;
        public int ImageChangesPerFrame = 0;

        // Timing Information -  THREAD SAFETY - don't use these outside the UpdaterProcess thread  
        public double LastFrameTime = 0;         // real time seconds of the last simulator.update and viewer.prepareframe
        public double LastUserInputTime = 0;     // real time seconds when we last started Viewer.HandleUserInput()

        private ElapsedTime FrameElapsedTime = new ElapsedTime();
        private ElapsedTime UserInputElapsedTime = new ElapsedTime();

        public ElapsedTime GetFrameElapsedTime()
        {
            if (LastFrameTime != 0)
            {
                FrameElapsedTime.RealSeconds = (float)(Program.RealTime - LastFrameTime);
                FrameElapsedTime.ClockSeconds = Viewer.Simulator.GetElapsedClockSeconds(FrameElapsedTime.RealSeconds);
            }
            LastFrameTime = Program.RealTime;
            return FrameElapsedTime;
        }

        public ElapsedTime GetUserInputElapsedTime()
        {
            if (LastUserInputTime != 0)
            {
                UserInputElapsedTime.RealSeconds = (float)(Program.RealTime - LastUserInputTime);
                UserInputElapsedTime.ClockSeconds = Viewer.Simulator.GetElapsedClockSeconds(UserInputElapsedTime.RealSeconds);
            }
            LastUserInputTime = Program.RealTime;
            return UserInputElapsedTime;
        }

        Stopwatch sw = new Stopwatch();

        public RenderProcess( Viewer3D viewer3D )
        {
            //Thread.CurrentThread.Priority = ThreadPriority.Highest;
            
            System.Windows.Forms.Control control = System.Windows.Forms.Control.FromHandle(this.Window.Handle);
            Form = control.FindForm();
            Viewer = viewer3D;
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            Viewer.Configure(this);
			Viewer.RenderProfiler = new Profiler("Render");
			// The UpdaterProcess, started after us, will replace this.
			Viewer.UpdaterProfiler = new Profiler("Updater");
		}

        /// <summary>
        /// Allows the game to perform any initialization it needs after the graphics device has started
        /// </summary>
        protected override void Initialize()
        {
            Materials.Initialize(this);
            Viewer.Initialize(this);
            Viewer.LoadPrep();  // Does initial load before 3D window is displayed
            Viewer.Load(this);  // after this Load is done in a background thread.
            Viewer.LoaderProcess.Run();
            CurrentFrame = new RenderFrame( this );
            if (Viewer.UpdaterProcess != null)
            {   // if its a multiprocessor machine, set up background frame updater
                NextFrame = new RenderFrame( this );
                Viewer.UpdaterProcess.Run();
            }
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

            if ( Form.WindowState == System.Windows.Forms.FormWindowState.Minimized
                && totalRealSeconds - LastFrameTime > 0.1 ) 
            {  // keep the everything running at a slower pace while the window is minimized
                FrameUpdate(gameTime);
            }

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
                FrameUpdate(gameTime);
            }

			Viewer.RenderProfiler.Start();

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

			Viewer.RenderProfiler.Stop();
        }

        private void FrameUpdate(GameTime gameTime)
        {
            double actualRealTime = gameTime.TotalRealTime.TotalSeconds;

            if (Viewer.UpdaterProcess != null)
            {   // multi processor machine
                // Wait for updater to finish, and flag if its slow
                if (!Viewer.UpdaterProcess.Finished)
                    UpdateSlow = true;
                else
                    UpdateSlow = false;

                Viewer.UpdaterProcess.WaitTillFinished();

                // Time to read the keyboard - must be done in XNA Game thread
                UserInput.Update(Viewer);

                // launch updater to prepare the next frame
                SwapFrames(ref CurrentFrame, ref NextFrame);
                Viewer.UpdaterProcess.StartUpdate(NextFrame, actualRealTime);
            }
            else
            {   // single processor machine
                UserInput.Update(Viewer);

				Viewer.UpdaterProfiler.Start();
                Program.RealTime = actualRealTime;
                ElapsedTime frameElapsedTime = GetFrameElapsedTime();

				try
				{
					ComputeFPS(frameElapsedTime.RealSeconds);

					// Update the simulator
					Viewer.Simulator.Update(frameElapsedTime.ClockSeconds);

					Viewer.HandleUserInput(GetUserInputElapsedTime());
					UserInput.Handled();

					Viewer.HandleMouseMovement();

					// Prepare the frame for drawing
					CurrentFrame.Clear();
					Viewer.PrepareFrame(CurrentFrame, frameElapsedTime);
					CurrentFrame.Sort();
				}
				catch (Exception error)
				{
					Viewer.ProcessReportError(error);
				}

                // Update the loader - it should only copy volatile data and return
                if (Program.RealTime - Viewer.LoaderProcess.LastUpdate > LoaderProcess.UpdatePeriod)
                    Viewer.LoaderProcess.StartUpdate();

				Viewer.UpdaterProfiler.Stop();
			}

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

		float lastElapsedTime = 0;

		public void ComputeFPS(float elapsedRealTime)
		{
			if (elapsedRealTime > 0.00001)
			{
				// Smoothing filter length
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
