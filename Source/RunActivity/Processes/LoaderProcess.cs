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
        public const double UpdatePeriod = 0.1;       // 10 times per second 
        public double LastUpdate = 0;          // last time we were upated

        Viewer3D Viewer;              //   by the 3D viewer 10 times a second
        Thread LoaderThread;

        public bool Finished { get { return State.Finished; } }

        ProcessState State = new ProcessState();   // manage interprocess signalling

        public LoaderProcess(Viewer3D viewer )
        {
            Viewer = viewer;
            LoaderThread = new Thread(LoadLoop);
        }

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        /// <summary>
        /// LoadPrep and Load
        /// </summary>
        public void StartUpdate( )
        {
            if (!State.Finished)
            {
                // the loader will often fall behind, in that case let it finish
                // before issueing a new command.
                Viewer.RenderProcess.LoaderSlow = true;  // diagnostic info
                return;
            }
            Viewer.RenderProcess.LoaderSlow = false;

            Viewer.LoadPrep();  

            State.SignalStart();

            LastUpdate = Program.RealTime;
        }

        public void Run( )
        {
            LoaderThread.Start();
        }

        public void Stop()
        {
            LoaderThread.Abort();
        }

        public void LoadLoop()
        {
			Viewer.LoaderProfiler = new Profiler("Loader");

            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();
				Viewer.LoaderProfiler.Start();

				try
				{
					Viewer.Load(Viewer.RenderProcess);  // complete scan and load as necessary
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

                State.SignalFinish();
				Viewer.LoaderProfiler.Stop();
            }

        }

    } // LoaderProcess
}
