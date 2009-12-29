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
using System.Diagnostics;

namespace ORTS
{
    public class UpdaterProcess
    {
        RenderFrame Frame;       //     this frame has been             Note: when frame is null, then update simulator only
        double NewRealTime;    //  real time seconds of the requested frame.
        Viewer3D Viewer;       //     3D viewer and the 
        Thread UpdaterThread;    // The updater thread calls the
        public bool Finished { get { return State.Finished; } }
        ProcessState State = new ProcessState();  // manage interprocess signalling

        // Profiling
        public Stopwatch UpdateTimer = new Stopwatch();

        public UpdaterProcess( Viewer3D viewer )
        {
            Viewer = viewer;
            UpdaterThread = new Thread(UpdateLoop);
            //UpdaterThread.Priority = ThreadPriority.AboveNormal;
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
        /// Executes in the RenderProcess thread.
        /// </summary>
        public void StartUpdate(RenderFrame frame, double newRealTime )
        {
            if (!State.Finished)   
            {
                System.Diagnostics.Debug.Assert( false, "Can't overlap updates");
                return;
            }
            Frame = frame;
            NewRealTime = newRealTime;
            State.SignalStart();   
        }



        public void UpdateLoop()
        {
            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();

                UpdateTimer.Start();
                Program.RealTime = NewRealTime;
                ElapsedTime frameElapsedTime = Viewer.RenderProcess.GetFrameElapsedTime();

                Viewer.RenderProcess.ComputeFPS( frameElapsedTime.RealSeconds );

                // Update the simulator 
                Viewer.Simulator.Update( frameElapsedTime.ClockSeconds );

                // Handle user input, its was read is in RenderProcess thread
                if (UserInput.Ready)
                {
                    Viewer.HandleUserInput( Viewer.RenderProcess.GetUserInputElapsedTime() );
                    UserInput.Handled();
                }

                // Prepare the frame for drawing
                if (Frame != null)
                {
                    Frame.Clear();
                    Viewer.PrepareFrame(Frame, frameElapsedTime );
                    Frame.Sort();
                }

                // Signal finished so RenderProcess can start drawing
                State.SignalFinish();

                // Update the loader - it should only copy volatile data and return
                if (Program.RealTime - Viewer.LoaderProcess.LastUpdate > LoaderProcess.UpdatePeriod)
                    Viewer.LoaderProcess.StartUpdate();

                UpdateTimer.Stop();
            }
        }

    } // Updater Process
}
