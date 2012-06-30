// COPYRIGHT 2009, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.IO;
using System.Threading;

namespace ORTS
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
