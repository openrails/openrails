// COPYRIGHT 2014, 2015 by the Open Rails project.
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
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace Tests
{
    // TODO: This class needs to be removed and replaced with a `JsonReaderTests`-style warning counter, instead of modifying global state (`Trace.Listeners`)
    /// <summary>
    /// This class can be used to test for Trace.TraceWarning() calls.
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
        static AssertWarnings Listener = new AssertWarnings();

        static void Initialize()
        {
            // Prevent warnings from going to xunit.
            // We assume that xunit takes control back for the next unit test (meaning that the listener will be removed again for the next test).
            Trace.Listeners.Clear();
            // We now intercept the trace warnings with our own listener.
            Trace.Listeners.Add(Listener);
        }

        /// <summary>
        /// Declare that no warnings are expected to be generated during the following test.
        /// </summary>
        public static void NotExpected()
        {
            Initialize();
            Listener.Set(false);
        }

        /// <summary>
        /// Declare that warnings are expected to be generated during the following test.
        /// </summary>
        public static void Expected()
        {
            Initialize();
            Listener.Set(true);
        }

        /// <summary>
        /// Declare that a specific warning is expected to be generated during the specified code.
        /// </summary>
        /// <param name="pattern">Pattern to match the warning against; if there is no match, the test fails.</param>
        /// <param name="code">Code which is expected to generate a matching warning.</param>
        public static void Matching(string pattern, Action code)
        {
            Initialize();
            Listener.InternalMatching(pattern, code);
        }

        bool WarningExpected;
        bool WarningOccurred;
        string LastWarning;

        AssertWarnings()
        {
        }

        void Set(bool expected)
        {
            WarningExpected = expected;
            WarningOccurred = false;
            LastWarning = null;
        }

        void InternalMatching(string pattern, Action callback)
        {
            Set(true);
            LastWarning = null;
            callback.Invoke();
            Assert.True(WarningOccurred, "Expected a warning, but did not get it");
            if (WarningOccurred && pattern != null)
            {
                Assert.True(Regex.IsMatch(LastWarning, pattern), LastWarning + " does not match pattern " + pattern);
            }
            Set(false);
        }

        public override void Write(string message)
        {
            //Not sure what this is needed for exactly, calling a fail until we know something better
            Assert.True(false, "Unexpected TraceListener.Write(string) call");
        }

        public override void WriteLine(string message)
        {
            //Not sure what this is needed for exactly, calling a fail until we know something better
            Assert.True(false, "Unexpected TraceListener.WriteLine(string) call");
        }

        public override void WriteLine(object o)
        {
            //Not sure what this is needed for exactly, calling a fail until we know something better
            Assert.True(false, "Unexpected TraceListener.WriteLine(object) call");
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            LastWarning = "";
            Assert.True(WarningExpected, "Unexpected warning");
            WarningOccurred = true;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            LastWarning = message;
            Assert.True(WarningExpected, "Unexpected warning: " + LastWarning);
            WarningOccurred = true;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            LastWarning = String.Format(format, args);
            Assert.True(WarningExpected, "Unexpected warning: " + LastWarning);
            WarningOccurred = true;
        }
    }
}
