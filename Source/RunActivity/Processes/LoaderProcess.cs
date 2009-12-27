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
            LoopTimer.Stop();
            LoaderThread.Abort();
        }

        public void LoadLoop()
        {
            LoopTimer.Start();
            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();

                BusyTimer.Start();
                Viewer.Load(Viewer.RenderProcess);  // complete scan and load as necessary

                State.SignalFinish();

                BusyTimeEnd();
            }

        }

        // Profiling
        public int UtilizationPercent
        {
            get  
            {
                long loopMilliseconds = lastLoopMilliseconds +LoopTimer.ElapsedMilliseconds;
                long busyMilliseconds = lastBusyMilliseconds +BusyTimer.ElapsedMilliseconds;
                if (loopMilliseconds != 0)
                    lastUtilitationPercent = (int)(busyMilliseconds * 100 / loopMilliseconds);
                return lastUtilitationPercent;
            }
        }
        private long lastLoopMilliseconds;
        private long lastBusyMilliseconds;
        private int lastUtilitationPercent;

        // Start the loop timer when the process is launched
        public Stopwatch LoopTimer = new Stopwatch();
        // Start the busy timer when your code runs
        Stopwatch BusyTimer = new Stopwatch();
        // Stop the busy timer and compute utilization
        public void BusyTimeEnd()
        {
            lastLoopMilliseconds = LoopTimer.ElapsedMilliseconds;  // these two should be atomic
            lastBusyMilliseconds = BusyTimer.ElapsedMilliseconds;
            LoopTimer.Reset();
            LoopTimer.Start();
            BusyTimer.Reset();
        }


    } // LoaderProcess
}
