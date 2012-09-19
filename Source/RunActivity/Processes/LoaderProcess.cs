// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using System.Threading;

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
                State = new ProcessState("Loader");
                Thread = new Thread(LoaderThread);
                Thread.Start();
            }
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
            State.WaitTillFinished();
        }

        [ThreadName("Loader")]
        void LoaderThread()
        {
            Profiler.SetThread();

            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();
                try
                {
                    if (!DoLoad())
                        return;
                }
                finally
                {
                    // Signal finished so RenderProcess can start drawing
                    State.SignalFinish();
                }
            }
        }

        [CallOnThread("Updater")]
        public void StartLoad()
        {
            Debug.Assert(Finished);
            if (Threaded)
                State.SignalStart();
            else
                DoLoad();
        }

        [ThreadName("Loader")]
        bool DoLoad()
        {
            if (Debugger.IsAttached)
            {
                Load();
            }
            else
            {
                try
                {
                    Load();
                }
                catch (Exception error)
                {
                    if (!(error is ThreadAbortException))
                    {
                        // Unblock anyone waiting for us, report error and die.
                        if (Threaded)
                            State.SignalFinish();
                        Viewer.ProcessReportError(error);
                        return false;
                    }
                }
            }
            return true;
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            Profiler.Start();
            try
            {
                Viewer.Load();
            }
            finally
            {
                Profiler.Stop();
            }
        }
    }
}
