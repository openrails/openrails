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
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ORTS
{
    public class LoaderProcess
    {
        public readonly bool Threaded;
        public readonly Profiler Profiler = new Profiler("Loader");
        readonly Viewer3D Viewer;
        readonly Thread Thread;
        readonly ProcessState State;

        public LoaderProcess(Viewer3D viewer)
        {
            Threaded = true;
            Viewer = viewer;
            if (Threaded)
            {
                State = new ProcessState();
                Thread = new Thread(LoadLoop);
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

        [ThreadName("Loader")]
        void LoadLoop()
        {
            ProcessState.SetThreadName("Loader Process");

            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();
                try
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
                            if (!(error is ThreadAbortException))
                            {
                                // Unblock anyone waiting for us, report error and die.
                                State.SignalFinish();
                                Viewer.ProcessReportError(error);
                                return;
                            }
                        }
                    }
                }
                finally
                {
                    // Signal finished so RenderProcess can start drawing
                    State.SignalFinish();
                }
            }
        }

        public const double UpdatePeriod = 0.1;       // 10 times per second 
        public double LastUpdateRealTime = 0;          // last time we were upated

        [CallOnThread("Updater")]
        public void StartUpdate()
        {
            // the loader will often fall behind, in that case let it finish
            // before issueing a new command.
            if (!Finished)
                return;

            Viewer.LoadPrep();

            State.SignalStart();

            LastUpdateRealTime = Viewer.RealTime;
        }

        [ThreadName("Loader")]
        public void Update()
        {
            Profiler.Start();

            try
            {
                Viewer.Load(Viewer.RenderProcess);  // complete scan and load as necessary
            }
            finally
            {
                Profiler.Stop();
            }
        }
    } // LoaderProcess
}
