// COPYRIGHT 2014 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Xunit;

namespace Tests
{
    /// <summary>
    /// This class can be used to test for Trace.Tracewarning calls from tested objects.
    /// Instead of having the warnings go to the output window of xunit, they are captured by this class.
    /// This means that if a warning is not expected, a fail will result.
    /// And if you want to test that a warning is given, you can test for that also.
    /// Two methods are present that can be called from within a test:
    /// AssertWarnings.Active:  start monitoring warnings
    /// AssertWarnings.ExpectWarning: the code that is given to this method will be executed and will be tested for indeed giving a warning.
    ///     use this as: AssertWarnings.ExpectWarning( () => {code_to_execute;});
    /// </summary>
    class AssertWarnings : TraceListener
    {
        static AssertWarnings listener;  // singleton, but not even available from outside

        /// <summary>
        /// Activate the monitoring of warnings. Warnings that appear when not expected will initiate a fail.
        /// Probably you should call this at the beginning of each test-method for classes that generate Trace.Tracewarning s.
        /// Note, also ExpectedWarning will activate.
        /// </summary>
        public static void Activate()
        {
            if (listener == null)
            {
                listener = new AssertWarnings();
            }
            // Prevent warfnings to go to Xunit output window.
            // We assume that Xunit takes control back for the next unit test (meaning that the listener will be removed again for the next test
            Trace.Listeners.Clear();
            // We now intercept the trace warnings with our own listener
            Trace.Listeners.Add(listener);
            listener.Reset();
        }

        /// <summary>
        /// Call this methed if you expect the testCode to generate a warning
        /// </summary>
        /// <param name="testCode">Code that will be executed</param>
        /// <param name="pattern">Pattern to match the warning against; if there is no match, a test fail will result.</param>
        public static void ExpectWarning(Assert.ThrowsDelegate testCode, string pattern)
        {
            Activate();
            listener._ExpectWarning(testCode, pattern);
        }

        /// <summary>
        /// Call this method if you expect a warning, but you do not want to catch it (because e.g. also an exception will be thrown).
        /// </summary>
        public static void ExpectAWarning()
        {
            Activate();
            listener._ExpectWarning();
        }

        bool expectingAWarning;
        bool warningHappened;
        string lastWarning;

        AssertWarnings()
        {
        }

        void Reset()
        {
            expectingAWarning = false;
            warningHappened = false;
        }

        void _ExpectWarning()
        {
            expectingAWarning = true;
            warningHappened = false;
        }

        void _ExpectWarning(Assert.ThrowsDelegate testCode, string pattern)
        {
            _ExpectWarning();
            testCode.Invoke();
            expectingAWarning = false;
            Assert.True(warningHappened, "Expected a warning, but did not get it");
            if (pattern == null)
            {
                return;
            }
            Assert.True(System.Text.RegularExpressions.Regex.IsMatch(lastWarning, pattern), lastWarning + " does not match pattern: " + pattern);
        
        }

        public override void Write(string message)
        {   //Not sure what this is needed for exactly, calling a fail until we know something better
            //base.Write((object) message);
            Assert.True(false, "warning is called 1");
        }

        public override void WriteLine(string message)
        {   //Not sure what this is needed for exactly, calling a fail until we know something better
            //base.WriteLine((object)message);
            Assert.True(expectingAWarning, "Unexpected tracewarning, possibly a Debug.Assert");
            warningHappened = true;
        }

        public override void WriteLine(object o)
        {   //Not sure what this is needed for exactly, calling a fail until we know something better
            //base.WriteLine((object)message);
            Assert.True(false, "warning is called 3");
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            Assert.True(expectingAWarning, "Unexpected tracewarning");
            warningHappened = true;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            lastWarning = message;
            Assert.True(expectingAWarning, "Unexpected tracewarning: " + lastWarning);
            warningHappened = true;

        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            lastWarning = String.Format(format, args);
            Assert.True(expectingAWarning, "Unexpected tracewarning: " + lastWarning);
            warningHappened = true;
        }

    }
}
