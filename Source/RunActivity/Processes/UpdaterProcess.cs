// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ORTS
{
    public class UpdaterProcess
    {
        public readonly bool Threaded;
        public readonly Profiler Profiler = new Profiler("Updater");
        readonly Viewer3D Viewer;
        readonly Thread Thread;
        readonly ProcessState State;

        public UpdaterProcess(Viewer3D viewer)
        {
            Threaded = System.Environment.ProcessorCount > 1;
            Viewer = viewer;
            if (Threaded)
            {
                State = new ProcessState();
                Thread = new Thread(UpdateLoop);
            }
        }

        public void Run()
        {
            if (Threaded)
                Thread.Start();
        }

        public void Stop()
        {
            if (Threaded)
                Thread.Abort();
        }

        public bool Finished
        {
            get
            {
                // Non-threaded updater is always "finished".
                return !Threaded || State.Finished;
            }
        }

        public void WaitTillFinished()
        {
            // Non-threaded updater never waits.
            if (Threaded)
                State.WaitTillFinished();
        }

        [ThreadName("Updater")]
        void UpdateLoop()
        {
            ProcessState.SetThreadName("Updater Process");

            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();

                if (Debugger.IsAttached)
                {
                    Update();
                }
                else
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception error)
                    {
                        if (!(error is ThreadAbortException))
                        {
                            // Unblock anyone waiting for us, report error and die.
                            State.SignalFinish();
                            Viewer.ProcessReportError(error);
                            // Finally unblock any process that may have started us, while the message was showing
                            State.SignalFinish();
                            return;
                        }
                    }
                }

                // Signal finished so RenderProcess can start drawing
                State.SignalFinish();
            }
        }

        [CallOnThread("Render")]
        public void StartUpdate(RenderFrame frame, double totalRealSeconds)
        {
            if (!Finished)
                throw new InvalidOperationException("Can't overlap updates.");
            CurrentFrame = frame;
            TotalRealSeconds = totalRealSeconds;
            if (Threaded)
            {
                State.SignalStart();
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    Update();
                }
                else
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception error)
                    {
                        Viewer.ProcessReportError(error);
                    }
                }
            }
        }

        RenderFrame CurrentFrame;
        double TotalRealSeconds;
        double LastTotalRealSeconds = -1;

        [ThreadName("Updater")]
        public void Update()
        {
            Profiler.Start();

            // The first time we update, the TotalRealSeconds will be ~time
            // taken to load everything. We'd rather not skip that far through
            // the simulation so the first time we deliberately have an
            // elapsed real and clock time of 0.0s.
            if (LastTotalRealSeconds == -1)
                LastTotalRealSeconds = TotalRealSeconds;

            Viewer.RealTime = TotalRealSeconds;
            var elapsedTime = new ElapsedTime(Viewer.Simulator.GetElapsedClockSeconds((float)(TotalRealSeconds - LastTotalRealSeconds)), (float)(TotalRealSeconds - LastTotalRealSeconds));
            LastTotalRealSeconds = TotalRealSeconds;

            try
            {
                Viewer.RenderProcess.ComputeFPS(elapsedTime.RealSeconds);
                Viewer.Simulator.Update(elapsedTime.ClockSeconds);
                Viewer.HandleUserInput(elapsedTime);
                Viewer.HandleMouseMovement();
                UserInput.Handled();
                CurrentFrame.Clear();
                Viewer.PrepareFrame(CurrentFrame, elapsedTime);
                CurrentFrame.Sort();
            }
            finally
            {
                Profiler.Stop();

                // Update the loader - it should only copy volatile data and return.
                if (Viewer.RealTime - Viewer.LoaderProcess.LastUpdateRealTime > LoaderProcess.UpdatePeriod)
                    Viewer.LoaderProcess.StartUpdate();
            }
        }
    } // Updater Process
}
