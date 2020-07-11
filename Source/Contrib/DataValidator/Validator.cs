// COPYRIGHT 2017 by the Open Rails project.
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
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DataValidator
{
    abstract class Validator
    {
        protected readonly string File;

        protected Validator(string file)
        {
            File = file.ToLowerInvariant();
        }

        protected void Equal<T>(TraceEventType type, T expected, T actual, string item)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                Trace.TraceInformation("Got expected {0} value {1}", item, actual);
            }
            else
            {
                if (type == TraceEventType.Information)
                    Trace.TraceInformation("Expected {1} to be {2}; got {3} at {0}", File, item, expected, actual);
                else if (type == TraceEventType.Warning)
                    Trace.TraceWarning("Expected {1} to be {2}; got {3} at {0}", File, item, expected, actual);
                else
                    Trace.TraceError("Expected {1} to be {2}; got {3} at {0}", File, item, expected, actual);
            }
        }

        protected void Valid<T>(TraceEventType type, Func<T, bool> validator, T value, string item)
        {
            if (validator(value))
            {
                Trace.TraceInformation("Got valid {0} value {1}", item, value);
            }
            else
            {
                if (type == TraceEventType.Information)
                    Trace.TraceInformation("Got invalid {1} value {2} at {0}", File, item, value);
                else if (type == TraceEventType.Warning)
                    Trace.TraceWarning("Got invalid {1} value {2} at {0}", File, item, value);
                else
                    Trace.TraceError("Got invalid {1} value {2} at {0}", File, item, value);
            }
        }

        protected void ValidFileRef(TraceEventType type, string value, string item)
        {
            Valid(type, file => System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(File), file)), value, item + " file ref");
        }
    }
}
