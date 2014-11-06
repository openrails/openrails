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

using ORTS.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace ORTS.Processes
{
    public class LoaderProcess
    {
        public readonly Profiler Profiler = new Profiler("Loader");
        readonly ProcessState State = new ProcessState("Loader");
        readonly Game Game;
        readonly Thread Thread;
        readonly WatchdogToken WatchdogToken;

        public LoaderProcess(Game game)
        {
            Game = game;
            Thread = new Thread(LoaderThread);
            WatchdogToken = new WatchdogToken(Thread);
            WatchdogToken.SpecialDispensationFactor = 6;
        }

        public void Start()
        {
            Game.WatchdogProcess.Register(WatchdogToken);
            Thread.Start();
        }

        public void Stop()
        {
            Game.WatchdogProcess.Unregister(WatchdogToken);
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
        /// Returns whether the loading process has been terminated, i.e. all loading should stop.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All loading code should periodically (e.g. between loading each file) check this and exit as soon as it is
        /// seen to be true.
        /// </para>
        /// <para>
        /// Reading this property implicitly causes the <see cref="WatchdogToken"/> to be pinged, informing the
        /// <see cref="WatchdogProcess"/> that the loader is still responsive. Therefore the remarks about the
        /// <see cref="WatchdogToken.Ping()"/> method apply to this property regarding when it should and should not
        /// be used.
        /// </para>
        /// </remarks>
        public bool Terminated
        {
            get
            {
                // Specially for the loader process: this keeps it "alive" while objects are loading, as they check Terminated periodically.
                WatchdogToken.Ping();
                return State.Terminated;
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
