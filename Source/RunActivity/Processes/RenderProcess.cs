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
    }


    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class RenderProcess : Microsoft.Xna.Framework.Game
    {
        System.Windows.Forms.Form Form;    // the 3D view is drawn on this form
        public Viewer3D Viewer;
        public GraphicsDeviceManager GraphicsDeviceManager;

        public bool ShadowMappingOn = false;
        public float ShadowDistanceLimit = 100;  // don't generate shadows beyond this distance

        RenderFrame CurrentFrame;   // a frame contains a list of primitives to draw at a specified time
        RenderFrame NextFrame;      // we prepare the next frame in the background while the current one is rendering,

        public bool Stopped = false;  // use for shutdown

        public void ToggleFullScreen() { ToggleFullScreenRequested = true; } // Interprocess signalling.
        private bool ToggleFullScreenRequested = false;

        public new bool IsMouseVisible = false;  // handles cross thread issues by signalling RenderProcess of a change

        // Diagnostic information
        public float SmoothedFrameRate = 1000;     // information displayed by InfoViewer in upper left
        public float MinFrameRate = 1000;
        public float SmoothJitter = 0;
        public float Jitter = 0;  // difference between when a frame should be rendered vs when it was rendered
        public bool UpdateSlow = false;  // true if the render loop finishes faster than the update loop.
        public bool LoaderSlow = false;  // true if the loader loop is falling behind
        public int PrimitiveCount = 0;
        public int PrimitivesPerFrame = 0;
        public int RenderStateChangesCount = 0;
        public int RenderStateChangesPerFrame = 0;
        public int ImageChangesCount = 0;
        public int ImageChangesPerFrame = 0;

        // Timing Information -  THREAD SAFETY - don't use these outside the UpdaterProcess thread  
        public double LastFrameTime = 0;         // real time seconds of the last simulator.update and viewer.prepareframe
        public double LastUserInputTime = 0;     // real time seconds when we last started Viewer.HandleUserInput()

        private ElapsedTime FrameElapsedTime = new ElapsedTime();
        private ElapsedTime UserInputElapsedTime = new ElapsedTime();

        // Profiling
        public Stopwatch RenderTime = new Stopwatch();
        public Stopwatch UpdateTime = new Stopwatch();

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
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs after the graphics device has started
        /// </summary>
        protected override void Initialize()
        {
            // TODO catch errors and disable shadows
            InitShadows();
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
        protected override void Draw(GameTime gameTime)
        {
            RenderTime.Start();

            if (gameTime.ElapsedRealTime.TotalSeconds > 0.001)
            {  // a zero elapsed time indicates the window needs to be redrawn with the same content
                // ie after restoring from minimized, or uncovering a window
                FrameUpdate(gameTime);
            }

            /* When using SynchronizeWithVerticalRetrace = true, then this isn't required
            // if the loader is running slow, limit render's frame rates to give loader some GPU time
            if (LoaderSlow )
            {
                Thread.Sleep(10);
            }
             */

            CurrentFrame.Draw(GraphicsDevice);

            // Diagnositics
            // double totalRealSeconds = gameTime.TotalRealTime.TotalSeconds;
            // TODO - compute Jitter = totalRealSeconds - CurrentFrame.TargetRenderTimeS;
            PrimitivesPerFrame = PrimitiveCount;
            PrimitiveCount = 0;
            RenderStateChangesPerFrame = RenderStateChangesCount;
            RenderStateChangesCount = 0;
            ImageChangesPerFrame = ImageChangesCount;
            ImageChangesCount = 0;

            base.Draw(gameTime);

            RenderTime.Stop();
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
                UserInput.Update();

                // launch updater to prepare the next frame
                SwapFrames(ref CurrentFrame, ref NextFrame);
                Viewer.UpdaterProcess.StartUpdate(NextFrame, actualRealTime);
            }
            else
            {   // single processor machine
                UserInput.Update();

                UpdateTime.Start();
                Program.RealTime = actualRealTime;
                ElapsedTime frameElapsedTime = GetFrameElapsedTime();

                ComputeFPS( frameElapsedTime.RealSeconds );

                // Update the simulator
                Viewer.Simulator.Update( frameElapsedTime.ClockSeconds );

                if ( UserInput.Ready )
                {
                    Viewer.HandleUserInput( GetUserInputElapsedTime() );
                    UserInput.Handled();
                }

                // Prepare the frame for drawing
                CurrentFrame.Clear();
                Viewer.PrepareFrame(CurrentFrame, frameElapsedTime );
                CurrentFrame.Sort();

                // Update the loader - it should only copy volatile data and return
                if (Program.RealTime - Viewer.LoaderProcess.LastUpdate > LoaderProcess.UpdatePeriod)
                    Viewer.LoaderProcess.StartUpdate();

                UpdateTime.Stop();

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

        public void ComputeFPS( float elapsedRealTime )
        {

            if (elapsedRealTime > 0.00001)
            {
                // Smoothing filter length
                float rate = 3.0f / elapsedRealTime;

                // Jitter
                float jitter = Math.Abs(lastElapsedTime - elapsedRealTime);
                lastElapsedTime = elapsedRealTime;
                if (Math.Abs(jitter - SmoothJitter) > 0.01)
                    SmoothJitter = jitter;
                else
                    SmoothJitter = (SmoothJitter * (rate - 1.0f) / rate) + (jitter / rate);

                // Frame Rate - Min and Smooth
                float frameRate = 1.0f / elapsedRealTime;
                if (frameRate < MinFrameRate)
                    MinFrameRate = frameRate;
                else
                    MinFrameRate = (MinFrameRate * (rate - 1.0f) / rate) + (frameRate / rate);

                if (Math.Abs(frameRate - SmoothedFrameRate) > 5.0)
                    SmoothedFrameRate = frameRate;
                else
                    SmoothedFrameRate = (SmoothedFrameRate * (rate - 1.0f) / rate) + (frameRate / rate);
            }
        }


        // The shadow map render target, depth buffer, and texture
        public RenderTarget2D shadowRenderTarget;
        public DepthStencilBuffer shadowDepthBuffer;
        public Texture2D shadowMap;

        int shadowMapWidthHeight = 4096;

        public void InitShadows()
        {
            SurfaceFormat shadowMapFormat = SurfaceFormat.Unknown;

            GraphicsDeviceCapabilities capabilities = GraphicsAdapter.DefaultAdapter.GetCapabilities(DeviceType.Hardware);
            if (capabilities.MaxTextureHeight < shadowMapWidthHeight)
                shadowMapWidthHeight = capabilities.MaxTextureHeight;
            if (capabilities.MaxTextureWidth < shadowMapWidthHeight)
                shadowMapWidthHeight = capabilities.MaxTextureWidth;

            // Check to see if the device supports a 32 or 16 bit 
            // floating point render target
            if (GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware,
                               GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
                               TextureUsage.Linear, QueryUsages.None,
                               ResourceType.RenderTarget, SurfaceFormat.Single) == true)
            {
                shadowMapFormat = SurfaceFormat.Single;
            }
            else if (GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(
                               DeviceType.Hardware,
                               GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
                               TextureUsage.Linear, QueryUsages.None,
                               ResourceType.RenderTarget, SurfaceFormat.HalfSingle)
                               == true)
            {
                shadowMapFormat = SurfaceFormat.HalfSingle;
            }

            // Create new floating point render target
            shadowRenderTarget = new RenderTarget2D(GraphicsDevice,
                                                    shadowMapWidthHeight,
                                                    shadowMapWidthHeight,
                                                    1, shadowMapFormat);

            // Create depth buffer to use when rendering to the shadow map
            shadowDepthBuffer = new DepthStencilBuffer(GraphicsDevice,
                                                       shadowMapWidthHeight,
                                                       shadowMapWidthHeight,
                                                       DepthFormat.Depth24);
        }


    }// RenderProcess
}
