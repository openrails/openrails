// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using System.Threading;
using Orts.Processes;
using ORTS.Common;
using CancellationToken = ORTS.Common.CancellationToken;
using CancellationTokenSource = ORTS.Common.CancellationTokenSource;

namespace Orts.Viewer3D.Processes
{
    public class LoaderProcess
    {
        public readonly Profiler Profiler = new Profiler("Loader");
        readonly ProcessState State = new ProcessState("Loader");
        readonly Game Game;
        readonly Thread Thread;
        readonly WatchdogToken WatchdogToken;
        readonly ORTS.Common.CancellationTokenSource CancellationTokenSource;

        public LoaderProcess(Game game)
        {
            Game = game;
            Thread = new Thread(LoaderThread);
            WatchdogToken = new WatchdogToken(Thread);
            WatchdogToken.SpecialDispensationFactor = 6;
            CancellationTokenSource = new ORTS.Common.CancellationTokenSource(WatchdogToken.Ping);
        }

        public void Start()
        {
            Game.WatchdogProcess.Register(WatchdogToken);
            Thread.Start();
        }

        public void Stop()
        {
            Game.WatchdogProcess.Unregister(WatchdogToken);
            CancellationTokenSource.Cancel();
            State.SignalTerminate();
        }

        public bool Finished
        {
            get
            {
                return State.Finished;
            }
        }

        /// <summary>
        /// Returns a token (copyable object) which can be queried for the cancellation (termination) of the loader.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All loading code should periodically (e.g. between loading each file) check the token and exit as soon
        /// as it is cancelled (<see cref="CancellationToken.IsCancellationRequested"/>).
        /// </para>
        /// <para>
        /// Reading <see cref="CancellationToken.IsCancellationRequested"/> causes the <see cref="WatchdogToken"/> to
        /// be pinged, informing the <see cref="WatchdogProcess"/> that the loader is still responsive. Therefore the
        /// remarks about the <see cref="WatchdogToken.Ping()"/> method apply to the token regarding when it should
        /// and should not be used.
        /// </para>
        /// </remarks>
        public ORTS.Common.CancellationToken CancellationToken
        {
            get
            {
                return CancellationTokenSource.Token;
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
            Game.SetThreadLanguage();

            while (true)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();
                if (State.Terminated)
                    break;
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
        internal void StartLoad()
        {
            Debug.Assert(State.Finished);
            State.SignalStart();
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
                    // Unblock anyone waiting for us, report error and die.
                    CancellationTokenSource.Cancel();
                    State.SignalTerminate();
                    Game.ProcessReportError(error);
                    return false;
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
                WatchdogToken.Ping();
                Game.State.Load();
            }
            finally
            {
                Profiler.Stop();
            }
        }
    }
}
