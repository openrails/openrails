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

namespace Orts.Viewer3D.Processes
{
    public class UpdaterProcess
    {
        public readonly Profiler Profiler = new Profiler("Updater");
        readonly ProcessState State = new ProcessState("Updater");
        readonly Game Game;
        readonly Thread Thread;
        readonly WatchdogToken WatchdogToken;

        public UpdaterProcess(Game game)
        {
            Game = game;
            Thread = new Thread(UpdaterThread);
            WatchdogToken = new WatchdogToken(Thread);
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

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        [ThreadName("Updater")]
        void UpdaterThread()
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
                    if (!DoUpdate())
                        return;
                }
                finally
                {
                    // Signal finished so RenderProcess can start drawing
                    State.SignalFinish();
                }
            }
        }

        RenderFrame CurrentFrame;
        double TotalRealSeconds;

        [CallOnThread("Render")]
        internal void StartUpdate(RenderFrame frame, double totalRealSeconds)
        {
            Debug.Assert(State.Finished);
            CurrentFrame = frame;
            TotalRealSeconds = totalRealSeconds;
            State.SignalStart();
        }

        [ThreadName("Updater")]
        bool DoUpdate()
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
                    // Unblock anyone waiting for us, report error and die.
                    State.SignalTerminate();
                    Game.ProcessReportError(error);
                    return false;
                }
            }
            return true;
        }

        [CallOnThread("Updater")]
        public void Update()
        {
            Profiler.Start();
            try
            {
                WatchdogToken.Ping();
                CurrentFrame.Clear();
                if (Game.State != null)
                {
                    Game.State.Update(CurrentFrame, TotalRealSeconds);
                    CurrentFrame.Sort();
                }
            }
            finally
            {
                Profiler.Stop();
            }
        }
    }
}
