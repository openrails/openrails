// COPYRIGHT 2009, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ORTS
{
    /// <summary>
    /// Provides interprocess signalling.
    /// Manages a process as finished, or started
    /// with thread blocking calls to wait for the desired state
    /// without spin poll wait loops
    /// </summary>
    public class ProcessState
    {
        /// <summary>
        /// Initializes a process state object to 
        /// finished state.
        /// </summary>
        public ProcessState()
        {
        }

        public bool Finished { get { return finishedFlag; } }

        public void SignalFinish()     // use OS thread signalling to eliminate spin waits
        {
            finishedFlag = true;
            StartEvent.Reset();
            FinishedEvent.Set();
        }

        public void SignalStart()
        {
            finishedFlag = false;
            FinishedEvent.Reset();
            StartEvent.Set();
        }

        public void WaitTillFinished()
        {
            FinishedEvent.WaitOne();
        }

        public void WaitTillStarted()
        {
            StartEvent.WaitOne();
        }

        private bool finishedFlag = true;
        ManualResetEvent StartEvent = new ManualResetEvent(false);
        ManualResetEvent FinishedEvent = new ManualResetEvent(true);

        public static void SetThreadName(string name)
        {
            // This is so that you can identify threads from debuggers like Visual Studio.
            try
            {
                Thread.CurrentThread.Name = name;
            }
            catch { }

            // This is so that you can identify threads from programs like Process Monitor. The call
            // should always fail but will appear in Process Monitor's log against the correct thread.
            try
            {
                File.ReadAllBytes(@"DEBUG\THREAD\" + name);
            }
            catch { }
        }
    }
}
