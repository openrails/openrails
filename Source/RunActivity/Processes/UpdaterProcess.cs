///
/// Background process prepares the next frame for rendering.
/// Handles keyboard and mouse input
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ORTS
{
    public class UpdaterProcess
    {
        public const double UpdatePeriod = 0.1;       // 10 times per second maximum - we'll call the viewer's Update 
        public double LastUpdate = 0;          // last time we were updated

        RenderFrame Frame;       //     this frame has been             Note: when frame is null, then update simulator only
        GameTime GameTime;       //         updated to this time

        Viewer3D Viewer;       //     3D viewer and the 

        Thread UpdaterThread;    // The updater thread calls the

        public bool Finished { get { return State.Finished; } }

        ProcessState State = new ProcessState();  // manage interprocess signalling

        public UpdaterProcess( Viewer3D viewer )
        {
            Viewer = viewer;
            UpdaterThread = new Thread(Updater);
            UpdaterThread.Priority = ThreadPriority.AboveNormal;
        }

        public void Run()
        {
            UpdaterThread.Start();
        }

        public void Stop()
        {
            UpdaterThread.Abort();
        }

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        /// <summary>
        /// Note:  caller must pass gametime as a threadsafe copy
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="gameTime"></param>
        public void Update(RenderFrame frame, GameTime gameTime)
        {
            if (!State.Finished)
            {
                System.Diagnostics.Debug.Assert( false, "Can't overlap updates");
                return;
            }
            Frame = frame;
            GameTime = gameTime;
            if( frame != null )
                Frame.TargetRenderTimeS = gameTime.TotalRealTime.TotalSeconds;
            State.SignalStart();
        }

        public void Updater()
        {
            while (Thread.CurrentThread.ThreadState == ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();

                // Update the simulator 
                Viewer.Simulator.Update(GameTime);

                // Handle user input, its was read is in RenderProcess thread
                if (UserInput.Ready)
                {
                    Viewer.HandleUserInput();
                    UserInput.Handled();
                }

                // Update slowly changing items
                double totalRealSeconds = GameTime.TotalRealTime.TotalSeconds;
                if (totalRealSeconds - LastUpdate > UpdatePeriod)  
                    Viewer.Update(GameTime);

                // Prepare the frame for drawing
                if (Frame != null)
                {
                    Frame.Clear();
                    Viewer.PrepareFrame(Frame, GameTime);
                    Frame.Sort();
                }

                // Signal finished so RenderProcess can start drawing
                State.SignalFinish();

                // Update the loader - it should only copy volatile data and return
                if (totalRealSeconds - Viewer.LoaderProcess.LastUpdate > LoaderProcess.UpdatePeriod)
                    Viewer.LoaderProcess.Update(GameTime);

            }
        }
    }
}
