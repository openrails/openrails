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


namespace ORTS
{
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
        public double Jitter = 0;  // difference between when a frame should be rendered vs when it was rendered
        public bool UpdateSlow = false;  // true if the render loop finishes faster than the update loop.
        public bool LoaderSlow = false;  // true if the loader loop is falling behind
        public int PrimitiveCount = 0;
        public int PrimitivesPerFrame = 0;
        public int RenderStateChangesCount = 0;
        public int RenderStateChangesPerFrame = 0;
        public int ImageChangesCount = 0;
        public int ImageChangesPerFrame = 0;

        public RenderProcess( Viewer3D viewer3D )
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            
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
            Viewer.Initialize(this);
            Viewer.LoadPrep();  // Does initial load before 3D window is displayed
            Viewer.Load(this);  // after this Load is done in a background thread.
            Viewer.LoaderProcess.Run();
            CurrentFrame = new RenderFrame();
            if (Viewer.UpdaterProcess != null)
            {   // if its a multiprocessor machine, set up background frame updater
                NextFrame = new RenderFrame();
                Viewer.UpdaterProcess.Run();
            }
            base.Initialize();
        }

        /// <summary>
        /// Called regularly.   Used to update the simulator class when
        /// the window is minimized.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            double totalRealSeconds = gameTime.TotalRealTime.TotalSeconds;

            if ( Form.WindowState == System.Windows.Forms.FormWindowState.Minimized
                && totalRealSeconds - Viewer.Simulator.LastUpdate > 0.1)  // 10 times a second should be enough to keep it running
            {  // keep the simulator running while the window is minimized
                UpdateEverything(gameTime);
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
        protected override void Draw(GameTime gameTime)
        {
            UpdateEverything(gameTime);

            CurrentFrame.Draw(GraphicsDevice);

            // Diagnositics
            double totalRealSeconds = gameTime.TotalRealTime.TotalSeconds;
            Jitter = totalRealSeconds - CurrentFrame.TargetRenderTimeS;
            PrimitivesPerFrame = PrimitiveCount;
            PrimitiveCount = 0;
            RenderStateChangesPerFrame = RenderStateChangesCount;
            RenderStateChangesCount = 0;
            ImageChangesPerFrame = ImageChangesCount;
            ImageChangesCount = 0;

            base.Draw(gameTime);
        }


        private void UpdateEverything(GameTime gameTime)
        {
            double totalRealSeconds = gameTime.TotalRealTime.TotalSeconds;


            if (Viewer.UpdaterProcess != null)
            {   // multi processor machine
                if (!Viewer.UpdaterProcess.Finished)
                {
                    UpdateSlow = true;
                    Viewer.UpdaterProcess.WaitTillFinished();
                }
                else
                {
                    UpdateSlow = false;
                }

                if (totalRealSeconds - UserInput.LastUpdate > UserInput.UpdatePeriod)  // do this now to ensure no conflict with updater thread
                    UserInput.Update(gameTime);

                SwapFrames(ref CurrentFrame, ref NextFrame);
                GameTime RenderTimeNextFrame = new GameTime(gameTime.TotalRealTime.Add(gameTime.ElapsedRealTime), gameTime.ElapsedRealTime,
                                                         gameTime.TotalGameTime.Add(gameTime.ElapsedGameTime), gameTime.ElapsedGameTime,
                                                         gameTime.IsRunningSlowly);
                Viewer.UpdaterProcess.Update(NextFrame, RenderTimeNextFrame);
            }
            else
            {   // single processor machine
                if (totalRealSeconds - UserInput.LastUpdate > UserInput.UpdatePeriod)
                {
                    UserInput.Update(gameTime);
                    Viewer.HandleUserInput();
                }
                if (totalRealSeconds - Viewer.Simulator.LastUpdate > Simulator.UpdatePeriod)  // limit rate on low spec machines
                    Viewer.Simulator.Update(gameTime);
                if (totalRealSeconds - Viewer.UpdaterProcess.LastUpdate > UpdaterProcess.UpdatePeriod)
                    Viewer.Update(gameTime);
                if (totalRealSeconds - Viewer.LoaderProcess.LastUpdate > LoaderProcess.UpdatePeriod)
                    Viewer.LoaderProcess.Update(gameTime);
                CurrentFrame.Clear();
                CurrentFrame.TargetRenderTimeS = totalRealSeconds;
                Viewer.PrepareFrame(CurrentFrame, gameTime);
                CurrentFrame.Sort();
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

    }
}
