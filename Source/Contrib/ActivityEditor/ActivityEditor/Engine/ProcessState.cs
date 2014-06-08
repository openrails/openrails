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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 


using System;
using System.IO;
using System.Threading;

namespace ActivityEditor.Engine
{
    public class ProcessState
    {
        public bool Finished { get; private set; }
        ManualResetEvent StartEvent = new ManualResetEvent(false);
        ManualResetEvent FinishEvent = new ManualResetEvent(true);
#if DEBUG_THREAD_PERFORMANCE
        StreamWriter DebugFileStream;
#endif

        public ProcessState(string name)
        {
            Finished = true;
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream = new StreamWriter(File.OpenWrite("debug_thread_" + name.ToLowerInvariant() + "_state.csv"));
            DebugFileStream.Write("Time,Event\n");
#endif
        }

        public void SignalStart()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},SS\n", DateTime.Now.Ticks);
#endif
            Finished = false;
            FinishEvent.Reset();
            StartEvent.Set();
        }

        public void SignalFinish()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},SF\n", DateTime.Now.Ticks);
#endif
            Finished = true;
            StartEvent.Reset();
            FinishEvent.Set();
        }

        public void WaitTillStarted()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTS+\n", DateTime.Now.Ticks);
#endif
            StartEvent.WaitOne();
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTS-\n", DateTime.Now.Ticks);
#endif
        }

        public void WaitTillFinished()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTF+\n", DateTime.Now.Ticks);
#endif
            FinishEvent.WaitOne();
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTF-\n", DateTime.Now.Ticks);
#endif
        }
    }
}
