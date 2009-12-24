using System;
using System.Collections.Generic;
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

    }
}
